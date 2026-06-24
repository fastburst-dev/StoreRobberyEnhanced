using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreRobberyEnhanced.Scripts.Systems
{
    public static class ClerkExtensions
    {
        public static void PlayAmbientSpeech(this Ped ped, string speechFile, bool immediately = false, string[] queue = null)
        {
            if (ped == null || !ped.Exists())
                return;

            string text = speechFile;

            try
            {
                // Stop current speech if requested
                if (immediately)
                {
                    Function.Call(Hash.STOP_PED_SPEAKING, ped.Handle, true);
                }

                // If queue provided, try fallback speech lines
                if (queue != null && queue.Length > 0)
                {
                    // If main line fails, try queued lines
                    if (!Function.Call<bool>(Hash.IS_AMBIENT_SPEECH_PLAYING, ped.Handle))
                    {
                        for (int i = 0; i < queue.Length; i++)
                        {
                            if (Function.Call<bool>(Hash.IS_AMBIENT_SPEECH_PLAYING, ped.Handle))
                            {
                                text = queue[i];
                                break;
                            }
                        }
                    }
                }

                // Enable director mode audio flag (required for some speech types)
                Function.Call(Hash.SET_AUDIO_FLAG, "IsDirectorModeActive", true);

                // Play the speech
                Function.Call(
                    Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE,
                    ped.Handle,
                    text,
                    "SPEECH_PARAMS_FORCE"
                );

                // Disable flag
                Function.Call(Hash.SET_AUDIO_FLAG, "IsDirectorModeActive", false);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("PlayAmbientSpeech", ex);
            }
        }

        public static void TaskPlayAnim(this Ped ped, string animDict, string animFile, int animFlag, int duration)
        {
            if (ped == null || !ped.Exists())
                return;

            // Request dictionary
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);

            // Wait up to 1 second for it to load
            DateTime timeout = DateTime.Now + TimeSpan.FromSeconds(1);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
            {
                Script.Yield();
                if (DateTime.Now >= timeout)
                    return;
            }

            // Play animation
            Function.Call(
                Hash.TASK_PLAY_ANIM,
                ped.Handle,
                animDict,
                animFile,
                4.0f,     // speed
                -4.0f,    // speedMult
                duration,
                animFlag,
                0.0f,     // playbackRate
                false,
                false,
                false
            );
        }

        public static void WaitUntilExist(this Entity entity)
        {
            while (entity == null)
            {
                Script.Wait(0);
            }
        }

        public static bool IsPlayingAnim(this Ped ped, string animDict, string animFile)
        {
            if (ped == null || !ped.Exists())
                return false;

            return Function.Call<bool>(
                Hash.IS_ENTITY_PLAYING_ANIM,
                ped.Handle,
                animDict,
                animFile,
                3
            );
        }

        public static void SetAnimTime(this Ped ped, string animDict, string animFile, float time)
        {
            if (ped == null || !ped.Exists())
                return;

            Script.Wait(0);

            Function.Call(
                Hash.SET_ENTITY_ANIM_CURRENT_TIME,
                ped.Handle,
                animDict,
                animFile,
                time
            );
        }

        public static float GetAnimTime(this Ped ped, string animDict, string animFile)
        {
            if (ped == null || !ped.Exists())
                return 0f;

            return Function.Call<float>(
                Hash.GET_ENTITY_ANIM_CURRENT_TIME,
                ped.Handle,
                animDict,
                animFile
            );
        }

        public static void PlaceOnGround(this Entity entity, bool isWorld = false)
        {
            if (!isWorld)
            {
                RaycastResult hit = World.Raycast(
                    entity.Position,
                    entity.UpVector * -10f,
                    1000f,
                    IntersectFlags.Everything,
                    entity
                );

                entity.Position = new Vector3(
                    hit.HitPosition.X,
                    hit.HitPosition.Y,
                    hit.HitPosition.Z
                );
            }
            else
            {
                entity.Position = new Vector3(
                    entity.Position.X,
                    entity.Position.Y,
                    World.GetGroundHeight(entity.Position)
                );
            }
        }

        public static int GetCurrentInterior()
        {
            Vector3 pos = Game.Player.Character.Position;
            return Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, pos.X, pos.Y, pos.Z);
        }

    }
}
