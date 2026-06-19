using System;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Systems
{
    internal class CooldownSystem
    {
        private readonly StoreContext _ctx;
        private readonly ClerkSystem _clerks;
        private int _lastSubtitleTime = 0;
        public CooldownSystem(StoreContext ctx)
        {
            _ctx = ctx;
        }

        // ------------------------------------------------------------
        // DEBUG FORCE COOLDOWN
        // ------------------------------------------------------------        
        public void DebugForceCooldown()
        {
            try
            {
                DebugLogger.Info("DebugForceCooldown() called");

                var store = _ctx.GetNearestStore();
                if (store == null)
                {
                    _ctx.Ui.ShowNotification("~r~No store nearby");
                    return;
                }

                DebugLogger.Info($"Forcing 30s debug cooldown on store {store.Id}");

                // Simulate completed robbery state
                store.IsRobbed = false;
                store.SafeCracked = true;
                store.PendingCompletion = false;
                store.PendingPayout = 0;

                // Activate cooldown
                store.CooldownActive = true;
                store.LastRobbedUtc = DateTime.UtcNow;

                // Apply blocker + blip
                ApplyCooldownBlocker(store);

                if (_ctx.Config.EnableBlips)
                    UpdateStoreBlip(store);

                _ctx.SaveStoreState(store);

                _ctx.Ui.ShowNotification("~c~Cooldown forced (debug, 30s)");

                // ⭐ NEW: mark debug cooldown end time
                store.DebugCooldownEndUtc = DateTime.UtcNow.AddSeconds(30);

                DebugLogger.Info($"Debug cooldown active until {store.DebugCooldownEndUtc}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.DebugForceCooldown", ex);
            }
        }

        // ------------------------------------------------------------
        // INITIAL STATE
        // ------------------------------------------------------------
        public void ApplyInitialState()
        {
            try
            {
                DebugLogger.Info("ApplyInitialState() starting");

                int count = _ctx.Stores.Count;
                for (int i = 0; i < count; i++)
                {
                    TrackedStore store = _ctx.Stores[i];

                    bool inCooldown = IsStoreInCooldown(store);
                    store.CooldownActive = inCooldown;

                    DebugLogger.Trace($"Store {store.Id}: initial cooldown = {inCooldown}");

                    if (inCooldown)
                        ApplyCooldownBlocker(store);
                    else
                        RemoveCooldownBlocker(store);

                    RegisterDoor(store);
                }

                DebugLogger.Info("ApplyInitialState() complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.ApplyInitialState", ex);
            }
        }

        // ------------------------------------------------------------
        // PER-STORE UPDATE
        // ------------------------------------------------------------
        public void UpdateStoreCooldown(TrackedStore store, Ped player)
        {
            try
            {
                // ⭐ DEBUG COOLDOWN HANDLING
                if (store.DebugCooldownEndUtc != DateTime.MinValue)
                {
                    if (DateTime.UtcNow >= store.DebugCooldownEndUtc)
                    {
                        DebugLogger.Info($"Debug cooldown expired for store {store.Id}");
                        store.DebugCooldownEndUtc = DateTime.MinValue;

                        RemoveCooldownBlocker(store);
                        ResetStore(store);
                        _ctx.Ui.ShowNotification("~g~Debug cooldown expired — store reset");
                    }
                    else
                    {
                        // Show lockout subtitle just like real cooldown
                        EnforceCooldownLockout(store, player);
                    }

                    return;
                }

                DebugLogger.Trace($"UpdateStoreCooldown({store.Id})");

                if (store.CooldownActive && !IsStoreInCooldown(store))
                {
                    DebugLogger.Info($"Cooldown expired for store {store.Id}, resetting");
                    ResetStore(store);
                    _ctx.Ui.ShowNotification("~g~Store reset.");
                }

                if (store.CooldownActive)
                {
                    DebugLogger.Trace($"Store {store.Id} still cooling down → enforcing lockout");
                    EnforceCooldownLockout(store, player);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.UpdateStoreCooldown", ex);
            }
        }

        // ------------------------------------------------------------
        // COOLDOWN CHECK
        // ------------------------------------------------------------
        public bool IsStoreInCooldown(TrackedStore store)
        {
            try
            {
                if (store.LastRobbedUtc == DateTime.MinValue)
                    return false;

                TimeSpan span = TimeSpan.FromMinutes(_ctx.Config.CooldownMinutes);
                bool result = (DateTime.UtcNow - store.LastRobbedUtc) < span;

                DebugLogger.Trace($"IsStoreInCooldown({store.Id}) = {result}");
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.IsStoreInCooldown", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // BLOCKER PROP
        // ------------------------------------------------------------
        public void ApplyCooldownBlocker(TrackedStore store)
        {
            DebugLogger.Trace($"ApplyCooldownBlocker({store.Id})");

            if (store.CooldownBlocker != null && store.CooldownBlocker.Exists())
            {
                DebugLogger.Trace($"Store {store.Id} already has blocker");
                return;
            }

            try
            {
                int blockerHash = Function.Call<int>(Hash.GET_HASH_KEY, "prop_barrier_work05");
                Function.Call(Hash.REQUEST_MODEL, blockerHash);

                DateTime start = DateTime.UtcNow;
                while (!Function.Call<bool>(Hash.HAS_MODEL_LOADED, blockerHash) &&
                       (DateTime.UtcNow - start).TotalSeconds < 3)
                {
                    Script.Wait(0);
                }

                Vector3 spawnPos = store.DoorPos + (store.DoorForward * 0.25f);

                store.CooldownBlocker = World.CreateProp(
                    blockerHash,
                    spawnPos,
                    true,   // dynamic = true
                    true    // placeOnGround = true
                );

                if (store.CooldownBlocker != null && store.CooldownBlocker.Exists())
                {
                    store.CooldownBlocker.IsPositionFrozen = true;
                    store.CooldownBlocker.IsCollisionEnabled = true;
                    store.CooldownBlocker.IsVisible = false;

                    // 🔧 Align with door heading
                    store.CooldownBlocker.Heading = store.DoorHeading;

                    Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, store.CooldownBlocker.Handle, true, true);

                    DebugLogger.Info($"Cooldown blocker created for store {store.Id}");
                }
                else
                {
                    DebugLogger.Info($"Cooldown blocker FAILED for store {store.Id}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.ApplyCooldownBlocker", ex);
            }
        }

        // ------------------------------------------------------------
        // BLOCKER REMOVAL
        // ------------------------------------------------------------
        public void RemoveCooldownBlocker(TrackedStore store)
        {
            DebugLogger.Trace($"RemoveCooldownBlocker({store.Id})");

            try
            {
                if (store.CooldownBlocker != null && store.CooldownBlocker.Exists())
                {
                    store.CooldownBlocker.Delete();
                    DebugLogger.Info($"Cooldown blocker removed for store {store.Id}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.RemoveCooldownBlocker", ex);
            }

            store.CooldownBlocker = null;
        }

        // ------------------------------------------------------------
        // DOOR SYSTEM
        // ------------------------------------------------------------
        public void RegisterDoor(TrackedStore store)
        {
            try
            {
                DebugLogger.Trace($"RegisterDoor({store.Id})");

                if (store.DoorModelHash == 0)
                {
                    DebugLogger.Trace($"Store {store.Id} has no door model");
                    return;
                }

                if (store.DoorSystemId == 0)
                {
                    store.DoorSystemId = store.Id + 10000;

                    DebugLogger.Info($"Adding door to system: ID={store.DoorSystemId}");

                    Function.Call(Hash.ADD_DOOR_TO_SYSTEM,
                        store.DoorSystemId,
                        store.DoorModelHash,
                        store.DoorPos.X,
                        store.DoorPos.Y,
                        store.DoorPos.Z,
                        false,
                        false,
                        false
                    );
                }

                int state = store.CooldownActive ? 1 : 0;

                Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE,
                    store.DoorSystemId,
                    state,
                    false,
                    false
                );

                DebugLogger.Trace($"Door state for store {store.Id} set to {state}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.RegisterDoor", ex);
            }
        }

        // ------------------------------------------------------------
        // LOCKOUT ENFORCEMENT
        // ------------------------------------------------------------
        private void EnforceCooldownLockout(TrackedStore store, Ped player)
        {
            try
            {
                float dist = player.Position.DistanceTo(store.StorePos);
                DebugLogger.Trace($"EnforceCooldownLockout({store.Id}) dist={dist}");

                if (dist < 35f)
                {
                    if (Game.GameTime - _lastSubtitleTime > 1000)
                    {
                        _ctx.Ui.ShowSubtitle("Store closed. Recently robbed.", 3000);
                        _lastSubtitleTime = Game.GameTime;
                    }

                    if (store.CooldownBlocker == null || !store.CooldownBlocker.Exists())
                    {
                        DebugLogger.Info($"Cooldown blocker missing for store {store.Id}, reapplying");
                        ApplyCooldownBlocker(store);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.EnforceCooldownLockout", ex);
            }
        }

        // ------------------------------------------------------------
        // RESET STORE
        // ------------------------------------------------------------
        private void ResetStore(TrackedStore store)
        {
            try
            {
                DebugLogger.Info($"ResetStore({store.Id})");

                store.IsRobbed = false;
                store.SafeCracked = false;
                store.HeatLevel = 0;
                store.CooldownActive = false;
                store.PendingPayout = 0;
                store.PendingCompletion = false;
                store.AlarmTriggered = false;
                store.SilentRobbery = false;
                store.ClerkKilledWithGun = false;
                store.ClerkDeathHandled = false;
                store.ClerkReacted = false;
                store.ClerkStalling = false;
                store.StallStartUtc = DateTime.MinValue;
                store.StallDurationMs = 0;
                store.ClerkSurrenderStage = 0;
                store.IsRobberyActive = false;
                _ctx.SetRobberyActive(false);

                // ⭐ DO NOT DELETE THE CLERK HERE
                // Let ClerkReplacementSystem handle cleanup safely.
                if (store.Clerk != null && store.Clerk.Exists())
                {
                    DebugLogger.Info($"[ResetStore] Marking clerk for delayed cleanup at store {store.Id}");
                    // Leave the ped in the world — replacement system will delete it.
                }

                RemoveCooldownBlocker(store);

                if (_ctx.Config.EnableBlips)
                    UpdateStoreBlip(store);

                _ctx.SaveStoreState(store);

                DebugLogger.Info($"Store {store.Id} reset complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.ResetStore", ex);
            }
        }

        // ------------------------------------------------------------
        // BLIP UPDATE
        // ------------------------------------------------------------
        public void UpdateStoreBlip(TrackedStore store)
        {
            try
            {
                DebugLogger.Trace($"UpdateStoreBlip({store.Id})");

                if (!_ctx.StoreBlips.TryGetValue(store.Id, out Blip blip))
                {
                    DebugLogger.Info($"Store {store.Id} has no blip entry");
                    return;
                }

                if (blip == null || !blip.Exists())
                {
                    DebugLogger.Info($"Store {store.Id} blip missing or deleted");
                    return;
                }

                _ctx.Blips.RefreshBlip(store.Id);

                DebugLogger.Trace($"Blip refreshed for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.UpdateStoreBlip", ex);
            }
        }

        // ------------------------------------------------------------
        // GLOBAL HEAT DECAY
        // ------------------------------------------------------------
        public void UpdateGlobalHeat()
        {
            try
            {
                if ((DateTime.UtcNow - _ctx.LastHeatUpdate).TotalMinutes > 5)
                {
                    DebugLogger.Trace("Global heat decay tick");

                    if (_ctx.GlobalHeatLevel > 0)
                        _ctx.GlobalHeatLevel--;

                    _ctx.LastHeatUpdate = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("CooldownSystem.UpdateGlobalHeat", ex);
            }
        }
    }
}
