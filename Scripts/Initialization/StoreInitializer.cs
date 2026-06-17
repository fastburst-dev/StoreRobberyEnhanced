using System;
using System.Linq;  
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using StoreRobberyEnhanced.Data;


namespace StoreRobberyEnhanced.Initialization
{
    internal static class StoreInitializer
    {
        public static bool IsDLcLoaded(string dlcName)
        {
            return Function.Call<bool>(Hash.IS_DLC_PRESENT, Game.GenerateHash(dlcName));
        }

        public static void BuildStores(StoreContext ctx)
        {
            ctx.Stores.Clear();
            LoadAllStoreIPLs();
            // Allow IPLs/interiors to stream before building cameras
            Script.Wait(500);

            int id = 0;

            // =====================================================================
            // 24/7 SUPERMARKETS (0–8)
            // =====================================================================

            // 0 - 24/7 Supermarket (Strawberry)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Stawberry)",
                new Vector3(27.306f, -1345.502f, 29.497f),           // Exterior StorePos
                new Vector3(24.40f, -1345.20f, 29.49f),              // ClerkPos (INTERIOR)
                242.5f,                                              // ClerkHeading
                new Vector3(24.40f, -1345.20f, 29.49f),              // RegisterPos
                90.00f,                                              // RegisterHeading
                new Vector3(28.34f, -1339.23f, 29.49f),              // SafePos
                356.39f,                                             // SafeHeading
                new Vector3(29.15f, -1349.65f, 29.32f),              // DoorPos
                "v_ilev_gasdoor",
                0.00f,                                               // DoorHeading
                3.0f
            ));

            // 1 - 24/7 Supermarket (Chumash)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Chumash)",
                new Vector3(-3242.650f, 1003.800f, 12.830f),
                new Vector3(-3244.55f, 1000.10f, 12.83f),            // ClerkPos
                330.00f,                                             // ClerkHeading
                new Vector3(-3242.60f, 1000.40f, 12.83f),            // RegisterPos
                0.00f,                                               // RegisterHeading
                new Vector3(-3250.06f, 1004.46f, 12.83f),            // SafePos
                81.24F,                                              // SafeHeading
                new Vector3(-3239.50f, 1004.40f, 12.51f),            // DoorPos
                "v_ilev_gasdoor",
                80.00f,                                              // DoorHeading
                3.0f
            ));

            // 2 - 24/7 Supermarket (Banham Canyon)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Banham Canyon)",
                new Vector3(-3040.77f, 588.65f, 7.90f),
                new Vector3(-3041.10f, 583.70f, 7.90f),              // ClerkPos
                340.00f,                                             // ClerkHeading
                new Vector3(-3038.60f, 588.60f, 7.90f),              // RegisterPos
                0.00f,                                               // RegisterHeading
                new Vector3(-3040.10f, 590.70f, 7.90f),              // SafePos
                111.69f,                                             // SafeHeading
                new Vector3(-3038.15f, 589.65f, 7.90f),              // DoorPos
                "v_ilev_gasdoor",
                100.00f,                                             // DoorHeading
                3.0f
            ));

            // 3 - 24/7 Supermarket (Clinton Ave)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Clinton Ave)",
                new Vector3(376.675f, 325.75f, 103.56f),
                new Vector3(373.05f, 328.70f, 103.55f),              // ClerkPos
                230.00f,                                             // ClerkHeading
                new Vector3(373.50f, 327.40f, 103.57f),              // RegisterPos
                255.00f,                                             // RegisterHeading
                new Vector3(378.22f, 333.32f, 103.56f),              // SafePos
                341.31f,                                             // SafeHeading
                new Vector3(376.55f, 322.95f, 103.57f),              // DoorPos
                "v_ilev_gasdoor",
                350.00f,                                             // DoorHeading
                3.0f
            ));

            // 4 - 24/7 Supermarket (Harmony)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Harmony)",
                new Vector3(544.405f, 2670.425f, 42.156f),
                new Vector3(549.50f, 2669.00f, 42.15f),              // ClerkPos
                55.00f,                                              // ClerkHeading
                new Vector3(549.40f, 2670.40f, 42.15f),              // RegisterPos
                180.00f,                                             // RegisterHeading
                new Vector3(546.32f, 2662.75f, 42.15f),              // SafePos
                186.56f,                                             // SafeHeading
                new Vector3(543.95f, 2673.05f, 42.15f),              // DoorPos
                "v_ilev_gasdoor",
                190.00f,                                             // DoorHeading
                3.0f
            ));

            // 5 - 24/7 Supermarket (Grand Senora Desert)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Grand Senora Desert)",
                new Vector3(2679.925f, 3283.550f, 55.24f),
                new Vector3(2675.95f, 3280.40f, 55.24f),             // ClerkPos
                306.00f,                                             // ClerkHeading
                new Vector3(2671.60f, 3286.00f, 55.20f),             // RegisterPos
                330.00f,                                             // RegisterHeading
                new Vector3(2672.77f, 3286.59f, 55.24f),             // SafePos
                61.26f,                                              // SafeHeading
                new Vector3(2682.40f, 3282.35f, 55.24f),             // DoorPos
                "v_ilev_gasdoor",
                60.00f,                                              // DoorHeading
                3.0f
            ));

            // 6 - 24/7 Supermarket (Sandy Shores)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Sandy Shores)",
                new Vector3(1963.475f, 3742.45f, 32.34f),
                new Vector3(1958.725f, 3741.90f, 32.34f),            // ClerkPos
                275.00f,                                             // ClerkHeading
                new Vector3(1959.70f, 3742.40f, 32.30f),             // RegisterPos
                300.00f,                                             // RegisterHeading
                new Vector3(1959.21f, 3748.84f, 32.34f),             // SafePos
                15.66f,                                              // SafeHeading
                new Vector3(1965.40f, 3740.35f, 32.34f),             // DoorPos
                "v_ilev_gasdoor",
                30.00f,                                              // DoorHeading
                3.0f
            ));

            // 7 - 24/7 Supermarket (Grapeseed)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Grapeseed)",
                new Vector3(1728.440f, 6414.130f, 35.037f),
                new Vector3(1728.80f, 6417.35f, 35.04f),             // ClerkPos
                220.00f,                                             // ClerkHeading
                new Vector3(1728.90f, 6414.80f, 35.04f),             // RegisterPos
                180.00f,                                             // RegisterHeading
                new Vector3(1734.83f, 6420.85f, 35.03f),             // SafePos
                343.00f,                                             // SafeHeading
                new Vector3(1731.00f, 6411.00f, 35.00f),             // DoorPos
                "v_ilev_gasdoor",
                338.00f,                                             // DoorHeading
                3.0f
            ));

            // 8 - 24/7 Supermarket (Palomino Highlands)
            ctx.Stores.Add(CreateStore(
                id++,
                "24/7 Supermarket (Palomino Highlands)",
                new Vector3(2555.500f, 380.800f, 108.600f),
                new Vector3(2554.85f, 380.85f, 108.62f),            // ClerkPos
                358.00f,                                            // ClerkHeading
                new Vector3(2557.00f, 383.38f, 108.62f),            // RegisterPos
                160.27f,                                            // RegisterHeading
                new Vector3(2549.27f, 384.91f, 108.62f),            // SafePos
                84.84f,                                             // SafeHeading
                new Vector3(2559.78f, 385.45f, 108.62f),            // DoorPos
                "v_ilev_gasdoor",
                95.0f,                                              // DoorHeading
                3.0f
            ));

            // =====================================================================
            // ACE LIQUOR (9)
            // =====================================================================

            // 9 - Liquor Ace (Route 68)
            ctx.Stores.Add(CreateStore(
                id++,
                "Liquor Ace (Route 68)",
                new Vector3(1392.500f, 3606.000f, 34.900f),
                new Vector3(1392.45f, 3606.55f, 34.98f),             // ClerkPos
                195.00f,                                             // ClerkHeading
                new Vector3(1390.90f, 3605.80f, 34.90f),             // RegisterPos
                200.00f,                                             // RegisterHeading
                new Vector3(1394.92f, 3613.89f, 34.98f),             // SafePos
                15.69f,                                              // SafeHeading
                new Vector3(1394.45f, 3606.55f, 34.98f),             // DoorPos
                "v_ilev_gasdoor",                                    // Using generic mesh per your instruction
                0.00f,                                               // DoorHeading
                3.0f
            ));

            // =====================================================================
            // LTD GASOLINE (10–14)
            // =====================================================================

            // 10 - LTD Gasoline (Davis)
            ctx.Stores.Add(CreateStore(
                id++,
                "LTD Gasoline (Davis)",
                new Vector3(-48.5f, -1757.5f, 29.4f),
                new Vector3(-46.56f, -1758.20f, 29.42f),             // ClerkPos
                50.00f,                                              // ClerkHeading
                new Vector3(-46.70f, -1757.80f, 29.42f),             // RegisterPos
                50.00f,                                              // RegisterHeading
                new Vector3(-43.3559f, -1748.3580f, 29.4210f),       // SafePos
                50.15f,                                              // SafeHeading
                new Vector3(-53.25f, -1756.90f, 29.42f),             // DoorPos
                "v_ilev_gasdoor",
                320.00f,                                             // DoorHeading
                3.0f
            ));

            // 11 - LTD Gasoline (Little Seoul)
            ctx.Stores.Add(CreateStore(
                id++,
                "LTD Gasoline (Little Seoul)",
                new Vector3(-707.4f, -914.6f, 19.2f),
                new Vector3(-706.20f, -913.60f, 19.22f),             // ClerkPos
                90.00f,                                              // ClerkHeading
                new Vector3(-705.60f, -912.60f, 19.22f),             // RegisterPos
                90.00f,                                              // RegisterHeading
                new Vector3(-709.69f, -904.05f, 19.21f),             // SafePos
                90.00f,                                              // SafeHeading
                new Vector3(-711.72f, -917.05f, 19.22f),             // DoorPos
                "v_ilev_gasdoor",
                0.00f,                                               // DoorHeading
                3.0f
            ));

            // 12 - LTD Gasoline (Richman Glen)
            ctx.Stores.Add(CreateStore(
                id++,
                "LTD Gasoline (Richman Glen)",
                new Vector3(-1822.90f, 792.05f, 138.20f),
                new Vector3(-1819.90f, 794.40f, 138.20f),            // ClerkPos
                140.00f,                                             // ClerkHeading
                new Vector3(-1818.90f, 794.00f, 138.20f),            // RegisterPos
                140.00f,                                             // RegisterHeading
                new Vector3(-1829.19f, 798.84f, 138.19f),            // SafePos
                136.25f,                                             // SafeHeading
                new Vector3(-1822.050f, 787.95f, 138.20f),           // DoorPos
                "v_ilev_gasdoor",
                45.00f,                                              // DoorHeading
                3.0f
            ));

            // 13 - LTD Gasoline (Mirror Park)
            ctx.Stores.Add(CreateStore(
                id++,
                "LTD Gasoline (Mirror Park)",
                new Vector3(1161.80f, -322.88f, 69.20f),
                new Vector3(1165.05f, -322.60f, 69.20f),             // ClerkPos
                100.00f,                                             // ClerkHeading
                new Vector3(1165.70f, -322.60f, 69.20f),             // RegisterPos
                100.00f,                                             // RegisterHeading
                new Vector3(1159.55f, -314.04f, 69.20f),             // SafePos
                97.00f,                                              // SafeHeading
                new Vector3(1159.60f, -327.00f, 69.20f),             // DoorPos
                "v_ilev_gasdoor",
                235.00f,                                             // DoorHeading
                3.0f
            ));

            // 14 - LTD Gasoline (Grapeseed)
            ctx.Stores.Add(CreateStore(
                id++,
                "LTD Gasoline (Grapeseed)",
                new Vector3(1698.3f, 4924.4f, 42.1f),
                new Vector3(1697.85f, 4922.85f, 42.10f),             // ClerkPos
                320.00f,                                             // ClerkHeading
                new Vector3(1696.70f, 4924.00f, 42.10f),             // RegisterPos
                320.00f,                                             // RegisterHeading
                new Vector3(1707.94f, 4936.36f, 42.06f),             // Corrected SafePos (typo fixed)
                315.88f,                                             // SafeHeading
                new Vector3(1698.45f, 4929.40f, 42.10f),             // DoorPos
                "v_ilev_gasdoor",
                0.00f,                                               // DoorHeading
                3.0f
            ));

            // =====================================================================
            // ROB'S LIQUOR (15–20)
            // =====================================================================

            // 15 - Rob's Liquor (Vespucci Canals)
            ctx.Stores.Add(CreateStore(
                id++,
                "Rob's Liquor (Vespucci Canals)",
                new Vector3(-1221.9f, -908.3f, 12.3f),
                new Vector3(-1221.75f, -908.70f, 12.33f),            // ClerkPos
                30.00f,                                              // ClerkHeading
                new Vector3(-1221.90f, -906.60f, 12.33f),            // RegisterPos
                30.00f,                                              // RegisterHeading
                new Vector3(-1220.80f, -916.02f, 11.32f),            // SafePos
                118.63f,                                             // SafeHeading
                new Vector3(-1226.55f, -902.35f, 12.33f),            // DoorPos
                "v_ilev_ra_door4r",
                215.00f,                                             // DoorHeading
                3.0f
            ));

            // 16 - Rob's Liquor (Pacific Bluffs)
            ctx.Stores.Add(CreateStore(
                id++,
                "Rob's Liquor (Pacific Bluffs)",
                new Vector3(-2966.4f, 391.0f, 15.0f),
                new Vector3(-2966.27f, 390.85f, 15.00f),             // ClerkPos
                90.00f,                                              // ClerkHeading
                new Vector3(-2966.80f, 392.70f, 15.00f),             // RegisterPos
                90.00f,                                              // RegisterHeading
                new Vector3(-2959.62f, 387.15f, 14.04f),             // SafePos
                168.93f,                                             // SafeHeading
                new Vector3(-2974.00f, 390.75f, 15.00f),             // DoorPos
                "v_ilev_ra_door4r",
                260.00f,                                             // DoorHeading
                3.0f
            ));

            // 17 - Rob's Liquor (Morningwood)
            ctx.Stores.Add(CreateStore(
                id++,
                "Rob's Liquor (Morningwood)",
                new Vector3(-1487.5f, -378.0f, 40.2f),
                new Vector3(-1486.50f, -377.60f, 40.20f),            // ClerkPos
                130.00f,                                             // ClerkHeading
                new Vector3(-1485.90f, -376.60f, 40.20f),            // RegisterPos
                130.00f,                                             // RegisterHeading
                new Vector3(-1478.87f, -375.43f, 39.16f),            // SafePos
                227.58f,                                             // SafeHeading (corrected from 22758f)
                new Vector3(-1491.15f, -383.80f, 40.16f),            // DoorPos
                "v_ilev_ra_door4r",
                320.00f,                                             // DoorHeading
                3.0f
            ));

            // 18 - Rob's Liquor (Murrieta Heights)
            ctx.Stores.Add(CreateStore(
                id++,
                "Rob's Liquor (Murrieta Heigts)",
                new Vector3(1134.2f, -982.4f, 46.4f),
                new Vector3(1133.95f, -982.60f, 46.41f),             // ClerkPos
                275.00f,                                             // ClerkHeading
                new Vector3(1132.70f, -980.80f, 46.41f),             // RegisterPos
                275.00f,                                             // RegisterHeading
                new Vector3(1126.81f, -980.15f, 45.41f),             // SafePos
                359.61f,                                             // SafeHeading
                new Vector3(1141.55f, -981.00f, 46.41f),             // DoorPos
                "v_ilev_ra_door4r",
                95.00f,                                              // DoorHeading
                3.0f
            ));
            
            // 19 - Rob's Liquor (Grand Senora Desert)
            ctx.Stores.Add(CreateStore(
                id++,
                "Rob's Liquor (Grand Senora Desert)",
                new Vector3(1166.66f, 2708.02f, 38.157f),
                new Vector3(1165.85f, 2711.05f, 38.157f),            // ClerkPos (placeholder from your file)
                175.00f,                                             // ClerkHeading
                new Vector3(1165.80f, 2711.70f, 38.157f),            // RegisterPos
                90.00f,                                              // RegisterHeading
                new Vector3(1169.30f, 2717.81f, 37.15f),             // SafePos
                270.44f,                                             // SafeHeading
                new Vector3(1166.55f, 2711.05f, 38.157f),            // DoorPos
                "v_ilev_ra_door4r",
                0.00f,                                               // DoorHeading
                3.0f
            ));

            // =====================================================================
            // 24/7 SUPERMARKETS (20–21) FUTURE EXPANSION (currently using generic locations for testing)
            // =====================================================================

            // frdfare's Vinewood Plaza 24/7 Add-On expansion
            if (IsDLcLoaded("vwp_247shop"))
            {
                // 20 - 24/7 Supermarket (Vinewood Plaza)
                ctx.Stores.Add(CreateStore(
                    id++,
                    "24/7 Supermarket (Vinewood Plaza)",
                    new Vector3(200.728f, -29.773f, 69.917f),
                    new Vector3(192.274f, -21.512f, 69.920f),             // ClerkPos
                    255.00f,                                              // ClerkHeading
                    new Vector3(192.274f, -21.512f, 69.920f),             // RegisterPos
                    255.00f,                                              // RegisterHeading
                    new Vector3(198.773f, -16.020f, 69.920f),             // SafePos
                    335.55f,                                             // SafeHeading
                    new Vector3(200.728f, -29.773f, 69.917f),             // DoorPos
                    "v_ilev_gasdoor",
                    75.00f,                                             // DoorHeading
                    3.0f
                ));
            }

            // frdfare's Little Seoul 24/7 Add-On expansion
            if (IsDLcLoaded("vsb_247shop"))
            {
                // 21 - 24/7 Supermarket (Little Seoul)
                ctx.Stores.Add(CreateStore(
                    id++,
                    "24/7 Supermarket (Little Seoul)",
                    new Vector3(-693.725f, -858.959f, 23.727f),
                    new Vector3(-691.570f, -859.322f, 23.758f),             // ClerkPos
                    90.00f,                                              // ClerkHeading
                    new Vector3(-691.570f, -859.322f, 23.758f),             // RegisterPos
                    90.00f,                                              // RegisterHeading
                    new Vector3(-691.636f, -867.996f, 23.700f),             // SafePos
                    185.55f,                                             // SafeHeading
                    new Vector3(-697.084f, -859.229f, 23.693f),             // DoorPos
                    "v_ilev_gasdoor",
                    270.00f,                                             // DoorHeading
                    3.0f
                ));
            }

            // frdfare's Vinewood Blvd 24/7 Add-On expansion
            if (IsDLcLoaded("vwb_247shop"))
            {
                // 22 - 24/7 Supermarket (Vinewood Blvd)
                ctx.Stores.Add(CreateStore(
                    id++,
                    "24/7 Supermarket (Vinewood Blvd)",
                    new Vector3(153.277f, 240.093f, 106.976f),
                    new Vector3(149.760f, 243.379f, 106.976f),             // ClerkPos
                    240.00f,                                              // ClerkHeading
                    new Vector3(149.760f, 243.379f, 106.976f),             // RegisterPos
                    240.00f,                                              // RegisterHeading
                    new Vector3(155.271f, 247.452f, 106.976f),             // SafePos
                    335.45f,                                             // SafeHeading
                    new Vector3(152.501f, 237.292f, 106.971f),             // DoorPos
                    "v_ilev_gasdoor",
                    340.00f,                                             // DoorHeading
                    3.0f
                ));
            }

            // Patoche's Paleto Gas Station Add-On expansion
            if (IsDLcLoaded("int_paletogas"))
            {
                // 23 - 24/7 Supermarket (Paleto Bay)
                ctx.Stores.Add(CreateStore(
                    id++,
                    "24/7 Supermarket (Paleto Bay)",
                    new Vector3(164.011f, 6638.728f, 31.699f),
                    new Vector3(162.138f, 6643.184f, 31.699f),             // ClerkPos
                    180.00f,                                              // ClerkHeading
                    new Vector3(162.138f, 6643.184f, 31.699f),             // RegisterPos
                    180.00f,                                              // RegisterHeading
                    new Vector3(168.833f, 6644.659f, 31.699f),             // SafePos
                    320.45f,                                             // SafeHeading
                    new Vector3(162.127f, 6636.585f, 31.556f),             // DoorPos
                    "v_ilev_gasdoor",
                    314.00f,                                             // DoorHeading
                    3.0f
                ));
            }

            // frdfare's Elgin/Hawick 24/7 Add-On expansion
            if (IsDLcLoaded("elg_247shop"))
            {
                // 24 - 24/7 Supermarket (Elgin/Hawick)
                ctx.Stores.Add(CreateStore(
                    id++,
                    "24/7 Supermarket (Elgin/Hawick)",
                    new Vector3(533.340f, -156.150f, 57.045f),
                    new Vector3(535.999f, -152.930f, 57.045f),             // ClerkPos
                    135.00f,                                              // ClerkHeading
                    new Vector3(535.999f, -152.930f, 57.045f),             // RegisterPos
                    135.00f,                                              // RegisterHeading
                    new Vector3(549.873f, -153.367f, 57.041f),             // SafePos
                    335.00f,                                             // SafeHeading
                    new Vector3(527.295f, -157.474f, 57.078f),             // DoorPos
                    "v_ilev_gasdoor",
                    270.00f,                                             // DoorHeading
                    3.0f
                ));
            }

            // frdfare's Korea Plaza 24/7 Add-On expansion
            if (IsDLcLoaded("kt_247shop"))
            {
                // 25 - 24/7 Supermarket (Korea Plaza)
                ctx.Stores.Add(CreateStore(
                    id++,
                    "24/7 Supermarket (Korea Plaza)",
                    new Vector3(-578.48f, -1014.375f, 22.325f),
                    new Vector3(-575.35f, -1015.198f, 22.325f),             // ClerkPos
                    88.85f,                                              // ClerkHeading
                    new Vector3(-575.35f, -1015.198f, 22.325f),             // RegisterPos
                    88.85f,                                              // RegisterHeading
                    new Vector3(-591.75f, -1012.621f, 22.325f),             // SafePos
                    80.00f,                                             // SafeHeading
                    new Vector3(-578.15f, -1016.550f, 22.325f),             // DoorPos
                    "v_ilev_gasdoor",
                    358.00f,                                             // DoorHeading
                    3.0f
                ));
            }
        }

        private static List<Vector3> DefaultCamerasFor(int storeId, TrackedStore store)
        {
            // Rockstar interior camera offsets
            Vector3[] cams24_7 =
            {
                new Vector3(-2.10f, -1.20f, 2.20f),
                new Vector3( 2.30f, -1.10f, 2.20f),
                new Vector3( 0.10f,  3.40f, 2.20f)
            };

            Vector3[] camsLTD =
            {
                new Vector3(-2.00f, -1.00f, 2.30f),
                new Vector3( 2.10f, -1.00f, 2.30f),
                new Vector3( 0.00f,  3.20f, 2.30f)
            };

            Vector3[] camsRobs =
            {
                new Vector3(-2.40f, -1.20f, 2.10f),
                new Vector3( 2.40f, -1.20f, 2.10f),
                new Vector3( 0.00f,  3.60f, 2.10f)
            };

            Vector3[] camsAce =
            {
                new Vector3(-2.60f, -1.40f, 2.00f),
                new Vector3( 2.60f, -1.40f, 2.00f),
                new Vector3( 0.00f,  3.80f, 2.00f)
            };

            // Interior rotation list (confirmed by you)
            float[] interiorRot =
            {
                242.5f, 0.0f, 0.0f, 255.0f, 70.0f,
                330.0f, 300.0f, 180.0f, 320.0f, 200.0f,
                50.0f, 90.0f, 140.0f, 100.0f, 320.0f,
                30.0f, 90.0f, 130.0f, 275.0f, 90.0f,
                90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f
            };

            // Determine interior type
            Vector3[] offsets;

            if (storeId <= 8)
                offsets = cams24_7;
            else if (storeId == 9)
                offsets = camsAce;
            else if (storeId >= 10 && storeId <= 14)
                offsets = camsLTD;
            else
                offsets = camsRobs;

            // ✅ FIX: ensure at least 3 cameras per store
            int camCount = store.Cameras.Count;
            if (camCount == 0)
                camCount = 3;

            List<Vector3> result = new List<Vector3>();

            float rot = interiorRot[storeId];
            float rad = rot * (float)(Math.PI / 180.0);

            float sin = (float)Math.Sin(rad);
            float cos = (float)Math.Cos(rad);

            for (int i = 0; i < camCount; i++)
            {
                Vector3 off = offsets[i % offsets.Length];

                // Rotate offset by interior rotation
                float rx = off.X * cos - off.Y * sin;
                float ry = off.X * sin + off.Y * cos;

                Vector3 world = new Vector3(
                    store.ClerkPos.X + rx,
                    store.ClerkPos.Y + ry,
                    store.ClerkPos.Z + off.Z
                );

                result.Add(world);
            }

            return result;
        }

        private static TrackedStore CreateStore(
            int id,
            string name,
            Vector3 storePos,
            Vector3 clerkPos,
            float clerkHeading,
            Vector3 registerPos,
            float registerHeading,
            Vector3 safePos,
            float safeHeading,
            Vector3 doorPos,
            string doorModelName,
            float doorHeading,
            float radius)
        {
            TrackedStore s = new TrackedStore
            {
                Id = id,
                Name = name,
                StorePos = storePos,
                ClerkPos = clerkPos,
                ClerkHeading = clerkHeading,
                RegisterPos = registerPos,
                RegisterHeading = registerHeading,
                SafePos = safePos,
                SafeHeading = safeHeading,
                DoorPos = doorPos,
                DoorModelHash = Function.Call<int>(Hash.GET_HASH_KEY, doorModelName),
                DoorHeading = doorHeading,
                Radius = radius,
                Cameras = new List<CameraData>() // Will be replaced by DefaultCamerasFor
            };

            // ⭐ NEW — assign interior ID
            s.InteriorId = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, clerkPos.X, clerkPos.Y, clerkPos.Z);

            // Build cameras using the rebuilt Rockstar-accurate system
            var camPositions = DefaultCamerasFor(id, s);
            s.Cameras = camPositions.Select(pos => new CameraData { Position = pos }).ToList();
            return s;
        }

        private static void LoadAllStoreIPLs()
        {
            // 24/7
            Function.Call(Hash.REQUEST_IPL, "v_24hr");
            Function.Call(Hash.REQUEST_IPL, "v_24hr_lod");

            // LTD
            Function.Call(Hash.REQUEST_IPL, "v_ltd");
            Function.Call(Hash.REQUEST_IPL, "v_ltd_lod");

            // Rob's Liquor
            Function.Call(Hash.REQUEST_IPL, "v_rob_liq");
            Function.Call(Hash.REQUEST_IPL, "v_rob_liq_lod");

            // Ace Liquor (unique interior)
            // No dedicated IPL — uses base interior
        }

        public static void RebuildCamerasForAllStores(StoreContext ctx)
        {
            foreach (var store in ctx.Stores)
            {
                var camPositions = DefaultCamerasFor(store.Id, store);
                store.Cameras = camPositions.Select(pos => new CameraData { Position = pos }).ToList();
            }
        }
    }
}
