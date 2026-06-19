using System;
using GTA;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Systems
{
    internal class PoliceSystem
    {
        private readonly StoreContext _ctx;
        public bool SuppressPoliceForDebug = false;

        public PoliceSystem(StoreContext ctx)
        {
            _ctx = ctx;
        }

        // ------------------------------------------------------------
        // MAIN ENTRY
        // ------------------------------------------------------------
        public void UpdatePoliceLogic(TrackedStore store, Ped player)
        {
            try
            {
                // SafeCrack + SilentRobbery guards (unchanged)
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    store.HeatLevel = 0;
                    store.AlarmTriggered = false;
                    store.ClerkCallingPolice = false;
                    store.SilentAlarmPressed = false;

                    DebugLogger.Trace("[PoliceSystem] Suppressed — SafeCrack active");
                    return;
                }

                if (store.SilentRobbery)
                {
                    store.HeatLevel = 0;
                    store.AlarmTriggered = false;
                    store.ClerkCallingPolice = false;
                    store.SilentAlarmPressed = false;
                    Game.Player.WantedLevel = 0;

                    DebugLogger.Trace("[PoliceSystem] Suppressed — SilentRobbery flag");
                    return;
                }

                if (SuppressPoliceForDebug)
                    return;

                // ⭐ NEW: if this is NOT one of our clerks, never treat it as a robbery
                if (store.Clerk == null || !store.Clerk.Exists() || !_ctx.Clerks.IsOurClerk(store.Clerk))
                {
                    store.IsRobberyActive = false;
                    store.HeatLevel = 0;
                    store.AlarmTriggered = false;
                    store.ClerkDeathHandled = false;
                    store.ClerkCallingPolice = false;
                    store.SilentAlarmPressed = false;
                    return;
                }

                // ⭐ If dummy clerk exists and no robbery is active, never escalate or trigger police
                if (!store.IsRobberyActive)
                    return;

                if (!_ctx.AnyRobberyActive)
                    return;

                if (!_ctx.Config.EnablePolice)
                    return;

                //HandleCameraTriggeredAlarm(store);
                HandleClerkDeathAlarm(store);
                HandleSilentAlarm(store);
                HandleClerkCallingPolice(store);
                HandleRepeatRobberyEscalation(store);
                HandleMaskEscalation(store);
                HandleFightEscalation(store);
                HandleTimeEscalation(store);
                HandleRecognitionEscalation(store);

                ApplyHeatEffects(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.UpdatePoliceLogic", ex);
            }
        }

        // ------------------------------------------------------------
        // CAMERA DETECTION (Patched + All-Cameras-Destroyed Guard)
        // ------------------------------------------------------------
        private void HandleCameraTriggeredAlarm(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                // ⭐ Suppress camera alarms during SafeCrack or SilentRobbery
                if ((_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning) || store.SilentRobbery)
                    return;

                // ------------------------------------------------------------
                // ⭐ FIX #1 — If ALL cameras are destroyed, camera alarms cannot trigger
                // ------------------------------------------------------------
                bool anyCameraAlive = false;
                foreach (var cam in store.Cameras)
                {
                    if (!cam.Destroyed)
                    {
                        anyCameraAlive = true;
                        break;
                    }
                }

                if (!anyCameraAlive)
                {
                    DebugLogger.Trace(
                        $"HandleCameraTriggeredAlarm: all cameras destroyed → no camera alarm possible for store {store.Id}"
                    );
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ NORMAL CAMERA GRACE CHECK
                // ------------------------------------------------------------
                foreach (CameraData cam in store.Cameras)
                {
                    if (cam.Destroyed)
                        continue;

                    // ⭐ If grace hasn't started, camera cannot trigger yet
                    if (!cam.GraceActive)
                        continue;

                    // ⭐ Clerk must have reacted (aiming gun, threatening, etc.)
                    if (!store.ClerkReacted)
                        continue;

                    // ⭐ Ensure grace duration is initialized
                    if (cam.GraceDurationSeconds <= 0)
                        cam.GraceDurationSeconds = _ctx.Config.CameraGraceSeconds;

                    // ⭐ Calculate elapsed grace time
                    double elapsed = (DateTime.UtcNow - cam.GraceStartUtc).TotalSeconds;

                    // ⭐ Only trigger after grace period expires
                    if (elapsed >= cam.GraceDurationSeconds)
                    {
                        DebugLogger.Info(
                            $"[PoliceSystem] Camera alarm triggered at {store.Name} after grace period ({elapsed:0.0}s >= {cam.GraceDurationSeconds}s)"
                        );

                        TriggerPolice(store, 2, "~r~Camera detected suspicious activity!");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleCameraTriggeredAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK DEATH (FULLY PATCHED)
        // ------------------------------------------------------------
        private void HandleClerkDeathAlarm(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                // ------------------------------------------------------------
                // ⭐ Suppress clerk death alarms during SafeCrack or SilentRobbery
                // ------------------------------------------------------------
                if ((_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning) || store.SilentRobbery)
                {
                    store.ClerkDeathHandled = false;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ Ignore knocked out but not dead clerks (prevents death alarm if player just knocks clerk out instead of killing)
                // ------------------------------------------------------------
                if (store.Clerk != null && store.Clerk.Exists())
                {
                    if (!store.Clerk.IsDead && store.Clerk.IsRagdoll)
                    {
                        DebugLogger.Trace($"Clerk knocked out, not dead — no death alarm for store {store.Id}");
                        store.ClerkDeathHandled = false;
                        return;
                    }
                }

                // ------------------------------------------------------------
                // ⭐ Only react if the clerk death was actually processed
                // ------------------------------------------------------------
                if (!store.ClerkDeathHandled)
                    return;

                // ------------------------------------------------------------
                // ⭐ Ignore death alarms if all cameras are destroyed
                // (no one can "see" the body)
                // ------------------------------------------------------------
                bool anyCameraAlive = false;
                foreach (var cam in store.Cameras)
                {
                    if (!cam.Destroyed)
                    {
                        anyCameraAlive = true;
                        break;
                    }
                }

                if (!anyCameraAlive)
                {
                    DebugLogger.Trace($"Clerk death ignored — all cameras destroyed for store {store.Id}");
                    store.ClerkDeathHandled = false;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ Trigger appropriate police escalation
                // ------------------------------------------------------------
                DebugLogger.Info($"[PoliceSystem] Clerk death alarm at {store.Name}, killedWithGun={store.ClerkKilledWithGun}");

                if (store.ClerkKilledWithGun)
                    TriggerPolice(store, 3, "~r~Clerk shot! Police alerted!");
                else
                    TriggerPolice(store, 2, "~r~Clerk found dead! Police alerted!");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleClerkDeathAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // SILENT ALARM (FULLY PATCHED)
        // ------------------------------------------------------------
        private void HandleSilentAlarm(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                // ------------------------------------------------------------
                // ⭐ Suppress silent alarm during SafeCrack or SilentRobbery
                // ------------------------------------------------------------
                if ((_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning) || store.SilentRobbery)
                {
                    store.SilentAlarmPressed = false;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ If clerk never pressed the silent alarm, nothing to do
                // ------------------------------------------------------------
                if (!store.SilentAlarmPressed)
                    return;

                // ------------------------------------------------------------
                // ⭐ Clerk must have reacted before silent alarm can escalate
                // (prevents silent alarm from firing before robbery is recognized)
                // ------------------------------------------------------------
                if (!store.ClerkReacted)
                    return;

                // ------------------------------------------------------------
                // ⭐ Ignore silent alarm if all cameras are destroyed
                // (no one can "see" the clerk pressing it)
                // ------------------------------------------------------------
                bool anyCameraAlive = false;
                foreach (var cam in store.Cameras)
                {
                    if (!cam.Destroyed)
                    {
                        anyCameraAlive = true;
                        break;
                    }
                }

                if (!anyCameraAlive)
                {
                    DebugLogger.Trace($"Silent alarm ignored — all cameras destroyed for store {store.Id}");
                    store.SilentAlarmPressed = false;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ Delay before silent alarm actually triggers police
                // ------------------------------------------------------------
                double elapsed = (DateTime.UtcNow - store.SilentAlarmUtc).TotalSeconds;

                if (elapsed >= _ctx.Config.SilentAlarmDelaySeconds)
                {
                    DebugLogger.Info($"[PoliceSystem] Silent alarm triggered at {store.Name}");
                    TriggerPolice(store, 1, "~y~Silent alarm triggered!");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleSilentAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK CALLING POLICE (FULLY PATCHED)
        // ------------------------------------------------------------
        private void HandleClerkCallingPolice(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive || store.AlarmTriggered)
                    return;

                // ------------------------------------------------------------
                // ⭐ Suppress clerk calls during SafeCrack or SilentRobbery
                // ------------------------------------------------------------
                if ((_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning) || store.SilentRobbery)
                {
                    store.ClerkCallingPolice = false;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ Clerk must have reacted before calling police
                // (prevents clerk calling police just because cameras saw player)
                // ------------------------------------------------------------
                if (!store.ClerkReacted)
                    return;

                // ------------------------------------------------------------
                // ⭐ Clerk cannot call police if all cameras are destroyed
                // ------------------------------------------------------------
                bool anyCameraAlive = false;
                foreach (var cam in store.Cameras)
                {
                    if (!cam.Destroyed)
                    {
                        anyCameraAlive = true;
                        break;
                    }
                }

                if (!anyCameraAlive)
                {
                    DebugLogger.Trace($"Clerk cannot call police — all cameras destroyed for store {store.Id}");
                    store.ClerkCallingPolice = false;
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ If clerk is not currently calling police, nothing to do
                // ------------------------------------------------------------
                if (!store.ClerkCallingPolice)
                    return;

                // ------------------------------------------------------------
                // ⭐ Delay before police are actually called
                // ------------------------------------------------------------
                double elapsed = (DateTime.UtcNow - store.ClerkCallStartUtc).TotalSeconds;

                if (elapsed >= _ctx.Config.ClerkCallDelaySeconds)
                {
                    DebugLogger.Info($"[PoliceSystem] Clerk called police at {store.Name}");
                    TriggerPolice(store, 2, "~r~Clerk called the police!");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleClerkCallingPolice", ex);
            }
        }

        // ------------------------------------------------------------
        // REPEAT ROBBERY ESCALATION
        // ------------------------------------------------------------
        private void HandleRepeatRobberyEscalation(TrackedStore store)
        {
            try
            {
                if (!_ctx.Config.EnablePolice)
                    return;

                if (store.RepeatRobberyEscalationApplied)
                    return;

                // ⭐ NEW: Never escalate on first robbery
                if (store.TimesRobbed <= 1)
                    return;

                if (store.TimesRobbed >= 4)
                {
                    store.HeatLevel += 2;
                    store.RepeatRobberyEscalationApplied = true;
                    DebugLogger.Info($"[PoliceSystem] Repeat robbery escalation (+2 heat) at {store.Name}");
                }
                else if (store.TimesRobbed >= 2)
                {
                    store.HeatLevel += 1;
                    store.RepeatRobberyEscalationApplied = true;
                    DebugLogger.Info($"[PoliceSystem] Repeat robbery escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleRepeatRobberyEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // MASK ESCALATION
        // ------------------------------------------------------------
        private void HandleMaskEscalation(TrackedStore store)
        {
            try
            {
                // ⭐ FULL suppression for debug escape
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (!_ctx.Config.EnablePolice)
                    return;

                if (store.MaskEscalationApplied)
                    return;

                // ⭐ Only escalate on repeat robberies
                if (store.TimesRobbed <= 1)
                    return;

                if (store.PlayerMaskedAtStart)
                {
                    store.HeatLevel += 1;
                    store.MaskEscalationApplied = true;
                    DebugLogger.Info($"[PoliceSystem] Mask escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleMaskEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK FIGHT-BACK ESCALATION
        // ------------------------------------------------------------
        private void HandleFightEscalation(TrackedStore store)
        {
            try
            {
                // ⭐ FULL suppression for debug escape
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (store.FightEscalationApplied)
                    return;

                if (store.ReactionType == ClerkReactionType.FightPistol ||
                    store.ReactionType == ClerkReactionType.FightShotgun)
                {
                    store.HeatLevel += 1;
                    store.FightEscalationApplied = true;
                    DebugLogger.Info($"[PoliceSystem] Fight escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleFightEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // TIME ESCALATION
        // ------------------------------------------------------------
        private void HandleTimeEscalation(TrackedStore store)
        {
            try
            {
                // ⭐ FULL suppression for debug escape
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                if (!store.IsRobberyActive || !store.ClerkReacted)
                    return;

                if (store.TimeEscalationApplied)
                    return;

                // ⭐ Only escalate on repeat robberies
                if (store.TimesRobbed <= 1)
                    return;

                double elapsed = (DateTime.UtcNow - store.RobberyStartUtc).TotalSeconds;
                if (elapsed >= _ctx.Config.TimeEscalationSeconds)
                {
                    store.HeatLevel += 1;
                    store.TimeEscalationApplied = true;
                    DebugLogger.Info($"[PoliceSystem] Time escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleTimeEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // CLERK RECOGNITION ESCALATION
        // ------------------------------------------------------------
        private void HandleRecognitionEscalation(TrackedStore store)
        {
            try
            {
                // ⭐ FULL suppression for debug escape
                if (_ctx.Police.SuppressPoliceForDebug)
                    return;

                // ⭐ Only escalate during an active robbery
                if (!store.IsRobberyActive)
                    return;

                if (store.ClerkRecognizedPlayer)
                {
                    store.HeatLevel += 1;
                    store.ClerkRecognizedPlayer = false;

                    DebugLogger.Info($"[PoliceSystem] Recognition escalation (+1 heat) at {store.Name}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.HandleRecognitionEscalation", ex);
            }
        }

        // ------------------------------------------------------------
        // APPLY HEAT EFFECTS
        // ------------------------------------------------------------
        private void ApplyHeatEffects(TrackedStore store)
        {
            try
            {
                // ⭐ FULL suppression for debug escape
                if (_ctx.Police.SuppressPoliceForDebug)
                {
                    Game.Player.WantedLevel = 0;
                    return;
                }

                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive)
                    return;

                // ⭐ Prevent wanted level during SafeCrack or SilentRobbery
                if ((_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning) || store.SilentRobbery)
                {
                    Game.Player.WantedLevel = 0;
                    return;
                }

                int wanted = Game.Player.WantedLevel;

                if (store.HeatLevel == 1 && wanted < 1)
                    Game.Player.WantedLevel = 1;

                if (store.HeatLevel == 2 && wanted < 2)
                    Game.Player.WantedLevel = 2;

                if (store.HeatLevel >= 3 && wanted < 3)
                    Game.Player.WantedLevel = 3;

                if (store.HeatLevel > 3)
                    store.HeatLevel = 3;

                DebugLogger.Trace($"ApplyHeatEffects: store={store.Name}, heat={store.HeatLevel}, wanted={Game.Player.WantedLevel}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.ApplyHeatEffects", ex);
            }
        }

        // ------------------------------------------------------------
        // TRIGGER POLICE
        // ------------------------------------------------------------
        private void TriggerPolice(TrackedStore store, int wantedLevel, string message)
        {
            try
            {
                // ⭐ FULL suppression for debug escape
                if (_ctx.Police.SuppressPoliceForDebug)
                {
                    DebugLogger.Trace($"[TriggerPolice] Suppressed for debug at store {store.Id}");
                    return;
                }

                if (!_ctx.Config.EnablePolice)
                    return;

                if (!store.IsRobberyActive)
                    return;

                // ⭐ Suppress ALL police triggers during SafeCrack or SilentRobbery
                if ((_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning) || store.SilentRobbery)
                {
                    DebugLogger.Trace($"[TriggerPolice] Suppressed — SafeCrack or SilentRobbery active for store {store.Id}");
                    return;
                }

                // ⭐ Prevent double-triggering
                if (store.AlarmTriggered)
                    return;

                // ⭐ Prevent police triggers if all cameras are destroyed
                bool anyCameraAlive = false;
                foreach (var cam in store.Cameras)
                {
                    if (!cam.Destroyed)
                    {
                        anyCameraAlive = true;
                        break;
                    }
                }

                if (!anyCameraAlive)
                {
                    DebugLogger.Trace($"[TriggerPolice] Suppressed — all cameras destroyed for store {store.Id}");
                    return;
                }

                // ⭐ Mark alarm as triggered
                store.AlarmTriggered = true;

                // ⭐ Apply wanted level escalation
                if (wantedLevel > Game.Player.WantedLevel)
                    Game.Player.WantedLevel = wantedLevel;

                // ⭐ UI feedback
                _ctx.Ui.ShowNotification(message);

                // ⭐ Heat system
                store.HeatLevel += 1;
                _ctx.GlobalHeatLevel += 1;

                DebugLogger.Info(
                    $"[PoliceSystem] TriggerPolice: store={store.Name}, wanted={wantedLevel}, heat={store.HeatLevel}, globalHeat={_ctx.GlobalHeatLevel}"
                );

                // ⭐ Persist state
                _ctx.SaveStoreState(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PoliceSystem.TriggerPolice", ex);
            }
        }
    }
}
