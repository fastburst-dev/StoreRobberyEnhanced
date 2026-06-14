using GTA;
using GTA.Math;
using StoreRobberyEnhanced.Systems;
using StoreRobberyEnhanced.UI;
using System;

namespace StoreRobberyEnhanced.Debug
{
    internal static class DebugActions
    {
        private static StoreContext _ctx;
        public static bool IsReady => StoreContext.GlobalUi != null && _ctx != null;

        public static void Init(UiHelpers ui, StoreContext ctx)
        {
            // ui parameter ignored — we always use StoreContext.GlobalUi
            _ctx = ctx;
        }

        // ------------------------------------------------------------
        // ROBBERY START
        // ------------------------------------------------------------
        public static void TriggerRobberyStart()
        {
            if (_ctx.Robberies.TryStartDebugRobbery(out string msg))
                StoreContext.GlobalUi.ShowNotification("~g~RobberyStart OK~s~: " + msg);
            else
                StoreContext.GlobalUi.ShowNotification("~r~RobberyStart FAILED~s~: " + msg);
        }

        // ------------------------------------------------------------
        // SAFE CRACK (INSTANT)
        // ------------------------------------------------------------
        public static void TriggerSafeCrack()
        {
            if (_ctx.Safes.DebugForceSafeCrack(out string msg))
                StoreContext.GlobalUi.ShowNotification("~g~SafeCrack OK~s~: " + msg);
            else
                StoreContext.GlobalUi.ShowNotification("~r~SafeCrack FAILED~s~: " + msg);
        }

        // ------------------------------------------------------------
        // SAFE CRACK MINIGAME
        // ------------------------------------------------------------
        public static void TriggerSafeCrackMini()
        {
            if (_ctx.SafeCrack == null || _ctx.SafeState == null)
            {
                StoreContext.GlobalUi.ShowNotification("~r~SafeCrack system not initialized yet");
                return;
            }

            if (_ctx.Safes.DebugStartSafeCrack(out string msg))
                StoreContext.GlobalUi.ShowNotification("~g~SafeCrack Minigame OK~s~: " + msg);
            else
                StoreContext.GlobalUi.ShowNotification("~r~SafeCrack Minigame FAILED~s~: " + msg);
        }

        // ------------------------------------------------------------
        // CAMERA ALARM
        // ------------------------------------------------------------
        public static void TriggerCameraAlarm()
        {
            _ctx.Cameras.DebugTriggerAlarm();
            StoreContext.GlobalUi.ShowNotification("~y~Camera Alarm Triggered");
        }

        // ------------------------------------------------------------
        // ESCAPE
        // ------------------------------------------------------------
        public static void TriggerEscape()
        {
            _ctx.Robberies.DebugForceEscape();
            StoreContext.GlobalUi.ShowNotification("~b~Escape Triggered");
        }

        // ------------------------------------------------------------
        // PAYOUT
        // ------------------------------------------------------------
        public static void TriggerPayout()
        {
            int amount = _ctx.Robberies.DebugForcePayout();
            StoreContext.GlobalUi.ShowNotification($"~g~Payout Triggered: ~w~${amount}");
        }

        // ------------------------------------------------------------
        // COOLDOWN
        // ------------------------------------------------------------
        public static void TriggerCooldown()
        {
            var s = _ctx.GetNearestStore();

            if (s == null)
            {
                StoreContext.GlobalUi.ShowNotification("~r~No store found near player");
                return;
            }

            StoreContext.GlobalUi.ShowNotification($"DoorPos: {s.DoorPos}");

            _ctx.Cooldowns.DebugForceCooldown();
            StoreContext.GlobalUi.ShowNotification("~c~Cooldown Triggered");
        }

        // ------------------------------------------------------------
        // STALKER FORCE MESSAGES
        // ------------------------------------------------------------
        public static void TriggerStalker()
        {
            _ctx.Stalker.DebugForceStalker();
            StoreContext.GlobalUi.ShowNotification("~r~Stalker Messages Triggered");
        }

        // ------------------------------------------------------------
        // STALKER FORCE CALL
        // ------------------------------------------------------------
        public static void TriggerStalkerCall()
        {
            _ctx.Stalker.DebugForceStalkerCall();
            StoreContext.GlobalUi.ShowNotification("~r~Stalker Call Triggered");
        }

        // ------------------------------------------------------------
        // UI TEST
        // ------------------------------------------------------------
        public static void TriggerUI()
        {
            StoreContext.GlobalUi.ShowNotification("~b~UI Test Triggered");
            StoreContext.GlobalUi.ShowHelpText("This is a debug help text.");
        }

        // ------------------------------------------------------------
        // BANNER TEST
        // ------------------------------------------------------------
        public static void TriggerBanner()
        {
            StoreContext.GlobalUi.ShowHeistPassedBanner("~y~MISSION PASSED", "500000", "24/7 Supermarket (Vinewood Plaza)");
        }

        // ------------------------------------------------------------
        // TIMER TEST
        // ------------------------------------------------------------
        public static void TriggerTimer()
        {
            StoreContext.GlobalUi.SetTimerText("Debug Timer: 10", 10);
        }

        // ------------------------------------------------------------
        // MULTIPOS TEST
        // ------------------------------------------------------------
        public static void TriggerMultiPos()
        {
            try
            {
                Ped p = Game.Player.Character;
                if (p == null || !p.Exists())
                {
                    StoreContext.GlobalUi.ShowNotification("~r~Player not found.");
                    return;
                }

                Vector3 pos = p.Position;
                float heading = p.Heading;

                string formatted =
                    $"new Vector3({pos.X:0.000}f, {pos.Y:0.000}f, {pos.Z:0.000}f), Heading={heading:0.00}f";

                StoreContext.GlobalUi.ShowNotification($"~b~POS:~w~ {pos.X:0.000}, {pos.Y:0.000}, {pos.Z:0.000}\n~b~Heading:~w~ {heading:0.00}");
                StoreContext.GlobalUi.ShowNotification("~g~MultiPos captured");

                DebugLogger.Info($"[MultiPos] {formatted}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugActions.TriggerMultiPos", ex);
            }
        }

        // ------------------------------------------------------------
        // MISC ACTIONS TEST
        // ------------------------------------------------------------
        public static void TriggerMiscActions()
        {
            try
            {
                StoreChecker();

                if (DebugState.EnableProfiler)
                    DebugProfiler.DumpToFile();

                if (DebugState.EnableFileManager)
                    DebugFileManager.WriteSnapshot("StoreSnapshot", _ctx.Stores);

                DebugLogger.Info($"[MiscActions] GlobalHeat={_ctx.GlobalHeatLevel}");

                StoreContext.GlobalUi.ShowNotification("~b~Misc Actions Executed");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("DebugActions.TriggerMiscActions", ex);
            }
        }

        // ------------------------------------------------------------
        // SCENARIO FULL ROBBERY TEST
        // ------------------------------------------------------------
        public static void TriggerScenarioFullRobbery()
        {
            StoreChecker();
            StoreContext.GlobalUi.ShowNotification("~b~Scenario Full Robbery");
            _ctx.Scenarios.RunFullRobberyScenario();
        }

        // ------------------------------------------------------------
        // SCENARIO QUICK LOOT TEST
        // ------------------------------------------------------------
        public static void TriggeScenarioQuickLoot()
        {
            StoreChecker();
            StoreContext.GlobalUi.ShowNotification("~b~Scenario Quick Loot");
            _ctx.Scenarios.RunQuickLootScenario();
        }

        // ------------------------------------------------------------
        // CAMERA DEBUG OVERLAY
        // ------------------------------------------------------------
        public static bool CameraDebugEnabled = false;

        public static void ToggleCameraDebug()
        {
            CameraDebugEnabled = !CameraDebugEnabled;
            string state = CameraDebugEnabled ? "~g~ENABLED" : "~r~DISABLED";
            StoreContext.GlobalUi.ShowNotification($"~b~Camera Debug Overlay {state}");
        }

        // ------------------------------------------------------------
        // STORE DIAGNOSTICS
        // ------------------------------------------------------------
        public static void TriggerStoreDiagnostics()
        {
            var s = _ctx.GetNearestStore();
            if (s == null)
            {
                StoreContext.GlobalUi.ShowNotification("~r~No store found near player");
                return;
            }

            string msg =
                $"~b~STORE DIAGNOSTICS~w~\n" +
                $"ID: ~y~{s.Id}~w~\n" +
                $"Name: {s.Name}\n" +
                $"~g~Clerk Position~w~\n" +
                $"Pos: {s.ClerkPos}\n" +
                $"Heading: {s.ClerkHeading}\n" +
                $"~g~Register~w~\n" +
                $"Pos: {s.RegisterPos}\n" +
                $"Heading: {s.RegisterHeading}\n" +
                $"~g~Safe~w~\n" +
                $"Pos: {s.SafePos}\n" +
                $"Heading: {s.SafeHeading}\n" +
                $"~g~Door~w~\n" +
                $"Pos: {s.DoorPos}\n" +
                $"Radius: {s.Radius}\n" +
                $"~g~Cameras:~w~ {s.Cameras.Count}\n" +
                $"Robbed: {s.IsRobbed}   Cooldown: {s.CooldownActive}";

            StoreContext.GlobalUi.ShowNotification(msg);
        }

        public static void StoreChecker()
        {
            var s = _ctx.GetNearestStore();
            if (s == null)
            {
                StoreContext.GlobalUi.ShowNotification("~r~No store found near player");
                return;
            }
        }
    }
}
