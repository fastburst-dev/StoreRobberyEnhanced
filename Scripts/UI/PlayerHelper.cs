using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;
using System.Collections.Generic;

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
                bool result = weapon != null && !_notWeapons.Contains(weapon.Hash);

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
                    hash == WeaponHash.Wrench ||
                    hash == WeaponHash.BattleAxe ||
                    hash == WeaponHash.PoolCue ||
                    hash == WeaponHash.StoneHatchet ||
                    hash == WeaponHash.CandyCane ||
                    hash == WeaponHash.Snowball ||
                    hash == WeaponHash.Ball;


                DebugLogger.Trace($"IsMeleeWeapon({hash}) = {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("PlayerHelper.IsMeleeWeapon", ex);
                return false;
            }
        }

        public static readonly HashSet<WeaponHash> _notWeapons = new HashSet<WeaponHash>()
        {
            {WeaponHash.Unarmed},
            {WeaponHash.Firework},
            {WeaponHash.Snowball},
            {WeaponHash.Ball},
            {WeaponHash.AcidPackage},
            {WeaponHash.PetrolCan},
            {WeaponHash.Parachute},
            {WeaponHash.FireExtinguisher},
            {WeaponHash.HazardousJerryCan},
            {WeaponHash.FertilizerCan},
        };

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

        // ------------------------------------------------------------
        // IS HOLDING PHONE CHECK
        // ------------------------------------------------------------
        public bool IsHoldingPhone()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                // All known phone models
                string[] phoneModels =
                {
                    "prop_amb_phone",
                    "prop_amb_phone_01",
                    "prop_amb_phone_02",
                    "prop_npc_phone",
                    "prop_phone_ing",
                    "prop_phone_ing_02",
                    "prop_phone_ing_03"
                };

                foreach (var model in phoneModels)
                {
                    int hash = Function.Call<int>(Hash.GET_HASH_KEY, model);
                    Prop phone = Function.Call<Prop>(Hash.GET_CLOSEST_OBJECT_OF_TYPE,
                        player.Position.X, player.Position.Y, player.Position.Z,
                        1.0f, hash, false, false, false);

                    if (phone != null && phone.Exists() && phone.IsAttachedTo(player))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

    }
}