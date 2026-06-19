using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Scripts.Systems;
using StoreRobberyEnhanced.UI;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace StoreRobberyEnhanced.Systems
{
    internal class ClerkSystem
    {
        private readonly StoreContext _ctx;
        private readonly Random _rng;
        private readonly ClerkHelperSystem _clerkHelper;
        private readonly SpeechManagerSystem _speech = new();

        private DateTime _nextPoliceCallAttempt = DateTime.MinValue;

        public ClerkSystem(StoreContext ctx)
        {
            _ctx = ctx;
            _rng = new Random();
        }

        public enum ClerkPhase
        {
            None = 0,
            Stall = 1,
            RegisterOpening = 2,
            CashGrab = 3,
            BagToss = 4,
            Flee = 5,
            Surrender = 6
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

                // ⭐ PATCH 10 — CLERK STATE MACHINE INTEGRITY GUARD
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

                    // ⭐ FIX — SafeCrack cancels surrender logic
                    store.ClerkSurrender = false;
                    store.ClerkSurrenderStage = 0;

                    return;
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

                // ⭐ PATCH 11 — GLOBAL ROBBERY FLOW CONSISTENCY CONTROLLER
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

                // Clerk fully surrendered — but robbery MUST continue (safe cracking still allowed)
                if (store.ClerkSurrenderStage == 3)
                {
                    // Play idle surrender animation, but DO NOT end robbery
                    RunIdleSurrenderBehavior(store, clerk);

                    // Keep robbery active
                    store.IsRobberyActive = true;
                    store.IsRobbed = true;

                    // DO NOT return — allow robbery system to continue
                }

                // If robbery ended → no further escalation allowed
                if (store.RobberyEnded)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;
                    store.ClerkFleeing = false;
                    store.ClerkSurrenderStage = 0;
                    store.ClerkSurrender = false;

                    // ⭐ NEW: clear surrender idle anim if still playing
                    if (IsPlayingAnim(clerk, "random@arrests@busted", "idle_a") ||
                        IsPlayingAnim(clerk, "random@arrests@busted", "idle_b") ||
                        IsPlayingAnim(clerk, "random@arrests@busted", "idle_c"))
                    {
                        DebugLogger.Info($"[RESET] Clearing surrender idle on clerk {clerk.Handle} after robbery end.");
                        Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, clerk);
                    }

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
                }// If SafeCrack running → freeze clerk
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    store.ClerkStalling = false;
                    store.ClerkOpeningRegister = false;
                    store.ClerkGrabbingCash = false;
                    store.ClerkThrowingBag = false;
                    store.ClerkPanicking = false;

                    // ⭐ FIX — SafeCrack cancels surrender logic
                    store.ClerkSurrender = false;
                    store.ClerkSurrenderStage = 0;

                    return;
                }


                // ⭐ SAFETY RESET: only if clerk is actually stuck AND no robbery is active
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

                // ⭐ PATCH 7 — REACTION SAFETY GUARDS
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.SilentRobbery)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                //if (store.ClerkFleeing || clerk.IsFleeing)
                //    return;

                if (clerk.IsRagdoll)
                    return;

                if (!store.IsPlayerInsideStore)
                    return;

                // ⭐ NEW: greet player when they enter
                PlayClerkEntryGreeting(store, clerk);

                // NORMAL IDLE LOGIC
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

                // ⭐ PATCH 7 — THREAT VALIDATION
                if (!store.ClerkReacted)
                {
                    if (PlayerThreatValid(store, clerk, player))
                    {
                        BeginFearReaction(store, clerk);
                        return;
                    }
                }

                // REMAINING BEHAVIOR
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

                // ⭐ PRE-CLEANUP — Remove any existing clerk stuck in surrender or idle
                if (store.Clerk != null && store.Clerk.Exists())
                {
                    // Clear surrender/idle animations
                    if (Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, store.Clerk.Handle, "random@arrests@busted", "idle_a", 3) ||
                        Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, store.Clerk.Handle, "random@arrests@busted", "idle_b", 3) ||
                        Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, store.Clerk.Handle, "random@arrests@busted", "idle_c", 3))
                    {
                        DebugLogger.Info($"[ForceSpawnClerk] Clearing surrender idle on clerk {store.Clerk.Handle} before respawn.");
                        Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, store.Clerk);
                    }

                    // Delete old clerk safely
                    store.Clerk.MarkAsNoLongerNeeded();
                    store.Clerk.Delete();
                    store.Clerk = null;
                }

                // ⭐ SPAWN NEW CLERK
                Ped clerk = World.CreatePed(PedHash.Business01AMM, store.ClerkPos, store.ClerkHeading);

                if (clerk == null || !clerk.Exists())
                {
                    // DebugLogger.Warn($"[ForceSpawnClerk] Failed to spawn clerk for store {store.Id}");
                    return;
                }

                store.Clerk = clerk;

                // ⭐ Record spawn time for interior detach logic
                store.ClerkSpawnTime = Game.GameTime;
                store.IsOurClerk = true;

                clerk.IsPersistent = true;
                clerk.BlockPermanentEvents = true;
                clerk.Task.ClearAllImmediately();

                // Idle state
                store.ClerkIdle = true;

                // ⭐ SECOND FIX — STOP FUTURE CLERK SWEEPS
                store.DefaultClerkRemoved = true;
                store.LastClerkSweepUtc = DateTime.UtcNow + TimeSpan.FromHours(12);

                // ⭐ RESET CLERK STATE MACHINE
                store.ClerkReacted = false;
                store.ClerkSurrenderStage = 0;
                store.ClerkStalling = false;
                store.ClerkOpeningRegister = false;
                store.ClerkGrabbingCash = false;
                store.ClerkThrowingBag = false;
                store.ClerkPanicking = false;
                store.ClerkFleeing = false;
                store.ClerkRecognizedPlayer = false;

                // ⭐ Ensure clerk starts clean (no surrender or panic)
                Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, clerk);
                Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, clerk, true);
                Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, clerk, true);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, clerk.Handle, 46, false);

                //DebugLogger.Info($"ForceSpawnClerk: Clerk spawned cleanly and sweeps disabled for store {store.Id}");
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
                    // DebugLogger.Warn($"SpawnDummyClerk: Failed to spawn dummy clerk for store {store.Id}");
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

                // DebugLogger.Info($"SpawnDummyClerk: Dummy clerk spawned for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.SpawnDummyClerk", ex);
            }
        }

        // ------------------------------------------------------------
        // PATCH I — Unified Player Threat Validation (Silent + Loud)
        // NULL-SAFE + PATCH U COMPATIBLE + NO DUPLICATE LOGIC
        // ------------------------------------------------------------

        public bool PlayerThreatValid(TrackedStore store, Ped clerk, Ped player)
        {
            try
            {
                // SAFETY
                if (store == null || clerk == null || player == null)
                    return false;

                if (!clerk.Exists() || !player.Exists())
                    return false;

                // ⭐ Position safety
                if (clerk.Position == Vector3.Zero || player.Position == Vector3.Zero)
                    return false;

                PlayerHelper ph = _ctx.Player;
                if (ph == null)
                    return false;

                // 1. DISTANCE
                float dist = player.Position.DistanceTo(clerk.Position);

                if (store.SilentRobbery)
                {
                    if (dist > 3.0f)
                        return false;
                }
                else
                {
                    if (dist > 20.0f)
                        return false;
                }

                // WEAPON INFO
                Weapon current = player.Weapons?.Current;
                bool hasWeapon = current != null && current.Hash != WeaponHash.Unarmed;

                bool isMelee = false;
                bool isGun = false;

                if (hasWeapon)
                {
                    // ⭐ Use YOUR melee list
                    isMelee = ph.IsMeleeWeapon(current.Hash);
                    isGun = !isMelee;
                }

                bool isAiming = ph.IsAiming();

                // 2. LINE OF SIGHT CHECK
                if (!store.SilentRobbery)
                {
                    // ⭐ For loud robberies, LOS is *informational* only.
                    // Glass/doorframes often break HAS_ENTITY_CLEAR_LOS_TO_ENTITY at the door.
                    bool los = true;
                    try { los = ph.IsInLOS(clerk); }
                    catch { los = true; }

                    DebugLogger.Trace($"PlayerThreatValid: loud, dist={dist:F1}, los={los}");
                    // ❌ Do NOT early-return on !los for loud mode.
                    // We want aiming from the doorway to always count as a threat.
                }
                else
                {
                    // Silent robbery can still be strict if you want LOS here later.
                }

                // ============================================================
                // ⭐ SILENT ROBBERY MODE
                // ============================================================
                if (store.SilentRobbery)
                {
                    // Must be melee (your list)
                    if (!isMelee)
                        return false;

                    // Must NOT aim
                    if (isAiming)
                        return false;

                    // Must be masked
                    if (!ph.IsMasked())
                        return false;

                    // Must be in front arc
                    Vector3 toPlayer = (player.Position - clerk.Position).Normalized;
                    float dot = Vector3.Dot(clerk.ForwardVector, toPlayer);
                    if (dot < 0.0f)
                        return false;

                    return true;
                }

                // ============================================================
                // ⭐ LOUD ROBBERY MODE (MERGED WITH OLD IsThreateningSoft)
                // ============================================================

                // 1. OLD: Direct free-aim at clerk (guns only)
                if (isGun &&
                    Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY, Game.Player, clerk))
                    return true;

                // 2. OLD: Gun out + close range (<6.5m)
                if (isGun && dist < 20.5f)
                    return true;

                // 3. OLD: Aiming a gun anywhere = threat
                if (isGun && isAiming)
                    return true;

                // 4. NEW: Melee = threat (loud mode)
                if (isMelee)
                    return true;

                // 5. NEW: Doorway aiming support (LOS optional)
                if (isGun && isAiming)
                    return true;

                return false;
            }
            catch
            {
                return false;
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

        public void SetClerkPhase(TrackedStore store, ClerkPhase phase)
        {
            store.CurrentPhase = phase;

            // Clear all old flags
            store.ClerkOpeningRegister = false;
            store.ClerkGrabbingCash = false;
            store.ClerkThrowingBag = false;
            store.ClerkPanicking = false;
            store.ClerkFleeing = false;
            store.ClerkStalling = false;

            // Set the correct flag
            switch (phase)
            {
                case ClerkPhase.Stall:
                    store.ClerkStalling = true;
                    break;

                case ClerkPhase.RegisterOpening:
                    store.ClerkOpeningRegister = true;
                    break;

                case ClerkPhase.CashGrab:
                    store.ClerkGrabbingCash = true;
                    break;

                case ClerkPhase.BagToss:
                    store.ClerkThrowingBag = true;
                    break;

                case ClerkPhase.Flee:
                    store.ClerkFleeing = true;
                    break;

                case ClerkPhase.Surrender:
                    store.ClerkSurrender = true;
                    store.ClerkSurrenderStage = 1;
                    break;
            }
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
        // PATCH F — Animation Safety Check
        // ------------------------------------------------------------
        public bool IsClerkBusy(Ped clerk)
        {
            if (clerk == null || !clerk.Exists())
                return true;

            // If ANY animation is playing, clerk is busy
            return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "mp_common", "givetake1_a", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "mp_common", "givetake2_a", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "busted", "idle_2_hands_up", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "busted", "idle_2_hands_up2", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "busted", "idle_a", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "busted", "idle_b", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "random@arrests@busted", "idle_a", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "random@arrests@busted", "idle_b", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "random@arrests@busted", "idle_c", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "rcmme_tracey1", "nervous_loop", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "oddjobs@shop_robbery@rob_till", "enter", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "oddjobs@shop_robbery@rob_till", "loop", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "anim@heists@ornate_bank@grab_cash", "idle", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "mp_am_hold_up", "purchase_beer_shopkeeper", 3);
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
        // SAFE SPEECH WRAPPER (SHVDN 3.9.0 SAFE)
        // ------------------------------------------------------------
        public void SafePlaySpeech(Ped ped, string speechName, string speechParam)
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
                        0 | 16,
                        0f,
                        false, false, false
                    );
                }

                // ⭐ PLAY QUIET REGISTER / MONEY SOUND
                // "ROBBERY_MONEY" is a subtle cash-handling sound used in GTA V
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "ROBBERY_MONEY", "HUD_AWARDS");
                Script.Wait(300); // small delay to avoid sound overlap

                // ⭐ PATCH D — Only spawn bag AFTER animation starts
                _ctx.Robberies.SpawnLootBag(store, clerk);

                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");

                // ⭐ PLAYER NOTIFICATION
                _ctx.Ui.ShowNotification("~g~Clerk quietly hands over the register cash.~s~ Crack the safe before leaving.");
                DebugLogger.Info($"Played silent robbery anim for store {store.Id} on clerk {clerk.Handle}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.PlaySilentRobberyAnim", ex);
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
        // SMALL HELPER: GREETING SPEECH
        // ------------------------------------------------------------
        private void PlayClerkEntryGreeting(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Already greeted this entry
                if (store.GreetedPlayer)
                    return;

                // Do NOT greet during robbery or surrender
                if (store.IsRobberyActive || store.ClerkReacted || store.ClerkSurrenderStage > 0)
                    return;

                // Do NOT greet if clerk is busy with a full-body task
                if (clerk.IsInCombat || clerk.IsFleeing || clerk.IsRagdoll)
                    return;

                // Mark greeted
                store.GreetedPlayer = true;

                // ⭐ Play greeting speech
                SafePlaySpeech(clerk, _speech.Get("Idle"), "SPEECH_PARAMS_FORCE");

                // ⭐ Optional: small upper-body wave animation (safe)
                Function.Call(Hash.REQUEST_ANIM_DICT, "gestures@m@standing@casual");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "gestures@m@standing@casual"))
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "gestures@m@standing@casual",
                        "gesture_hello",
                        4.0f,
                        -4.0f,
                        1500,
                        (int)(AnimationFlags.Loop | AnimationFlags.UpperBodyOnly),
                        0f,
                        false, false, false
                    );
                }

                DebugLogger.Info($"[GREET] Clerk greeted player at store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.PlayClerkEntryGreeting", ex);
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

                    SafePlaySpeech(clerk, _speech.Get("Idle"), "SPEECH_PARAMS_FORCE");

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
        // SURRENDER IDLE BEHAVIOR
        // ------------------------------------------------------------
        public void RunIdleSurrenderBehavior(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                // Cooldown so we don't spam anim requests
                int now = Game.GameTime;
                if (now - store.LastIdleTime < 4000) // 4‑second buffer
                    return;

                string dict = "random@arrests@busted";
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

                    SafePlaySpeech(clerk, _speech.Get("Surrender"), "SPEECH_PARAMS_FORCE");

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

                // ⭐ FULL ROBBERY START FLAGS (missing before)
                //store.IsRobbed = true;
                store.IsRobberyActive = true;
                store.PendingCompletion = true;

                store.ClerkReacted = true;
                store.ClerkIdle = false;

                clerk.Task.ClearAllImmediately();
                clerk.Task.HandsUp(-1);

                //Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, clerk, "SHOP_CLERK_REACT", "SPEECH_PARAMS_FORCE", 0);
                SafePlaySpeech(clerk, _speech.Get("Threat"), "SPEECH_PARAMS_FORCE");

                // Recognition escalation
                if (store.TimesRobbed >= 2)
                    store.ClerkRecognizedPlayer = true;

                // 🎲 Random chance to fight back (10–20% typical)
                int roll = _rng.Next(0, 100);
                if (roll < 30) // 30% chance to fight
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
                if (store.ReactionType == ClerkReactionType.NormalPanic)
                    store.ClerkStalling = true;
                store.StallStartUtc = DateTime.UtcNow;
                store.StallDurationMs = _rng.Next(3000, 7000);

                // ⭐ PATCH U — Lock phase
                SetClerkPhase(store, ClerkPhase.Stall);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.BeginFearReaction", ex);
            }
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

                // STILL STALLING?
                if ((DateTime.UtcNow - store.StallStartUtc).TotalMilliseconds < store.StallDurationMs)
                {
                    if (!IsPlayingAnim(clerk, "rcmme_tracey1", "nervous_loop"))
                    {
                        Function.Call(Hash.REQUEST_ANIM_DICT, "rcmme_tracey1");

                        if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "rcmme_tracey1"))
                        {
                            Function.Call(
                                Hash.TASK_PLAY_ANIM,
                                clerk.Handle,
                                "rcmme_tracey1",
                                "nervous_loop",
                                8.0f,
                                -8.0f,
                                7000,
                                (int)(AnimationFlags.Loop | AnimationFlags.UpperBodyOnly),
                                0f,
                                false, false, false
                            );
                        }
                    }

                    // ⭐ ALWAYS speak during stall, not only when animation starts
                    SafePlaySpeech(clerk, _speech.Get("Stall"), "SPEECH_PARAMS_FORCE");

                    _ctx.Ui.ShowNotification("~y~The clerk is stalling...~s~ Wait for them to open the register.");
                    return;
                }

                // ⭐ PATCH 9A — Stall finished → transition safety
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

                // ⭐ PATCH U — Lock phase
                SetClerkPhase(store, ClerkPhase.RegisterOpening);

                // Speech
                SafePlaySpeech(clerk, _speech.Get("Stall"), "SPEECH_PARAMS_FORCE");

                PlayAnimNative(clerk, "rcmme_tracey1", "nervous_loop", AnimationFlags.None);
                DebugLogger.Info($"[ANIM] Clerk at store {store.Id} finished stalling and is now opening the register.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessStall", ex);
            }
        }

        // ------------------------------------------------------------
        // REGISTER OPENING (PATCH 9B APPLIED + PATCH S + PATCH U)
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

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                    return;

                // STILL IN FIRST ANIMATION PHASE?
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                // FIRST PHASE: PLAY "ENTER" ANIMATION
                if (!store.ClerkGrabbingCash)
                {
                    // ⭐ PATCH U — Transition to CashGrab
                    SetClerkPhase(store, ClerkPhase.CashGrab);

                    // Safety: clear tasks only if clerk is stable
                    if (!clerk.IsRagdoll && !store.ClerkFleeing)
                        clerk.Task.ClearAllImmediately();

                    // ⭐ PATCH B — Animation Failure Fallback
                    if (SafeLoadAnimDict("oddjobs@shop_robbery@rob_till"))
                    {
                        Function.Call(
                            Hash.TASK_PLAY_ANIM,
                            clerk.Handle,
                            "oddjobs@shop_robbery@rob_till",
                            "enter",
                            4.0f, -4.0f,
                            1000,
                            (int)(AnimationFlags.Loop | AnimationFlags.UpperBodyOnly),
                            0f,
                            false, false, false
                        );

                        SafePlaySpeech(clerk, _speech.Get("Register"), "SPEECH_PARAMS_FORCE");

                        _ctx.Ui.ShowNotification("~y~The clerk is opening the register...~s~ Get ready to grab the cash!");

                        if (_ctx.Config.EnableStalkerMsg)
                            _ctx.Stalker.QueueRobberyMessage();

                        DebugLogger.Info($"[ANIM] Clerk at store {store.Id} is opening the register.");
                    }
                    else
                    {
                        DebugLogger.Warn($"[PATCH B] Animation dict failed to load for store {store.Id}. Halting robbery phase.");
                        SetClerkPhase(store, ClerkPhase.Stall);
                        return;
                    }

                    store.ClerkAnimStartUtc = DateTime.UtcNow;
                    store.ClerkAnimDurationMs = 1500;
                    return;
                }

                // SECOND PHASE: IDLE AT OPEN REGISTER
                SetClerkPhase(store, ClerkPhase.BagToss);

                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                {
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "oddjobs@shop_robbery@rob_till",
                        "exit",
                        4.0f,
                        -4.0f,
                        1000,
                        (int)(AnimationFlags.Loop | AnimationFlags.UpperBodyOnly),
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
        // CASH GRAB (PATCH 9C APPLIED + PATCH U + PATCH S)
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

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                    return;

                // STILL IN PREVIOUS PHASE?
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;
                
                // Safety: only clear tasks if clerk is stable
                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                    clerk.Task.ClearAllImmediately();

                SafePlaySpeech(clerk, _speech.Get("CashGrab"), "SPEECH_PARAMS_FORCE");

                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                });

                // LOAD ANIM DICT
                Function.Call(Hash.REQUEST_ANIM_DICT, "oddjobs@shop_robbery@rob_till");

                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "oddjobs@shop_robbery@rob_till"))
                {
                    // Play the give-money animation
                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "oddjobs@shop_robbery@rob_till",
                        "loop",
                        8.0f,
                        -8.0f,
                        10000,
                        1 | 2,
                        0f,
                        false, false, false
                    );

                    SafePlaySpeech(clerk, _speech.Get("CashGrab"), "SPEECH_PARAMS_FORCE");

                    _ctx.Ui.ShowNotification("~y~The clerk is grabbing the cash...~s~ Get ready to toss the bag!");
                    DebugLogger.Info($"[ANIM] Clerk at store {store.Id} is grabbing cash from the register.");
                }
                else
                {
                    DebugLogger.Warn($"[PATCH B] Animation dict failed to load for store {store.Id}. Halting robbery phase.");

                    SetClerkPhase(store, ClerkPhase.Stall);
                    return;
                }

                // SET TIMING FOR NEXT PHASE
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 9000;

                // PATCH 9C — SAFE PAYOUT (ONE-SHOT)
                int payout = _ctx.Rng.Next(_ctx.Config.RegisterMinAmount, _ctx.Config.RegisterMaxAmount + 1);
                payout = (int)(payout * _ctx.Config.PayoutMultiplier);

                store.PendingPayout += payout;

                // ⭐ PATCH U — Transition to Bag Toss Phase
                SetClerkPhase(store, ClerkPhase.BagToss);

            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessCashGrab", ex);
            }
        }

        // ------------------------------------------------------------
        // BAG TOSS (PATCH D — Safe Bag Toss Logic + PATCH S + PATCH U)
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

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ Wait for previous animation to finish
                if ((DateTime.UtcNow - store.ClerkAnimStartUtc).TotalMilliseconds < store.ClerkAnimDurationMs)
                    return;

                // ⭐ Safety: only clear tasks if clerk is stable
                if (!clerk.IsRagdoll && !store.ClerkFleeing)
                    clerk.Task.ClearAllImmediately();

                // PLAY BAG TOSS ANIMATION
                if (SafeLoadAnimDict("mp_common"))
                {
                    clerk.Task.ClearAllImmediately();

                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "mp_common",
                        "givetake2_a",   // ⭐ Bag toss animation
                        4.0f, -4.0f,
                        1000,
                        (int)(AnimationFlags.None | AnimationFlags.UpperBodyOnly),
                        0f,
                        false, false, false
                    );

                    SafePlaySpeech(clerk, _speech.Get("BagToss"), "SPEECH_PARAMS_FORCE");

                    _ctx.Ui.ShowNotification("~y~The clerk is tossing the bag...~s~ Grab it, crack the safe and get out of there!");

                    _ctx.Ui.ShowSubtitle("~o~Remember there is a safe in the office — crack it too.", 5000);

                    if (_ctx.Config.EnableStalkerMsg)
                        _ctx.Stalker.QueueRobberyMessage();

                    DebugLogger.Info($"[ANIM] Clerk at store {store.Id} is tossing the bag.");
                }
                else
                {
                    DebugLogger.Warn($"[PATCH B] Animation dict failed to load for store {store.Id}. Halting bag toss.");
                    SetClerkPhase(store, ClerkPhase.Stall);
                    return;
                }

                // ⭐ PATCH U — Transition to Surrended Phase
                SetClerkPhase(store, ClerkPhase.Flee);
                store.ClerkSurrenderStage = 0;
                store.ClerkFleeing = true;
                DebugLogger.Info($"Store Surrender State {store.ClerkSurrenderStage} and clerk flee status {store.ClerkFleeing}.");

                // Set timer for next phase
                store.ClerkAnimStartUtc = DateTime.UtcNow;
                store.ClerkAnimDurationMs = 2000;

                // ⭐ PATCH D — Only spawn bag AFTER animation starts
                _ctx.Robberies.SpawnLootBag(store, clerk);

            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessBagToss (PATCH D + PATCH U)", ex);
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
                    //Do NOT advance phases while animation is active
                    return;
                }

                // ⭐ Simple cower behavior (safe)
                if (!clerk.IsInCombat && !clerk.IsFleeing)
                {
                    clerk.Task.ClearAllImmediately();
                    clerk.Task.Cower(-1);
                    _ctx.Ui.ShowNotification("~r~The clerk is panicking and cowering on the ground!~s~ Grab the bag and crack the safe!");
                    DebugLogger.Info($"[ANIM] Clerk at store {store.Id} is panicking and cowering.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessPanic", ex);
            }
        }

        // ------------------------------------------------------------
        // FLEE / SURRENDER OVERRIDE (PATCH 9E APPLIED + PATCH U)
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

                // ⭐ PATCH 9E — Suppression states
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.RobberyEnded)
                    return;

                if (store.CooldownActive)
                    return;

                if (store.SilentRobbery)
                    return;

                //if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                //    return;

                // ⭐ PATCH F — Prevent phase advancement while animations are still running
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ Fleeing is disabled — clerks surrender instead
                store.ClerkFleeing = false;

                SafePlaySpeech(clerk, _speech.Get("Surrender"), "SPEECH_PARAMS_FORCE");
                // ⭐ PATCH U — Transition to Surrender Phase
                if (store.ClerkSurrenderStage == 0)
                {
                    SetClerkPhase(store, ClerkPhase.Surrender);
                    DebugLogger.Info($"[ANIM 1] Clerk at store {store.Id} is about to surrender");
                    StartClerkSurrender(store, clerk);
                }
                else
                {
                    store.ClerkSurrenderStage = 2;
                    UpdateClerkSurrender(store, clerk);
                    DebugLogger.Info($"[ANIM 1] Clerk at store {store.Id} is starting to surrender.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkSystem.ProcessFlee (PATCH 9E + PATCH U)", ex);
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

                // ⭐ PATCH L — Prevent animation overlap
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ Begin surrender
                ClearAllClerkPhases(store);
                store.ClerkFleeing = true;
                store.ClerkSurrenderStage = 2;
                store.ClerkSurrender = true;

                //clerk.Task.ClearAllImmediately();
                //clerk.Task.HandsUp(8000);

                if (SafeLoadAnimDict("busted"))
                {
                    clerk.Task.ClearAllImmediately();

                    // ⭐ Move clerk slightly backward to avoid clipping into counter
                    Vector3 backward = clerk.ForwardVector * -0.25f;   // adjust distance as needed
                    clerk.Position += backward;

                    Function.Call(
                        Hash.TASK_PLAY_ANIM,
                        clerk.Handle,
                        "busted",
                        "idle_2_hands_up",   // ⭐ Bag toss animation
                        4.0f, -4.0f,
                        8000,
                        (int)(AnimationFlags.None | AnimationFlags.UpperBodyOnly),
                        0f,
                        false, false, false
                    );

                    // Speech
                    SafePlaySpeech(clerk, _speech.Get("Surrender"), "SPEECH_PARAMS_FORCE");

                    _ctx.Ui.ShowNotification("~y~The clerk is surrendering!~s~ Grab the bag, crack the safe and get out of there!");
                    DebugLogger.Info($"[AMIN 2] Clerk at store {store.Id} started surrender sequence.");
                }                
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StartClerkSurrender (PATCH L)", ex);
            }
        }

        // ------------------------------------------------------------
        // UPDATE CLERK SURRENDER (PATCH L — Finalized + PATCH S + PATCH U)
        // ------------------------------------------------------------
        private void UpdateClerkSurrender(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;

                //// ⭐ PATCH U — Lock phase
                //SetClerkPhase(store, ClerkPhase.Surrender);

                // ⭐ Clerk must be stable
                if (clerk.IsDead || clerk.IsRagdoll)
                    return;

                // ⭐ PATCH P — Ragdoll recovery
                if (HandleClerkRagdoll(store))
                    return;
               
                // ⭐ PATCH S — Animation integrity enforcement (HandsUp must persist)
                if (store.ClerkSurrenderStage >= 3) // hands-up or final idle
                {
                    bool handsUpActive = Function.Call<bool>(
                        Hash.IS_ENTITY_PLAYING_ANIM,
                        clerk.Handle,
                        "busted",
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

                        if (SafeLoadAnimDict("busted"))
                        {
                            clerk.Task.ClearAllImmediately();

                            Function.Call(
                                Hash.TASK_PLAY_ANIM,
                                clerk.Handle,
                                "busted",
                                "idle_b",   // ⭐ Bag toss animation
                                4.0f, -4.0f,
                                8000,
                                (int)(AnimationFlags.Loop | AnimationFlags.UpperBodyOnly),
                                0f,
                                false, false, false
                            );
                        }

                        // Final idle surrender
                        clerk.Task.ClearAllImmediately();
                        clerk.Task.HandsUp(-1);

                        store.ClerkSurrenderStage = 3;

                        // Speech
                        SafePlaySpeech(clerk, _speech.Get("Surrender"), "SPEECH_PARAMS_FORCE");

                        _ctx.Ui.ShowNotification("~y~The clerk is fully surrendered!~s~ Grab the bag, crack the safe and get out of there!");
                        DebugLogger.Info($"[ANIM 2] Clerk at store {store.Id} is fully surrendered.");
                        break;

                    case 3:
                        // Final idle — nothing more to do
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UpdateClerkSurrender (PATCH L + PATCH U)", ex);
            }
        }

        // ------------------------------------------------------------
        // FIGHT OR FLIGHT PISTOL / SHOTGUN (PATCH 9F+DISTANCE APPLIED)
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

                // ⭐ Distance gate — allow reaction from doorway
                float dist = clerk.Position.DistanceTo(player.Position);
                if (dist > 22.5f) // try 20–25f for door-range
                    return;

                // ⭐ LOS — relax this, door/glass often blocks flag 17
                bool los = Function.Call<bool>(
                    Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY,
                    clerk.Handle,
                    player.Handle,
                    1 // more permissive than 17
                );
                // If this is still too strict, temporarily comment this out to confirm:
                // if (!los)
                //     return;

                // ⭐ Facing — only block if really turned away
                Vector3 toPlayer = (player.Position - clerk.Position).Normalized;
                float dot = Vector3.Dot(clerk.ForwardVector, toPlayer);
                if (dot < -0.25f) // only reject if facing > ~105° away
                    return;

                // ⭐ Must not be in another phase
                if (store.ClerkStalling || store.ClerkOpeningRegister || store.ClerkGrabbingCash || store.ClerkThrowingBag || store.ClerkPanicking)
                    return;

                // ⭐ Force clerk into combat-ready state
                clerk.BlockPermanentEvents = false;
                clerk.AlwaysKeepTask = false;
                clerk.IsPositionFrozen = false;

                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, clerk.Handle, 46, true); // Always fight
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, clerk.Handle, 5, true);  // Can fight armed
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, clerk.Handle, 1, true);  // Use cover
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, clerk.Handle, 3, true);  // Aggressive
                Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, clerk.Handle, 0, false);   // Disable flee
                Function.Call(Hash.SET_PED_COMBAT_ABILITY, clerk.Handle, 2);           // Professional
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, clerk.Handle, 2);          // Offensive
                Function.Call(Hash.SET_PED_COMBAT_RANGE, clerk.Handle, 2);             // Far range

                _ctx.Ui.ShowSubtitle("~r~The clerk is fighting back!~s~ Watch out, they are armed!~n~The robbery is a complete bust, get out of there while you still can!", 4000);
                // ------------------------------------------------------------
                // ⭐ FIGHT BACK
                // ------------------------------------------------------------
                switch (store.ReactionType)
                {
                    case ClerkReactionType.FightPistol:
                        clerk.Weapons.Give(WeaponHash.Pistol, 60, true, true);
                        clerk.Task.ClearAllImmediately();
                        Script.Wait(50);
                        clerk.Task.FightAgainst(player);
                        clerk.Task.Combat(player);
                        // ⭐ Speech AFTER combat assignment
                        Script.Wait(150); // small buffer so animation doesn’t cancel speech
                        SafePlaySpeech(clerk, _speech.Get("Fight"), "SPEECH_PARAMS_FORCE");
                        break;

                    case ClerkReactionType.FightShotgun:
                        clerk.Weapons.Give(WeaponHash.PumpShotgun, 20, true, true);
                        clerk.Task.ClearAllImmediately();
                        Script.Wait(50);
                        clerk.Task.FightAgainst(player);
                        clerk.Task.Combat(player);
                        // ⭐ Speech AFTER combat assignment
                        Script.Wait(150); // small buffer so animation doesn’t cancel speech
                        SafePlaySpeech(clerk, _speech.Get("Fight"), "SPEECH_PARAMS_FORCE");
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

                // Block during surrender
                if (store.ClerkSurrender || store.ClerkSurrenderStage > 0)
                    return;

                // ⭐ NEW — Block if clerk is busy with ANY animation
                if (IsClerkBusy(clerk))
                    return;

                // ⭐ NEW — Block if player is aiming at clerk
                if (_ctx.Player.IsAiming())
                    return;

                // ⭐ NEW — Cooldown between alarm attempts
                if (store.LastSilentAlarmAttemptUtc > DateTime.UtcNow)
                    return;

                // Set next attempt window (8 seconds)
                store.LastSilentAlarmAttemptUtc = DateTime.UtcNow.AddSeconds(8);

                // ⭐ NEW — Lower chance dramatically (1–3%)
                int chance = store.ClerkRecognizedPlayer ? 3 : 1;
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
                        (int)(AnimationFlags.None | AnimationFlags.UpperBodyOnly),
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
                        (int)(AnimationFlags.None | AnimationFlags.UpperBodyOnly),
                        0f,
                        false, false, false
                    );
                }

                //// Trigger police response
                //Game.Player.WantedLevel = Math.Max(Game.Player.WantedLevel, 2);

                // Speech
                SafePlaySpeech(clerk, _speech.Get("SilentAlarm"), "SPEECH_PARAMS_FORCE");

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

                // Block during active clerk phases
                if (store.ClerkStalling || store.ClerkOpeningRegister || store.ClerkGrabbingCash || store.ClerkThrowingBag)
                    return;

                if (store.ClerkCallingPolice)
                    return;

                if (!store.ClerkReacted)
                    return;

                if (store.ClerkFleeing)
                    return;

                // Player still threatening → clerk does NOT call police
                if (PlayerThreatValid(store, clerk, player))
                    return;
                //if (_ctx.Player.IsThreatening(clerk))
                //    return;

                if (!store.IsRobberyActive)
                    return;

                // ⭐ Only call police if player LEFT the store
                if (!store.IsPlayerInsideStore)
                {
                    // ⭐ NEW — Cooldown between police call attempts
                    if (DateTime.UtcNow < _nextPoliceCallAttempt)
                        return;

                    _nextPoliceCallAttempt = DateTime.UtcNow.AddSeconds(10); // 10s cooldown
                    store.GreetedPlayer = false;

                    // ⭐ NEW — Lower chance dramatically (1–5%)
                    int chance = store.ClerkRecognizedPlayer ? 5 : 2;
                    if (_rng.Next(0, 100) >= chance)
                        return;

                    store.ClerkCallingPolice = true;
                    store.ClerkCallStartUtc = DateTime.UtcNow;

                    SafePlaySpeech(clerk, _speech.Get("SilentAlarm"), "SPEECH_PARAMS_FORCE");

                    //// ⭐ PATCH 8B — SAFE HEAT INCREMENT
                    //store.HeatLevel += 1;
                    //Game.Player.WantedLevel = Math.Max(Game.Player.WantedLevel, 2);

                    DebugLogger.Info($"Police called for robbery at store {store.Id}");
                    
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

                // KO / DEATH DETECTION

                // 1) NON-LETHAL KNOCKOUT — ONLY IF ALIVE
                if (clerkExists && !isDead && health > 0 && IsPedKnockedOut(clerk))
                {
                    store.ClerkKilledWithGun = false;
                    store.SilentRobbery = true;

                    // ⭐ ENSURE ROBBERY STATE IS ACTIVE
                    store.IsRobbed = true;
                    store.IsRobberyActive = true;
                    store.PendingCompletion = true;
                    if (store.RobberyStartUtc == DateTime.MinValue)
                        store.RobberyStartUtc = DateTime.UtcNow;

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
                    _ctx.SetRobberyActive(true);

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

                        // ⭐ ENSURE ROBBERY STATE IS ACTIVE
                        store.IsRobbed = true;
                        store.IsRobberyActive = true;
                        store.PendingCompletion = true;
                        if (store.RobberyStartUtc == DateTime.MinValue)
                            store.RobberyStartUtc = DateTime.UtcNow;

                        _ctx.Ui.TextNotification(
                            "DIA_POLICE",
                            "All Units Responding",
                            "LOS ANGELES PD",
                            "Reported armed robbery in progress, shots fired at " + store.Name
                        );

                        _ctx.Stalker.QueueGunKillMessage();

                        // Gun kill ALWAYS activates robbery
                        _ctx.SetRobberyActive(true);

                        DebugLogger.Info($"[GUN KILL] Clerk {clerk?.Handle} shot and killed at store {store.Id} / {store.Name}");
                    }
                    // 3) LETHAL KILL (MELEE)
                    else if (isDead && melee)
                    {
                        store.ClerkKilledWithGun = false;

                        // ⭐ ENSURE ROBBERY STATE IS ACTIVE
                        store.IsRobbed = true;
                        store.IsRobberyActive = true;
                        store.PendingCompletion = true;
                        if (store.RobberyStartUtc == DateTime.MinValue)
                            store.RobberyStartUtc = DateTime.UtcNow;

                        _ctx.Ui.TextNotification(
                            "DIA_POLICE",
                            "Robbery Reported",
                            "LOS ANGELES PD",
                            "Clerk found injured at " + store.Name
                        );

                        _ctx.Stalker.QueueMeleeKillMessage();

                        // ⭐ Ensure global robbery flag is active for StalkerSystem
                        _ctx.SetRobberyActive(true);

                        DebugLogger.Info($"[MELEE KILL] Clerk {clerk.Handle} killed via melee at store {store.Id} / {store.Name}");
                    }
                }

                // NEW SYSTEM CLEANUP
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

    }
}
