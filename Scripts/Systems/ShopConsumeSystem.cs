using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.UI;

namespace StoreRobberyEnhanced.Systems
{
    internal class ShopConsumeSystem
    {
        private readonly StoreContext _ctx;

        // Queue of items waiting to be consumed
        private readonly Queue<string> _queue = new Queue<string>();

        // Cooldowns to prevent spam
        private readonly Dictionary<string, int> _cooldowns = new Dictionary<string, int>();
        private const int CONSUME_COOLDOWN_MS = 3000;

        public ShopConsumeSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                DebugLogger.Info("[SHOP] ShopConsumeSystem initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopConsumeSystem.ctor: {ex}");
            }
        }

        // Called by ShopMenuUI
        public void QueueItem(string itemId)
        {
            try
            {
                _queue.Enqueue(itemId);
                DebugLogger.Info($"[SHOP] ShopConsumeSystem: Queued item '{itemId}'");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopConsumeSystem.QueueItem: {ex}");
            }
        }

        // ============================================================
        // TICK HANDLING
        // ============================================================
        public void Tick()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

            // Nothing to consume
                if (_queue.Count == 0)
                    return;

                string itemId = _queue.Peek();

            // Cooldown check
                if (!IsReady(itemId))
                    return;

            // Begin consumption
                Consume(itemId);

            // Apply cooldown
                _cooldowns[itemId] = Game.GameTime + CONSUME_COOLDOWN_MS;

            // Remove from queue
                _queue.Dequeue();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopConsumeSystem.Tick: {ex}");
            }
        }

        private bool IsReady(string itemId)
        {
            try
            {
                if (!_cooldowns.ContainsKey(itemId))
                    return true;

                return Game.GameTime > _cooldowns[itemId];
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopConsumeSystem.IsReady: {ex}");
                return true;
            }
        }

        // ============================================================
        // CONSUME HANDLING
        // ============================================================
        private void Consume(string itemId)
        {
            try
            {
                Ped player = Game.Player.Character;

                if (PlayerHelper.IsPlayerBusy(player))
                {
                    DebugLogger.Warn("ShopConsumeSystem: Player busy, skipping consumption.");
                    return;
                }

                DebugLogger.Info($"[SHOP] ShopConsumeSystem: Consuming '{itemId}'");

                // ------------------------------------------------------------
                // LOAD ANIMATION
                // ------------------------------------------------------------
                const string animDict = "mp_player_inteat@burger";
                const string animName = "mp_player_int_eat_burger";

                PlayerHelper.RequestAnimDict(animDict);

                // ------------------------------------------------------------
                // CREATE PROP (CHOCOLATE BAR)
                // ------------------------------------------------------------
                int propHash = Function.Call<int>(Hash.GET_HASH_KEY, "prop_choc_ego");
                Vector3 pos = player.Position + new Vector3(0, 0, -1f);

                Prop snackProp = World.CreateProp(propHash, pos, true, false);

                if (snackProp != null && snackProp.Exists())
                {
                    // ⭐ FIXED: Correct AttachTo() signature
                    snackProp.AttachTo(player, new Vector3(0.08f, 0.02f, -0.02f), new Vector3(10f, 160f, 20f));
                }

                // ------------------------------------------------------------
                // PLAY ANIMATION
                // ------------------------------------------------------------
                player.Task.PlayAnimation(animDict, animName, 8f, -8f, 2500, AnimationFlags.UpperBodyOnly, 0f);

                int animEnd = Game.GameTime + 2500;
                bool cancelled = false;

                // ------------------------------------------------------------
                // SAFECRACK-STYLE CANCEL INPUT
                // ------------------------------------------------------------
                while (Game.GameTime < animEnd)
                {
                    bool cancelKey = Game.IsKeyPressed(System.Windows.Forms.Keys.Escape);
                    bool cancelPad = Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)Control.PhoneCancel);

                    if (cancelKey || cancelPad)
                    {
                        cancelled = true;
                        break;
                    }

                    Script.Yield();
                }

                // Cleanup prop
                PlayerHelper.DeleteProp(snackProp);

                if (cancelled)
                {
                    DebugLogger.Info("[SHOP] ShopConsumeSystem: Consumption cancelled.");
                    return;
                }

                // ------------------------------------------------------------
                // APPLY ITEM EFFECTS
                // ------------------------------------------------------------
                ApplyEffects(itemId);
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopConsumeSystem.Consume: {ex}");
            }
        }

        // ============================================================
        // APPLY ITEM EFFECTS
        // ============================================================
        private void ApplyEffects(string itemId)
        {
            try
            {
                Ped player = Game.Player.Character;

                switch (itemId)
                {
                    case "ps_and_qs":
                    case "egochaser":
                    case "meteorite":
                        player.Health = Math.Min(player.MaxHealth, player.Health + 15);
                        _ctx.Ui.ShowNotification("~y~Health restored by 15%.");
                        break;

                    case "sprunk":
                    case "e_colas":
                        player.Health = Math.Min(player.MaxHealth, player.Health + 50);
                        _ctx.Ui.ShowNotification("~o~Health restored by 50%.");
                        break;

                    case "bandage":
                        player.Health = Math.Min(player.MaxHealth, player.Health + 100);
                        _ctx.Ui.ShowNotification("~g~Health restored by 100%.");
                        break;

                    default:
                        DebugLogger.Warn($"ShopConsumeSystem: Unknown item '{itemId}'");
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"ShopConsumeSystem.ApplyEffects: {ex}");
            }
        }
    }
}
