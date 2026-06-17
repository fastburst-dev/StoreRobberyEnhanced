using GTA;
using GTA.Math;
using StoreRobberyEnhanced.Debug;

namespace StoreRobberyEnhanced.Config
{
    internal static class SafeCrackConfigLoader
    {
        public static SafeCrackSettings Load(IniConfig config)
        {
            SafeCrackSettings settings = new SafeCrackSettings();

            try
            {
                // ------------------------------------------------------------
                // ECONOMY (REUSE EXISTING STORE SETTINGS)
                // ------------------------------------------------------------
                settings.MinCash = config.SafeMinAmount;
                settings.MaxCash = config.SafeMaxAmount;

                // ------------------------------------------------------------
                // SAFECRACK-SPECIFIC SETTINGS (NEW)
                // ------------------------------------------------------------
                SimpleIni ini = new SimpleIni(config.MainIniPath);

                int cooldownDefault = 3000;
                bool padShakeDefault = true;
                bool loadOptionalDefault = false;

                settings.CooldownMs = ini.ReadInt("Store Settings", "SafeCrackCooldownMs", cooldownDefault);
                settings.PadShake = ini.ReadBool("Store Settings", "SafeCrackPadShake", padShakeDefault);
                settings.LoadOptionalSafes = ini.ReadBool("Store Settings", "SafeCrackLoadOptionalSafes", loadOptionalDefault);

                ini.WriteInt("Store Settings", "SafeCrackCooldownMs", settings.CooldownMs);
                ini.WriteBool("Store Settings", "SafeCrackPadShake", settings.PadShake);
                ini.WriteBool("Store Settings", "SafeCrackLoadOptionalSafes", settings.LoadOptionalSafes);

                ini.Save();

                // ------------------------------------------------------------
                // VALIDATED SAFE POSITIONS (21 STORES)
                // ------------------------------------------------------------
                settings.SafeLocations.AddRange(new[]
                {
                    new Vector3(28.34f, -1339.23f, 29.49f),
                    new Vector3(-3250.06f, 1004.46f, 12.83f),
                    new Vector3(-3040.10f, 590.70f, 7.90f),
                    new Vector3(378.22f, 333.32f, 103.56f),
                    new Vector3(546.32f, 2662.75f, 42.15f),
                    new Vector3(2672.77f, 3286.59f, 55.24f),
                    new Vector3(1959.21f, 3748.84f, 32.34f),
                    new Vector3(1734.83f, 6420.85f, 35.03f),
                    new Vector3(2549.27f, 384.91f, 108.62f),
                    new Vector3(1394.92f, 3613.89f, 34.98f),
                    new Vector3(-43.3559f, -1748.3580f, 29.4210f),
                    new Vector3(-709.69f, -904.05f, 19.21f),
                    new Vector3(-1829.19f, 798.84f, 138.19f),
                    new Vector3(1159.55f, -314.04f, 69.20f),
                    new Vector3(1707.94f, 4936.36f, 42.06f),
                    new Vector3(-2959.62f, 387.15f, 14.04f),
                    new Vector3(1126.81f, -980.15f, 45.41f),
                    new Vector3(-1478.87f, -375.43f, 39.16f),
                    new Vector3(1169.30f, 2717.81f, 37.15f),
                    new Vector3(-1220.80f, -916.02f, 11.32f),
                    new Vector3(198.773f, -16.020f, 69.920f),
                    new Vector3(-691.636f, -867.996f, 23.700f),
                    new Vector3(155.271f, 247.452f, 106.976f),
                    new Vector3(168.833f, 6644.659f, 31.699f),
                    new Vector3(549.873f, -153.367f, 57.041f),
                    new Vector3(-591.75f, -1012.621f, 22.325f)
                });

                settings.SafeRotations.AddRange(new[]
                {
                    356.39f,
                    81.24f,
                    111.69f,
                    341.31f,
                    186.56f,
                    61.26f,
                    15.66f,
                    343.00f,
                    84.84f,
                    15.69f,
                    50.15f,
                    90.00f,
                    136.25f,
                    97.00f,
                    315.88f,
                    168.93f,
                    359.61f,
                    227.58f,
                    270.44f,
                    118.63f,
                    335.55f,
                    185.55f,
                    335.45f,
                    320.45f,
                    335.00f,
                    80.00f
                });
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogException("SafeCrackConfigLoader.Load", ex);
            }

            return settings;
        }
    }
}
