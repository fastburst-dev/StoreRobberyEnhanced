using GTA;
using System;
using System.Threading.Tasks;
using StoreRobberyEnhanced.UI;

namespace StoreRobberyEnhanced.Debug
{
    internal class DebugScenarios
    {
        private bool _runningScenario = false;
        private readonly UiHelpers _ui;
        private readonly StoreContext _ctx;

        public DebugScenarios(UiHelpers ui, StoreContext ctx)
        {
            _ui = ui;
            _ctx = ctx;
        }

        internal async void RunFullRobberyScenario()
        {
            if (_runningScenario)
            {
                _ui.ShowNotification("~r~Scenario already running.");
                return;
            }

            _runningScenario = true;
            DebugLogger.Info("Starting Full Robbery Scenario");
            DebugEvents.Emit(DebugEvents.EventType.Custom, "Scenario", "FullRobbery");

            try
            {
                // ⭐ Ensure clerk exists before starting robbery
                var store = _ctx.GetNearestStore();
                if (store == null)
                {
                    _ui.ShowNotification("~r~No store found for scenario.");
                    _runningScenario = false;
                    return;
                }

                int attempts = 0;
                while ((store.Clerk == null || !store.Clerk.Exists()) && attempts < 60)
                {
                    await Delay(100);
                    attempts++;
                }

                if (store.Clerk == null || !store.Clerk.Exists())
                {
                    _ui.ShowNotification("~r~Clerk failed to spawn. Scenario aborted.");
                    _runningScenario = false;
                    return;
                }

                DebugLogger.Info("Scenario: RobberyStart");
                DebugActions.TriggerRobberyStart();
                await Delay(1500);

                DebugLogger.Info("Scenario: CameraAlarm");
                DebugActions.TriggerCameraAlarm();
                await Delay(1500);

                DebugLogger.Info("Scenario: SafeCrack");
                DebugActions.TriggerSafeCrack();
                await Delay(2000);

                DebugLogger.Info("Scenario: Escape");
                DebugActions.TriggerEscape();
                await Delay(1500);

                DebugLogger.Info("Scenario: Payout");
                DebugActions.TriggerPayout();
                await Delay(1500);

                DebugLogger.Info("Scenario: Cooldown");
                DebugActions.TriggerCooldown();

                _ui.ShowNotification("~g~Full Robbery Scenario Complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("FullRobberyScenario", ex);
            }
            finally
            {
                _runningScenario = false;
            }
        }


        internal async void RunQuickLootScenario()
        {
            if (_runningScenario)
            {
                _ui.ShowNotification("~r~Scenario already running.");
                return;
            }

            _runningScenario = true;
            DebugLogger.Info("Starting Quick Loot Scenario");
            DebugEvents.Emit(DebugEvents.EventType.Custom, "Scenario", "QuickLoot");

            try
            {
                _ctx.Robberies.TryStartDebugRobbery(out _);
                await Delay(1000);

                _ctx.Safes.DebugForceSafeCrack(out _);
                await Delay(1500);

                _ctx.Robberies.DebugForcePayout();

                _ui.ShowNotification("~g~Quick Loot Scenario Complete");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("QuickLootScenario", ex);
            }
            finally
            {
                _runningScenario = false;
            }
        }

        private static async Task Delay(int ms)
        {
            int end = Game.GameTime + ms;
            while (Game.GameTime < end)
            {
                await Task.Yield();   // returns control to SHVDN main loop
            }
        }

    }
}
