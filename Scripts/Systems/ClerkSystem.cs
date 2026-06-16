using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.UI;
using System;

namespace StoreRobberyEnhanced.Systems
{
    internal class ClerkSystem
    {
        private readonly StoreContext _ctx;
        private readonly Random _rng;
        private DateTime _nextPoliceCallAttempt = DateTime.MinValue;

        public ClerkSystem(StoreContext ctx)
        {
            _ctx = ctx;
            _rng = new Random();
        }

        private bool IsThreateningSoft(Ped player, Ped clerk)
        {
            if (player == null || !player.Exists() || clerk == null || !clerk.Exists())
                return false;

            WeaponHash hash = player.Weapons.Current.Hash;
            bool isMelee = _ctx.Player.IsMeleeWeapon(hash);

            // ------------------------------------------------------------
            // ⭐ 1. Direct aim at clerk (ONLY guns count)
            // ------------------------------------------------------------
            if (!isMelee &&
                Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY, Game.Player, clerk))
                return true;

            // ------------------------------------------------------------
            // ⭐ 2. Gun out + close range (melee does NOT count)
            // ------------------------------------------------------------
            if (!isMelee &&
                hash != WeaponHash.Unarmed &&
                player.Position.DistanceTo(clerk.Position) < 4.5f)
                return true;

            // ------------------------------------------------------------
            // ⭐ 3. Aiming a gun (melee aim is NOT a threat)
            // ------------------------------------------------------------
            if (!isMelee &&
                Game.IsControlPressed(Control.Aim) &&
                hash != WeaponHash.Unarmed)
                return true;

            // ------------------------------------------------------------
            // ⭐ 4. Melee weapons NEVER trigger clerk reaction
            // ------------------------------------------------------------
            return false;
        }
        
        // ------------------------------------------------------------
        // MAIN UPDATE (PATCH 7 + PATCH 10 + PATCH 11 APPLIED)
        // ------------------------------------------------------------
        public void UpdateClerk(TrackedStore store, Ped player)
        {
            try
            {
                if (store == null)
                    return;

                // ⭐ HARD STOP: if clerk died, never run clerk logic again
                if (store.ClerkDeathHandledCheck)
                    return;

                // ⭐ Cooldown → clerk logic disabled
                if (store.CooldownActive)
                    return;

                if (player == null || !player.Exists())
                    return;

                // BLOCK spawning until replacement system has removed defaults
                if (!store.DefaultClerkRemoved)
                    return;

                // Ensure we have a real clerk
                if (store.Clerk == null || !store.Clerk.Exists())
                {
                    SpawnClerk(store);
                    store.IsRobberyActive = false;
                    store.ClerkReacted = false;
                    store.HeatLevel = 0;
                    return;
                }

                Ped clerk = store.Clerk;

                if (clerk == null || !clerk.Exists())
                    return;

                // ------------------------------------------------------------
                // ⭐ PATCH 10 — CLERK STATE MACHINE INTEGRITY GUARD
                // ------------------------------------------------------------

                // ⭐ SAFE DEATH CHECK — prevents false positives inside interiors
                bool reallyDead = clerk.IsDead || clerk.Health <= 0 || clerk.IsInjured || Function.Call<bool>(Hash.IS_PED_FATALLY_INJURED, clerk);

                if (reallyDead)
                {
                    DebugLogger.Info($"[UpdateClerk] Detected real death for store {store.Id}, calling HandleClerkDeath");
                    HandleClerkDeath(store);
                    return;
                }

                // If robbery ended → no clerk phases allowed
                if (store.RobberyEnded || store.CooldownActive)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;
                    store.ClerkFleeing = false;
                    store.ClerkSurrenderStage = 0;
                    return;
                }

                // If SafeCrack running → freeze clerk
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;
                }

                // If SilentRobbery → clerk must never react
                if (store.SilentRobbery)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;
                    store.ClerkFleeing = false;
                    store.ClerkSurrenderStage = 0;
                    return;
                }

                // Ensure only ONE phase is active
                int activePhases =
                    (store.ClerkStalling ? 1 : 0) +
                    (store.ClerkOpeningRegister ? 1 : 0) +
                    (store.ClerkGrabbingCash ? 1 : 0) +
                    (store.ClerkThrowingBag ? 1 : 0) +
                    (store.ClerkPanicking ? 1 : 0) +
                    (store.ClerkFleeing ? 1 : 0);

                if (activePhases > 1)
                {
                    DebugLogger.Warn($"[PATCH10] Clerk state corruption detected for store {store.Id} — resetting to surrender.");

                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;

                    store.ClerkFleeing = true;
                    store.ClerkSurrenderStage = 0;
                }

                // ------------------------------------------------------------
                // ⭐ PATCH 11 — GLOBAL ROBBERY FLOW CONSISTENCY CONTROLLER
                // ------------------------------------------------------------

                // If robbery is not active → ensure all clerk states are off
                if (!store.IsRobberyActive)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;
                    store.ClerkFleeing = false;
                    store.ClerkSurrenderStage = 0;
                }

                // If clerk has surrendered → robbery must end
                if (store.ClerkSurrenderStage == 3 && store.IsRobberyActive)
                {
                    DebugLogger.Info($"[PATCH11] Clerk surrendered — ending robbery for store {store.Id}");

                    store.IsRobberyActive = false;
                    store.RobberyEnded = true;

                    // Start cooldown
                    store.CooldownActive = true;
                    store.CooldownStartUtc = DateTime.UtcNow;

                    // Finalize payout
                    if (store.PendingPayout > 0)
                    {
                        _ctx.Robberies.FinalizePayout(store);
                        store.PendingPayout = 0;
                    }

                    // Prevent further escalation
                    store.AlarmTriggered = true;
                    return;
                }

                // If robbery ended → no further escalation allowed
                if (store.RobberyEnded)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;

                    if (store.ClerkFleeing)
                        ProcessFlee(store, clerk);

                    return;
                }

                // If cooldown active → no robbery logic allowed
                if (store.CooldownActive)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;
                    store.ClerkFleeing = false;
                    store.ClerkSurrenderStage = 0;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ SAFETY RESET: only if clerk is actually stuck AND no robbery is active
                // ------------------------------------------------------------
                bool usingScenario = Function.Call<bool>(Hash.IS_PED_USING_ANY_SCENARIO, clerk);

                if (!store.IsRobberyActive && (clerk.IsRagdoll || usingScenario))
                {
                    DebugLogger.Info($"[RESET] Forcing task clear on clerk {clerk.Handle} (ragdoll={clerk.IsRagdoll} scenario={usingScenario})");
                    Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, clerk);
                }

                // HARD GUARD: never run behavior on dummy clerk
                if (store.DummyClerk != null && store.DummyClerk.Exists() &&
                    clerk.Handle == store.DummyClerk.Handle)
                {
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ PATCH 7 — REACTION SAFETY GUARDS
                // ------------------------------------------------------------

                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                if (store.ClerkFleeing || clerk.IsFleeing)
                    return;

                if (clerk.IsRagdoll)
                    return;

                if (!store.IsPlayerInsideStore)
                    return;

                // ------------------------------------------------------------
                // NORMAL IDLE LOGIC
                // ------------------------------------------------------------
                if (!store.ClerkReacted &&
                    !store.ClerkStalling &&
                    !store.ClerkOpeningRegister &&
                    !store.ClerkGrabbingCash &&
                    !store.ClerkThrowingBag &&
                    !store.ClerkPanicking &&
                    !store.ClerkFleeing)
                {
                    RunIdleBehavior(store, clerk);
                }

                // ------------------------------------------------------------
                // ⭐ PATCH 7 — THREAT VALIDATION
                // ------------------------------------------------------------
                if (!store.ClerkReacted)
                {
                    Weapon weapon = player.Weapons.Current;
                    bool isGun =
                        weapon != null &&
                        weapon.Hash != WeaponHash.Unarmed &&
                        weapon.Group != WeaponGroup.Melee;

                    if (isGun)
                    {
                        bool aiming = Game.IsControlPressed(Control.Aim);

                        bool los = Function.Call<bool>(
                            Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY,
                            clerk.Handle,
                            player.Handle,
                            17
                        );

                        if (aiming && los && IsThreateningSoft(player, clerk))
                        {
                            BeginFearReaction(store, clerk);
                            return;
                        }
                    }
                }

                // ------------------------------------------------------------
                // REMAINING BEHAVIOR
                // ------------------------------------------------------------
                if (store.ClerkStalling)
                {
                    ProcessStall(store, clerk);
                    return;
                }

                if (store.ClerkOpeningRegister)
                {
                    ProcessRegisterOpening(store, clerk);
                    return;
                }

                if (store.ClerkGrabbingCash)
                {
                    ProcessCashGrab(store, clerk);
                    return;
                }

                if (store.ClerkThrowingBag)
                {
                    ProcessBagToss(store, clerk);
                    return;
                }

                if (store.ClerkPanicking)
                {
                    ProcessPanic(store, clerk);
                    return;
                }

                if (store.ClerkFleeing)
                {
                    ProcessFlee(store, clerk);
                    return;
                }

                TryTriggerSilentAlarm(store, clerk);
                TryTriggerPoliceCall(store, clerk, player);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.UpdateClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // HELPER: Determine if a ped is one of our custom clerks
        // ------------------------------------------------------------
        public bool IsOurClerk(Ped ped)
        {
            if (ped == null || !ped.Exists())
                return false;

            foreach (TrackedStore s in _ctx.Stores)
            {
                if (s.Clerk != null && s.Clerk.Exists() && s.Clerk.Handle == ped.Handle)
                    return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        // SPAWN CLERK
        // ------------------------------------------------------------
        private void SpawnClerk(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // If clerk already exists, do nothing
                if (store.Clerk != null && store.Clerk.Exists())
                    return;

                // Replace with our clerk model
                Ped clerk = World.CreatePed(PedHash.Business01AMM, store.ClerkPos, store.ClerkHeading);

                if (clerk == null || !clerk.Exists())
                    return;

                store.IsOurClerk = true;
                store.Clerk = clerk;

                // ⭐ Record spawn time for interior detach logic
                store.ClerkSpawnTime = Game.GameTime;

                clerk.IsPersistent = true;
                clerk.BlockPermanentEvents = true;

                store.ClerkIdle = true;

                // ⭐ Reset clerk state machine after spawn
                store.ClerkReacted = false;
                store.ClerkSurrenderStage = 0;
                store.ClerkStalling = false;
                store.ClerkOpeningRegister = false;
                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = false;
                store.ClerkPanicking = false;
                store.ClerkFleeing = false;
                store.ClerkRecognizedPlayer = false;

            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.SpawnClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // FORCE SPAWN CLERK (Used by ClerkReplacementSystem)
        // ------------------------------------------------------------
        public void ForceSpawnClerk(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // If clerk already exists, do nothing
                if (store.Clerk != null && store.Clerk.Exists())
                    return;

                Ped clerk = World.CreatePed(PedHash.Business01AMM, store.ClerkPos, store.ClerkHeading);

                if (clerk == null || !clerk.Exists())
                    return;

                store.Clerk = clerk;

                // ⭐ Record spawn time for interior detach logic
                store.ClerkSpawnTime = Game.GameTime;

                store.IsOurClerk = true;

                clerk.IsPersistent = true;
                clerk.BlockPermanentEvents = true;
                clerk.Task.ClearAllImmediately();

                // Idle state
                store.ClerkIdle = true;

                // ------------------------------------------------------------
                // ⭐ SECOND FIX — STOP FUTURE CLERK SWEEPS
                // ------------------------------------------------------------
                store.DefaultClerkRemoved = true;

                // Push next sweep far into the future so ClerkReplacementSystem stops touching this store
                store.LastClerkSweepUtc = DateTime.UtcNow + TimeSpan.FromHours(12);

                // ⭐ Reset clerk state machine after spawn
                store.ClerkReacted = false;
                store.ClerkSurrenderStage = 0;
                store.ClerkStalling = false;
                store.ClerkOpeningRegister = false;
                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = false;
                store.ClerkPanicking = false;
                store.ClerkFleeing = false;
                store.ClerkRecognizedPlayer = false;

                DebugLogger.Info($"ForceSpawnClerk: Clerk spawned and sweeps disabled for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ForceSpawnClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // SPAWN DUMMY CLERK
        // ------------------------------------------------------------
        public void SpawnDummyClerk(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // If dummy already exists, skip
                if (store.DummyClerk != null && store.DummyClerk.Exists())
                    return;

                // Spawn underground so player never sees it
                Vector3 spawnPos = store.ClerkPos + new Vector3(0f, 0f, -10f);

                Ped dummy = World.CreatePed(PedHash.ShopKeep01, spawnPos, store.ClerkHeading);

                if (dummy == null || !dummy.Exists())
                {
                    DebugLogger.Warn($"SpawnDummyClerk: Failed to spawn dummy clerk for store {store.Id}");
                    return;
                }

                store.DummyClerk = dummy;

                // Make invisible and non-interactive
                dummy.IsVisible = false;
                dummy.IsCollisionEnabled = false;
                dummy.IsInvincible = true;
                dummy.IsPersistent = true;
                dummy.BlockPermanentEvents = true;

                dummy.Task.ClearAllImmediately();
                dummy.IsPositionFrozen = true;

                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, dummy.Handle, true, true);

                DebugLogger.Info($"SpawnDummyClerk: Dummy clerk spawned for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.SpawnDummyClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // SMALL HELPER: ANIM CHECK
        // ------------------------------------------------------------
        public bool IsPlayingAnim(Ped ped, string dict, string name)
        {
            if (ped == null || !ped.Exists())
                return false;

            try
            {
                return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped.Handle, dict, name, 3);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.IsPlayingAnim", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // PATCH O — Player Inside Store Boundary
        // ------------------------------------------------------------
        public bool PlayerInsideRobberyZone(TrackedStore store, Ped player)
        {
            if (player == null || !player.Exists())
                return false;

            // Use your existing PlayerHelper for positional checks
            return _ctx.Player.IsInsideStore(store, store.Radius);
        }

        // ------------------------------------------------------------
        // PATCH F — Animation Safety Check
        // ------------------------------------------------------------
        public bool IsClerkBusy(Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return true;

            // If ANY animation is playing, clerk is busy
            return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "mp_common", "givetake1_a", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "mp_common", "givetake2_a", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "anim@heists@ornate_bank@grab_cash", "enter", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "anim@heists@ornate_bank@grab_cash", "idle", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "mp_am_hold_up", "purchase_beer_shopkeeper", 3);
        }

        // ------------------------------------------------------------
        // PATCH G — Clerk Position Validation
        // ------------------------------------------------------------
        public bool IsClerkAtRegister(TrackedStore store, Ped clerk, float tolerance = 1.25f)
        {
            if (store == null || clerk == null || !clerk.Exists())
                return false;

            float dist = clerk.Position.DistanceTo(store.RegisterPos);
            return dist <= tolerance;
        }

        // ------------------------------------------------------------
        // PATCH I — Unified Player Threat Validation (Silent + Loud)
        // Uses PlayerHelper for LOS, melee, aiming, masking
        // ------------------------------------------------------------
        public bool PlayerThreatValid(TrackedStore store, Ped clerk, Ped player)
        {
            if (player == null || !player.Exists())
                return false;

            PlayerHelper ph = _ctx.Player;

            // ------------------------------------------------------------
            // 1. DISTANCE CHECK
            // ------------------------------------------------------------
            float dist = player.Position.DistanceTo(clerk.Position);

            if (store.SilentRobbery)
            {
                // Silent robbery requires very close range
                if (dist > 3.0f)
                    return false;
            }
            else
            {
                // Loud robbery threat radius
                if (dist > 8.0f)
                    return false;
            }

            // ------------------------------------------------------------
            // 2. LINE OF SIGHT CHECK
            // ------------------------------------------------------------
            if (!store.SilentRobbery)
            {
                // Loud robbery requires LOS
                if (!ph.IsInLOS(clerk))
                    return false;
            }
            // Silent robbery does NOT require LOS

            // ------------------------------------------------------------
            // 3. WEAPON + THREAT CHECK
            // ------------------------------------------------------------
            Weapon current = player.Weapons.Current;
            bool hasWeapon = current != null && current.Hash != WeaponHash.Unarmed;

            bool isMelee = hasWeapon && ph.IsMeleeWeapon(current.Hash);
            bool isGun = hasWeapon && !isMelee;

            bool isAiming = ph.IsAiming();

            // ------------------------------------------------------------
            // SILENT ROBBERY LOGIC
            // ------------------------------------------------------------
            if (store.SilentRobbery)
            {
                // Must be melee
                if (!isMelee)
                    return false;

                // Must NOT aim
                if (isAiming)
                    return false;

                // Must be masked
                if (!ph.IsMasked())
                    return false;

                return true;
            }

            // ⭐ PATCH O — Silent robbery anti‑cheese
            if (store.SilentRobbery)
            {
                // Must stay in front arc of clerk
                Vector3 toPlayer = (player.Position - clerk.Position).Normalized;
                float dot = Vector3.Dot(clerk.ForwardVector, toPlayer);

                // If player moves behind clerk → silent robbery breaks
                if (dot < 0.0f)
                    return false;

                // Must remain in melee range
                if (dist > 3.0f)
                    return false;
            }

            // ------------------------------------------------------------
            // LOUD ROBBERY LOGIC
            // ------------------------------------------------------------
            // Gun + aiming = threat
            if (isGun && isAiming)
                return true;

            // Melee = threat (aiming optional)
            if (isMelee)
                return true;

            // No threat
            return false;
        }

        // ------------------------------------------------------------
        // PATCH P — Clerk Ragdoll Recovery
        // ------------------------------------------------------------
        public bool HandleClerkRagdoll(TrackedStore store)
        {
            Ped clerk = store.Clerk;
            if (clerk == null || !clerk.Exists())
                return false;

            // If clerk is ragdolled → recover safely
            if (clerk.IsRagdoll)
            {
                DebugLogger.Warn($"[PATCH P] Clerk ragdolled at store {store.Id}. Resetting to stall.");

                ClearAllClerkPhases(store);
                store.ClerkStalling = true;
                store.PendingCompletion = false;

                // Force clerk to stand up
                clerk.Task.ClearAllImmediately();
                Function.Call(Hash.RESET_PED_RAGDOLL_TIMER, clerk.Handle);

                return true; // ragdoll handled
            }

            return false; // no ragdoll
        }

        // ------------------------------------------------------------
        // PATCH Q — Clerk Position & Heading Validation
        // ------------------------------------------------------------
        public bool ValidateClerkPosition(TrackedStore store)
        {
            Ped clerk = store.Clerk;
            if (clerk == null || !clerk.Exists())
                return false;

            Vector3 expectedPos = store.ClerkPos;
            float expectedHeading = store.ClerkHeading;

            float dist = clerk.Position.DistanceTo(expectedPos);

            // If clerk is too far from expected position → correct it
            if (dist > 0.75f)
            {
                DebugLogger.Warn($"[PATCH Q] Clerk displaced {dist:F2}m at store {store.Id}. Repositioning.");

                // Clear all phases safely
                ClearAllClerkPhases(store);
                store.ClerkStalling = true;
                store.PendingCompletion = false;

                // Teleport clerk back to correct position
                clerk.Position = expectedPos;
                clerk.Heading = expectedHeading;

                // Freeze momentarily to prevent sliding
                clerk.Task.ClearAllImmediately();
                Function.Call(Hash.TASK_STAND_STILL, clerk.Handle, 500);

                return true; // correction applied
            }

            // Heading drift correction
            float headingDiff = Math.Abs(clerk.Heading - expectedHeading);
            if (headingDiff > 25f)
            {
                DebugLogger.Warn($"[PATCH Q] Clerk heading drift {headingDiff:F1}° at store {store.Id}. Correcting.");

                clerk.Heading = expectedHeading;
                return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        // PATCH J — State Machine Integrity Check
        // ------------------------------------------------------------
        public bool ValidateClerkStateMachine(TrackedStore store)
        {
            int activeCount = 0;

            if (store.ClerkStalling) activeCount++;
            if (store.ClerkOpeningRegister) activeCount++;
            if (store.ClerkGrabbingCash) activeCount++;
            if (store.ClerkThrowingBag) activeCount++;
            if (store.ClerkPanicking) activeCount++;
            if (store.ClerkFleeing) activeCount++;

            // No phase active → invalid
            if (activeCount == 0)
                return false;

            // More than one phase active → invalid
            if (activeCount > 1)
                return false;

            return true;
        }

        // ------------------------------------------------------------
        // PATCH R — LOS Persistence Check
        // ------------------------------------------------------------
        public bool ClerkLostLOS(TrackedStore store, Ped player)
        {
            // Silent robbery does NOT require LOS
            if (store.SilentRobbery)
                return false;

            Ped clerk = store.Clerk;
            if (clerk == null || !clerk.Exists())
                return false;

            // Use PlayerHelper LOS logic
            bool hasLOS = _ctx.Player.IsInLOS(clerk);

            return !hasLOS;
        }

        // ------------------------------------------------------------
        // PATCH S — Animation Integrity Check
        // ------------------------------------------------------------
        public bool EnsureClerkAnimation(TrackedStore store, string animDict, string animName)
        {
            Ped clerk = store.Clerk;
            if (clerk == null || !clerk.Exists())
                return false;

            // If clerk is ragdolled, animation cannot continue
            if (clerk.IsRagdoll)
                return false;

            // If animation is missing or cancelled, restart it
            if (!IsPlayingAnim(clerk, animDict, animName))
            {
                DebugLogger.Warn($"[PATCH S] Clerk animation '{animName}' cancelled at store {store.Id}. Restarting.");

                Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
                int timeout = Game.GameTime + 2000;
                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict) && Game.GameTime < timeout)
                    Script.Yield();

                clerk.Task.PlayAnimation(animDict, animName, 8f, -8f, -1, AnimationFlags.Loop, 0f);

                return true; // animation restarted
            }

            return false; // animation intact
        }

        // ------------------------------------------------------------
        // SAFE LOAD: ANIM CHECK
        // ------------------------------------------------------------
        public bool SafeLoadAnimDict(string dict)
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, dict);

            int timeout = Game.GameTime + 2000;
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict))
            {
                Script.Yield();
                if (Game.GameTime > timeout)
                {
                    DebugLogger.Warn($"Anim dict failed to load: {dict}");
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------
        // NATIVE ANIMATION WRAPPER (SHVDN 3.9.0 SAFE, SIMPLE REQUEST)
        // ------------------------------------------------------------
        public void PlayAnimNative(Ped ped, string dict, string anim, AnimationFlags flags)
        {
            if (ped == null || !ped.Exists())
                return;

            // ⭐ BLOCK MOST ROOT-MOTION HOLD-UP ANIMS, BUT ALLOW REGISTER OPEN
            if (dict == "mp_am_hold_up")
            {
                // Allow the specific register animation we actually use
                if (!string.Equals(anim, "purchase_beer_shopkeeper", StringComparison.OrdinalIgnoreCase))
                {
                    DebugLogger.Info($"[ANIM-BLOCK] Suppressed root-motion anim {dict}/{anim} on ped {ped.Handle}");
                    return;
                }
            }

            try
            {
                Function.Call(Hash.REQUEST_ANIM_DICT, dict);

                DebugLogger.Info($"[ANIM] Requesting anim {dict}/{anim} on ped {ped.Handle}");

                Function.Call(
                    Hash.TASK_PLAY_ANIM,
                    ped.Handle,
                    dict,
                    anim,
                    8.0f,
                    -8.0f,
                    -1,
                    (int)flags,
                    0,
                    false, false, false
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException($"ClerkSystem.PlayAnimNative {dict}/{anim}", ex);
            }
        }

        // ------------------------------------------------------------
        // SILENT ROBBERY COSMETIC ANIM
        // ------------------------------------------------------------
        public void PlaySilentRobberyAnim(TrackedStore store)
        {
            try
            {
                var clerk = store.Clerk;
                if (clerk == null || !clerk.Exists())
                    return;

                // Cosmetic-only animation
                clerk.Task.ClearAllImmediately();

                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_common");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mp_common"))
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "mp_common",
                        "givetake1_a",   // subtle handover motion
                        4.0f,
                        -4.0f,
                        1500,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );
                }

                // ------------------------------------------------------------
                // ⭐ PLAY QUIET REGISTER / MONEY SOUND
                // ------------------------------------------------------------
                // "ROBBERY_MONEY" is a subtle cash-handling sound used in GTA V
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "ROBBERY_MONEY", "HUD_AWARDS");
                Script.Wait(300); // small delay to avoid sound overlap
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");

                // ------------------------------------------------------------
                // ⭐ PLAYER NOTIFICATION
                // ------------------------------------------------------------
                _ctx.Ui.ShowNotification("~g~Clerk quietly hands over the register cash.~s~ Crack the safe before leaving.");
                DebugLogger.Info($"Played silent robbery anim for store {store.Id} on clerk {clerk.Handle}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.PlaySilentRobberyAnim", ex);
            }
        }

        // ------------------------------------------------------------
        // IDLE BEHAVIOR
        // ------------------------------------------------------------
        private void RunIdleBehavior(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                if (store.ClerkReacted || store.ClerkStalling || store.ClerkOpeningRegister ||
                    store.ClerkGrabbingCash || store.ClerkThrowingBag ||
                    store.ClerkPanicking || store.ClerkFleeing)
                    return;

                if (!store.ClerkIdle)
                    return;

                // Cooldown so we don't spam anim requests
                int now = Game.GameTime;
                if (now - store.LastIdleTime < 4000) // 4‑second buffer
                    return;

                string dict = "amb@world_human_shopkeeper@male@idle_a";
                string[] idles = { "idle_a", "idle_b", "idle_c" };

                bool playing =
                    IsPlayingAnim(clerk, dict, "idle_a") ||
                    IsPlayingAnim(clerk, dict, "idle_b") ||
                    IsPlayingAnim(clerk, dict, "idle_c");

                if (!playing)
                {
                    string anim = idles[_rng.Next(idles.Length)];
                    DebugLogger.Info(string.Format("[IDLE] Starting idle '{0}' on clerk {1}", anim, clerk.Handle));
                    //clerk.Task.PlayAnimation(dict, anim, 4f, -1, AnimationFlags.Loop);
                    PlayAnimNative(clerk, dict, anim, AnimationFlags.Loop);

                    // Record timestamp so we don't restart immediately
                    store.LastIdleTime = now;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.RunIdleBehavior", ex);
            }
        }

        // ------------------------------------------------------------
        // FEAR REACTION
        // ------------------------------------------------------------
        private void BeginFearReaction(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                store.ClerkReacted = true;
                store.ClerkIdle = false;
                store.IsRobberyActive = true;

                // ⭐ ADD THESE TWO LINES
                store.RobberyStartUtc = DateTime.UtcNow;
                _ctx.Stalker.ResetForNewRobbery();

                clerk.Task.ClearAllImmediately();
                clerk.Task.HandsUp(-1);

                Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, clerk, "SHOP_CLERK_REACT", "SPEECH_PARAMS_FORCE", 0);

                // Recognition escalation
                if (store.TimesRobbed >= 2)
                    store.ClerkRecognizedPlayer = true;

                // 🎲 Random chance to fight back (10–20% typical)
                int roll = _rng.Next(0, 100);
                if (roll < 15) // 15% chance to fight
                {
                    // Pick weapon type randomly
                    bool useShotgun = _rng.Next(0, 2) == 0;
                    store.ReactionType = useShotgun ? ClerkReactionType.FightShotgun : ClerkReactionType.FightPistol;

                    _ctx.Ui.ShowNotification("~r~The clerk has decided to fight back!~s~");

                    DebugLogger.Info($"Clerk at store {store.Id} decided to fight back ({store.ReactionType})");

                    // Trigger combat behavior immediately
                    ProcessFeelingFroggy(store, clerk);
                    return;
                }

                // Default panic behavior
                if (store.ReactionType == 0)
                    store.ReactionType = ClerkReactionType.NormalPanic;

                // Stall
                store.ClerkStalling = true;
                store.StallStartUtc = DateTime.UtcNow;
                store.StallDurationMs = _rng.Next(3000, 7000);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.BeginFearReaction", ex);
            }
        }

        // ------------------------------------------------------------
        // HELPER: Clear all clerk phases (used for safety resets)
        // ------------------------------------------------------------
        public void ClearAllClerkPhases(TrackedStore store)
        {
            store.ClerkStalling = false;
            store.ClerkOpeningRegister = false;
            store.ClerkGrabbingCash = false;
            store.ClerkThrowingBag = false;
            store.ClerkPanicking = false;
            store.ClerkFleeing = false;
        }

        // ------------------------------------------------------------
        // STALL PROCESSING (PATCH 9A + PATCH F + PATCH G APPLIED)
        // ------------------------------------------------------------
        private void ProcessStall(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;

                // ⭐ PATCH Q — Position/heading validation
                if (ValidateClerkPosition(store))
                    return;

                // ⭐ PATCH 9A — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ Clerk cannot continue stall if invalid state
                if (clerk.IsDead || clerk.IsRagdoll || store.ClerkFleeing)
                    return;

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ PATCH G — Clerk must be near register to continue stall
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH G] Clerk displaced from register during ProcessStall. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH O — Player must remain inside store boundary
                if (!PlayerInsideRobberyZone(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH O] Player left store boundary during {nameof(ProcessStall)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH I — Player must be threatening clerk (LOS + distance)
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH I] Player not threatening clerk during {nameof(ProcessStall)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH J — Validate state machine integrity
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH J] Invalid clerk state machine detected during {nameof(ProcessStall)}. Resetting to stall.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH R — LOS persistence enforcement
                if (ClerkLostLOS(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH R] Player broke LOS during {nameof(ProcessStall)} at store {store.Id}. Pausing robbery.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    store.PendingCompletion = false;

                    _ctx.Ui.ShowNotification("~r~You broke line of sight — robbery paused.");
                    return;
                }

                // ------------------------------------------------------------
                // STILL STALLING?
                // ------------------------------------------------------------
                if ((DateTime.UtcNow - store.StallStartUtc).TotalMilliseconds < store.StallDurationMs)
                {
                    // Ensure nervous idle is playing
                    if (!IsPlayingAnim(clerk, "missheist_agency2aig_2", "look_around_guard"))
                    {
                        Function.Call(Hash.REQUEST_ANIM_DICT, "missheist_agency2aig_2");

                        if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "missheist_agency2aig_2"))
                        {
                            Function.Call(
                                Hash.TASK_PLAY_ANIM,
                                clerk.Handle,
                                "missheist_agency2aig_2",
                                "look_around_guard",
                                4.0f,
                                -4.0f,
                                -1,
                                (int)AnimationFlags.Loop,
                                0f,
                                false, false, false
                            );

                            _ctx.Ui.ShowNotification("~y~The clerk is stalling...~s~ Wait for them to open the register.");
                            DebugLogger.Info($"Clerk at store {store.Id} is stalling for {store.StallDurationMs} ms.");
                        }
                    }

                    return;
                }

                // ------------------------------------------------------------
                // ⭐ PATCH 9A — Stall finished → transition safety
                // ------------------------------------------------------------
                ClearAllClerkPhases(store);
                store.ClerkOpeningRegister = true;

                // ⭐ PATCH A — SAFE MOVEMENT TO REGISTER (NO TELEPORTING)
                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                {
                    float distToRegister = clerk.Position.DistanceTo(store.RegisterPos);

                    if (distToRegister > 0.75f)
                    {
                        clerk.Task.ClearAllImmediately();
                        clerk.Task.GoStraightTo(
                            store.RegisterPos,
                            3000,
                            PedMoveBlendRatio.Walk,
                            store.RegisterHeading,
                            0f
                        );
                    }
                }

                // Begin register opening
                store.ClerkOpeningRegister = true;
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1800;

                PlayAnimNative(clerk, "mp_am_hold_up", "purchase_beer_shopkeeper", AnimationFlags.None);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessStall", ex);
            }
        }

        // ------------------------------------------------------------
        // REGISTER OPENING (PATCH 9B APPLIED + PATCH S)
        // ------------------------------------------------------------
        private void ProcessRegisterOpening(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;

                // ⭐ PATCH Q — Position/heading validation
                if (ValidateClerkPosition(store))
                    return;

                // ⭐ PATCH 9B — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ Clerk cannot continue register opening if invalid state
                if (clerk.IsDead || clerk.IsRagdoll || store.ClerkFleeing)
                    return;

                // ⭐ PATCH S — Animation integrity enforcement
                // Register opening uses "enter" from grab_cash dict
                if (EnsureClerkAnimation(store, "anim@heists@ornate_bank@grab_cash", "enter"))
                    return;

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                {
                    // Do NOT advance phases while animation is active
                    return;
                }

                // ⭐ PATCH G — Clerk must be near register to continue this phase
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH G] Clerk displaced from register during {nameof(ProcessRegisterOpening)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ------------------------------------------------------------
                // STILL IN FIRST ANIMATION PHASE?
                // ------------------------------------------------------------
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                {
                    // Prevent idle reset during transition
                    return;
                }

                // ⭐ PATCH H — Validate register actually opened
                // Clerk must be at register
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH H] Clerk displaced before register opened at store {store.Id}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // Clerk must be facing the register
                Vector3 toRegister = (store.RegisterPos - clerk.Position).Normalized;
                float dot = Vector3.Dot(clerk.ForwardVector, toRegister);
                if (dot < 0.35f) // ~70° cone
                {
                    DebugLogger.Warn($"[PATCH H] Clerk not facing register after open animation at store {store.Id}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // Animation must have actually played
                if (!IsPlayingAnim(clerk, "anim@heists@ornate_bank@grab_cash", "enter") &&
                    !IsPlayingAnim(clerk, "mp_am_hold_up", "purchase_beer_shopkeeper"))
                {
                    DebugLogger.Warn($"[PATCH H] Register open animation never played at store {store.Id}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH O — Player must remain inside store boundary
                if (!PlayerInsideRobberyZone(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH O] Player left store boundary during {nameof(ProcessRegisterOpening)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH I — Player must be threatening clerk (Silent + Loud)
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH I] Player not threatening clerk during {nameof(ProcessRegisterOpening)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH J — Validate state machine integrity
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH J] Invalid clerk state machine detected during {nameof(ProcessRegisterOpening)}. Resetting to stall.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH R — LOS persistence enforcement
                if (ClerkLostLOS(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH R] Player broke LOS during {nameof(ProcessRegisterOpening)} at store {store.Id}. Pausing robbery.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    store.PendingCompletion = false;

                    _ctx.Ui.ShowNotification("~r~You broke line of sight — robbery paused.");
                    return;
                }

                // ------------------------------------------------------------
                // FIRST PHASE: PLAY "ENTER" ANIMATION
                // ------------------------------------------------------------
                if (!store.ClerkGrabbingCash)
                {
                    store.ClerkOpeningRegister = false;
                    store.ClerkStalling = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;
                    store.ClerkFleeing = false;

                    ClearAllClerkPhases(store);
                    store.ClerkGrabbingCash = true;

                    // Safety: clear tasks only if clerk is stable
                    if (!clerk.IsRagdoll && !store.ClerkFleeing)
                        clerk.Task.ClearAllImmediately();

                    // ⭐ PATCH B — Animation Failure Fallback System
                    if (SafeLoadAnimDict("anim@heists@ornate_bank@grab_cash"))
                    {
                        Function.Call(
                            Hash.TASK_PLAY_ANIM,
                            clerk.Handle,
                            "anim@heists@ornate_bank@grab_cash",
                            "enter",
                            8.0f, -8.0f,
                            1500,
                            (int)AnimationFlags.None,
                            0f,
                            false, false, false
                        );

                        _ctx.Ui.ShowNotification("~y~The clerk is opening the register...~s~ Get ready to grab the cash!");
                        DebugLogger.Info($"Clerk at store {store.Id} is opening the register.");
                    }
                    else
                    {
                        DebugLogger.Warn($"[PATCH B] Animation dict failed to load for store {store.Id}. Halting robbery phase.");

                        ClearAllClerkPhases(store);
                        store.ClerkStalling = true;
                        store.ClerkOpeningRegister = false;
                        store.ClerkGrabbingCash = false;
                        store.ClerkThrowingBag = false;

                        _ctx.Ui.ShowNotification("~r~Clerk froze — animation failed. Robbery paused safely.");
                        return;
                    }

                    // Set timer for next phase
                    store.ClerkAnimStartUtc = DateTime.UtcNow;
                    store.ClerkAnimDurationMs = 1500;
                    return;
                }

                // ------------------------------------------------------------
                // SECOND PHASE: IDLE AT OPEN REGISTER
                // ------------------------------------------------------------
                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = true;

                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "anim@heists@ornate_bank@grab_cash",
                        "idle",
                        8.0f,
                        -8.0f,
                        -1,
                        (int)AnimationFlags.Loop,
                        0f,
                        false, false, false
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessRegisterOpening", ex);
            }
        }

        // ------------------------------------------------------------
        // CASH GRAB (PATCH 9C APPLIED)
        // ------------------------------------------------------------
        private void ProcessCashGrab(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;

                // ⭐ PATCH Q — Position/heading validation
                if (ValidateClerkPosition(store))
                    return;

                // ⭐ PATCH 9C — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ Clerk cannot continue cash grab if invalid state
                if (clerk.IsDead || clerk.IsRagdoll || store.ClerkFleeing)
                    return;

                // ⭐ PATCH S — Animation integrity enforcement
                if (EnsureClerkAnimation(store, "mp_common", "givetake1_a"))
                    return;

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                {
                    // Do NOT advance phases while animation is active
                    return;
                }

                // ⭐ PATCH G — Clerk must be near register to continue this phase
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH G] Clerk displaced from register during {nameof(ProcessCashGrab)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH O — Player must remain inside store boundary
                if (!PlayerInsideRobberyZone(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH O] Player left store boundary during {nameof(ProcessCashGrab)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH I — Player must be threatening clerk (LOS + distance)
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH I] Player not threatening clerk during {nameof(ProcessCashGrab)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH J — Validate state machine integrity
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH J] Invalid clerk state machine detected during {nameof(ProcessCashGrab)}. Resetting to stall.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH R — LOS persistence enforcement
                if (ClerkLostLOS(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH R] Player broke LOS during {nameof(ProcessCashGrab)} at store {store.Id}. Pausing robbery.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    store.PendingCompletion = false;

                    _ctx.Ui.ShowNotification("~r~You broke line of sight — robbery paused.");
                    return;
                }

                // ------------------------------------------------------------
                // STILL IN PREVIOUS PHASE?
                // ------------------------------------------------------------
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;
                
                // ------------------------------------------------------------
                // TRANSITION TO BAG TOSS PHASE
                // ------------------------------------------------------------
                store.ClerkGrabbingCash = false;
                store.ClerkOpeningRegister = false;
                store.ClerkStalling = false;
                store.ClerkPanicking = false;
                store.ClerkFleeing = false;
                ClearAllClerkPhases(store);
                store.ClerkThrowingBag = true;

                // Safety: only clear tasks if clerk is stable
                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                    clerk.Task.ClearAllImmediately();

                // ------------------------------------------------------------
                // LOAD ANIM DICT
                // ------------------------------------------------------------
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_common");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mp_common"))
                {
                    // Play the give-money animation
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "mp_common",
                        "givetake1_a",
                        8.0f,
                        -8.0f,
                        1500,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );

                    _ctx.Ui.ShowNotification("~y~The clerk is grabbing the cash...~s~ Get ready to toss the bag!");
                    DebugLogger.Info($"Clerk at store {store.Id} is grabbing cash from the register.");
                }
                else
                {
                    DebugLogger.Warn($"[PATCH B] Animation dict failed to load for store {store.Id}. Halting robbery phase.");

                    // Halt progression safely
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;           // revert to stall phase
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;

                    // Notify player
                    _ctx.Ui.ShowNotification("~r~Clerk froze — animation failed. Robbery paused safely.");
                    return; // stop phase advancement
                }

                // ------------------------------------------------------------
                // SET TIMING FOR NEXT PHASE
                // ------------------------------------------------------------
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1500;

                // ------------------------------------------------------------
                // PATCH 9C — SAFE PAYOUT (ONE-SHOT)
                // ------------------------------------------------------------
                int payout = _ctx.Rng.Next(_ctx.Config.RegisterMinAmount, _ctx.Config.RegisterMaxAmount + 1);
                payout = (int)(payout * _ctx.Config.PayoutMultiplier);

                store.PendingPayout += payout;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessCashGrab", ex);
            }
        }

        // ------------------------------------------------------------
        // BAG TOSS (PATCH D — Safe Bag Toss Logic + PATCH S)
        // ------------------------------------------------------------
        private void ProcessBagToss(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;

                // ⭐ PATCH Q — Position/heading validation
                if (ValidateClerkPosition(store))
                    return;

                // ⭐ PATCH D — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ Clerk cannot toss bag if invalid state
                if (clerk.IsDead || clerk.IsRagdoll || store.ClerkFleeing)
                    return;

                // ⭐ PATCH S — Animation integrity enforcement (Bag Toss)
                // Ensures "givetake2_a" cannot be cancelled
                if (EnsureClerkAnimation(store, "mp_common", "givetake2_a"))
                    return;

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ PATCH D — Ensure clerk is at the register
                float distToRegister = clerk.Position.DistanceTo(store.RegisterPos);
                if (distToRegister > 1.25f)
                {
                    DebugLogger.Warn($"[PATCH D] Clerk too far from register for bag toss (dist={distToRegister}). Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH D — Ensure clerk is facing the register
                Vector3 toRegister = (store.RegisterPos - clerk.Position).Normalized;
                float dot = Vector3.Dot(clerk.ForwardVector, toRegister);
                if (dot < 0.35f)
                {
                    DebugLogger.Warn($"[PATCH D] Clerk not facing register for bag toss. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH O — Player must remain inside store boundary
                if (!PlayerInsideRobberyZone(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH O] Player left store boundary during {nameof(ProcessBagToss)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH I — Player must be threatening clerk
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH I] Player not threatening clerk during {nameof(ProcessBagToss)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH J — Validate state machine integrity
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH J] Invalid clerk state machine detected during {nameof(ProcessBagToss)}. Resetting to stall.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH R — LOS persistence enforcement
                if (ClerkLostLOS(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH R] Player broke LOS during {nameof(ProcessBagToss)} at store {store.Id}. Pausing robbery.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    store.PendingCompletion = false;

                    _ctx.Ui.ShowNotification("~r~You broke line of sight — robbery paused.");
                    return;
                }

                // ⭐ Wait for previous animation to finish
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                // ⭐ Safety: only clear tasks if clerk is stable
                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                    clerk.Task.ClearAllImmediately();

                // ------------------------------------------------------------
                // ⭐ PATCH D — Load anim dict safely
                // ------------------------------------------------------------
                Function.Call(Hash.REQUEST_ANIM_DICT, "mp_common");

                if (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mp_common"))
                {
                    DebugLogger.Warn($"[PATCH D] Bag toss anim dict failed to load for store {store.Id}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ------------------------------------------------------------
                // PLAY BAG TOSS ANIMATION
                // ------------------------------------------------------------
                ClearAllClerkPhases(store);
                store.ClerkThrowingBag = true;

                if (SafeLoadAnimDict("mp_common"))
                {
                    clerk.Task.ClearAllImmediately();

                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "mp_common",
                        "givetake2_a",   // ⭐ Bag toss animation
                        8.0f, -8.0f,
                        1200,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );

                    _ctx.Ui.ShowNotification("~y~The clerk is tossing the bag...~s~ Grab it, crack the safe and get out of there!");
                    DebugLogger.Info($"Clerk at store {store.Id} is tossing the bag.");
                }
                else
                {
                    DebugLogger.Warn($"[PATCH B] Animation dict failed to load for store {store.Id}. Halting bag toss.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ PATCH D — Only spawn bag AFTER animation starts
                // ------------------------------------------------------------
                _ctx.Robberies.SpawnLootBag(store, clerk);

                // ------------------------------------------------------------
                // TRANSITION TO SURRENDER SEQUENCE
                // ------------------------------------------------------------
                store.ClerkPanicking = false;
                store.ClerkFleeing = true;
                store.ClerkSurrenderStage = 0;

                // Set timer for next phase
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1200;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessBagToss (PATCH D)", ex);
            }
        }

        // ------------------------------------------------------------
        // PANIC (PATCH 9E APPLIED)
        // ------------------------------------------------------------
        private void ProcessPanic(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;

                // ⭐ PATCH Q — Position/heading validation
                if (ValidateClerkPosition(store))
                    return;

                // ⭐ PATCH 9E — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ Clerk cannot panic if invalid state
                if (clerk.IsDead || clerk.IsRagdoll || store.ClerkFleeing)
                    return;

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                {
                    // Do NOT advance phases while animation is active
                    return;
                }

                // ⭐ PATCH G — Clerk must be near register to continue this phase
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH G] Clerk displaced from register during {nameof(ProcessPanic)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH O — Player must remain inside store boundary
                if (!PlayerInsideRobberyZone(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH O] Player left store boundary during {nameof(ProcessPanic)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }


                // ⭐ PATCH I — Player must be threatening clerk (LOS + distance)
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH I] Player not threatening clerk during {nameof(ProcessPanic)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH J — Validate state machine integrity
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH J] Invalid clerk state machine detected during {nameof(ProcessPanic)}. Resetting to stall.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH R — LOS persistence enforcement
                if (ClerkLostLOS(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH R] Player broke LOS during {nameof(ProcessPanic)} at store {store.Id}. Pausing robbery.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    store.PendingCompletion = false;

                    _ctx.Ui.ShowNotification("~r~You broke line of sight — robbery paused.");
                    return;
                }

                // ⭐ Simple cower behavior (safe)
                if (!clerk.IsInCombat && !clerk.IsFleeing)
                {
                    clerk.Task.ClearAllImmediately();
                    clerk.Task.Cower(-1);
                    _ctx.Ui.ShowNotification("~r~The clerk is panicking and cowering on the ground!~s~ Grab the bag and crack the safe!");
                    DebugLogger.Info($"Clerk at store {store.Id} is panicking and cowering.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessPanic", ex);
            }
        }

        // ------------------------------------------------------------
        // FLEE / SURRENDER OVERRIDE (PATCH 9E APPLIED)
        // ------------------------------------------------------------
        private void ProcessFlee(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;

                // ⭐ PATCH Q — Position/heading validation
                if (ValidateClerkPosition(store))
                    return;

                // ⭐ PATCH 9E — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                {
                    // Do NOT advance phases while animation is active
                    return;
                }

                // ⭐ PATCH G — Clerk must be near register to continue this phase
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH G] Clerk displaced from register during {nameof(ProcessFlee)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH O — Player must remain inside store boundary
                if (!PlayerInsideRobberyZone(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH O] Player left store boundary during {nameof(ProcessFlee)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH I — Player must be threatening clerk (LOS + distance)
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH I] Player not threatening clerk during {nameof(ProcessFlee)}. Halting phase.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH J — Validate state machine integrity
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH J] Invalid clerk state machine detected during {nameof(ProcessFlee)}. Resetting to stall.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH R — LOS persistence enforcement
                if (ClerkLostLOS(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH R] Player broke LOS during {nameof(ProcessFlee)} at store {store.Id}. Pausing robbery.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    store.PendingCompletion = false;

                    _ctx.Ui.ShowNotification("~r~You broke line of sight — robbery paused.");
                    return;
                }

                // ⭐ Fleeing is disabled — clerks surrender instead
                store.ClerkFleeing = false;

                if (store.ClerkSurrenderStage == 0)
                {
                    StartClerkSurrender(store, clerk);
                }
                else
                {
                    UpdateClerkSurrender(store, clerk);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessFlee", ex);
            }
        }

        // ------------------------------------------------------------
        // START CLERK SURRENDER (PATCH L — Finalized)
        // ------------------------------------------------------------
        private void StartClerkSurrender(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ Clerk must be stable
                if (clerk.IsDead || clerk.IsRagdoll)
                    return;

                // ⭐ PATCH L — Validate state machine
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH L] Invalid state machine at surrender start. Resetting.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH L — Player must be threatening
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH L] Player not threatening at surrender start. Resetting.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH L — Clerk must be at register
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH L] Clerk displaced at surrender start. Resetting.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH L — Clerk must face player
                Vector3 toPlayer = (Game.Player.Character.Position - clerk.Position).Normalized;
                float dot = Vector3.Dot(clerk.ForwardVector, toPlayer);
                if (dot < 0.25f)
                {
                    DebugLogger.Warn($"[PATCH L] Clerk not facing player at surrender start. Resetting.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH L — Prevent animation overlap
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ Begin surrender
                ClearAllClerkPhases(store);
                store.ClerkFleeing = true;
                store.ClerkSurrenderStage = 1;

                clerk.Task.ClearAllImmediately();
                clerk.Task.HandsUp(-1);

                DebugLogger.Info($"[PATCH L] Clerk at store {store.Id} started surrender sequence.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StartClerkSurrender (PATCH L)", ex);
            }
        }

        // ------------------------------------------------------------
        // UPDATE CLERK SURRENDER (PATCH L — Finalized + PATCH S)
        // ------------------------------------------------------------
        private void UpdateClerkSurrender(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ Clerk must be stable
                if (clerk.IsDead || clerk.IsRagdoll)
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;

                // ⭐ PATCH Q — Position/heading validation
                if (ValidateClerkPosition(store))
                    return;

                // ⭐ PATCH L — Validate state machine
                if (!ValidateClerkStateMachine(store))
                {
                    DebugLogger.Warn($"[PATCH L] Invalid state machine during surrender. Resetting.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH L — Player must be threatening
                if (!PlayerThreatValid(store, clerk, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH L] Player not threatening during surrender. Resetting.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH L — Clerk must be at register
                if (!IsClerkAtRegister(store, clerk))
                {
                    DebugLogger.Warn($"[PATCH L] Clerk displaced during surrender. Resetting.");
                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    return;
                }

                // ⭐ PATCH R — LOS persistence enforcement
                if (ClerkLostLOS(store, Game.Player.Character))
                {
                    DebugLogger.Warn($"[PATCH R] Player broke LOS during {nameof(UpdateClerkSurrender)} at store {store.Id}. Pausing robbery.");

                    ClearAllClerkPhases(store);
                    store.ClerkStalling = true;
                    store.PendingCompletion = false;

                    _ctx.Ui.ShowNotification("~r~You broke line of sight — robbery paused.");
                    return;
                }

                // ⭐ PATCH S — Animation integrity enforcement (HandsUp must persist)
                if (store.ClerkSurrenderStage >= 2) // hands-up or final idle
                {
                    // Check if clerk is still playing the hands-up animation
                    bool handsUpActive = Function.Call<bool>(
                        Hash.IS_ENTITY_PLAYING_ANIM,
                        clerk.Handle,
                        "random@arrests",
                        "idle_2_hands_up",
                        3
                    );

                    if (!handsUpActive)
                    {
                        DebugLogger.Warn($"[PATCH S] Clerk dropped hands during surrender at store {store.Id}. Restarting hands-up.");

                        clerk.Task.ClearAllImmediately();
                        clerk.Task.HandsUp(-1);

                        // Do NOT advance surrender stage until animation is stable
                        return;
                    }
                }

                // ⭐ PATCH L — Prevent animation overlap
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ Surrender stage logic
                switch (store.ClerkSurrenderStage)
                {
                    case 1:
                        // Hands up already playing
                        store.ClerkSurrenderStage = 2;
                        store.ClerkAnimStartUtc = DateTime.UtcNow;
                        store.ClerkAnimDurationMs = 2000;
                        break;

                    case 2:
                        // Wait for hands-up duration
                        if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                            return;

                        // Final idle surrender
                        clerk.Task.ClearAllImmediately();
                        clerk.Task.HandsUp(-1);

                        store.ClerkSurrenderStage = 3;
                        _ctx.Ui.ShowNotification("~y~The clerk is fully surrendered!~s~ Grab the bag, crack the safe and get out of there!");
                        DebugLogger.Info($"[PATCH L] Clerk at store {store.Id} is fully surrendered.");
                        break;

                    case 3:
                        // Final idle — nothing more to do
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UpdateClerkSurrender (PATCH L)", ex);
            }
        }

        // ------------------------------------------------------------
        // FIGHT OR FLIGHT PISTOL / SHOTGUN (PATCH 9F APPLIED)
        // ------------------------------------------------------------
        private void ProcessFeelingFroggy(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // ⭐ PATCH 9F — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ Clerk cannot fight if invalid state
                if (clerk.IsDead || clerk.IsRagdoll || store.ClerkFleeing)
                    return;

                // ⭐ Wait for previous animation to finish
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // ⭐ Must have LOS to player
                bool los = Function.Call<bool>(
                    Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY,
                    clerk.Handle,
                    player.Handle,
                    17
                );
                if (!los)
                    return;

                // ⭐ Must be facing player (prevents 180° instant snap)
                Vector3 toPlayer = (player.Position - clerk.Position).Normalized;
                float dot = Vector3.Dot(clerk.ForwardVector, toPlayer);
                if (dot < 0.25f) // ~75° cone
                    return;

                // ⭐ Must not be in another phase
                if (store.ClerkStalling ||
                    store.ClerkOpeningRegister ||
                    store.ClerkGrabbingCash ||
                    store.ClerkThrowingBag ||
                    store.ClerkPanicking)
                    return;

                // ------------------------------------------------------------
                // ⭐ FIGHT BACK
                // ------------------------------------------------------------
                switch (store.ReactionType)
                {
                    case ClerkReactionType.FightPistol:
                        clerk.Weapons.Give(WeaponHash.Pistol, 60, true, true);
                        clerk.Task.Combat(player);
                        break;

                    case ClerkReactionType.FightShotgun:
                        clerk.Weapons.Give(WeaponHash.PumpShotgun, 20, true, true);
                        clerk.Task.Combat(player);
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessFeelingFroggy", ex);
            }
        }

        // ------------------------------------------------------------
        // SILENT ALARM
        // ------------------------------------------------------------
        private void TryTriggerSilentAlarm(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                if (store.SilentAlarmPressed)
                    return;

                if (!store.ClerkReacted)
                    return;

                // Chance-based trigger
                int chance = store.ClerkRecognizedPlayer ? 40 : 20;
                if (_rng.Next(0, 100) >= chance)
                    return;

                // Mark alarm pressed
                store.SilentAlarmPressed = true;
                store.SilentAlarmUtc = DateTime.UtcNow;

                // Clear tasks so animation can play
                //clerk.Task.ClearAllImmediately();
                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                    clerk.Task.ClearAll();

                // Load keypad animation
                Function.Call(Hash.REQUEST_ANIM_DICT, "anim@heists@keypad@");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "anim@heists@keypad@"))
                {
                    // Press keypad
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "anim@heists@keypad@",
                        "enter",
                        8.0f,
                        -8.0f,
                        1500,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );
                }

                // Set up next phase timing
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 1500;

                // ⭐ After animation finishes, clerk will hold idle pose
                Script.Wait(1500);
                if (clerk != null && clerk.Exists())
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "anim@heists@keypad@",
                        "idle_a",
                        8.0f,
                        -8.0f,
                        2000,
                        (int)AnimationFlags.None,
                        0f,
                        false, false, false
                    );
                }

                //// Trigger police response
                //Game.Player.WantedLevel = Math.Max(Game.Player.WantedLevel, 2);

                // Speech
                SafePlaySpeech(clerk, "GENERIC_SHOCKED_MED", "SPEECH_PARAMS_FORCE");

                DebugLogger.Info($"Silent alarm triggered at store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.TryTriggerSilentAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // POLICE CALL (PATCH 8B APPLIED)
        // ------------------------------------------------------------
        private void TryTriggerPoliceCall(TrackedStore store, Ped clerk, Ped player)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists() || player == null || !player.Exists())
                    return;

                // ⭐ PATCH 8B — HEAT SAFETY GUARDS
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                if (store.ClerkStalling || store.ClerkOpeningRegister || store.ClerkGrabbingCash || store.ClerkThrowingBag)
                    return;

                if (store.ClerkCallingPolice)
                    return;

                if (!store.ClerkReacted)
                    return;

                if (store.ClerkFleeing)
                    return;

                // Player still threatening → clerk does NOT call police
                if (_ctx.Player.IsThreatening(clerk))
                    return;

                if (!store.IsRobberyActive)
                    return;

                // If player leaves the store radius, clerk may call police
                if (!store.IsPlayerInsideStore)
                {
                    if (DateTime.UtcNow < _nextPoliceCallAttempt)
                        return;

                    _nextPoliceCallAttempt = DateTime.UtcNow.AddSeconds(5); // 5s cooldown

                    int chance = store.ClerkRecognizedPlayer ? 50 : 25;
                    if (_rng.Next(0, 100) < chance)
                    {
                        store.ClerkCallingPolice = true;
                        store.ClerkCallStartUtc = DateTime.UtcNow;

                        SafePlaySpeech(clerk, "GENERIC_SHOCKED_MED", "SPEECH_PARAMS_FORCE");

                        //// ⭐ PATCH 8B — SAFE HEAT INCREMENT
                        //store.HeatLevel += 1;
                        //Game.Player.WantedLevel = Math.Max(Game.Player.WantedLevel, 2);

                        DebugLogger.Info($"Police called for robbery at store {store.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.TryTriggerPoliceCall", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK DEATH HANDLING — SAFE KO DETECTION
        // ------------------------------------------------------------
        private bool IsPedKnockedOut(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists())
                    return false;

                // KO states that are NOT death
                return ped.IsRagdoll ||
                       ped.IsInjured ||
                       (ped.Health <= 1 && !ped.IsDead);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.IsPedKnockedOut", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // CLERK DEATH HANDLING — SAFE RAGDOLL KO
        // ------------------------------------------------------------
        private void KnockOutPed(Ped ped)
        {
            try
            {
                if (ped == null || !ped.Exists())
                    return;

                ped.Health = 1; // keep alive
                ped.Armor = 0;

                // Clear tasks safely
                ped.Task.ClearAllImmediately();

                // Apply ragdoll KO
                ped.SetToRagdoll(5000, 5000, 0);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.KnockOutPed", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK DEATH HANDLING LOGIC
        // ------------------------------------------------------------
        private void HandleClerkDeath(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                // ⭐ HARD STOP: if death already handled, never process again
                if (store.ClerkDeathHandled)
                    return;

                // ⭐ Arm the fuse immediately so any re-entrant calls are no-ops
                store.ClerkDeathHandled = true;

                Ped clerk = store.Clerk;
                Ped player = Game.Player.Character;

                // If clerk reference is gone, treat as dead
                bool clerkExists = (clerk != null && clerk.Exists());
                bool isDead = clerkExists && clerk.IsDead;
                int health = clerkExists ? clerk.Health : 0;

                DebugLogger.Info($"[DeathCheck] exists={clerkExists}, isDead={isDead}, health={health}");

                // ============================================================
                // KO / DEATH DETECTION
                // ============================================================

                // 1) NON-LETHAL KNOCKOUT — ONLY IF ALIVE
                if (clerkExists && !isDead && health > 0 && IsPedKnockedOut(clerk))
                {
                    store.ClerkKilledWithGun = false;
                    store.SilentRobbery = true;

                    // Force KO ragdoll
                    KnockOutPed(clerk);

                    // UI + Stalker
                    _ctx.Ui.TextNotification(
                        "DIA_SILENT",
                        "Silent Takedown",
                        "LOS ANGELES PD",
                        "Clerk knocked out silently."
                    );

                    _ctx.Stalker.QueueKnockoutMessage();

                    DebugLogger.Info($"[KO] Clerk {clerk.Handle} knocked out at store {store.Id} / {store.Name}");
                }
                else
                {
                    // Determine weapon
                    WeaponHash weapon = WeaponHash.Unarmed;
                    if (player != null && player.Exists())
                        weapon = player.Weapons.Current.Hash;

                    bool melee = _ctx.Player.IsMeleeWeapon(weapon);

                    // 2) LETHAL KILL (GUN) — DEAD OR HANDLE INVALID
                    if (!clerkExists || (isDead && !melee))
                    {
                        store.ClerkKilledWithGun = true;

                        _ctx.Ui.TextNotification(
                            "DIA_POLICE",
                            "All Units Responding",
                            "LOS ANGELES PD",
                            "Reported armed robbery in progress, shots fired at " + store.Name
                        );

                        _ctx.Stalker.QueueGunKillMessage();

                        // Gun kill ALWAYS activates robbery
                        store.IsRobberyActive = true;

                        DebugLogger.Info($"[GUN KILL] Clerk {clerk?.Handle} shot and killed at store {store.Id} / {store.Name}");
                    }
                    // 3) LETHAL KILL (MELEE)
                    else if (isDead && melee)
                    {
                        store.ClerkKilledWithGun = false;

                        _ctx.Ui.TextNotification(
                            "DIA_POLICE",
                            "Robbery Reported",
                            "LOS ANGELES PD",
                            "Clerk found injured at " + store.Name
                        );

                        _ctx.Stalker.QueueMeleeKillMessage();

                        DebugLogger.Info($"[MELEE KILL] Clerk {clerk.Handle} killed via melee at store {store.Id} / {store.Name}");
                    }
                }

                // ============================================================
                // NEW SYSTEM CLEANUP
                // ============================================================
                store.Clerk = null;
                store.IsOurClerk = false;
                store.ClerkIdle = false;
                store.ClerkReacted = false;
                store.ClerkStalling = false;
                store.ClerkOpeningRegister = false;
                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = false;
                store.ClerkPanicking = false;
                store.ClerkFleeing = false;

                // Dummy clerk safety
                if (store.DummyClerk != null && store.DummyClerk.Exists())
                {
                    store.DummyClerk.Delete();
                    store.DummyClerk = null;
                }

                store.ClerkDeathHandledCheck = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.HandleClerkDeath", ex);
            }
        }

        // ------------------------------------------------------------
        // SAFE SPEECH WRAPPER (SHVDN 3.9.0 SAFE)
        // ------------------------------------------------------------
        private void SafePlaySpeech(Ped ped, string speechName, string speechParam)
        {
            if (ped == null || !ped.Exists())
                return;

            try
            {
                // SHVDN 3.9.0: use native PLAY_PED_AMBIENT_SPEECH_NATIVE
                Function.Call(
                    Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE,
                    ped.Handle,
                    speechName,
                    speechParam,
                    0 // p3 (always 0 in game scripts)
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException($"ClerkSystem.SafePlaySpeech {speechName}", ex);
            }
        }
    }
}
