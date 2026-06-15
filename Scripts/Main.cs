using GTA;
using GTA.Native;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.UI;
using System;
using System.Reflection;

namespace StoreRobberyEnhanced
{
    public class Main : Script
    {
        private StoreContext _ctx;
        private bool _initialized;

        private DebugController _debug;

        private static string ScriptVersion =>
            Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public void ShowLoadedNotification()
        {
            try
            {
                if (_ctx.Config.EnableMessages)
                {
                    GTA.UI.Notification.PostTicker($"~b~Store Robbery Enhanced v{ScriptVersion}~w~ is now active.", true);

                    Script.Wait(2000); // Ensure the notification has time to display before logging

                    GTA.UI.Screen.ShowHelpText("Store Robbery Enhanced is loaded : ~n~We found (" + _ctx.Stores.Count.ToString() + ") Convinence stores across the map. ~n~You can disable this message in the MainSettings.ini located in scripts/StoreRobberyEnhanced", -1, true, false);
                }
                DebugLogger.Info($"Loaded notification shown (v{ScriptVersion})");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.ShowLoadedNotification", ex);
            }
        }

        public Main()
        {
            try
            {
                DebugLogger.Info("Main constructor called");

                Tick += WaitForGameLoad;
                Aborted += OnAborted;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // WAIT FOR GAME LOAD
        // ------------------------------------------------------------
        private void WaitForGameLoad(object sender, EventArgs e)
        {
            try
            {
                Ped player = Game.Player.Character;

                if (player == null || !player.Exists())
                    return;

                if (_initialized)
                    return;

                DebugLogger.Info("Game loaded — initializing mod");

                _initialized = true;
                Tick -= WaitForGameLoad;

                _ctx = new StoreContext(this);
                _ctx.Initialize();

                // ⭐ Initialize DebugLogger using INI setting
                DebugLogger.Initialize(_ctx.Config.EnableLogging);
                DebugEvents.Initialize(_ctx.Config.EnableEvents);
                DebugFileManager.Initialize(_ctx.Config.EnableFileManager);
                DebugProfiler.Initialize(_ctx.Config.EnableProfiler);

                // ⭐ DebugActions + DebugController now use the unified UI instance
                DebugActions.Init(StoreContext.GlobalUi, _ctx);
                _debug = new DebugController(this, StoreContext.GlobalUi, _ctx);

                _debug.ApplyKeybindConfig(
                    _ctx.Config.ModifierKey,
                    _ctx.Config.ToggleKey,
                    new System.Collections.Generic.Dictionary<int, string>
                    {
                        { _ctx.Config.Action_RobberyStart, "RobberyStart" },
                        { _ctx.Config.Action_SafeCrack, "SafeCrack" },
                        { _ctx.Config.Action_SafeCrackMini, "SafeCrackMini" },
                        { _ctx.Config.Action_CameraAlarm, "CameraAlarm" },
                        { _ctx.Config.Action_Escape, "Escape" },
                        { _ctx.Config.Action_Payout, "Payout" },
                        { _ctx.Config.Action_Cooldown, "Cooldown" },
                        { _ctx.Config.Action_Stalker, "Stalker" },
                        { _ctx.Config.Action_StalkerCall, "StalkerCall" },
                        { _ctx.Config.Action_UI, "UI" },
                        { _ctx.Config.Action_Banner, "Banner" },
                        { _ctx.Config.Action_Timer, "Timer" },
                        { _ctx.Config.Action_StoreDiag, "StoreDiag" },
                        { _ctx.Config.Action_MultiPos, "MultiPos" },
                        { _ctx.Config.Action_MiscActions, "MiscActions" },
                        { _ctx.Config.Scenario_FullRobbery, "ScenarioFullRobbery" },
                        { _ctx.Config.Scenario_QuickLoot, "ScnearioQuickLoor" },
                        { _ctx.Config.Action_CameraDebug, "CameraDebug" }
                    }
                );

                Tick += OnTick;

                DebugLogger.Info("Main initialization complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.WaitForGameLoad", ex);
            }
        }

        // ------------------------------------------------------------
        // MAIN TICK LOOP (BANNER-SAFE)
        // ------------------------------------------------------------
        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                // ============================================================
                // PLAYER DEATH / BUSTED ROBBERY RESET (SHVDN 3.9.0 SAFE)
                // ============================================================
                bool playerDead = Game.Player.Character.IsDead;

                // SHVDN 3.9.0-compatible busted detection
                bool playerBusted =
                    !playerDead &&
                    Game.Player.WantedLevel == 0 &&
                    !Game.Player.CanControlCharacter &&
                    Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true);

                if (playerDead || playerBusted)
                {
                    //DebugLogger.Info("[DeathReset] Player died/busted — clearing robbery state");

                    foreach (var store in _ctx.Stores)
                    {
                        if (store.IsRobberyActive || store.IsRobbed)
                        {
                            DebugLogger.Info($"[DeathReset] Resetting store {store.Id}");

                            // Hard stop all robbery state
                            store.IsRobberyActive = false;
                            store.IsRobbed = false;
                            store.RobberyEnded = true;
                            store.PendingCompletion = false;
                            store.PendingPayout = 0;
                            store.SafeCracked = false;

                            // Clear escalation + alarms
                            store.AlarmTriggered = false;
                            store.SilentRobbery = false;
                            store.ClerkCallingPolice = false;
                            store.ClerkReacted = false;
                            store.ClerkDeathHandled = false;
                            store.ClerkKilledWithGun = false;
                            store.ClerkSurrenderStage = 0;
                            store.HeatLevel = 0;

                            // DO NOT ENTER COOLDOWN ROBBERY FAILED, NOT SUCCESS
                            store.CooldownActive = false;
                            store.LastRobbedUtc = DateTime.MinValue;

                            // DO NOT APPLY COOLDOWN IF DEAD OR BUSTED AS ROBBERY IS A FAILURE, NOT A SUCCESS
                            //_ctx.Cooldowns.ApplyCooldownBlocker(store);
                            //_ctx.Cooldowns.UpdateStoreBlip(store);

                            if (_ctx.Stalker != null)
                            {
                                _ctx.Stalker.CleanupPhone();
                                _ctx.Stalker.ResetAfterDeath();
                            }

                            // Persist
                            _ctx.SaveStoreState(store);
                        }
                    }

                    // Clear wanted level
                    Game.Player.WantedLevel = 0;

                    // Reset SafeCrack suppression
                    if (_ctx.SafeCrack != null)
                        _ctx.SafeCrack.ResetState();

                    // Reset stalker system
                    if (_ctx.Stalker != null)
                        _ctx.Stalker.ResetAfterDeath();

                    DebugLogger.Info("[DeathReset] Global robbery state cleared");
                }

                // ============================================================
                // NORMAL GAME UPDATE
                // ============================================================
                if (_ctx != null)
                {
                    _ctx.Update();
                }

                // SafeCrack UI
                if (_ctx != null &&
                    _ctx.SafeState != null &&
                    _ctx.SafeState.Active)
                {
                    _ctx.SafeCrackUI.Draw(_ctx.SafeState, _ctx.SafeCrackSettings);
                }

                // -------------------------------
                // STALKER SYSTEM TICK INTEGRATION
                // -------------------------------
                if (_ctx.Stalker != null)
                {
                    _ctx.Stalker.ProcessEvents();
                    _ctx.Stalker.UpdatePhone();
                }

                // Debug overlays
                if (DebugState.OverlayVisible)
                    DebugOverlay.Draw(_ctx.Config);

                if (DebugState.OverlayVisible)
                    DebugStoreOverlay.Draw(_ctx);

                // Banner LAST
                StoreContext.GlobalUi.Draw();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.OnTick", ex);
            }
        }

        // ------------------------------------------------------------
        // CLEANUP ON ABORT
        // ------------------------------------------------------------
        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                DebugLogger.Info("Main.OnAborted called");

                if (_ctx != null)
                    _ctx.CleanupOnAbort();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("Main.OnAborted", ex);
            }
        }
    }
}
