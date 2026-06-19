using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using StoreRobberyEnhanced.Config;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Initialization;
using StoreRobberyEnhanced.Minigame;
using StoreRobberyEnhanced.Scripts.Systems;
using StoreRobberyEnhanced.Systems;
using StoreRobberyEnhanced.UI;
using System;
using System.Collections.Generic;
using System.IO;

namespace StoreRobberyEnhanced
{
    internal class StoreContext
    {
        private readonly Script _script;

        // ------------------------------------------------------------
        // CORE SYSTEMS
        // ------------------------------------------------------------
        internal IniConfig Config { get; private set; }
        internal UiHelpers Ui { get; private set; }
        public static UiHelpers GlobalUi => Active?.Ui;

        internal PlayerHelper Player { get; private set; }
        internal ClerkHelperSystem clerkHelper { get; private set; }
        public static StoreContext Active { get; private set; }

        public StoreContext()
        {
            Active = this;
        }

        // Backing field for global robbery state
        private bool _anyRobberyActive;
        // Allows other systems to toggle global robbery state safely
        public void SetRobberyActive(bool active)
        {
            _anyRobberyActive = active;
        }

        // ------------------------------------------------------------
        // GAMEPLAY SYSTEMS
        // ------------------------------------------------------------
        internal ClerkSystem Clerks { get; private set; }
        internal CameraSystem Cameras { get; private set; }
        internal RobberySystem Robberies { get; private set; }
        internal CooldownSystem Cooldowns { get; private set; }
        internal SafeSystem Safes { get; private set; }
        internal StalkerSystem Stalker { get; private set; }
        internal ClerkReplacementSystem ClerkReplacement { get; private set; }

        internal DebugScenarios Scenarios { get; private set; }
        internal BlipSystem Blips { get; private set; }
        internal PoliceSystem Police { get; private set; }
        internal ShopSystem Shops { get; private set; }

        internal ShopConsumeSystem ConsumeSystem { get; private set; }

        internal SafeCrackState SafeState { get; private set; }
        internal SafeCrackController SafeCrack { get; private set; }
        internal SafeCrackSettings SafeCrackSettings { get; private set; }
        internal ISafeCrackUI SafeCrackUI { get; private set; }

        internal bool DebugModeSafeCrack = false;

        // ------------------------------------------------------------
        // DATA
        // ------------------------------------------------------------
        internal List<TrackedStore> Stores { get; private set; }
        internal Dictionary<int, Blip> StoreBlips { get; private set; }

        // ------------------------------------------------------------
        // GLOBAL HEAT
        // ------------------------------------------------------------
        internal int GlobalHeatLevel;
        internal DateTime LastHeatUpdate;
        
        // ------------------------------------------------------------
        // RNG
        // ------------------------------------------------------------
        internal Random Rng { get; private set; }

        public ObjectPool MenuPool { get; } = new ObjectPool();

        public StoreContext(Script script)
        {
            try
            {
                _script = script;

                Active = this;

                Stores = new List<TrackedStore>();
                StoreBlips = new Dictionary<int, Blip>();

                Rng = new Random();
                LastHeatUpdate = DateTime.UtcNow;

                DebugLogger.Info("StoreContext constructed");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // ENSURE STORESTATE.INI EXISTS + AUTO‑MERGE NEW STORES
        // ------------------------------------------------------------
        private void EnsureStoreStatePopulated()
        {
            try
            {
                string path = Config.StoreStatePath;

                // Ensure file exists
                if (!File.Exists(path))
                    File.WriteAllText(path, "");

                SimpleIni ini = new SimpleIni(path);
                bool modified = false;

                foreach (TrackedStore store in Stores)
                {
                    string sec = "Store" + store.Id;

                    // ⭐ If section missing → create full default entry
                    if (!ini.SectionExists(sec))
                    {
                        DebugLogger.Info($"StoreState.ini missing section for store {store.Id} — adding it.");
                        Config.WriteDefaultStoreState(ini, store);
                        modified = true;
                        continue;
                    }

                    // ⭐ If section exists → ensure all required keys exist
                    if (Config.AddMissingStoreKeys(ini, store))
                    {
                        DebugLogger.Info($"StoreState.ini updated with missing keys for store {store.Id}");
                        modified = true;
                    }
                }

                if (modified)
                {
                    ini.Save();
                    DebugLogger.Info("StoreState.ini updated with new store entries.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.EnsureStoreStatePopulated", ex);
            }
        }

        // ------------------------------------------------------------
        // INITIALIZATION
        // ------------------------------------------------------------
        public void Initialize()
        {
            try
            {
                DebugLogger.Info("StoreContext.Initialize() starting");

                // 1. Load config
                Config = new IniConfig(_script);
                Config.LoadSettings();

                // 2. Core helpers
                Ui = new UiHelpers(Config);
                Player = new PlayerHelper(this);
                clerkHelper = new ClerkHelperSystem(this);

                // 3. Build stores
                StoreInitializer.BuildStores(this);

                // ⭐ FIX: Rebuild cameras AFTER IPLs and store state load
                Script.Wait(500);
                StoreInitializer.RebuildCamerasForAllStores(this);

                // 4. Ensure StoreState.ini exists and is populated
                EnsureStoreStatePopulated();

                // 5. Load store state
                foreach (TrackedStore store in Stores)
                    Config.LoadStoreState(store);

                // 6. Subsystems
                Clerks = new ClerkSystem(this);
                Cameras = new CameraSystem(this);
                Robberies = new RobberySystem(this);
                Cooldowns = new CooldownSystem(this);
                Stalker = new StalkerSystem(this);
                ClerkReplacement = new ClerkReplacementSystem(this);

                // ⭐ REQUIRED — this was missing
                Safes = new SafeSystem(this);

                // ⭐ Load SafeCrack settings
                SafeCrackSettings = SafeCrackConfigLoader.Load(Config);

                // ⭐ Initialize SafeCrack minigame
                SafeState = new SafeCrackState();

                // ⭐ Create UI instance and expose it
                SafeCrackUI = new SafeCrackUI();

                SafeCrack = new SafeCrackController(
                    SafeState,
                    SafeCrackSettings,
                    new SafeCrackLogic(),
                    new SafeCrackInput(),
                    SafeCrackUI,
                    new SafeCrackAnimation(),
                    Ui,
                    Robberies,
                    this,
                    Config
                );

                SafeCrackEvents.SafeCracked += (pos, payout) =>
                {
                    // var store = GetNearestStore();
                    var store = SafeCrack.CurrentStore;

                    if (store == null)
                        return;

                    store.SafeCracked = true;
                    store.PendingPayout += payout;

                    DebugLogger.Info($"[SafeCrack] Store {store.Id} safe cracked, added payout={payout}, totalPending={store.PendingPayout}");

                    // ⭐ Persist the safe state
                    if (!DebugModeSafeCrack)
                        SaveStoreState(store);
                };

                // ⭐ PHASE 3 — POLICE SYSTEM
                Police = new PoliceSystem(this);

                // ⭐ NEW — Shop systems
                Shops = new ShopSystem(this);
                ConsumeSystem = new ShopConsumeSystem(this);

                // ⭐ PATCH 3 — Initialize Debug Scenario System
                Scenarios = new DebugScenarios(Ui, this);

                // 7. Blip system
                Blips = new BlipSystem(this);
                Blips.Initialize();

                 // 8. Subsystem initialization
                Stalker.LoadFromIni();

                // 9. Apply cooldown state
                Cooldowns.ApplyInitialState();

                // 10. Notify player script is active
                if (_script is Main tracker)
                    tracker.ShowLoadedNotification();

                // 11. Validate store data
                ValidateStoreData();

                DebugLogger.Info("StoreContext.Initialize() complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.Initialize", ex);
            }
        }

        // ------------------------------------------------------------
        // MAIN UPDATE LOOP
        // ------------------------------------------------------------
        public void Update()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // ⭐ Clamp wanted level briefly after native clerk removal
                var now = DateTime.UtcNow;
                foreach (var store in Stores)
                {
                    if (store.NativeClerkRemovedRecently)
                    {
                        if ((now - store.NativeClerkRemovedUtc).TotalSeconds < 3)
                        {
                            // Kill any star spike caused by Rockstar shop logic
                            Game.Player.WantedLevel = 0;
                        }
                        else
                        {
                            store.NativeClerkRemovedRecently = false;
                        }
                    }
                }

                // ------------------------------------------------------------
                // ⭐ WANTED LEVEL SUPPRESSION WINDOW (Escape / Cleanup)
                // ------------------------------------------------------------
                bool anySuppressionActive = false;

                foreach (var store in Stores)
                {
                    if (store.WantedSuppressionEndUtc > now)
                    {
                        anySuppressionActive = true;
                        break;
                    }
                }

                if (anySuppressionActive)
                {
                    // Hard‑force NO wanted while any store is in suppression window
                    Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
                    Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
                    Game.Player.WantedLevel = 0;
                }
                else
                {
                    // Restore normal behavior when no store is in suppression
                    Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                    Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
                }

                // ⭐ Update blips first
                Blips.Update();

                // Global systems
                Stalker.ProcessEvents();
                Cooldowns.UpdateGlobalHeat();

                // Per-store systems
                int count = Stores.Count;
                for (int i = 0; i < count; i++)
                {
                    TrackedStore store = Stores[i];

                    // 0) Update inside-store state (consistent for all systems)
                    store.IsPlayerInsideStore =
                        player.Position.DistanceTo(store.StorePos) <= store.Radius;

                    // 1) Cooldown / availability
                    Cooldowns.UpdateStoreCooldown(store, player);

                    // 2) Clerk replacement: neutralize natives + spawn dummy + ensure our clerk
                    ClerkReplacement.UpdateForStore(store, player);

                    // 3) Robbery logic
                    Robberies.UpdateRobbery(store, player);

                    // 4) Cameras (safe now that clerks are neutralized / dummy present)
                    Cameras.UpdateStoreCameras(this, store);

                    // 5) Our clerk behavior
                    DebugState.LastStore = store;
                    Clerks.UpdateClerk(store, player);
                    
                    // 6) Police / heat per store
                    Police.UpdatePoliceLogic(store, player);
                }

                // ⭐ Shop menu + interaction system
                Shops.Tick();

                // ⭐ Shop item consumption system
                ConsumeSystem.Tick();

                // ⭐ SafeCrack logic tick (UI is drawn in Main.OnFrameRender)
                if (SafeState.Active)
                    SafeCrack.Update();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.Update", ex);
            }
        }

        // ------------------------------------------------------------
        // SAVE STORE STATE
        // ------------------------------------------------------------
        public void SaveStoreState(TrackedStore store)
        {
            try
            {
                Config.SaveStoreState(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.SaveStoreState", ex);
            }
        }

        // ------------------------------------------------------------
        // CLEANUP ON ABORT
        // ------------------------------------------------------------
        public void CleanupOnAbort()
        {
            try
            {
                DebugLogger.Info("StoreContext.CleanupOnAbort()");

                if (Blips != null)
                    Blips.Cleanup();

                if (Cameras != null)
                    Cameras.CleanupBlipsAndProps();

                int count = Stores.Count;
                for (int i = 0; i < count; i++)
                {
                    TrackedStore store = Stores[i];

                    if (store.Clerk != null && store.Clerk.Exists())
                        store.Clerk.Delete();

                    // ⭐ NEW — cleanup dummy clerk as well
                    if (store.DummyClerk != null && store.DummyClerk.Exists())
                        store.DummyClerk.Delete();

                    if (store.CooldownBlocker != null && store.CooldownBlocker.Exists())
                        store.CooldownBlocker.Delete();
                }

                // Abort SafeCrack if running
                if (SafeState != null && SafeState.Active)
                    SafeCrack.Abort();

            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.CleanupOnAbort", ex);
            }
        }

        // ------------------------------------------------------------
        // GET NEAREST STORE
        // ------------------------------------------------------------
        public TrackedStore GetNearestStore(float maxDistance = 100f)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return null;

                Vector3 pos = player.Position;

                TrackedStore nearest = null;
                float bestDist = maxDistance;

                int count = Stores.Count;
                for (int i = 0; i < count; i++)
                {
                    TrackedStore store = Stores[i];
                    if (store == null)
                        continue;

                    float dist = pos.DistanceTo(store.StorePos);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        nearest = store;
                    }
                }

                return nearest;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.GetNearestStore", ex);
                return null;
            }
        }

        // ------------------------------------------------------------
        // VALIDATE STORE DATA
        // ------------------------------------------------------------
        private void ValidateStoreData()
        {
            try
            {
                DebugLogger.Info("Validating store data...");

                foreach (var s in Stores)
                {
                    if (s == null)
                    {
                        DebugLogger.Info("Null store entry detected");
                        Ui.ShowNotification("~r~Null store entry detected");
                        continue;
                    }

                    // Required fields
                    if (s.ClerkPos == Vector3.Zero)
                        Ui.ShowNotification($"~r~{s.Name}: Missing ClerkPos");

                    if (s.RegisterPos == Vector3.Zero)
                        Ui.ShowNotification($"~r~{s.Name}: Missing RegisterPos");

                    if (s.SafePos == Vector3.Zero)
                        Ui.ShowNotification($"~r~{s.Name}: Missing SafePos");

                    if (s.DoorPos == Vector3.Zero)
                        Ui.ShowNotification($"~r~{s.Name}: Missing DoorPos");

                    if (s.DoorModelHash == 0)
                        Ui.ShowNotification($"~r~{s.Name}: Invalid DoorModelHash");

                    if (s.Cameras == null || s.Cameras.Count == 0)
                        Ui.ShowNotification($"~r~{s.Name}: No cameras defined");

                    if (s.Radius <= 0)
                        Ui.ShowNotification($"~r~{s.Name}: Invalid radius");
                }

                DebugLogger.Info("Store data validation complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StoreContext.ValidateStoreData", ex);
            }
        }

        // ------------------------------------------------------------
        // GLOBAL ROBBERY STATE
        // ------------------------------------------------------------
        public bool AnyRobberyActive
        {
            get
            {
                try
                {
                    // If any store is active OR global flag is set
                    foreach (var s in Stores)
                        if (s.IsRobberyActive)
                            return true;
                    return _anyRobberyActive;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("StoreContext.AnyRobberyActive", ex);
                    return false;
                }
            }
        }

    }
}
