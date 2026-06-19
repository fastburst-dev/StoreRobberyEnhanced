using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Minigame;
using StoreRobberyEnhanced.UI;
using System;
using System.Threading.Tasks;
using static StoreRobberyEnhanced.Systems.ClerkSystem;

namespace StoreRobberyEnhanced.Systems
{
    internal class RobberySystem
    {
        private readonly StoreContext _ctx;
        private readonly ClerkSystem _clerks;
        private int _lastTimerUpdate;

        // DEBUG TIMER FIELDS
        private bool _testTimerActive = false;
        private int _testTimerEnd = 0;
        // DEBUG ESCAPE STATE
        private bool _debugEscapeActive = false;
        private int _debugEscapeStoreId = -1;
        private int _lastDebugSubtitleTime = 0;

        public RobberySystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                _lastTimerUpdate = 0;

                DebugLogger.Info("RobberySystem initialized");

                if (_ctx.Config.EnableDebug && _ctx.Config.EnableDebugTimer)
                {
                    _testTimerActive = true;
                    _testTimerEnd = Game.GameTime + 15000;

                    DebugLogger.Info("Debug timer enabled (15 seconds)");
                    StoreContext.GlobalUi.SetTimerText("TEST TIMER: 15", 15);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.ctor", ex);
            }
        }

        
        // ------------------------------------------------------------
        // DEBUG ROBBERY START
        // ------------------------------------------------------------
        public bool TryStartDebugRobbery(out string msg)
        {
            try
            {
                var store = _ctx.GetNearestStore();
                if (store == null)
                {
                    msg = "No store nearby";
                    DebugLogger.Info(msg);
                    return false;
                }

                if (store.IsRobbed || store.CooldownActive)
                {
                    msg = "Store already robbed or on cooldown";
                    DebugLogger.Info(msg);
                    return false;
                }

                DebugLogger.Info($"[ROBBERY] DebugRobbery: robbery started at store {store.Id}");
                StartRegisterRobbery(store);

                msg = store.Name;
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.TryStartDebugRobbery", ex);
                msg = "Error";
                return false;
            }
        }

        // Overload for SafeCrack integration
        public int DebugForcePayout(TrackedStore store)
        {
            // Forward to the existing parameterless version
            return DebugForcePayout();
        }

        // Overload for SafeCrack integration
        public void DebugForceEscape(TrackedStore store)
        {
            // Forward to the existing parameterless version
            DebugForceEscape();
        }

        // ------------------------------------------------------------
        // DEBUG FORCE ESCAPE (PATCHED)
        // ------------------------------------------------------------
        public void DebugForceEscape()
        {
            try
            {
                // 🔧 Reset lingering debug state before starting
                _debugEscapeActive = false;
                _debugEscapeStoreId = -1;

                var store = _ctx.GetNearestStore();
                if (store == null)
                    return;

                // 🔧 Clear any locked end state from previous run
                store.RobberyEnded = false;
                store.CooldownActive = false;

                // ⭐ Mark this as a debug escape run
                _debugEscapeActive = true;
                _debugEscapeStoreId = store.Id;

                // ⭐ Clear any stuck police state
                Game.Player.WantedLevel = 0;
                store.AlarmTriggered = false;
                store.PlayerMaskedAtStart = false;

                // ⭐ Enable debug police suppression
                _ctx.Police.SuppressPoliceForDebug = true;

                // ⭐ Force ALL required robbery state
                store.IsRobbed = true;
                store.IsRobberyActive = true;
                store.SafeCracked = true;
                store.PendingCompletion = true;
                store.CooldownActive = false;
                store.AlarmTriggered = false;

                // ⭐ Prevent camera auto-complete
                foreach (var cam in store.Cameras)
                    cam.Destroyed = false;

                // ⭐ Simulate robbery start so the REAL timer runs
                store.RobberyStartUtc = DateTime.UtcNow;

                // ⭐ Ensure payout exists
                if (store.PendingPayout <= 0)
                    store.PendingPayout = _ctx.Rng.Next(2500, 50000);

                DebugLogger.Info($"[ROBBERY] DebugForceEscape: armed for store {store.Id}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.DebugForceEscape", ex);
            }
        }

        // ------------------------------------------------------------
        // DEBUG FORCE PAYOUT (PATCHED)
        // ------------------------------------------------------------
        public int DebugForcePayout()
        {
            try
            {
                // 🔧 Reset lingering debug state before starting
                _debugEscapeActive = false;
                _debugEscapeStoreId = -1;

                var store = _ctx.GetNearestStore();
                if (store == null)
                    return 0;

                // ⭐ Mark this as a debug escape run
                _debugEscapeActive = true;
                _debugEscapeStoreId = store.Id;

                // ⭐ Clear any stuck police state
                Game.Player.WantedLevel = 0;
                store.AlarmTriggered = false;
                store.PlayerMaskedAtStart = false;

                // ⭐ Suppress ALL police (both systems)
                _ctx.Police.SuppressPoliceForDebug = true;

                // ⭐ Force all required robbery state
                store.IsRobbed = true;
                store.IsRobberyActive = true;
                store.SafeCracked = true;
                store.PendingCompletion = true;
                store.CooldownActive = false;
                store.AlarmTriggered = false;

                // ⭐ Prevent camera auto-complete (same fix as DebugForceEscape)
                foreach (var cam in store.Cameras)
                    cam.Destroyed = false;

                // ⭐ Simulate robbery start so the REAL timer runs
                store.RobberyStartUtc = DateTime.UtcNow;

                // ⭐ Ensure payout exists
                if (store.PendingPayout <= 0)
                    store.PendingPayout = _ctx.Rng.Next(2500, 50000);

                DebugLogger.Info($"[ROBBERY] DebugForcePayout: awarding payout + cooldown for store {store.Id}");

                int payout = store.PendingPayout;

                return payout;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.DebugForcePayout", ex);
                return 0;
            }
        }

        // ------------------------------------------------------------
        // DEBUG RESET STORE (FULLY PATCHED — FINAL VERSION)
        // ------------------------------------------------------------
        public void DebugResetStore(TrackedStore store)
        {
            // 🔧 Reset lingering debug state
            _debugEscapeActive = false;
            _debugEscapeStoreId = -1;
            store.RobberyEnded = false;

            // ------------------------------------------------------------
            // ⭐ CORE ROBBERY STATE
            // ------------------------------------------------------------
            store.IsRobbed = false;
            store.IsRobberyActive = false;
            store.SafeCracked = false;
            store.PendingCompletion = false;
            store.PendingPayout = 0;
            store.CooldownActive = false;
            store.PlayerMaskedAtStart = false;
            store.RobberyCompleted = false;

            // ------------------------------------------------------------
            // ⭐ STEALTH / ALARM / HEAT
            // ------------------------------------------------------------
            store.SilentRobbery = false;
            store.AlarmTriggered = false;
            store.HeatLevel = 0;
            store.ClerkCallingPolice = false;
            store.SilentAlarmPressed = false;

            // ------------------------------------------------------------
            // ⭐ ESCALATION FLAGS
            // ------------------------------------------------------------
            store.RepeatRobberyEscalationApplied = false;
            store.MaskEscalationApplied = false;
            store.FightEscalationApplied = false;
            store.TimeEscalationApplied = false;

            // ------------------------------------------------------------
            // ⭐ CLERK STATE
            // ------------------------------------------------------------
            store.ClerkReacted = false;
            store.ClerkRecognizedPlayer = false;
            store.ClerkKilledWithGun = false;
            store.ClerkDeathHandled = false;

            // ------------------------------------------------------------
            // ⭐ STALL STATE
            // ------------------------------------------------------------
            store.ClerkStalling = false;
            store.StallStartUtc = DateTime.MinValue;
            store.StallDurationMs = 0;

            // ------------------------------------------------------------
            // ⭐ TIMESTAMPS
            // ------------------------------------------------------------
            store.LastRobbedUtc = DateTime.MinValue;
            store.RobberyStartUtc = DateTime.MinValue;

            // ------------------------------------------------------------
            // ⭐ LOOT BAG
            // ------------------------------------------------------------
            if (store.LootBag != null && store.LootBag.Exists())
            {
                store.LootBag.Delete();
                store.LootBag = null;
            }

            if (store.Clerk != null && store.Clerk.Exists())
            {
                Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, store.Clerk);
                store.Clerk.MarkAsNoLongerNeeded();
                store.Clerk.Delete();
                store.Clerk = null;
            }

            // ------------------------------------------------------------
            // ⭐ DUMMY CLERK
            // ------------------------------------------------------------
            if (store.DummyClerk != null && store.DummyClerk.Exists())
            {
                store.DummyClerk.Delete();
                store.DummyClerk = null;
            }

            // Respawn dummy clerk cleanly
            _ctx.Clerks.SpawnDummyClerk(store);

            // ------------------------------------------------------------
            // ⭐ CAMERA STATE
            // ------------------------------------------------------------
            // DO NOT reset camera destruction — intentional gameplay
            // DO NOT reset camera grace — camera system handles this naturally

            // ------------------------------------------------------------
            // ⭐ DEBUG FLAGS
            // ------------------------------------------------------------
            Game.Player.WantedLevel = 0;
            _ctx.Police.SuppressPoliceForDebug = false;

            // ⭐ Remove any existing cooldown blocker before creating a new one
            _ctx.Cooldowns.RemoveCooldownBlocker(store);

            DebugLogger.Info($"[ROBBERY] DebugResetStore: store {store.Id} fully reset");

            // ------------------------------------------------------------
            // ⭐ SAVE CLEAN STATE
            // ------------------------------------------------------------
            _ctx.SaveStoreState(store);

            // ⭐ Debug reset ends all robbery activity globally
            _ctx.SetRobberyActive(false);
        }

        // ------------------------------------------------------------
        // MAIN ENTRY (FULLY PATCHED)
        // ------------------------------------------------------------
        public void UpdateRobbery(TrackedStore store, Ped player)
        {
            try
            {
                // ⭐ ABSOLUTE PRIORITY — SafeCrack owns ALL UI while running
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ UI SAFETY — NEVER UPDATE TIMER IF BANNER IS ACTIVE
                if (StoreContext.GlobalUi.IsBannerActive)
                {
                    StoreContext.GlobalUi.ClearTimer();
                }

                // ------------------------------------------------------------
                // ⭐ HARD STOP — ROBBERY ENDED
                // ------------------------------------------------------------
                if (store.RobberyEnded)
                {
                    store.IsRobbed = false;
                    store.IsRobberyActive = false;

                    // ⭐ ALWAYS clear timer when robbery ends
                    StoreContext.GlobalUi.ClearTimer();
                    return;
                }

                // ⭐ Non-blocking safe subtitle trigger
                if (store.NextSafeSubtitleUtc != DateTime.MinValue &&
                    DateTime.UtcNow >= store.NextSafeSubtitleUtc)
                {
                    _ctx.Ui.ShowSubtitle("There is a safe in the office — crack it too.", 4000);
                    store.NextSafeSubtitleUtc = DateTime.MinValue;
                }

                // BAG PICKUP
                if (store.LootBag != null && store.LootBag.Exists())
                {
                    float distBag = player.Position.DistanceTo(store.LootBag.Position);

                    if (distBag < 1.2f)
                    {
                        store.LootBag.Delete();
                        store.LootBag = null;

                        DebugLogger.Info($"[ROBBERY] Player picked up loot bag at store {store.Id}");

                        // Bag pickup does NOT pay immediately — payout is handled by PendingPayout
                        _ctx.Ui.ShowNotification("~g~Loot bag collected!");

                        // Optional: sound
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                    }
                }

                // ⭐ Debug escape subtitle loop
                if (_debugEscapeActive && store.Id == _debugEscapeStoreId && !store.CooldownActive)
                {
                    float distDebug = player.Position.DistanceTo(store.StorePos);

                    if (distDebug < _ctx.Config.EscapeDistance)
                    {
                        if (Game.GameTime - _lastDebugSubtitleTime > 1000)
                        {
                            _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                            _lastDebugSubtitleTime = Game.GameTime;
                        }
                    }
                }

                // ⭐ ABANDON LOGIC — only runs when robbery is active AND not completed
                if (store.IsRobbed && !store.RobberyCompleted)
                {
                    try
                    {
                        float dist = player.Position.DistanceTo(store.StorePos);

                        bool leftArea = dist > store.Radius;
                        bool beyondEscape = dist > _ctx.Config.EscapeDistance;
                        bool clerkDead = store.ClerkDeathHandled;

                        // ⭐ Only allow abandon BEFORE safe crack / payout
                        bool canAbandon =
                            !store.PendingCompletion &&
                            store.PendingPayout <= 0;

                        // ⭐ NON-VIOLENT ABANDON (clerk alive)
                        if (leftArea && beyondEscape && !clerkDead)
                        {
                            DebugLogger.Info($"[ABANDON] Player abandoned robbery at store {store.Id} with clerk alive.");

                            store.IsRobbed = false;
                            store.IsRobberyActive = false;
                            store.PendingCompletion = false;
                            store.PendingPayout = 0;
                            store.AlarmTriggered = false;
                            store.CooldownActive = false;
                            store.LastRobbedUtc = DateTime.MinValue;

                            if (store.Clerk != null && store.Clerk.Exists())
                            {
                                store.Clerk.Delete();
                                store.Clerk = null;
                            }

                            if (store.DummyClerk != null && store.DummyClerk.Exists())
                            {
                                store.DummyClerk.Delete();
                                store.DummyClerk = null;
                            }

                            store.ClerkDeathHandled = false;
                            store.ClerkDeathHandledCheck = false;
                            store.ClerkKilledWithGun = false;
                            store.ClerkReacted = false;
                            store.ClerkSurrender = false;
                            store.ClerkSurrenderStage = 0;
                            store.ClerkPanicking = false;

                            store.DefaultClerkRemoved = false;

                            Game.Player.WantedLevel = 0;
                            store.WantedSuppressionEndUtc = DateTime.UtcNow.AddSeconds(3);

                            StoreContext.GlobalUi.ClearTimer();
                            _ctx.Ui.ShowSubtitle("~y~Robbery was aborted. Try again later.", 6000);

                            _ctx.SaveStoreState(store);
                            return;
                        }

                        // ⭐ VIOLENT ABANDON (clerk dead)
                        if (leftArea && beyondEscape && clerkDead)
                        {
                            DebugLogger.Info($"[ABANDON-VIOLENT] Player killed clerk and fled store {store.Id}.");

                            store.IsRobbed = false;
                            store.IsRobberyActive = false;
                            store.PendingCompletion = false;
                            store.PendingPayout = 0;
                            store.AlarmTriggered = false;
                            store.CooldownActive = false;
                            store.LastRobbedUtc = DateTime.MinValue;

                            if (store.Clerk != null && store.Clerk.Exists())
                            {
                                store.Clerk.Delete();
                                store.Clerk = null;
                            }

                            if (store.DummyClerk != null && store.DummyClerk.Exists())
                            {
                                store.DummyClerk.Delete();
                                store.DummyClerk = null;
                            }

                            store.ClerkDeathHandled = false;
                            store.ClerkDeathHandledCheck = false;
                            store.ClerkKilledWithGun = false;
                            store.ClerkReacted = false;
                            store.ClerkSurrender = false;
                            store.ClerkSurrenderStage = 0;
                            store.ClerkPanicking = false;

                            store.DefaultClerkRemoved = false;

                            Game.Player.WantedLevel = 0;
                            store.WantedSuppressionEndUtc = DateTime.UtcNow.AddSeconds(3);

                            StoreContext.GlobalUi.ClearTimer();
                            _ctx.Ui.ShowSubtitle("~y~Robbery attempt has failed.", 6000);

                            _ctx.SaveStoreState(store);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("RobberySystem.AbandonLogic", ex);
                    }
                }

                // ⭐ ESCAPE + COMPLETION LOGIC — ALWAYS RUNS WHILE ROBBERY IS ACTIVE
                if (store.IsRobbed)
                {
                    UpdateRobberyTimer(store);
                    CheckCameraTriggeredAlarm(store);
                    CheckLeavingEarly(store, player);
                    CheckEarlyEscapeSuccess(store, player);
                }

                // ------------------------------------------------------------
                // ⭐ Debug timer override
                // ------------------------------------------------------------
                if (_ctx.Config.EnableDebugTimer && _testTimerActive)
                {
                    int remaining = (_testTimerEnd - Game.GameTime) / 1000;

                    if (remaining < 0)
                    {
                        DebugLogger.Info("[ROBBERY] Debug timer expired — showing heist banner");

                        _testTimerActive = false;
                        StoreContext.GlobalUi.ClearTimer();
                        StoreContext.GlobalUi.ShowHeistPassedBanner("~o~ROBBERY COMPLETE", "100000", "Convinence Store");
                        return;
                    }
                    else
                    {
                        StoreContext.GlobalUi.SetTimerText($"TEST TIMER: {remaining}", remaining);
                    }

                    return;
                }

                // ⭐ Cooldown stops all robbery logic
                if (store.CooldownActive)
                    return;

                // ⭐ Try to start a register robbery (handles silent + loud)
                TryStartRegisterRobbery(store, player);

                // ⭐ Prevent ANY system from restarting SafeCrack while active
                if (_ctx.SafeState.Active)
                    return;

                // ⭐ Run robbery logic again if robbery started this frame
                if (store.IsRobbed)
                {
                    UpdateRobberyTimer(store);
                    CheckCameraTriggeredAlarm(store);
                    CheckLeavingEarly(store, player);
                    CheckEarlyEscapeSuccess(store, player);
                }

                // ------------------------------------------------------------
                // ⭐ PATCHED SAFECRACK START VALIDATION (Silent + Loud + Dynamic Input)
                // ------------------------------------------------------------
                if (store.IsRobbed &&
                    store.SafePos != Vector3.Zero &&
                    !store.SafeCracked)
                {
                    // Cannot start if SafeCrack already running
                    if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                        return;

                    // ⭐ Detect input mode (Keyboard vs Controller)
                    bool usingController = Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE, 2) == false;

                    // ⭐ Unified input detection
                    bool pressedInteract =
                        Game.IsControlJustPressed(GTA.Control.Context) ||
                        Game.IsControlJustPressed(GTA.Control.FrontendAccept);

                    // ------------------------------------------------------------
                    // ⭐ SILENT ROBBERY SAFECRACK
                    // ------------------------------------------------------------
                    if (store.SilentRobbery)
                    {
                        float safeDistSilent = player.Position.DistanceTo(store.SafePos);

                        if (safeDistSilent <= 1.2f)
                        {
                            if (usingController)
                                _ctx.Ui.ShowHelpText("Press ~INPUT_FRONTEND_ACCEPT~ to crack the safe");
                            else
                                _ctx.Ui.ShowHelpText("Press ~y~E~w~ to crack the safe");

                            if (pressedInteract)
                            {
                                DebugLogger.Info($"[SafeCrack] Starting SafeCrack (SILENT) at store {store.Id}");
                                _ctx.SafeCrack.Start(store, store.SafePos, store.SafeHeading, player);
                            }
                        }

                        return;
                    }

                    // ------------------------------------------------------------
                    // ⭐ LOUD ROBBERY SAFECRACK
                    // ------------------------------------------------------------

                    // Clerk ragdoll check
                    if (store.Clerk != null && store.Clerk.Exists() && store.Clerk.IsRagdoll)
                        return;

                    // Cannot start if clerk is mid-animation
                    if (store.ClerkOpeningRegister || store.ClerkGrabbingCash || store.ClerkThrowingBag)
                        return;

                    float safeDist = player.Position.DistanceTo(store.SafePos);

                    if (safeDist <= 1.2f)
                    {
                        if (usingController)
                            _ctx.Ui.ShowHelpText("Press ~INPUT_FRONTEND_ACCEPT~ to crack the safe");
                        else
                            _ctx.Ui.ShowHelpText("Press ~y~E~w~ to crack the safe");

                        if (pressedInteract)
                        {
                            DebugLogger.Info($"[SafeCrack] Starting SafeCrack (LOUD) at store {store.Id}");
                            _ctx.SafeCrack.Start(store, store.SafePos, store.SafeHeading, player);
                        }
                    }
                }

                // ------------------------------------------------------------
                // ⭐ COMPLETION LOGIC
                // ------------------------------------------------------------
                if (store.IsRobbed &&
                    store.PendingCompletion &&
                    store.PendingPayout > 0 &&
                    (store.SafePos == Vector3.Zero || store.SafeCracked))
                {
                    CompleteRobbery(store, player);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.UpdateRobbery", ex);
            }
        }

        // ------------------------------------------------------------
        // START ROBBERY (FULLY PATCHED — FINAL + SILENT ROBBERY LOGIC)
        // ------------------------------------------------------------
        private void TryStartRegisterRobbery(TrackedStore store, Ped player)
        {
            try
            {
                // ⭐ SAFETY: If SafeCrack exists but isn't actually running, make sure it can't block loud robbery
                if (_ctx.SafeCrack != null && !_ctx.SafeCrack.IsRunning)
                {
                    // Hard reset of any stale state that might falsely report "running"
                    _ctx.SafeCrack.ResetState();
                }

                // Prevent double-starting ONLY if clerk has not reacted yet
                // (ClerkSystem sets IsRobberyActive early, so we must allow start)
                if (store.IsRobberyActive && !store.ClerkReacted)
                    return;

                // ------------------------------------------------------------
                // DEBUG TIMER GUARD
                // ------------------------------------------------------------
                if (_ctx.Config.EnableDebugTimer && _testTimerActive)
                    return;

                // ------------------------------------------------------------
                // INVALID STORE / CLERK
                // ------------------------------------------------------------
                if (store.IsRobbed || store.Clerk == null || !store.Clerk.Exists())
                    return;

                // ------------------------------------------------------------
                // ⭐ CRITICAL FIX — RESET COMPLETION FLAGS FOR NEW ROBBERY
                // ------------------------------------------------------------
                store.RobberyCompleted = false;     // <—— REQUIRED FIX
                store.RobberyEnded = false;         // <—— ensures timer + logic run
                store.PendingCompletion = false;    // <—— will be set true below
                // (PendingPayout is intentionally NOT reset here — silent robbery may add payout immediately)

                // ------------------------------------------------------------
                // DISTANCE CHECK
                // ------------------------------------------------------------
                float dist = player.Position.DistanceTo(store.Clerk.Position);
                if (dist > 12f)
                    return;

                // ------------------------------------------------------------
                // ⭐ SILENT ROBBERY CHECK (MASK + MELEE + CLOSE RANGE)
                // ------------------------------------------------------------
                bool isMasked = _ctx.Player.IsMasked();

                bool isMelee =
                    player.Weapons.Current != null &&
                    player.Weapons.Current.Group == WeaponGroup.Melee;

                bool closeEnough = dist < 3.0f;

                bool isPhysicallyAiming = _ctx.Player.IsAiming();

                bool noAim = !isPhysicallyAiming;

                bool noAlarm = !store.AlarmTriggered;

                // ------------------------------------------------------------
                // ⭐ FIX: Reset false ClerkReacted caused by clerk replacement sweeps
                // ------------------------------------------------------------
                if (store.ClerkReacted && isMasked && isMelee && closeEnough)
                {
                    DebugLogger.Trace($"Resetting false ClerkReacted for silent robbery attempt at store {store.Id}");
                    store.ClerkReacted = false;
                }

                bool clerkNotReacted = !store.ClerkReacted;

                bool canSilentRob =
                    isMasked &&
                    isMelee &&
                    closeEnough &&
                    noAim &&
                    clerkNotReacted &&
                    noAlarm;

                if (canSilentRob)
                {
                    DebugLogger.Info($"[ROBBERY] SilentRobbery activated at store {store.Id}");

                    store.SilentRobbery = true;
                    store.IsRobberyActive = true;
                    store.IsRobbed = true;
                    store.RobberyStartUtc = DateTime.UtcNow;
                    store.PendingCompletion = true;

                    // ⭐ Mark robbery as globally active (required for StalkerSystem)
                    _ctx.SetRobberyActive(true);

                    // ------------------------------------------------------------
                    // ⭐ THIRD FIX — HARD LOCK SILENT ROBBERY STATE
                    // ------------------------------------------------------------

                    store.ClerkReacted = false;
                    store.AlarmTriggered = false;
                    store.ClerkCallingPolice = false;
                    store.SilentAlarmPressed = false;

                    foreach (var cam in store.Cameras)
                    {
                        cam.GraceActive = false;
                        cam.GraceStartUtc = DateTime.UtcNow;
                        cam.GraceDurationSeconds = _ctx.Config.CameraGraceSeconds;
                    }

                    store.ClerkStalling = false;
                    store.StallStartUtc = DateTime.MinValue;
                    store.StallDurationMs = 0;

                    store.HeatLevel = 0;

                    DebugLogger.Info($"[ROBBERY] SilentRobbery HARD LOCK activated for store {store.Id}");

                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        _ctx.Ui.ShowNotification("~o~Silent robbery started.");
                    });

                    // ------------------------------------------------------------
                    // ⭐ COSMETIC CLERK ANIMATION FOR SILENT ROBBERY
                    // ------------------------------------------------------------
                    _ctx.Clerks.PlaySilentRobberyAnim(store);   // <—— ADD THIS LINE

                    // ------------------------------------------------------------
                    // REGISTER PAYOUT (STEALTH)
                    // ------------------------------------------------------------
                    int payout = _ctx.Rng.Next(_ctx.Config.RegisterMinAmount, _ctx.Config.RegisterMaxAmount + 1);
                    payout = (int)(payout * _ctx.Config.PayoutMultiplier);
                    store.PendingPayout += payout;

                    Task.Run(async () =>
                    {
                        await Task.Delay(3500);
                        _ctx.Ui.ShowSubtitle("~o~Silent robbery started, complete & leave quietly.", 4000);
                    });

                    _ctx.SaveStoreState(store);

                    if(_ctx.Config.EnableBlips)
                        _ctx.Cooldowns.UpdateStoreBlip(store);

                    return;
                }

                // ------------------------------------------------------------
                // ⭐ FIXED AIM CHECK (LOUD ROBBERY)
                // ------------------------------------------------------------
                // Use full aiming detection (controller, mouse, soft aim, LOS)                
                if (!isPhysicallyAiming)
                    return;

                // ------------------------------------------------------------
                // MUST BE ARMED (LOUD ROBBERY)
                // ------------------------------------------------------------
                if (!_ctx.Player.IsArmed())
                    return;

                // ------------------------------------------------------------
                // MASK STATE AT START
                // ------------------------------------------------------------
                store.PlayerMaskedAtStart = _ctx.Player.IsMasked();

                DebugLogger.Info($"[ROBBERY] Robbery started at store {store.Id}");

                // ------------------------------------------------------------
                // ⭐ SAFECRACK STEALTH SUPPRESSION (PATCHED)
                // Only suppress loud robbery if SafeCrack is actually running AND player is at the safe
                // ------------------------------------------------------------
                if (_ctx.SafeCrack != null &&
                    _ctx.SafeCrack.IsRunning &&
                    store.SafePos != Vector3.Zero &&
                    player.Position.DistanceTo(store.SafePos) < 2.0f)
                {
                    DebugLogger.Trace($"[RobberySystem] Suppressed — SafeCrack active near safe for store {store.Id}");

                    store.SilentRobbery = true;
                    store.AlarmTriggered = false;
                    store.ClerkCallingPolice = false;
                    store.SilentAlarmPressed = false;
                    store.HeatLevel = 0;

                    return;
                }

                // ------------------------------------------------------------
                // START LOUD ROBBERY
                // ------------------------------------------------------------
                StartRegisterRobbery(store);

                // ⭐ Mark robbery as globally active (required for StalkerSystem)
                _ctx.SetRobberyActive(true);

                if (_ctx.Config.EnableMessages)
                    _ctx.Ui.ShowNotification("~o~Robbery started!");

                // Subtitle #1
                _ctx.Ui.ShowSubtitle("Rob the store and escape.", 3000);

                if (_ctx.Config.EnableStalkerMsg)
                    _ctx.Stalker.QueueRobberyMessage();

                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "TIMER_STOP", "HUD_MINI_GAME_SOUNDSET");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.TryStartRegisterRobbery", ex);
            }
        }

        // ------------------------------------------------------------
        // REGISTER ROBBERY INITIALIZATION (FULLY PATCHED — NON-BLOCKING)
        // ------------------------------------------------------------
        private void StartRegisterRobbery(TrackedStore store)
        {
            try
            {
                // ⭐ CRITICAL FIX — RESET COMPLETION FLAGS FOR NEW ROBBERY
                store.RobberyCompleted = false;     // <—— REQUIRED FIX
                store.RobberyEnded = false;         // <—— ensures timer + logic run
                                                    // PendingCompletion will be set below
                                                    // PendingPayout is intentionally NOT reset here (silent robbery may add payout early)

                store.IsRobbed = true;
                store.IsRobberyActive = true; // ⭐ keep in sync with clerk/police/camera systems
                store.RobberyStartUtc = DateTime.UtcNow;
                store.PendingCompletion = true;
                store.ClerkSurrenderStage = 0;

                // ⭐ Respect SilentRobbery flag (stealth mode)
                if (store.SilentRobbery)
                {
                    DebugLogger.Trace($"[RobberySystem] SilentRobbery active — suppressing alarms for store {store.Id}");
                    store.AlarmTriggered = false;
                    store.ClerkCallingPolice = false;
                    store.SilentAlarmPressed = false;
                    store.HeatLevel = 0;
                }

                // Calculate payout
                int payout = _ctx.Rng.Next(_ctx.Config.RegisterMinAmount, _ctx.Config.RegisterMaxAmount + 1);
                payout = (int)(payout * _ctx.Config.PayoutMultiplier);
                store.PendingPayout += payout;

                DebugLogger.Info($"[ROBBERY] Register robbery payout: store={store.Id}, payout={payout}");

                // Subtitle #1
                _ctx.Ui.ShowSubtitle("Rob the store and escape.", 3000);

                // ⭐ Non-blocking follow-up subtitle for safe
                if (store.SafePos != Vector3.Zero)
                {
                    store.NextSafeSubtitleUtc = DateTime.UtcNow.AddMilliseconds(3200);
                    _ctx.Ui.ShowSubtitle("There is a safe in the office — crack it too.", 4000);
                }

                // Save state + update blip
                _ctx.SaveStoreState(store);

                if (_ctx.Config.EnableBlips)
                    _ctx.Cooldowns.UpdateStoreBlip(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.StartRegisterRobbery", ex);
            }
        }

        // ------------------------------------------------------------
        // ROBBERY TIMER (FULLY PATCHED — SILENT SAFE + POST‑COMPLETION SAFE)
        // ------------------------------------------------------------
        private void UpdateRobberyTimer(TrackedStore store)
        {
            try
            {
                // ⭐ UI SAFETY GUARD — NEVER UPDATE TIMER IF UI SHOULD BE HIDDEN
                if (StoreContext.GlobalUi.IsBannerActive)
                {
                    StoreContext.GlobalUi.ClearTimer();
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ HARD STOP — ROBBERY ENDED
                // ------------------------------------------------------------
                if (store.RobberyEnded)
                {
                    StoreContext.GlobalUi.ClearTimer();
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ SAFETY FIX — INVALID START TIME GUARD
                // Prevents instant-expire timer if RobberyStartUtc was never set
                // ------------------------------------------------------------
                if (store.RobberyStartUtc == DateTime.MinValue)
                {
                    StoreContext.GlobalUi.ClearTimer();
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ DEBUG ESCAPE — NO TIMER, NO POLICE
                // ------------------------------------------------------------
                if (_debugEscapeActive)
                {
                    StoreContext.GlobalUi.ClearTimer();
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ PAUSE TIMER DURING SAFECRACK (DO NOT CLEAR UI)
                // ------------------------------------------------------------
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    // Do NOT clear timer — SafeCrack manages its own UI
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ SILENT ROBBERY — NO ROBBERY TIMER, BUT DO NOT TOUCH UI
                // (SafeCrack uses the same GlobalUi timer, so never ClearTimer here)
                // ------------------------------------------------------------
                if (store.SilentRobbery)
                {
                    // Just skip robbery timer logic; SafeCrackController controls the timer text.
                    return;
                }

                // ⭐ DO NOT UPDATE TIMER DURING COOLDOWN
                if (store.CooldownActive)
                {
                    StoreContext.GlobalUi.ClearTimer();
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ CALCULATE REMAINING TIME
                // ------------------------------------------------------------
                double elapsed = (DateTime.UtcNow - store.RobberyStartUtc).TotalSeconds;
                int remaining = _ctx.Config.RobberyTimeLimit - (int)elapsed;

                // ------------------------------------------------------------
                // ⭐ TIMER EXPIRED (ONLY IF ROBBERY STILL ACTIVE)
                // ------------------------------------------------------------
                if (remaining <= 0)
                {
                    DebugLogger.Info($"[TIMER] Robbery timer expired for store {store.Id}");

                    // Only trigger timer-based police if NO other alarm fired
                    if (!store.AlarmTriggered)
                        TriggerPoliceIfNeeded(store);

                    // ⭐ CRITICAL FIX — HARD-END THE ROBBERY SO THE LOOP STOPS
                    store.RobberyEnded = true;                 // master kill switch
                    store.IsRobbed = false;                    // stops UpdateRobbery robbery branch
                    store.IsRobberyActive = false;             // stops all robbery logic
                    store.PendingCompletion = false;           // prevents re-entry into completion
                    store.RobberyStartUtc = DateTime.MinValue; // timer cannot compute elapsed

                    StoreContext.GlobalUi.ClearTimer();
                    return;
                }

                // ------------------------------------------------------------
                // ⭐ UPDATE UI ONCE PER SECOND
                // ------------------------------------------------------------
                if (Game.GameTime - _lastTimerUpdate > 1000)
                {
                    int mm = remaining / 60;
                    int ss = remaining % 60;

                    StoreContext.GlobalUi.SetTimerText($" Police in: {mm:00}:{ss:00}", remaining);
                    _lastTimerUpdate = Game.GameTime;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.UpdateRobberyTimer", ex);
            }
        }

        private void TriggerPoliceIfNeeded(TrackedStore store)
        {
            try
            {
                // ⭐ Debug override — completely disable timer-based police
                if (_ctx.Police.SuppressPoliceForDebug)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded suppressed for debug");
                    return;
                }

                // ⭐ PATCH 8C — Suppress after robbery ended
                if (store.RobberyEnded)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded suppressed — robbery ended");
                    return;
                }

                // ⭐ PATCH 8C — Suppress during cooldown
                if (store.CooldownActive)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded suppressed — cooldown active");
                    return;
                }

                // ⭐ PATCH 8C — Suppress during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded suppressed — SafeCrack active");
                    return;
                }

                // ⭐ PATCH 8C — Suppress during SilentRobbery
                if (store.SilentRobbery)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded suppressed — SilentRobbery active");
                    return;
                }

                // ⭐ If any other alarm already fired, skip timer police
                if (store.AlarmTriggered)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded skipped — alarm already triggered");
                    return;
                }

                // ⭐ Must be an active robbery
                if (!store.IsRobberyActive)
                {
                    DebugLogger.Info("TriggerPoliceIfNeeded skipped — robbery not active");
                    return;
                }

                StoreContext.GlobalUi.ClearTimer();

                if (store.PlayerMaskedAtStart)
                {
                    Game.Player.WantedLevel = 1;

                    if (_ctx.Config.EnableMessages)
                        _ctx.Ui.ShowNotification("~y~Police searching the area.");
                }
                else
                {
                    Game.Player.WantedLevel = 2;

                    if (_ctx.Config.EnableMessages)
                        _ctx.Ui.ShowNotification("~r~Police alerted!");
                }

                // ⭐ PATCH 8C — SAFE HEAT INCREMENT
                store.AlarmTriggered = true;
                store.HeatLevel += 1;

                DebugLogger.Info($"Police triggered by timer for store {store.Id}, heat={store.HeatLevel}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.TriggerPoliceIfNeeded", ex);
            }
        }

        // ------------------------------------------------------------
        // CAMERA-BASED ALARM (Patched: Dead-body only, ignore knockouts)
        // ------------------------------------------------------------
        private void CheckCameraTriggeredAlarm(TrackedStore store)
        {
            try
            {
                // ⭐ Disable camera alarms during debug escape
                if (_debugEscapeActive)
                    return;

                // ⭐ Ignore if default clerk was replaced safely
                if (store.Clerk == null || !store.Clerk.Exists() || !_ctx.Clerks.IsOurClerk(store.Clerk))
                    return;

                if (store.AlarmTriggered)
                    return;

                if (!_ctx.Config.EnableCameras)
                    return;

                // ------------------------------------------------------------
                // ⭐ NEW: Ignore knocked-out clerks (ragdoll but alive)
                // ------------------------------------------------------------
                if (store.Clerk != null && store.Clerk.Exists())
                {
                    if (!store.Clerk.IsDead && store.Clerk.IsRagdoll)
                    {
                        DebugLogger.Trace(
                            $"Camera ignored knocked-out clerk at store {store.Id} (ragdoll but alive)"
                        );
                        return;
                    }
                }

                // ------------------------------------------------------------
                // ⭐ DEAD CLERK DETECTION (ONLY trigger on actual death)
                // ------------------------------------------------------------
                int count = store.Cameras.Count;
                for (int i = 0; i < count; i++)
                {
                    CameraData cam = store.Cameras[i];

                    if (cam.Destroyed)
                        continue;

                    // ⭐ Camera sees dead clerk (NOT knocked out)
                    if (store.ClerkDeathHandled && !store.ClerkKilledWithGun)
                    {
                        DebugLogger.Info($"Camera detected dead clerk at store {store.Id}");

                        Game.Player.WantedLevel = 2;
                        store.AlarmTriggered = true;

                        if (_ctx.Config.EnableMessages)
                            _ctx.Ui.ShowNotification("~r~Camera detected the dead clerk!");

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.CheckCameraTriggeredAlarm", ex);
            }
        }

        // ------------------------------------------------------------
        // SPAWN LOOT BAG (PATCH K — Final Safety Layer)
        // ------------------------------------------------------------
        public void SpawnLootBag(TrackedStore store, Ped clerk)
        {
            try
            {
                if (store == null || clerk == null || !clerk.Exists())
                    return;
               
                // ⭐ Correct model: white trash bag
                Model bagModel = new Model("prop_cs_rub_binbag_01");

                if (!bagModel.IsValid || !bagModel.Request(2000))
                {
                    DebugLogger.Warn($"[ROBBERY] Failed to load trash bag model for store {store.Id}. Blocked.");
                    return;
                }

                // ⭐ Correct drop position
                Vector3 dropPos =
                    clerk.Position +
                    (clerk.ForwardVector * 1.35f) +
                    new Vector3(0f, 0f, -0.85f);

                Prop bag = World.CreateProp(
                    bagModel,
                    dropPos,
                    true,
                    true
                );

                if (bag == null || !bag.Exists())
                {
                    DebugLogger.Warn($"[ROBBERY] Bag spawn failed for store {store.Id}.");
                    return;
                }

                bag.IsPersistent = true;
                bag.IsPositionFrozen = false;

                // Store reference
                store.LootBag = bag;

                DebugLogger.Info($"[ROBBERY] Spawned BLACK TRASH BAG for store {store.Id} at {dropPos}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.SpawnLootBag (PATCH K)", ex);
            }
        }

        // ------------------------------------------------------------
        // CHECK LEAVING EARLY (Distance‑Limited Safe Warning)
        // ------------------------------------------------------------
        private void CheckLeavingEarly(TrackedStore store, Ped player)
        {
            try
            {
                if (store == null || player == null || !player.Exists())
                    return;

                if (store.StorePos == Vector3.Zero || store.Radius <= 0f)
                    return;

                if (!store.IsRobbed || store.RobberyEnded || store.CooldownActive)
                    return;

                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                float distToStore = player.Position.DistanceTo(store.StorePos);

                // ⭐ NEW: Only warn if player is within 15 meters of the store
                if (distToStore > 15f)
                    return; // stop warning entirely once they are far enough

                // ⭐ Safe not cracked yet → warn if they step too far away
                if (!store.SafeCracked &&
                    store.SafePos != Vector3.Zero &&
                    !store.CooldownActive)
                {
                    if (distToStore > 10f) // your original threshold
                    {
                        _ctx.Ui.ShowNotification("~y~Don't leave yet! Crack the safe to finish the robbery.");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.CheckLeavingEarly (PATCH O SAFE)", ex);

                if (store != null)
                {
                    store.RobberyEnded = true;
                    store.IsRobbed = false;
                }
            }
        }

        // ------------------------------------------------------------
        // FINAL EARLY COMPLETION (FULLY PATCHED & SILENT‑SAFE)
        // ------------------------------------------------------------
        private void CheckEarlyEscapeSuccess(TrackedStore store, Ped player)
        {
            try
            {
                // ⭐ Never run early escape during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ NEVER allow early escape during surrender
                if (store.ClerkSurrender || store.ClerkSurrenderStage > 0)
                    return;

                // ⭐ Debug override
                if (_debugEscapeActive && store.Id == _debugEscapeStoreId)
                {
                    float distdebug = player.Position.DistanceTo(store.StorePos);

                    if (distdebug < _ctx.Config.EscapeDistance)
                    {
                        _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                        if (_ctx.Config.EnableStalkerMsg)
                            _ctx.Stalker.QueueEscapeMessage();

                        _ctx.Stalker.TryTriggerCall();
                        return;
                    }

                    DebugLogger.Info($"[ROBBERY] Debug escape success at store {store.Id}");

                    // ⭐ FULL STATE RESET
                    store.RobberyEnded = true;
                    store.IsRobbed = false;
                    store.IsRobberyActive = false;
                    store.PendingCompletion = false;
                    store.RobberyStartUtc = DateTime.MinValue;

                    AwardPayout(store);
                    BeginCooldown(store);
                    return;
                }

                // ⭐ Must have robbed register + cracked safe
                if (!store.IsRobbed || !store.SafeCracked)
                    return;

                // ⭐ No early escape if alarm triggered
                if (store.AlarmTriggered)
                    return;

                // ⭐ Must lose cops first
                if (Game.Player.WantedLevel > 0)
                {
                    _ctx.Ui.ShowSubtitle("Escape the area & lose the cops.", 3000);
                    if (_ctx.Config.EnableStalkerMsg)
                        _ctx.Stalker.QueueEscapeMessage();

                    _ctx.Stalker.TryTriggerCall();
                    return;
                }

                float dist = player.Position.DistanceTo(store.StorePos);

                // ⭐ All cameras must be destroyed
                bool allCamsDown = true;
                foreach (var cam in store.Cameras)
                {
                    if (!cam.Destroyed)
                    {
                        allCamsDown = false;
                        break;
                    }
                }

                // ⭐ Early escape success
                if (dist > _ctx.Config.EscapeDistance && allCamsDown)
                {
                    DebugLogger.Info($"[ROBBERY] Early escape success at store {store.Id}");

                    // ⭐ FULL STATE RESET
                    store.RobberyEnded = true;
                    store.IsRobbed = false;
                    store.IsRobberyActive = false;
                    store.PendingCompletion = false;
                    store.RobberyStartUtc = DateTime.MinValue;

                    AwardPayout(store);
                    BeginCooldown(store);
                    return;
                }

                // ⭐ Still inside escape radius
                _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.CheckEarlyEscapeSuccess", ex);
            }
        }

        // ------------------------------------------------------------
        // FINAL COMPLETION (FULLY PATCHED & SILENT‑SAFE)
        // ------------------------------------------------------------
        private void CompleteRobbery(TrackedStore store, Ped player)
        {
            try
            {
                if (store == null || player == null || !player.Exists())
                    return;

                // ⭐ If store has a safe, require it to be cracked
                if (store.SafePos != Vector3.Zero && !store.SafeCracked)
                {
                    _ctx.Ui.ShowSubtitle("Crack the safe to finish the robbery.", 3000);
                    return;
                }

                // ⭐ Must have pending completion + payout
                if (!store.PendingCompletion || store.PendingPayout <= 0)
                    return;

                // ⭐ Loud robberies must lose cops first
                if (!store.SilentRobbery && Game.Player.WantedLevel > 0)
                {
                    _ctx.Ui.ShowSubtitle("Escape the area & lose the cops.", 3000);
                    if (_ctx.Config.EnableStalkerMsg)
                        _ctx.Stalker.QueueEscapeMessage();

                    _ctx.Stalker.TryTriggerCall();
                    return;
                }

                // ⭐ Debug escape path
                if (_debugEscapeActive && store.Id == _debugEscapeStoreId)
                {
                    float distdebug = player.Position.DistanceTo(store.StorePos);

                    if (distdebug > _ctx.Config.EscapeDistance)
                    {
                        _ctx.Ui.ShowSubtitle("Robbery complete! You escaped the area.", 3000);
                        if (_ctx.Config.EnableStalkerMsg)
                            _ctx.Stalker.QueueEscapeMessage();

                        _ctx.Stalker.TryTriggerCall();

                        DebugLogger.Info($"Robbery completion (debug escape) for store {store.Id}");

                        // ⭐ FULL STATE RESET
                        store.RobberyEnded = true;
                        store.IsRobbed = false;
                        store.IsRobberyActive = false;
                        store.PendingCompletion = false;
                        store.RobberyStartUtc = DateTime.MinValue;

                        _ctx.Ui.ClearTimer();
                        _ctx.SetRobberyActive(false);

                        AwardPayout(store);
                        BeginCooldown(store);
                        return;
                    }

                    // Still inside radius → just tell player to leave
                    _ctx.Ui.ShowSubtitle("Escape the area to finish the debug robbery.", 3000);
                    return;
                }

                float dist = player.Position.DistanceTo(store.StorePos);

                // ⭐ Must actually escape radius to complete
                if (dist <= _ctx.Config.EscapeDistance)
                {
                    // Still inside → robbery is STILL ACTIVE, do NOT reset anything
                    _ctx.Ui.ShowSubtitle("Robbery complete! Escape the area.", 3000);
                    return;
                }

                DebugLogger.Info($"[ROBBERY] Robbery completion triggered for store {store.Id}");

                // ------------------------------------------------------------
                // ⭐ CRITICAL FIX — STOP TIMER + STOP ALL ROBBERY LOGIC
                // ------------------------------------------------------------
                store.RobberyEnded = true;
                store.IsRobbed = false;
                store.IsRobberyActive = false;
                store.PendingCompletion = false;
                store.RobberyStartUtc = DateTime.MinValue;

                // ⭐ Clear UI timer immediately
                _ctx.Ui.ClearTimer();

                // ⭐ Robbery is fully complete — disable global robbery flag
                _ctx.SetRobberyActive(false);

                // ------------------------------------------------------------
                // PAYOUT + COOLDOWN
                // ------------------------------------------------------------
                AwardPayout(store);
                BeginCooldown(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.CompleteRobbery", ex);
            }
        }

        // ------------------------------------------------------------
        // PATCH 11 SUPPORT — Finalize payout (collection mode)
        // ------------------------------------------------------------
        public void FinalizePayout(TrackedStore store)
        {
            try
            {
                int payout = store.PendingPayout;
                if (payout <= 0)
                    return;

                // Instead of paying the player, record the collected amount
                store.CollectedPayout += payout;

                DebugLogger.Info($"[ROBBERY] Collected payout of ${payout} for store {store.Id} (awaiting successful escape)");

                // Reset pending payout so it isn't double-counted
                store.PendingPayout = 0;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.FinalizePayout", ex);
            }
        }

        // ------------------------------------------------------------
        // PAYOUT (FULLY PATCHED + PATCH 1 APPLIED)
        // ------------------------------------------------------------
        private void AwardPayout(TrackedStore store)
        {
            try
            {
                // Never award payout during SafeCrack
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ⭐ HARD STOP — prevent robbery loop from running after payout
                store.RobberyEnded = true;
                store.IsRobbed = false;
                store.IsRobberyActive = false;
                store.PendingCompletion = false;
                store.RobberyStartUtc = DateTime.MinValue;

                // Stop active robbery state (harmless duplicate, can remove later)
                store.IsRobberyActive = false;

                bool wasDebugEscape = _debugEscapeActive;
                int payout = store.PendingPayout;

                // Debug escape → do NOT pay player
                if (!wasDebugEscape)
                {
                    Game.Player.Money += payout;
                    StoreContext.GlobalUi.ShowHeistPassedBanner("~o~MISSION COMPLETE", $"{payout}", $"{store.Name}");

                    // ⭐ Prevent Shop Menu UI from overwriting the banner
                    ShopMenuUI.BlockUIForSeconds(3);
                }
                else
                {
                    DebugLogger.Info($"[ROBBERY] Awarding payout: store={store.Id}, payout={payout}, DebugState={wasDebugEscape}");
                    _ctx.Ui.ShowNotification("~y~DEBUG STATE ESCAPE COMPLETED~n~(no actual payout).");
                    StoreContext.GlobalUi.ShowHeistPassedBanner("~o~MISSION COMPLETE", $"{payout}", $"{store.Name}");

                    // ⭐ Prevent Shop Menu UI from overwriting the banner
                    ShopMenuUI.BlockUIForSeconds(3);
                }

                // Reset robbery flags
                store.IsRobbed = false;
                store.PendingCompletion = false;
                store.PendingPayout = 0;

                store.LastRobbedUtc = DateTime.UtcNow;
                store.RobberyStartUtc = DateTime.MinValue;

                // Clear stealth mode
                store.SilentRobbery = false;
                store.AlarmTriggered = false;
                store.ClerkCallingPolice = false;
                store.SilentAlarmPressed = false;

                // Clear UI timer
                //StoreContext.GlobalUi.ClearTimer();

                // Persist state
                _ctx.SaveStoreState(store);

                if (_ctx.Config.EnableBlips)
                    _ctx.Cooldowns.UpdateStoreBlip(store);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.AwardPayout", ex);
            }
        }

        // ------------------------------------------------------------
        // COOLDOWN (FULLY PATCHED — FINAL VERSION)
        // ------------------------------------------------------------
        private void BeginCooldown(TrackedStore store)
        {
            try
            {
                if (store == null)
                    return;

                DebugLogger.Info($"[CoolDown] BeginCooldown({store.Id})");

                bool wasDebugEscape = _debugEscapeActive;

                // ------------------------------------------------------------
                // ⭐ CORE COOLDOWN FLAGS
                // ------------------------------------------------------------
                store.CooldownActive = true;
                store.LastRobbedUtc = DateTime.UtcNow;

                store.IsRobberyActive = false;
                store.PendingCompletion = false;

                // Real robbery vs debug escape
                store.IsRobbed = !wasDebugEscape;

                // ------------------------------------------------------------
                // ⭐ FULL RESET (SAFE VERSION OF DebugResetStore)
                // ------------------------------------------------------------
                store.SilentRobbery = false;
                store.AlarmTriggered = false;
                store.ClerkCallingPolice = false;
                store.SilentAlarmPressed = false;

                // Escalation flags
                store.RepeatRobberyEscalationApplied = false;
                store.MaskEscalationApplied = false;
                store.TimeEscalationApplied = false;
                store.FightEscalationApplied = false;

                // Clerk reaction state
                store.ClerkReacted = false;
                store.ClerkRecognizedPlayer = false;
                store.ClerkKilledWithGun = false;
                store.ClerkDeathHandled = false;

                // Stall state
                store.ClerkStalling = false;
                store.StallStartUtc = DateTime.MinValue;
                store.StallDurationMs = 0;

                // Safe state
                store.SafeCracked = false;

                // Remove leftover loot bag
                if (store.LootBag != null && store.LootBag.Exists())
                {
                    store.LootBag.Delete();
                    store.LootBag = null;
                }

                if (store.Clerk != null && store.Clerk.Exists())
                {
                    Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, store.Clerk);
                    store.Clerk.MarkAsNoLongerNeeded();
                    store.Clerk.Delete();
                    store.Clerk = null;
                }

                //if (store.RobberyEnded && !store.CooldownActive)
                //{
                //    _clerks.ResetClerkAfterRobbery(store);
                //}

                // ------------------------------------------------------------
                // ⭐ APPLY COOLDOWN VISUALS + SAVE
                // ------------------------------------------------------------
                store.TimesRobbed++;

                // ⭐ Remove any existing cooldown blocker before creating a new one
                _ctx.Cooldowns.RemoveCooldownBlocker(store);

                // ⭐ APPLY COOLDOWN VISUALS + SAVE
                _ctx.Cooldowns.ApplyCooldownBlocker(store);
                _ctx.Cooldowns.UpdateStoreBlip(store);
                _ctx.SaveStoreState(store);

                if (store.Blip != null && store.Blip.Exists() && _ctx.Config.EnableBlips)
                    _ctx.Blips.RefreshBlip(store.Id);

                // ------------------------------------------------------------
                // ⭐ DEBUG ESCAPE CLEANUP (KEEP DebugResetStore)
                // ------------------------------------------------------------
                if (wasDebugEscape)
                {
                    // ⭐ REQUIRED — ensures clean test state for next debug run
                    DebugResetStore(store);
                    DebugLogger.Info($"[ROBBERY] Debug State cleared after cooldown: store={store.Id}");

                    // Clear debug escape state
                    _debugEscapeActive = false;
                    _debugEscapeStoreId = -1;
                    _ctx.Police.SuppressPoliceForDebug = false;
                }

                // Banner + payout handled in AwardPayout()
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RobberySystem.BeginCooldown", ex);
            }
        }
    }
}
