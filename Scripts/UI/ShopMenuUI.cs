using GTA;
using LemonUI.Elements;
using LemonUI.Menus;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;
using System.Drawing;
using static StoreRobberyEnhanced.Data.ShopItemData;

namespace StoreRobberyEnhanced.UI
{
    internal class ShopMenuUI
    {
        private readonly NativeMenu _menu;
        private readonly StoreContext _ctx;

        public NativeMenu Menu => _menu;
        public static DateTime UiBlockedUntil = DateTime.MinValue;

        public static void BlockUIForSeconds(int seconds)
        {
            UiBlockedUntil = DateTime.UtcNow.AddSeconds(seconds);
        }

        public ShopMenuUI(StoreContext ctx, TrackedStore store)
        {
            try
            {
                _ctx = ctx;

                // ============================================================
                // ⭐ HARD BLOCK: DO NOT INITIALIZE MENU DURING ROBBERY
                // Prevents UI creation during robbery phases.
                // ============================================================
                if (_ctx.AnyRobberyActive)
                {
                    DebugLogger.Trace("ShopMenuUI.ctor blocked: robbery active");
                    return;
                }

                // ============================================================
                // ⭐ HARD BLOCK: DO NOT INITIALIZE MENU DURING SAFECRACK
                // Prevents LemonUI texture creation during SafeCrack.
                // ============================================================
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    DebugLogger.Trace("ShopMenuUI.ctor blocked: SafeCrack running");
                    return;
                }

                // ============================================================
                // ⭐ HARD BLOCK: DO NOT INITIALIZE MENU WHEN UI IS BLOCKED
                // Protects Heist Passed banner and global UI states.
                // ============================================================
                if (DateTime.UtcNow < ShopMenuUI.UiBlockedUntil)
                {
                    DebugLogger.Trace("ShopMenuUI.ctor blocked: UI globally blocked");
                    return;
                }

                // ============================================================
                // ⭐ BUILD CORRECT SUBTITLE BASED ON STORE TYPE
                // ============================================================
                string subtitle = GetSubtitleForStore(store);

                // Remove store name text from banner, use dynamic subtitle
                _menu = new NativeMenu("", subtitle);

                // Add to global pool
                _ctx.MenuPool.Add(_menu);

                // ============================================================
                // ⭐ APPLY CORRECT ROCKSTAR BANNER BASED ON STORE TYPE
                // ============================================================
                _menu.Banner = new ScaledTexture(
                    new PointF(0f, 0f),
                    new SizeF(512f, 128f),
                    GetBannerTextureDict(store),
                    GetBannerTextureName(store)
                );

                // ============================================================
                // ⭐ BUILD MENU ITEMS
                // ============================================================
                BuildMenu();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.ctor: {ex}");
            }
        }

        // ============================================================
        // STORE NAME TO SUBTITLE MAPPING
        // ============================================================
        private string GetSubtitleForStore(TrackedStore store)
        {
            try
            {
                string name = store.Name.ToLower();

                if (name.Contains("rob"))
                    return "Rob's Liquor";

                if (name.Contains("ltd"))
                    return "LTD Gas Station";

                if (name.Contains("ace"))
                    return "Liquor Ace";

                // Default for all 24/7 stores
                return "24/7 Supermarket";
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.GetSubtitleForStore: {ex}");
                return "Store";
            }
        }

        // ============================================================
        // BANNER SELECTION
        // ============================================================
        private string GetBannerTextureDict(TrackedStore store)
        {
            try
            {
                string name = store.Name.ToLower();

                // LTD Gasoline
                if (name.Contains("ltd"))
                    return "shopui_title_gasstation";

                // Rob's Liquor (6 stores)
                if (name.Contains("rob"))
                    return "shopui_title_liquorstore2";

                // Ace Liquor (unique interior)
                if (name.Contains("ace"))
                    return "shopui_title_liquorstore";

                // Default 24/7
                return "shopui_title_conveniencestore";
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.GetBannerTextureDict: {ex}");
                return "shopui_title_conveniencestore";
            }
        }

        // Rockstar uses same dict + texture name for each store type, so we can reuse the dict name as the texture name
        private string GetBannerTextureName(TrackedStore store)
        {
            try
            {
                // Rockstar uses same dict + texture name
                return GetBannerTextureDict(store);
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.GetBannerTextureName: {ex}");
                return "shopui_title_conveniencestore";
            }
        }

        // ============================================================
        // MENU BUILDING
        // ============================================================
        private void BuildMenu()
        {
            try
            {
                foreach (var item in ShopItemDatabase.Items.Values)
                {
                    var menuItem = new NativeItem(item.Name, item.Description)
                    {
                        AltTitle = $"${item.Price}"   // Right‑aligned price
                    };

                    menuItem.Activated += (sender, args) =>
                    {
                        try
                        {
                            HandlePurchase(item);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Error($"ShopMenuUI.MenuItem.Activated: {ex}");
                        }
                    };

                    _menu.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.BuildMenu: {ex}");
            }
        }

        // ============================================================
        // SHOW THE MENU (called when player interacts with store)
        // ============================================================
        public void Show()
        {
            try
            {
                // ============================================================
                // ⭐ HARD BLOCK: DO NOT SHOW MENU DURING ANY ROBBERY
                // This prevents UI from appearing even if called externally.
                // ============================================================
                if (_ctx.AnyRobberyActive)
                {
                    DebugLogger.Trace("ShopMenuUI.Show blocked: robbery active");
                    return;
                }

                // ============================================================
                // ⭐ HARD BLOCK: DO NOT SHOW MENU DURING SAFECRACK
                // Prevents timer flicker and UI takeover.
                // ============================================================
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    DebugLogger.Trace("ShopMenuUI.Show blocked: SafeCrack running");
                    return;
                }

                // ============================================================
                // ⭐ HARD BLOCK: DO NOT SHOW MENU WHEN UI IS BLOCKED
                // Protects Heist Passed banner and other global UI states.
                // ============================================================
                if (DateTime.UtcNow < ShopMenuUI.UiBlockedUntil)
                {
                    DebugLogger.Trace("ShopMenuUI.Show blocked: UI globally blocked");
                    return;
                }

                // ============================================================
                // ⭐ SAFE SHOW
                // ============================================================
                _menu.Visible = true;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.Show: {ex}");
            }
        }

        // ============================================================
        // HIDE THE MENU (called when player walks away or forced close)
        // ============================================================
        public void Hide()
        {
            try
            {
                // ============================================================
                // ⭐ ALWAYS HIDE MENU DURING ROBBERY
                // Ensures no shop UI remains visible once robbery begins.
                // ============================================================
                if (_ctx.AnyRobberyActive)
                {
                    if (_menu.Visible)
                        DebugLogger.Trace("ShopMenuUI.Hide: forced hide due to robbery");
                    _menu.Visible = false;
                    return;
                }

                // ============================================================
                // ⭐ ALWAYS HIDE MENU DURING SAFECRACK
                // Prevents UI flicker and input conflicts.
                // ============================================================
                if (_ctx.SafeCrack != null && _ctx.SafeCrack.IsRunning)
                {
                    if (_menu.Visible)
                        DebugLogger.Trace("ShopMenuUI.Hide: forced hide due to SafeCrack");
                    _menu.Visible = false;
                    return;
                }

                // ============================================================
                // ⭐ ALWAYS HIDE MENU WHEN UI IS BLOCKED
                // Protects Heist Passed banner and other global UI states.
                // ============================================================
                if (DateTime.UtcNow < ShopMenuUI.UiBlockedUntil)
                {
                    if (_menu.Visible)
                        DebugLogger.Trace("ShopMenuUI.Hide: forced hide due to UI block");
                    _menu.Visible = false;
                    return;
                }

                // ============================================================
                // ⭐ NORMAL HIDE
                // ============================================================
                _menu.Visible = false;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.Hide: {ex}");
            }
        }

        // ============================================================
        // PURCHASE HANDLING
        // ============================================================
        private void HandlePurchase(ShopItemData item)
        {
            try
            {
                if (Game.Player.Money < item.Price)
                {
                    _ctx.Ui.ShowSubtitle("~r~Not enough money.");
                    return;
                }

                Game.Player.Money -= item.Price;

                // Hand off to ShopConsumeSystem for animation + effects
                _ctx.ConsumeSystem.QueueItem(item.Id);

                _ctx.Ui.ShowSubtitle(
                    $"Purchased ~g~{item.Name}~w~ for ~g~${item.Price}"
                );

                _menu.Visible = false;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopMenuUI.HandlePurchase: {ex}");
            }
        }        
    }
}
