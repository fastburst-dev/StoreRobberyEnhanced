using GTA;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Systems;
using StoreRobberyEnhanced.UI;

namespace StoreRobberyEnhanced.Debug
{
    internal class DebugController
    {
        private readonly Script _script;
        private readonly UiHelpers _ui;
        private bool _actionPressed = false;

        // Toggle key (default F9 = 120)
        private int _toggleKey = 120;

        private readonly StoreContext _ctx;

        public DebugController(Script script, UiHelpers ui, StoreContext ctx)
        {
            _script = script;
            _ui = ui;
            _ctx = ctx;
            _script.Tick += OnTick;
        }

        public void ApplyKeybindConfig(
            int modifierKey,
            int toggleKey,
            System.Collections.Generic.Dictionary<int, string> actionMap)
        {
            _toggleKey = toggleKey;
            DebugKeybinds.ApplyConfig(modifierKey, actionMap);
        }

        private void OnTick(object sender, System.EventArgs e)
        {
            if (!DebugActions.IsReady)
                return;

            // Toggle debug overlay
            if (DebugKeybinds.IsTogglePressed(_toggleKey))
            {
                _ui.ShowNotification("~b~Debug Overlay Toggled");
                DebugState.OverlayVisible = !DebugState.OverlayVisible;
            }

            // ------------------------------------------------------------
            // PATCH 12 — GLOBAL DEBUG STATE UPDATES
            // ------------------------------------------------------------

            // 1. Track SafeCrack running state
            DebugState.SafeCrackRunning =
                _ctx.SafeCrack != null &&
                _ctx.SafeCrack.IsRunning;

            // 2. Track the last store processed (for DebugOverlay)
            //    We pick the nearest store to the player each tick.
            Ped player = Game.Player.Character;
            if (player != null && player.Exists())
            {
                float bestDist = float.MaxValue;
                TrackedStore nearest = null;

                foreach (var s in _ctx.Stores)
                {
                    if (s == null)
                        continue;

                    float d = player.Position.DistanceTo(s.StorePos);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        nearest = s;
                    }
                }

                DebugState.LastStore = nearest;
            }

            // ------------------------------------------------------------
            // DEBUG ACTIONS (DEBOUNCED)
            // ------------------------------------------------------------
            if (DebugKeybinds.TryGetAction(out string action))
            {
                if (!_actionPressed)
                {
                    _actionPressed = true;
                    HandleDebugAction(action);
                }
            }
            else
            {
                _actionPressed = false;
            }

            // ------------------------------------------------------------
            // CAMERA DEBUG OVERLAY DRAW
            // ------------------------------------------------------------
            if (DebugActions.CameraDebugEnabled)
            {
                DebugCameraRender.Draw(_ctx);
            }
        }

        private void HandleDebugAction(string action)
        {
            switch (action)
            {
                case "RobberyStart": DebugActions.TriggerRobberyStart(); break;
                case "SafeCrack": DebugActions.TriggerSafeCrack(); break;
                case "SafeCrackMini": DebugActions.TriggerSafeCrackMini(); break;
                case "CameraAlarm": DebugActions.TriggerCameraAlarm(); break;
                case "Escape": DebugActions.TriggerEscape(); break;
                case "Payout": DebugActions.TriggerPayout(); break;
                case "Cooldown": DebugActions.TriggerCooldown(); break;
                case "Stalker": DebugActions.TriggerStalker(); break;
                case "StalkerCall": DebugActions.TriggerStalkerCall(); break;
                case "UI": DebugActions.TriggerUI(); break;
                case "Banner": DebugActions.TriggerBanner(); break;
                case "Timer": DebugActions.TriggerTimer(); break;
                case "StoreDiag": DebugActions.TriggerStoreDiagnostics(); break;
                case "MultiPos": DebugActions.TriggerMultiPos(); break;
                case "MiscAction": DebugActions.TriggerMiscActions(); break;
                case "ScenarioFullRobbery": DebugActions.TriggerScenarioFullRobbery(); break;
                case "ScnearioQuickLoot": DebugActions.TriggeScenarioQuickLoot(); break;
                case "CameraDebug": DebugActions.ToggleCameraDebug(); break;
            }
        }
    }
}
