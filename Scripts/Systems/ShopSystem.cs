using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using StoreRobberyEnhanced.UI;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Systems
{
    internal class ShopSystem
    {
        private readonly StoreContext _ctx;

        // One menu per store
        private readonly Dictionary<int, ShopMenuUI> _menus = new Dictionary<int, ShopMenuUI>();

        private const float INTERACT_DISTANCE = 2.0f;

        public ShopSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.ctor: {ex}");
            }
        }

        // ============================================================
        // TICK HANDLING
        // ============================================================
        public void Tick()
        {
            try
            {
                // ============================================================
                // ⭐ GLOBAL UI BLOCKER (Heist Passed banner protection)
                // Prevent ANY shop UI from drawing while the banner is active.
                // ============================================================
                if (DateTime.UtcNow < ShopMenuUI.UiBlockedUntil)
                    return;

                // ============================================================
                // ⭐ SAFETY: BLOCK SHOP UI DURING SAFECRACK
                // Prevents SafeCrack timer flicker caused by LemonUI drawing.
                // ============================================================
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return;

                // ============================================================
                // ⭐ BLOCK SHOP MENU DURING ANY ACTIVE ROBBERY
                // Prevents clerk interaction from interrupting robbery flow.
                // ============================================================
                if (_ctx.AnyRobberyActive)
                    return;

                // ============================================================
                // ⭐ PROCESS LEMONUI (only when absolutely safe)
                // LemonUI must NOT process during robbery or SafeCrack.
                // ============================================================
                if (!_ctx.AnyRobberyActive &&
                    (_ctx.SafeCrack == null || !_ctx.SafeCrack.IsRunning) &&
                    DateTime.UtcNow >= ShopMenuUI.UiBlockedUntil)
                {
                    _ctx.MenuPool.Process();
                }
                else
                {
                    // Debug trace for safety
                    DebugLogger.Trace("ShopSystem.Tick: MenuPool.Process skipped (unsafe state)");
                }

                var player = Game.Player.Character;
                if (!player.Exists())
                    return;

                // ------------------------------------------------------------
                // CLOSE MENU INPUT (ESC or B)
                // ------------------------------------------------------------
                bool cancelKey = Game.IsKeyPressed(System.Windows.Forms.Keys.Escape);
                bool cancelPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)Control.PhoneCancel);

                if (cancelKey || cancelPad)
                {
                    CloseAllMenus();
                    return;
                }

                // ------------------------------------------------------------
                // If any menu is open, do not show prompts
                // ------------------------------------------------------------
                if (IsAnyMenuOpen())
                    return;

                // ------------------------------------------------------------
                // STORE INTERACTION CHECK (Interior‑based)
                // ------------------------------------------------------------
                int playerInterior = Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, player.Handle);

                foreach (var store in _ctx.Stores)
                {
                    try
                    {
                        // ⭐ First: interior must match
                        if (store.InteriorId != playerInterior)
                            continue;

                        // ⭐ Second: BLOCK SHOP MENU IF ROBBERY ACTIVE AT THIS STORE
                        if (store.IsRobberyActive)
                            continue;

                        // ⭐ Third: check distance to clerk inside that interior
                        float dist = player.Position.DistanceTo(store.ClerkPos);

                        if (dist <= INTERACT_DISTANCE)
                        {
                            // ⭐ Prevent help text during SafeCrack (extra safety)
                            if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                                return;

                            _ctx.Ui.ShowHelpText("Press ~INPUT_FRONTEND_ACCEPT~ to shop");

                            bool interactKey = Game.IsKeyPressed(System.Windows.Forms.Keys.E);
                            bool interactPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)Control.FrontendAccept);

                            if (interactKey || interactPad)
                            {
                                OpenMenu(store);
                            }

                            return; // Only show prompt for the correct store
                        }
                    }
                    catch (Exception exStore)
                    {
                        DebugLogger.Error($"ShopSystem.Tick.StoreLoop: {exStore}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.Tick: {ex}");
            }
        }

        // ============================================================
        // MENU HANDLING
        // ============================================================
        private void OpenMenu(TrackedStore store)
        {
            try
            {
                // ============================================================
                // ⭐ HARD BLOCK: DO NOT OPEN MENU DURING ANY ROBBERY
                // Even if Tick() fails to block, this prevents UI conflicts.
                // ============================================================
                if (_ctx.AnyRobberyActive)
                {
                    DebugLogger.Trace("ShopSystem.OpenMenu blocked: robbery active");
                    return;
                }

                // ============================================================
                // ⭐ HARD BLOCK: DO NOT OPEN MENU IF THIS STORE IS BEING ROBBED
                // Prevents clerk interaction during robbery phases.
                // ============================================================
                if (store.IsRobberyActive)
                {
                    DebugLogger.Trace($"ShopSystem.OpenMenu blocked: store {store.Id} robbery active");
                    return;
                }

                // ============================================================
                // ⭐ HARD BLOCK: DO NOT OPEN MENU DURING SAFECRACK
                // Prevents SafeCrack timer flicker and UI takeover.
                // ============================================================
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    DebugLogger.Trace("ShopSystem.OpenMenu blocked: SafeCrack running");
                    return;
                }

                // ============================================================
                // ⭐ CREATE MENU IF NOT ALREADY BUILT
                // ============================================================
                if (!_menus.TryGetValue(store.Id, out var menu))
                {
                    menu = new ShopMenuUI(_ctx, store);
                    _menus.Add(store.Id, menu);
                }

                // ============================================================
                // ⭐ SHOW MENU (SAFE)
                // ============================================================
                menu.Show();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.OpenMenu: {ex}");
            }
        }

        // ============================================================
        // CLOSE ALL MENUS
        // ============================================================
        private void CloseAllMenus()
        {
            try
            {
                // ============================================================
                // ⭐ HARD BLOCK: CLOSE MENUS DURING ROBBERY
                // Ensures no shop UI remains open once robbery begins.
                // ============================================================
                if (_ctx.AnyRobberyActive)
                    DebugLogger.Trace("ShopSystem.CloseAllMenus: closing due to robbery state");

                // ============================================================
                // ⭐ HARD BLOCK: CLOSE MENUS DURING SAFECRACK
                // Prevents UI flicker and input conflicts.
                // ============================================================
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    DebugLogger.Trace("ShopSystem.CloseAllMenus: closing due to SafeCrack");

                // ============================================================
                // ⭐ CLOSE ALL LEMONUI MENUS
                // ============================================================
                foreach (var menu in _menus.Values)
                {
                    if (menu.Menu.Visible)
                    {
                        menu.Menu.Visible = false;
                        DebugLogger.Trace("ShopSystem.CloseAllMenus: menu closed");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.CloseAllMenus: {ex}");
            }
        }

        // ============================================================
        // CHECK IF ANY MENU IS CURRENTLY OPEN
        // ============================================================
        private bool IsAnyMenuOpen()
        {
            try
            {
                // ============================================================
                // ⭐ TREAT UI AS "OPEN" DURING ROBBERY
                // Prevents prompts and interaction during robbery phases.
                // ============================================================
                if (_ctx.AnyRobberyActive)
                    return true;

                // ============================================================
                // ⭐ TREAT UI AS "OPEN" DURING SAFECRACK
                // Prevents help text flicker and input conflicts.
                // ============================================================
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                    return true;

                // ============================================================
                // ⭐ TREAT UI AS "OPEN" WHEN UI IS BLOCKED
                // Ensures no prompts appear during Heist Passed banner.
                // ============================================================
                if (DateTime.UtcNow < ShopMenuUI.UiBlockedUntil)
                    return true;

                // ============================================================
                // ⭐ CHECK ALL LEMONUI MENUS
                // ============================================================
                foreach (var menu in _menus.Values)
                {
                    if (menu.Menu.Visible)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopSystem.IsAnyMenuOpen: {ex}");
                return false;
            }
        }
    }
}
