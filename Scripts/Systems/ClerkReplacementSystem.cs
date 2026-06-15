using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;
using System.Runtime.ConstrainedExecution;

namespace StoreRobberyEnhanced.Systems
{
    internal class ClerkReplacementSystem
    {
        private readonly StoreContext _ctx;
        private readonly TimeSpan _sweepInterval = TimeSpan.FromSeconds(3);

        // All Rockstar clerk models
        private readonly int[] _defaultClerkModels =
        {
            (int)PedHash.ShopKeep01,
            (int)PedHash.ShopMaskSMY,
            Function.Call<int>(Hash.GET_HASH_KEY, "mp_m_shopkeep_01"),
            Function.Call<int>(Hash.GET_HASH_KEY, "s_m_m_shopkeep_01")
        };

        public ClerkReplacementSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                DebugLogger.Info("ClerkReplacementSystem initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.ctor", ex);
            }
        }

        // Called every tick for the store update loop
        public void UpdateForStore(TrackedStore store, Ped player)
        {
            if (store == null)
                return;

            // Only care if player is near this store
            float dist = player.Position.DistanceTo(store.StorePos);
            if (dist > store.Radius + 10f)
                return;

            // Ensure replacement when near (Option 1)
            EnsureDefaultClerkRemoved(store);

            // Periodic sweep to prevent respawns (Option 3)
            if (DateTime.UtcNow - store.LastClerkSweepUtc >= _sweepInterval)
            {
                store.LastClerkSweepUtc = DateTime.UtcNow;
                EnsureDefaultClerkRemoved(store);
            }
        }

        // Main method to ensure default clerk is removed and replaced with our own
        private void EnsureDefaultClerkRemoved(TrackedStore store)
        {
            try
            {
                // ⭐ HARD STOP: never spawn a clerk during an active robbery
                if (store.IsRobberyActive)
                {
                    return;
                }

                // ⭐ HARD STOP: never spawn a clerk after death
                if (store.ClerkDeathHandledCheck)
                {
                    return;
                }

                // ⭐ Suppress wanted level only during replacement
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
                Game.Player.WantedLevel = 0;
                _ctx.Police.SuppressPoliceForDebug = true;

                // If our clerk already exists, just keep the area clean
                if (store.Clerk != null && store.Clerk.Exists())
                {
                    RemoveNearbyDefaultClerks(store, store.Clerk);
                    store.DefaultClerkRemoved = true;
                    return;
                }

                // First time: remove default clerk, then spawn ours
                RemoveNearbyDefaultClerks(store, null);

                _ctx.Clerks.ForceSpawnClerk(store);

                if (store.Clerk != null && store.Clerk.Exists())
                    store.DefaultClerkRemoved = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ClerkReplacementSystem.EnsureDefaultClerkRemoved", ex);
            }
            finally
            {
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
                _ctx.Police.SuppressPoliceForDebug = false;
            }
        }

        // Remove nearby default clerks, except for the provided ped to skip (can be null)
        private void RemoveNearbyDefaultClerks(TrackedStore store, Ped skip)
        {
            Vector3 pos = store.ClerkPos;
            float radius = 3.0f;

            Ped[] nearby = World.GetNearbyPeds(pos, radius);
            if (nearby == null || nearby.Length == 0)
                return;

            foreach (Ped ped in nearby)
            {
                if (ped == null || !ped.Exists())
                    continue;

                // Skip our real clerk
                if (store.Clerk != null && ped.Handle == store.Clerk.Handle)
                    continue;

                // Skip dummy clerk
                if (store.DummyClerk != null && ped.Handle == store.DummyClerk.Handle)
                    continue;

                // Skip explicitly provided ped
                if (skip != null && ped.Handle == skip.Handle)
                    continue;

                // Only treat Rockstar clerks as default clerks
                if (Array.IndexOf(_defaultClerkModels, ped.Model.Hash) != -1)
                {
                    // Neutralize default clerk safely
                    ped.Task.ClearAllImmediately();
                    ped.BlockPermanentEvents = true;
                    ped.IsInvincible = true;
                    ped.CanBeTargetted = false;

                    ped.Position = new Vector3(0f, 0f, 50f);
                    ped.IsPersistent = false;
                    ped.MarkAsNoLongerNeeded();

                    store.NativeClerkRemovedRecently = true;
                    store.NativeClerkRemovedUtc = DateTime.UtcNow;
                }
            }
        }
    }
}
