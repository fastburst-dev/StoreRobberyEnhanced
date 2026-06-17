using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Systems;
using StoreRobberyEnhanced.UI;
using System;
using static StoreRobberyEnhanced.Systems.ClerkSystem;

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

        public bool IsValidPhaseTransition(ClerkPhase from, ClerkPhase to)
        {
            return (from, to) switch
            {
                (ClerkPhase.Stall, ClerkPhase.RegisterOpening) => true,
                (ClerkPhase.RegisterOpening, ClerkPhase.CashGrab) => true,
                (ClerkPhase.CashGrab, ClerkPhase.BagToss) => true,
                (ClerkPhase.BagToss, ClerkPhase.Flee) => true,
                (ClerkPhase.Flee, ClerkPhase.Surrender) => true,

                // Allow staying in same phase
                (var a, var b) when a == b => true,

                _ => false
            };
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
            if (!_clerks.IsPlayingAnim(clerk, animDict, animName))
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
        
    }
}
