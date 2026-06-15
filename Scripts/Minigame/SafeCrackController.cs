using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Config;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Systems;
using StoreRobberyEnhanced.UI;
using System;

namespace StoreRobberyEnhanced.Minigame
{
    internal class SafeCrackController
    {
        // ------------------------------------------------------------
        // DEPENDENCIES
        // ------------------------------------------------------------
        private readonly SafeCrackState _state;
        private readonly SafeCrackSettings _settings;
        private readonly UiHelpers _UiHelp;
        private readonly StoreContext _ctx;
        private readonly IniConfig _config;

        private readonly ISafeCrackLogic _logic;
        private readonly ISafeCrackInput _input;
        private readonly ISafeCrackUI _ui;
        private readonly ISafeCrackAnimation _anim;

        private readonly RobberySystem _robberySystem;
        private TrackedStore _store;

        // ------------------------------------------------------------
        // INTERNAL STATE
        // ------------------------------------------------------------
        private bool _playerFrozen;
        private int _savedCameraMode = -1;

        // ⭐ REQUIRED BY RobberySystem
        public bool IsRunning { get; private set; }

        public TrackedStore CurrentStore => _store;

        // ------------------------------------------------------------
        // STATE MANAGEMENT
        // ------------------------------------------------------------
        public void ResetState()
        {
            IsRunning = false;
            SuppressStoreSystems();
        }

        // ------------------------------------------------------------
        // CONSTRUCTOR
        // ------------------------------------------------------------
        public SafeCrackController(
            SafeCrackState state,
            SafeCrackSettings settings,
            ISafeCrackLogic logic,
            ISafeCrackInput input,
            ISafeCrackUI ui,
            ISafeCrackAnimation anim,
            UiHelpers uiHelp,
            RobberySystem robberySystem,
            StoreContext ctx,
            IniConfig config)
        {
            _state = state;
            _settings = settings;

            _logic = logic;
            _input = input;
            _ui = ui;
            _anim = anim;

            _UiHelp = uiHelp;
            _robberySystem = robberySystem;

            _ctx = ctx;
            _config = config;
        }

        // ------------------------------------------------------------
        // DEBUG MODE CHECK
        // ------------------------------------------------------------
        private bool IsDebugMode()
        {
            return DebugState.IsDebugMode;
        }

        // ------------------------------------------------------------
        // START MINIGAME
        // ------------------------------------------------------------
        public void Start(TrackedStore store, Vector3 safePos, float safeHeading, Ped player)
        {
            try
            {
                // 🔒 HARD GUARD: Prevent double-start
                if (_state.Active)
                {
                    DebugLogger.Info("[SafeCrack] Start() ignored — already active");
                    return;
                }

                _store = store;

                DebugLogger.Info("[SafeCrack] Start() called");

                // ------------------------------------------------------------
                // ⭐ Cooldown check
                // ------------------------------------------------------------
                if (_state.CooldownActive)
                {
                    int now = Game.GameTime;
                    if (now < _state.CooldownEndTime)
                    {
                        DebugLogger.Info("[SafeCrack] Start() blocked — cooldown active");
                        return;
                    }

                    _state.CooldownActive = false;
                }

                // ------------------------------------------------------------
                // ⭐ Reset state
                // ------------------------------------------------------------
                _state.Reset();
                _state.Active = true;
                _state.Completed = false;
                _state.Failed = false;
                _state.ConfirmRequested = false;

                _state.SafePos = safePos;
                _state.SafeHeading = safeHeading;
                _state.Player = player;
                _state.StartTime = Game.GameTime;
                _state.LastUpdateTime = Game.GameTime;

                // ------------------------------------------------------------
                // ⭐ RE‑ENABLE SAFETY UI DRAWING
                // ------------------------------------------------------------
                _ui.Enable();   // ← THIS IS THE FIX

                // ------------------------------------------------------------
                // ⭐ STEALTH ROBBERY MODE ENABLED
                // ------------------------------------------------------------
                SuppressStoreSystems();

                // ------------------------------------------------------------
                // ⭐ Save camera mode
                // ------------------------------------------------------------
                _savedCameraMode = Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE);

                // ------------------------------------------------------------
                // ⭐ Teleport player if needed
                // ------------------------------------------------------------
                float dist = player.Position.DistanceTo(safePos);
                if (dist > 0.6f)
                {
                    player.Position = safePos;
                    player.Heading = safeHeading;
                }

                for (int i = 0; i < 10; i++)
                    Script.Yield();

                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, player.Handle,
                    safePos.X, safePos.Y, safePos.Z, false, false, false);

                Script.Yield();

                // ------------------------------------------------------------
                // ⭐ Freeze player
                // ------------------------------------------------------------
                player.Task.ClearAllImmediately();
                player.IsPositionFrozen = true;
                _playerFrozen = true;

                // ------------------------------------------------------------
                // ⭐ Initialize logic + animation
                // ------------------------------------------------------------
                _logic.Initialize(_state, _settings);
                _anim.Begin(player, safePos, safeHeading);

                // ------------------------------------------------------------
                // ⭐ Initialize UI
                // ------------------------------------------------------------
                _ui.Draw(_state, _settings);

                DebugLogger.Info("[SafeCrack] Minigame started successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("SafeCrackController.Start", ex);
                _state.Active = false;
            }
        }

        // ------------------------------------------------------------
        // UPDATE LOOP (PATCHED FOR BANNER SAFETY)
        // ------------------------------------------------------------
        public void Update()
        {
            // If the minigame is not active, nothing to do
            if (!_state.Active)
                return;

            // ⭐ HARD STOP: If the heist banner is active, do NOT draw or update SafeCrack UI
            if (_UiHelp.IsBannerActive)
                return;

            DisableGameplayControls();

            int now = Game.GameTime;
            if (now - _state.LastUpdateTime < 0)
                return;

            _state.LastUpdateTime = now;

            // ------------------------------------------------------------
            // TIMER
            // ------------------------------------------------------------
            int elapsed = now - _state.StartTime;
            int total = _config.SafeCrackTimeSeconds * 1000;
            int remaining = (total - elapsed) / 1000;

            if (remaining < 0)
            {
                DebugLogger.Info("[SafeCrack] Timer expired → fail");

                // SAFECRACK‑ONLY COOLDOWN
                _state.CooldownActive = true;
                _state.CooldownEndTime = Game.GameTime + _settings.CooldownMs;

                // PLAYER NOTIFICATION
                _UiHelp.ShowSubtitle("~r~Time has run out. The safe is locked. Try again after cooldown.", 4000);

                // FAIL CLEANLY
                _state.Failed = true;
                FailAndResetStore();
                return;
            }

            if (now - _state.LastTimerUpdate > 1000)
            {
                StoreContext.GlobalUi.SetTimerText($"Safe time left: {remaining}", remaining);
                _state.LastTimerUpdate = now;
            }

            // ------------------------------------------------------------
            // ELIGIBILITY CHECK
            // ------------------------------------------------------------
            if (!_logic.ValidatePlayerStillEligible(_state))
            {
                DebugLogger.Info("[SafeCrack] Player no longer eligible → fail");
                FailAndResetStore();
                return;
            }

            // ------------------------------------------------------------
            // INPUT + LOGIC
            // ------------------------------------------------------------
            bool confirmHeld = _state.ConfirmRequested;

            _input.Process(_state, _settings);

            if (confirmHeld)
                _state.ConfirmRequested = true;

            _logic.Update(_state, _settings);

            // ------------------------------------------------------------
            // UI (PATCHED)
            // ------------------------------------------------------------
            // ⭐ Do NOT draw SafeCrack UI if banner is active
            if (!_UiHelp.IsBannerActive)
                _ui.Draw(_state, _settings);

            // ------------------------------------------------------------
            // COMPLETION / FAILURE
            // ------------------------------------------------------------
            if (_state.Completed)
            {
                FinishSuccess();
                return;
            }

            if (_state.Failed)
            {
                FailAndResetStore();
                return;
            }
        }

        // ------------------------------------------------------------
        // SUCCESS HANDLER
        // ------------------------------------------------------------
        private void FinishSuccess()
        {
            if (!_state.Active)
                return;

            _state.Completed = true;
            _state.Active = false;

            _anim.EndSuccess(_state.Player);

            // ⭐ Heavy final clunk sound
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "COLLECTED", "HUD_AWARDS");

            int payout = _logic.CalculatePayout(_settings);
            _state.Payout = payout;

            Function.Call(Hash.SET_CONTROL_SHAKE, 0, 250, 200);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            
            _UiHelp.ShowSubtitle("~g~Safe cracked!", 3000);

            SafeCrackEvents.OnSafeCracked(_state.SafePos, payout);

            if (_store != null)
            {
                _store.SafeCracked = true;

                // ⭐ Persist safe cracked state to INI
                _ctx.SaveStoreState(_store);   // or StoreDataManager.SaveStoreState(_store)

                payout = (int)(payout * _ctx.Config.PayoutMultiplier);
                _store.PendingPayout += payout;

                _store.SilentRobbery = false;
                _store.AlarmTriggered = false;
                _store.ClerkCallingPolice = false;
                _store.SilentAlarmPressed = false;
            }

            _state.CooldownActive = true;
            _state.CooldownEndTime = Game.GameTime + _settings.CooldownMs;

            Stop(true);
        }

        // ------------------------------------------------------------
        // FAILURE HANDLER (DEBUG‑SAFE)
        // ------------------------------------------------------------
        private void FailAndResetStore()
        {
            Stop(false);

            if (_store != null)
            {
                _store.SilentRobbery = false;

                if (IsDebugMode())
                {
                    DebugLogger.Info("[SafeCrack] Debug mode → DebugResetStore()");
                    _robberySystem.DebugResetStore(_store);
                }
                else
                {
                    DebugLogger.Info("[SafeCrack] Normal mode → NOT resetting store");
                }
            }
        }

        // ------------------------------------------------------------
        // STOP MINIGAME
        // ------------------------------------------------------------
        public void Stop(bool success)
        {
            _state.Active = false;
            StoreContext.Active.SafeState.Active = false;   // ⭐ CRITICAL FIX
            _state.ConfirmRequested = false;

            if (Game.IsLoading)
                success = true;

            if (_state.Player != null && _state.Player.Exists())
            {
                _state.Player.Task.ClearAll();
            }

            if (_playerFrozen && _state.Player != null && _state.Player.Exists())
            {
                _state.Player.IsPositionFrozen = false;
                _playerFrozen = false;
            }

            if (_savedCameraMode != -1)
            {
                Function.Call(Hash.SET_FOLLOW_PED_CAM_VIEW_MODE, _savedCameraMode);
                _savedCameraMode = -1;
            }

            Function.Call(Hash.STOP_CONTROL_SHAKE, 0);

            _anim.End(_state.Player, success);
            _ui.Clear();

            // ⭐ NEW: always kill the SafeCrack timer UI when the minigame stops
            StoreContext.GlobalUi.ClearTimer();

            RestoreControls();

            if (_store != null)
            {
                _store.SilentRobbery = false;
            }

            if (!success && !_state.Completed)
            {
                Function.Call(Hash.SET_CONTROL_SHAKE, 0, 300, 255);
                _UiHelp.ShowSubtitle("~r~You stopped cracking the safe. Try again.", 3000);
            }
        }

        // ------------------------------------------------------------
        // ABORT (DEBUG‑SAFE)
        // ------------------------------------------------------------
        public void Abort()
        {
            if (!_state.Active)
                return;

            Stop(false);

            if (_store != null)
            {
                _store.SilentRobbery = false;

                if (IsDebugMode())
                {
                    DebugLogger.Info("[SafeCrack] Debug mode → DebugResetStore()");
                    _robberySystem.DebugResetStore(_store);
                }
            }
        }

        // ------------------------------------------------------------
        // CONTROL HELPERS
        // ------------------------------------------------------------
        private void DisableGameplayControls()
        {
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Attack);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Aim);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Reload);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.MeleeAttack1);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.MeleeAttack2);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.VehicleAccelerate);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.VehicleBrake);

            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookLeftRight);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.LookUpDown);
        }

        private void RestoreControls()
        {
            Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, 0);

            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.SelectWeapon);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.CharacterWheel);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.Phone);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.FrontendPause);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.RadioWheelLeftRight);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.RadioWheelUpDown);
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)Control.Cover);
        }

        // ------------------------------------------------------------
        // STORE SUPPRESSION (STEALTH MODE)
        // ------------------------------------------------------------
        private void SuppressStoreSystems()
        {
            if (_store == null)
                return;

            _store.IsRobberyActive = true;
            _store.SilentRobbery = true;

            _store.AlarmTriggered = false;
            _store.ClerkCallingPolice = false;
            _store.SilentAlarmPressed = false;

            _store.HeatLevel = 0;
            _store.RepeatRobberyEscalationApplied = false;
            _store.MaskEscalationApplied = false;
            _store.FightEscalationApplied = false;
            _store.TimeEscalationApplied = false;

            DebugLogger.Info("[SafeCrack] Store systems suppressed for stealth safecrack");
        }
    }
}
