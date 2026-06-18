using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Config;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Systems
{
    internal class BlipSystem
    {
        private readonly StoreContext _ctx;

        public BlipSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                DebugLogger.Info("BlipSystem initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("BlipSystem.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // INITIAL CREATION
        // ------------------------------------------------------------
        public void Initialize()
        {
            try
            {
                if (_ctx.Config.EnableBlips)
                {   
                    DebugLogger.Info("BlipSystem.Initialize()");
                    CreateStoreBlips();
                }
                else
                {
                    DebugLogger.Info("Blips are disabled in config, skipping blip creation");
                }
                
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("BlipSystem.Initialize", ex);
            }
        }

        // ------------------------------------------------------------
        // UPDATE LOOP
        // ------------------------------------------------------------
        public void Update()
        {
            try
            {
                if(_ctx.Config.EnableBlips)
                    UpdateBlipStates();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("BlipSystem.Update", ex);
            }
        }

        // ------------------------------------------------------------
        // CREATE BLIPS FOR ALL STORES
        // ------------------------------------------------------------
        private void CreateStoreBlips()
        {
            try
            {
                DebugLogger.Info("Creating store blips");

                foreach (TrackedStore store in _ctx.Stores)
                {
                    try
                    {
                        if (_ctx.StoreBlips.ContainsKey(store.Id))
                        {
                            Blip old = _ctx.StoreBlips[store.Id];
                            if (old != null && old.Exists())
                                old.Delete();

                            _ctx.StoreBlips.Remove(store.Id);
                        }

                        Blip blip = World.CreateBlip(store.StorePos);
                        if (blip == null)
                        {
                            DebugLogger.Trace($"Failed to create blip for store {store.Id}");
                            continue;
                        }

                        blip.Sprite = (BlipSprite)52;
                        blip.Color = BlipColor.White;
                        blip.Name = _ctx.Config.UseStoreNames ? store.Name : "Convenience Store";

                        Function.Call((Hash)0x234CDD44D996FD9A, blip, 1.0f);
                        Function.Call((Hash)0xBE8BE4FE60E27B72, blip, true);
                        Function.Call((Hash)0x9029B2F3DA924928, blip, 2);

                        bool robbedOrCooldown = store.IsRobbed || store.CooldownActive;
                        Function.Call((Hash)0x74513EA3E505181E, blip, robbedOrCooldown);

                        _ctx.StoreBlips[store.Id] = blip;

                        DebugLogger.Trace($"Created blip for store {store.Id}");
                    }
                    catch (Exception exStore)
                    {
                        DebugLogger.LogException($"BlipSystem.CreateStoreBlips(store={store.Id})", exStore);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("BlipSystem.CreateStoreBlips", ex);
            }
        }

        // ------------------------------------------------------------
        // UPDATE BLIP CHECKMARKS
        // ------------------------------------------------------------
        private void UpdateBlipStates()
        {
            try
            {
                foreach (TrackedStore store in _ctx.Stores)
                {
                    try
                    {
                        if (!_ctx.StoreBlips.ContainsKey(store.Id))
                            continue;

                        Blip blip = _ctx.StoreBlips[store.Id];
                        if (blip == null || !blip.Exists())
                            continue;

                        bool robbedOrCooldown = store.IsRobbed || store.CooldownActive;

                        Function.Call((Hash)0x74513EA3E505181E, blip, robbedOrCooldown);
                        blip.Color = BlipColor.White;
                    }
                    catch (Exception exStore)
                    {
                        DebugLogger.LogException($"BlipSystem.UpdateBlipStates(store={store.Id})", exStore);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("BlipSystem.UpdateBlipStates", ex);
            }
        }        

        // ------------------------------------------------------------
        // CLEANUP
        // ------------------------------------------------------------
        public void Cleanup()
        {
            try
            {
                DebugLogger.Info("Cleaning up store blips");

                foreach (Blip blip in _ctx.StoreBlips.Values)
                {
                    try
                    {
                        if (blip != null && blip.Exists())
                            blip.Delete();
                    }
                    catch (Exception exBlip)
                    {
                        DebugLogger.LogException("BlipSystem.Cleanup(blip)", exBlip);
                    }
                }

                _ctx.StoreBlips.Clear();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("BlipSystem.Cleanup", ex);
            }
        }

        // ------------------------------------------------------------
        // INTERNAL REFRESH
        // ------------------------------------------------------------
        internal void RefreshBlip(int storeId)
        {
            try
            {
                if (!_ctx.StoreBlips.ContainsKey(storeId))
                    return;

                Blip blip = _ctx.StoreBlips[storeId];
                if (blip == null || !blip.Exists())
                    return;

                TrackedStore store = _ctx.Stores[storeId];

                bool robbedOrCooldown = store.IsRobbed || store.CooldownActive;

                Function.Call((Hash)0x74513EA3E505181E, blip, robbedOrCooldown);
                blip.Color = BlipColor.White;

                DebugLogger.Trace($"Refreshed blip for store {storeId}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("BlipSystem.RefreshBlip", ex);
            }
        }
    }
}
