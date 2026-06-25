using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Debug;
using StoreRobberyEnhanced.Scripts.Systems;
using StoreRobberyEnhanced.UI;
using System;
using System.Collections.Generic;

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

        private string GetPropModelForItem(string itemId)
        {
            switch (itemId)
            {
                case "sprunk":
                    return "prop_ld_can_01";          // Sprunk can

                case "e_colas":
                    return "prop_ecola_can";          // Ecola can

                case "coffee":
                    return "prop_food_coffee";        // Coffee

                case "juice01":
                    return "prop_food_juice01";       // Juice 1
                
                case "beer1":
                    return "prop_cs_beer_bot_01";     // Beer 1

                case "beer2":
                    return "prop_cs_beer_bot_02";     // Beer 2
                
                case "beer40":
                    return "prop_cs_beer_bot_40oz_02";     // Beer 40oz
                
                case "whiskey":
                    return "prop_cs_whiskey_bottle";     // Bottle of Whiskey

                case "egochaser":
                    return "prop_choc_ego";           // EgoChaser bar

                case "ps_and_qs":
                    return "prop_candy_pqs";          // Ps & Qs bar

                case "meteorite":
                    return "prop_choc_meto";           // Meteorite bar

                case "sandwich":
                    return "prop_sandwich_01";        // Sandwich

                case "taco":
                    return "prop_taco_01";            // Taco 

                case "hotdog":
                    return "prop_cs_hotdog_01";       // Hotdog

                case "burger":
                    return "prop_cs_burger_01";       // Burger 
                
                case "donut":
                    return "prop_donut_01";           // Donut 

                default:
                    return "prop_ecola_can";
            }
        }

        private (string dict, string anim) GetAnimationForItem(string itemId)
        {
            switch (itemId)
            {
                case "sprunk":
                case "e_colas":
                case "coffee":
                case "juice01":
                case "beer1":
                case "beer2":
                case "beer40":
                case "whiskey":
                    return ("mini@sprunk", "PLYR_BUY_DRINK_PT2");

                case "egochaser":
                case "ps_and_qs":
                case "meteorite":
                case "sandwich":
                case "taco":
                case "hotdog":
                case "burger":
                case "donut":
                    return ("mp_player_inteat@burger", "mp_player_int_eat_burger_left");

                default:
                    return ("mini@sprunk", "PLYR_BUY_DRINK_PT2");
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

                var store = _ctx.GetNearestStore();
                if (store == null)
                    return;

                if (PlayerHelper.IsPlayerBusy(player))
                {
                    DebugLogger.Warn("ShopConsumeSystem: Player busy, skipping consumption.");
                    return;
                }

                DebugLogger.Info($"[SHOP] ShopConsumeSystem: Consuming '{itemId}'");

                Game.Player.CanControlCharacter = true;
                player.Task.ClearAllImmediately();
                player.PlayAmbientSpeech("GENERIC_BUY", false, null);

                // ------------------------------------------------------------
                // RESOLVE ANIMATION + PROP
                // ------------------------------------------------------------
                var (animDict, animName) = GetAnimationForItem(itemId);
                PlayerHelper.RequestAnimDict(animDict);

                string modelName = GetPropModelForItem(itemId);
                Model model = new Model(modelName);

                if (!model.IsLoaded)
                    model.Request(500);

                if (!model.IsLoaded)
                {
                    DebugLogger.Error($"[SHOP] Failed to load model '{modelName}'");
                    return;
                }

                // Spawn slightly offset to avoid collision issues (esp. Meteorite)
                Vector3 spawnPos = store.Clerk.Position + store.Clerk.ForwardVector * 0.2f + new Vector3(0f, 0f, 0.1f);
                Prop snackProp = World.CreateProp(model, spawnPos, true, true);

                if (snackProp == null || !snackProp.Exists())
                {
                    DebugLogger.Error($"[SHOP] Failed to create prop '{modelName}'");
                    return;
                }

                // ------------------------------------------------------------
                // CLERK HANDOFF ANIMATION
                // ------------------------------------------------------------
                store.Clerk.TaskPlayAnim("mp_am_hold_up", "purchase_beer_shopkeeper", 8, -1);
                Script.Wait(500);

                // Attach to clerk left hand
                snackProp.AttachTo(store.Clerk.Bones[Bone.SkelLeftHand], new Vector3(0.09f, 0.01f, 0.07f), new Vector3(-170f, 0f, 0f));

                Script.Wait(750);
                snackProp.Detach();
                Script.Wait(1000);

                // ------------------------------------------------------------
                // DETERMINE HAND + OFFSETS FOR PLAYER
                // ------------------------------------------------------------
                bool isDrink = itemId == "sprunk" 
                    || itemId == "e_colas" 
                    || itemId == "coffee"
                    || itemId == "juice01"
                    || itemId == "beer1"
                    || itemId == "beer2"
                    || itemId == "beer40"
                    || itemId == "whiskey";

                Bone handBone;
                Vector3 posOffset;
                Vector3 rotOffset;

                if (isDrink)
                {
                    handBone = Bone.PHRightHand;
                    posOffset = new Vector3(0.0f, 0.0f, 0.0f);
                    rotOffset = new Vector3(0.0f, 0.0f, 0.0f);
                }
                else
                {
                    handBone = Bone.PHLeftHand;
                    posOffset = new Vector3(0.025f, 0.015f, -0.025f);
                    rotOffset = new Vector3(0.0f, 0.0f, 0.0f);
                    //posOffset = new Vector3(0.08f, 0.02f, -0.02f);
                    //rotOffset = new Vector3(10f, 160f, 20f);

                    if (itemId == "taco" || itemId == "hotdog")
                    {
                        posOffset = new Vector3(0.055f, 0.015f, -0.025f);
                        rotOffset = new Vector3(0f, 0f, 90f);
                    }
                }

                // Attach to player hand via native
                Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY,
                    snackProp.Handle,
                    player.Handle,
                    player.Bones[handBone].Index,
                    posOffset.X, posOffset.Y, posOffset.Z,
                    rotOffset.X, rotOffset.Y, rotOffset.Z,
                    true,  // useSoftPinning
                    true,  // collision
                    false, // isPed
                    false, // vertexIndex
                    2,     // fixedRot
                    true   // invMassScale
                );

                // ------------------------------------------------------------
                // PLAY CONSUME ANIMATION
                // ------------------------------------------------------------
                if (isDrink)
                {
                    // Load anim dictionary properly
                    Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
                    while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
                        Script.Yield();

                    // Lock player so nothing interrupts
                    player.Task.ClearAllImmediately();
                    player.AlwaysKeepTask = true;
                    player.BlockPermanentEvents = true;

                    // Play the looping drink animation
                    player.TaskPlayAnim(animDict, animName, 1, 6000);

                    // Hold the drink animation for a fixed time (Rockstar uses ~2.8s)
                    int drinkEnd = Game.GameTime + 8000;
                    while (Game.GameTime < drinkEnd)
                    {
                        if (!player.IsPlayingAnim(animDict, animName))
                            break;

                        Script.Yield();
                    }

                    // Manually stop the drink animation
                    Function.Call(Hash.STOP_ANIM_TASK, player.Handle, animDict, animName, 1.0f);

                    // Play vending sound
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "VENDING_MACHINE", "VENDING_MACHINE", false);
                    Script.Wait(6000);

                    // Load throw anim
                    Function.Call(Hash.REQUEST_ANIM_DICT, "mini@sprunk");
                    while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mini@sprunk"))
                        Script.Yield();

                    // Play throw animation
                    player.TaskPlayAnim("mini@sprunk", "plyr_buy_drink_pt3", 0, -1);

                    // Wait for throw animation to finish
                    while (player.IsPlayingAnim("mini@sprunk", "plyr_buy_drink_pt3"))
                        Script.Yield();

                    // Restore player state
                    player.AlwaysKeepTask = false;
                    player.BlockPermanentEvents = false;

                    if (Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, Game.Player) == 0)
                        player.PlayAmbientSpeech("GENERIC_DRINK", false);

                    Script.Wait(750);

                    if (snackProp.Exists())
                    {
                        snackProp.Detach();
                        snackProp.ApplyForce(player.RightVector * -5f + player.UpVector * 5f);
                        snackProp.MarkAsNoLongerNeeded();
                    }
                }
                else
                {
                    const string finishDict = "mp_player_inteat@burger";
                    const string finishAnim = "mp_player_int_eat_burger_fp";

                    PlayerHelper.RequestAnimDict(finishDict);
                    player.TaskPlayAnim(finishDict, finishAnim, 1, 4000);
                    Script.Wait(900);

                    if (Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, Game.Player) == 0)
                        player.PlayAmbientSpeech("GENERIC_EAT", false);

                    Script.Wait(3000);

                    if (snackProp.Exists())
                    {
                        snackProp.Detach();
                        snackProp.MarkAsNoLongerNeeded();
                        PlayerHelper.DeleteProp(snackProp);
                    }
                }

                int animEnd = Game.GameTime + 1500;
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
                    case "sandwich":
                    case "taco":
                    case "hotdog":
                    case "burger":
                    case "donut":
                        player.Health = Math.Min(player.MaxHealth, player.Health + 5);
                        player.PlayAmbientSpeech("GENERIC_EAT", false, null);
                        _ctx.Ui.ShowNotification("~y~Health restored by 5%.");
                        break;

                    case "sprunk":
                    case "e_colas":
                    case "coffee":
                    case "juice01":
                    case "beer1":
                    case "beer2":
                    case "beer40":
                    case "whiskey":
                        player.Health = Math.Min(player.MaxHealth, player.Health + 15);
                        player.PlayAmbientSpeech("GENERIC_DRINK", false, null);
                        _ctx.Ui.ShowNotification("~o~Health restored by 15%.");
                        break;

                    case "bandage":
                        player.Health = Math.Min(player.MaxHealth, player.Health + 50);
                        _ctx.Ui.ShowNotification("~g~Health restored by 50%.");
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
