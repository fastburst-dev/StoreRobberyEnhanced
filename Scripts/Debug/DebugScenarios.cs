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
                DebugLogger.Info("Scenario: RobberyStart");
                _ctx.Robberies.TryStartDebugRobbery(out _);
                await Delay(1500);

                DebugLogger.Info("Scenario: CameraAlarm");
                _ctx.Cameras.DebugTriggerAlarm();
                await Delay(1500);

                DebugLogger.Info("Scenario: SafeCrack");
                _ctx.Safes.DebugForceSafeCrack(out _);
                await Delay(2000);

                DebugLogger.Info("Scenario: Escape");
                _ctx.Robberies.DebugForceEscape();
                await Delay(1500);

                DebugLogger.Info("Scenario: Payout");
                _ctx.Robberies.DebugForcePayout();
                await Delay(1500);

                DebugLogger.Info("Scenario: Cooldown");
                _ctx.Cooldowns.DebugForceCooldown();

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

        private static Task Delay(int ms)
        {
            return Task.Run(() => Script.Wait(ms));
        }
    }
}
