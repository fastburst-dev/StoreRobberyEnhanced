using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Systems;
using StoreRobberyEnhanced.UI;
using System;

namespace StoreRobberyEnhanced.Scripts.Systems
{
    internal class ClerkHelperSystem
    {
        private readonly StoreContext _ctx;
        private readonly Random _rng;
        private readonly ClerkSystem _clerks;
        private DateTime _nextPoliceCallAttempt = DateTime.MinValue;

        public ClerkHelperSystem(StoreContext ctx)
        {
            _ctx = ctx;
            _rng = new Random();
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
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "random@arrests", "idle_2_hands_up", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "rcmme_tracey1", "nervous_loop", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "oddjobs@shop_robbery@rob_till", "enter", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "oddjobs@shop_robbery@rob_till", "loop", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "anim@heists@ornate_bank@grab_cash", "idle", 3) ||
                   Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, clerk.Handle, "mp_am_hold_up", "purchase_beer_shopkeeper", 3);
        }

        // ------------------------------------------------------------
        // PATCH G — Clerk Position Validation
        // ------------------------------------------------------------
        public bool IsClerkAtRegister(TrackedStore store, Ped clerk, float tolerance = 4.25f)
        {
            if (store == null || clerk == null || !clerk.Exists())
                return false;

            float dist = clerk.Position.DistanceTo(store.RegisterPos);
            return dist <= tolerance;
        }

        // ------------------------------------------------------------
        // PATCH I — Unified Player Threat Validation (Silent + Loud)
        // NULL-SAFE + PATCH U COMPATIBLE + NO DUPLICATE LOGIC
        // ------------------------------------------------------------
        public bool PlayerThreatValid(TrackedStore store, Ped clerk, Ped player)
        {
            try
            {
                // ⭐ Absolute safety
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
                    bool los = false;
                    try { los = ph.IsInLOS(clerk); }
                    catch { return false; }

                    if (!los)
                        return false;
                }

                // ------------------------------------------------------------
                // 3. WEAPON + THREAT CHECK
                // ------------------------------------------------------------
                Weapon current = null;
                try { current = player.Weapons?.Current; }
                catch { current = null; }

                bool hasWeapon = current != null && current.Hash != WeaponHash.Unarmed;

                bool isMelee = false;
                bool isGun = false;

                if (hasWeapon)
                {
                    try { isMelee = ph.IsMeleeWeapon(current.Hash); }
                    catch { isMelee = false; }

                    isGun = !isMelee;
                }

                bool isAiming = false;
                try { isAiming = ph.IsAiming(); }
                catch { isAiming = false; }

                // ------------------------------------------------------------
                // 4. SILENT ROBBERY LOGIC
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
                    bool masked = false;
                    try { masked = ph.IsMasked(); }
                    catch { masked = false; }

                    if (!masked)
                        return false;

                    // Must stay in front arc of clerk
                    Vector3 toPlayer = (player.Position - clerk.Position).Normalized;
                    float dot = Vector3.Dot(clerk.ForwardVector, toPlayer);

                    if (dot < 0.0f)
                        return false;

                    return true;
                }

                // ------------------------------------------------------------
                // 5. LOUD ROBBERY LOGIC
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
            catch
            {
                // ⭐ Fail-safe: never crash
                return false;
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

                _clerks. ClearAllClerkPhases(store);
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
                _clerks.ClearAllClerkPhases(store);
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
    }
}
