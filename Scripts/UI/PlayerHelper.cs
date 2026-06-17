using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;

namespace StoreRobberyEnhanced.UI
{
    internal class PlayerHelper
    {
        private readonly StoreContext _ctx;

        public PlayerHelper(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                DebugLogger.Info("PlayerHelper initialized");
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // GENERIC HELPERS USED BY ShopConsumeSystem
        // ------------------------------------------------------------
        public static bool IsPlayerBusy(Ped player)
        {
            return player == null || !player.Exists() || player.IsInVehicle() || player.IsRagdoll || player.IsDead;
        }

        public static void RequestAnimDict(string dict)
        {
            try
            {
                Function.Call(Hash.REQUEST_ANIM_DICT, dict);
                int timeout = Game.GameTime + 2000;
                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict) && Game.GameTime < timeout)
                    Script.Yield();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.RequestAnimDict", ex);
            }
        }

        public static Prop CreateProp(string modelName, Vector3 pos)
        {
            try
            {
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, modelName);
                Function.Call(Hash.REQUEST_MODEL, hash);
                while (!Function.Call<bool>(Hash.HAS_MODEL_LOADED, hash))
                    Script.Yield();

                return World.CreateProp(hash, pos, true, false);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.CreateProp", ex);
                return null;
            }
        }

        public static void AttachProp(Prop prop, Ped player, int boneIndex, Vector3 offset, Vector3 rotation)
        {
            try
            {
                if (prop != null && prop.Exists())
                {
                    prop.AttachTo(player, offset, rotation);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.AttachProp", ex);
            }
        }

        public static void DeleteProp(Prop prop)
        {
            try
            {
                if (prop != null && prop.Exists())
                    prop.Delete();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.DeleteProp", ex);
            }
        }

        // ------------------------------------------------------------
        // GIVE SNACK TO PLAYER LOGIC
        // ------------------------------------------------------------
        public static void GiveSnackToPlayer(string itemId)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // ------------------------------------------------------------
                // BLOCK ACTION IF PLAYER IS BUSY
                // ------------------------------------------------------------
                if (player.IsInVehicle() || player.IsRagdoll || player.IsDead)
                {
                    DebugLogger.Warn("GiveSnackToPlayer: Player busy, skipping.");
                    return;
                }

                DebugLogger.Info($"GiveSnackToPlayer: Consuming item '{itemId}'");

                // ------------------------------------------------------------
                // LOAD ANIMATION DICTIONARY
                // ------------------------------------------------------------
                const string animDict = "mp_player_inteat@burger";
                const string animName = "mp_player_int_eat_burger";

                Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
                int timeout = Game.GameTime + 2000;
                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict) && Game.GameTime < timeout)
                    Script.Yield();

                // ------------------------------------------------------------
                // CREATE PROP (CHOCOLATE BAR)
                // ------------------------------------------------------------
                int propHash = Function.Call<int>(Hash.GET_HASH_KEY, "prop_choc_ego");
                Vector3 pos = player.Position + new Vector3(0, 0, -1f);

                Prop snackProp = World.CreateProp(propHash, pos, true, false);
                if (snackProp != null && snackProp.Exists())
                {
                    // ⭐ FIXED: Correct AttachTo() signature
                    snackProp.AttachTo(
                        player, // right hand bone index
                        new Vector3(0.08f, 0.02f, -0.02f),
                        new Vector3(10f, 160f, 20f)
                    );
                }

                // ------------------------------------------------------------
                // PLAY ANIMATION
                // ------------------------------------------------------------
                player.Task.PlayAnimation(animDict, animName, 8f, -8f, 2500, AnimationFlags.UpperBodyOnly, 0f);

                int animEnd = Game.GameTime + 2500;
                bool cancelled = false;

                // ------------------------------------------------------------
                // SAFECRACK-STYLE CANCEL INPUT
                // ESC (keyboard) or B (controller)
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

                // ------------------------------------------------------------
                // CLEANUP PROP
                // ------------------------------------------------------------
                if (snackProp != null && snackProp.Exists())
                    snackProp.Delete();

                // ------------------------------------------------------------
                // CANCEL BEHAVIOR
                // ------------------------------------------------------------
                if (cancelled)
                {
                    DebugLogger.Info("GiveSnackToPlayer: Snack consumption cancelled.");
                    return;
                }

                // ------------------------------------------------------------
                // APPLY ITEM EFFECTS
                // ------------------------------------------------------------
                switch (itemId)
                {
                    case "ps_and_qs":
                    case "egochaser":
                    case "meteorite":
                        player.Health = Math.Min(player.MaxHealth, player.Health + 15);
                        break;

                    default:
                        DebugLogger.Warn($"GiveSnackToPlayer: Unknown item '{itemId}'");
                        break;
                }

                DebugLogger.Info($"GiveSnackToPlayer: Successfully consumed '{itemId}'");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.GiveSnackToPlayer", ex);
            }
        }

        // ------------------------------------------------------------
        // BASIC STATES
        // ------------------------------------------------------------
        public bool IsAiming()
        {
            try
            {
                bool result = Game.IsControlPressed(Control.Aim);
                DebugLogger.Trace($"IsAiming() = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsAiming", ex);
                return false;
            }
        }

        public bool IsShooting()
        {
            try
            {
                bool result = Game.IsControlPressed(Control.Attack);
                DebugLogger.Trace($"IsShooting() = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsShooting", ex);
                return false;
            }
        }

        public bool IsArmed()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                Weapon weapon = player.Weapons.Current;
                bool result = weapon != null && weapon.Hash != WeaponHash.Unarmed;

                DebugLogger.Trace($"IsArmed() = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsArmed", ex);
                return false;
            }
        }

        public bool IsMasked()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                int maskDrawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, 1);
                int hatDrawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, 0);
                int accessoryDrawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, 7);

                bool masked =
                    maskDrawable != 0 ||
                    hatDrawable != 0 ||
                    accessoryDrawable != 0;

                DebugLogger.Trace($"IsMasked() = {masked} (mask={maskDrawable}, hat={hatDrawable}, acc={accessoryDrawable})");
                return masked;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsMasked", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // WEAPON TYPE CHECKS
        // ------------------------------------------------------------
        public bool IsMeleeWeapon(WeaponHash hash)
        {
            try
            {
                bool result =
                    hash == WeaponHash.Knife ||
                    hash == WeaponHash.Nightstick ||
                    hash == WeaponHash.Hammer ||
                    hash == WeaponHash.Bat ||
                    hash == WeaponHash.Crowbar ||
                    hash == WeaponHash.GolfClub ||
                    hash == WeaponHash.Bottle ||
                    hash == WeaponHash.Dagger ||
                    hash == WeaponHash.Hatchet ||
                    hash == WeaponHash.KnuckleDuster ||
                    hash == WeaponHash.Machete ||
                    hash == WeaponHash.Flashlight ||
                    hash == WeaponHash.SwitchBlade ||
                    hash == WeaponHash.PoolCue ||
                    hash == WeaponHash.Wrench;

                DebugLogger.Trace($"IsMeleeWeapon({hash}) = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsMeleeWeapon", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // POSITIONAL CHECKS
        // ------------------------------------------------------------
        public bool IsNear(Vector3 pos, float dist)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                bool result = player.Position.DistanceTo(pos) <= dist;
                DebugLogger.Trace($"IsNear(pos, {dist}) = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsNear", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // IS INSIDE STORE (NULL-SAFE + PATCH U + PATCH O COMPATIBLE)
        // ------------------------------------------------------------
        public bool IsInsideStore(TrackedStore store, float radiusOverride = -1f)
        {
            try
            {
                // ⭐ Absolute safety: store missing
                if (store == null)
                    return false;

                // ⭐ Store must be initialized
                if (store.StorePos == Vector3.Zero ||
                    store.RegisterPos == Vector3.Zero)
                    return false;

                // ⭐ Determine radius
                float radius = radiusOverride > 0f ? radiusOverride : store.Radius;

                // ⭐ Radius must be valid
                if (radius <= 0f || radius > 200f) // sanity check
                    return false;

                // ⭐ Player must exist
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                // ⭐ Distance check
                float dist = player.Position.DistanceTo(store.StorePos);

                // ⭐ Final inside check
                return dist <= radius;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("IsInsideStore (Patched)", ex);
                return false; // fail-safe
            }
        }

        // ------------------------------------------------------------
        // LINE OF SIGHT
        // ------------------------------------------------------------
        public bool IsInLOS(Entity target)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                if (target == null || !target.Exists())
                    return false;

                bool result = Function.Call<bool>(
                    Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY,
                    player.Handle,
                    target.Handle,
                    17
                );

                DebugLogger.Trace($"IsInLOS(target={target.Handle}) = {result}");
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsInLOS", ex);
                return false;
            }
        }

        // ------------------------------------------------------------
        // THREAT CHECK (GUN-ONLY)
        // ------------------------------------------------------------
        public bool IsThreatening(Ped target)
        {
            try
            {
                if (target == null || !target.Exists())
                    return false;

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                Weapon current = player.Weapons.Current;
                if (current == null)
                    return false;

                bool isGun =
                    current.Hash != WeaponHash.Unarmed &&
                    current.Group != WeaponGroup.Melee;

                if (!isGun)
                {
                    DebugLogger.Trace($"IsThreatening(target={target.Handle}) = false (melee ignored)");
                    return false;
                }

                bool result =
                    IsAiming() &&
                    IsInLOS(target);

                DebugLogger.Trace($"IsThreatening(target={target.Handle}) = {result} (gun={current.Hash})");
                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsThreatening", ex);
                return false;
            }

        }
    }
}
