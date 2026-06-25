using GTA;
using GTA.Native;
using StoreRobberyEnhanced.Config;

namespace StoreRobberyEnhanced.Minigame
{
    /// <summary>
    /// Handles player input for the SafeCrack minigame.
    /// Converts keyboard/controller input into rotation + confirm/cancel.
    /// 
    /// ⭐ This version is fully compatible with the patched SafeCrackController:
    /// - ConfirmRequested is preserved across frames
    /// - Rotation uses correct control group (0)
    /// - Confirm uses FrontendAccept (A button)
    /// - Cancel uses PhoneCancel (B button)
    /// - Keyboard fallback preserved
    /// </summary>
    internal class SafeCrackInput : ISafeCrackInput
    {
        private const float ROTATION_STEP = 0.8f;   // Keyboard rotation
        private const float ROTATION_SLOW = 0.45f;  // Controller rotation

        public void Process(SafeCrackState state, SafeCrackSettings settings)
        {
            if (!state.Active)
                return;

            // Reset per tick
            state.RotationSpeed = 0f;
            state.ConfirmRequested = false;

            // ------------------------------------------------------------
            // KEYBOARD ROTATION (A / D)
            // ------------------------------------------------------------
            if (Game.IsKeyPressed(System.Windows.Forms.Keys.A))
                state.RotationSpeed = -ROTATION_STEP;
            else if (Game.IsKeyPressed(System.Windows.Forms.Keys.D))
                state.RotationSpeed = ROTATION_STEP;

            // ------------------------------------------------------------
            // CONTROLLER ROTATION (Left Stick X)
            // ⭐ Correct group: 0 (movement group)
            // ⭐ SafeCrackController does NOT disable MoveLeftRight
            // ------------------------------------------------------------
            float axisX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.MoveLeftRight);

            // If frozen, MoveLeftRight returns 0 — fallback to ScriptedFlyLeftRight
            if (System.Math.Abs(axisX) < 0.05f)
                axisX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.FlyLeftRight);

            if (System.Math.Abs(axisX) > 0.05f)
                state.RotationSpeed = axisX * ROTATION_SLOW;

            // ------------------------------------------------------------
            // CONFIRM INPUT (E or A)
            // ⭐ Correct control: FrontendAccept (A button)
            // ⭐ SafeCrackController preserves ConfirmRequested across frames
            // ------------------------------------------------------------
            bool confirmKey = Game.IsKeyPressed(System.Windows.Forms.Keys.E);
            bool confirmPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)Control.FrontendAccept);

            if (confirmKey || confirmPad)
                state.ConfirmRequested = true;

            // ------------------------------------------------------------
            // CANCEL INPUT (ESC or B)
            // ⭐ Correct control: PhoneCancel (B button)
            // ------------------------------------------------------------
            bool cancelKey = Game.IsKeyPressed(System.Windows.Forms.Keys.Escape);
            bool cancelPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)Control.PhoneCancel);

            if (cancelKey || cancelPad)
                state.Failed = true;
        }
    }
}
