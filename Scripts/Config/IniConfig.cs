using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GTA;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Config;

namespace StoreRobberyEnhanced
{
    internal class IniConfig
    {
        private readonly Script _script;

        private string _settingsFolder;
        private string _mainIniPath;
        private string _storeStatePath;
        private string _stalkerIniPath;
        private string _debugIniPath;

        public string StoreStatePath => _storeStatePath;
        public string MainIniPath => _mainIniPath;
        public string DebugIniPath => _debugIniPath;

        // ------------------------------------------------------------
        // NON‑DEBUG SETTINGS
        // ------------------------------------------------------------

        // TIMER UI SETTINGS
        public float TimerPosX { get; private set; }
        public float TimerPosY { get; private set; }
        public float TimerScale { get; private set; }
        public bool TimerDropShadow { get; private set; }
        public bool TimerBackground { get; private set; }
        public float TimerBgWidth { get; private set; }
        public float TimerBgHeight { get; private set; }
        public float TimerBgOpacity { get; private set; }
        public int TimerBgR { get; private set; }
        public int TimerBgG { get; private set; }
        public int TimerBgB { get; private set; }

        // PUBLIC SETTINGS
        public int RegisterMinAmount { get; private set; }
        public int RegisterMaxAmount { get; private set; }
        public int SafeMinAmount { get; private set; }
        public int SafeMaxAmount { get; private set; }

        public float PayoutMultiplier { get; private set; }

        public bool EnableMessages { get; private set; }
        public bool EnablePolice { get; private set; }
        public bool EnableCameras { get; private set; }
        public bool UseStoreNames { get; private set; }

        public int CooldownMinutes { get; private set; }
        public int RobberyTimeLimit { get; private set; }
        public int CameraGraceSeconds { get; private set; }
        public int EscapeDistance { get; private set; }
        public int SafeCrackTimeSeconds { get; private set; }

        // ⭐ PHASE 3 ADDITIONS
        public int SilentAlarmDelaySeconds { get; private set; }
        public int ClerkCallDelaySeconds { get; private set; }
        public int TimeEscalationSeconds { get; private set; }

        // STALKER SETTINGS
        public bool EnableStalkerMsg { get; private set; }
        public bool EnableStalkerCall { get; private set; }
        public int StalkerCallChance { get; private set; }

        public string StalkerCallerImage { get; private set; }
        public string StalkerCallerName { get; private set; }
        public int StalkerMessageType { get; private set; }

        public int MaxMessagesPerRobbery { get; private set; }
        public int MessageCooldownSeconds { get; private set; }

        public List<string> StalkerRobberyMsgs { get; private set; } = new List<string>();
        public List<string> StalkerEscapeMsgs { get; private set; } = new List<string>();
        public List<string> StalkerKnockoutMsgs { get; private set; } = new List<string>();
        public List<string> StalkerGunKillMsgs { get; private set; } = new List<string>();
        public List<string> StalkerMeleeKillMsgs { get; private set; } = new List<string>();
        public List<string> StalkerCallAnsweredMsgs { get; private set; } = new List<string>();
        public List<string> StalkerCallIgnoredMsgs { get; private set; } = new List<string>();

        public IniConfig(Script script)
        {
            _script = script;
            SetupPaths();
            EnsureDefaultFilesExist();
        }

        private void SetupPaths()
        {
            string scriptsFolder = AppDomain.CurrentDomain.BaseDirectory;

            _settingsFolder = Path.Combine(scriptsFolder, "StoreRobberyEnhanced");
            Directory.CreateDirectory(_settingsFolder);

            _mainIniPath = Path.Combine(_settingsFolder, "MainSettings.ini");
            _storeStatePath = Path.Combine(_settingsFolder, "StoreState.ini");
            _stalkerIniPath = Path.Combine(_settingsFolder, "StalkerMessages.ini");
            _debugIniPath = Path.Combine(_settingsFolder, "DebugSettings.ini");
        }

        private void EnsureDefaultFilesExist()
        {
            if (!Directory.Exists(_settingsFolder))
                Directory.CreateDirectory(_settingsFolder);

            if (!File.Exists(_mainIniPath))
            {
                SimpleIni ini = new SimpleIni(_mainIniPath);

                // MAIN SETTINGS
                ini.WriteBool("Main Settings", "EnableMessages", true);
                ini.WriteBool("Main Settings", "EnablePolice", true);
                ini.WriteInt("Main Settings", "CooldownMinutes", 30);
                ini.WriteInt("Main Settings", "RobberyTimeLimit", 180);
                ini.WriteInt("Main Settings", "EscapeDistance", 100);

                // STORE SETTINGS
                ini.WriteInt("Store Settings", "RegisterMinAmount", 1000);
                ini.WriteInt("Store Settings", "RegisterMaxAmount", 6000);
                ini.WriteInt("Store Settings", "SafeMinAmount", 20000);
                ini.WriteInt("Store Settings", "SafeMaxAmount", 60000);
                ini.WriteBool("Store Settings", "EnableCameras", true);
                ini.WriteBool("Store Settings", "UseStoreNames", true);
                ini.WriteInt("Store Settings", "CameraGraceSeconds", 30);
                ini.WriteInt("Store Settings", "SafeCrackTimeSeconds", 60);
                ini.WriteString("Store Settings", "PayoutMultiplier", "2.0");

                // ⭐ PHASE 3 DEFAULTS
                ini.WriteInt("Police", "SilentAlarmDelaySeconds", 4);
                ini.WriteInt("Police", "ClerkCallDelaySeconds", 7);
                ini.WriteInt("Police", "TimeEscalationSeconds", 25);

                // STALKER SETTINGS
                ini.WriteBool("Stalker", "EnableStalkerMsg", true);
                ini.WriteBool("Stalker", "EnableStalkerCall", true);
                ini.WriteInt("Stalker", "StalkerCallChance", 25);
                ini.WriteString("Stalker", "CallerImage", "CHAR_ARTHUR");
                ini.WriteString("Stalker", "CallerName", "NO CALLER ID");
                ini.WriteInt("Stalker", "MaxMessagesPerRobbery", 5);
                ini.WriteInt("Stalker", "MessageCooldownSeconds", 20);
                ini.WriteInt("Stalker", "MessageType", 1);

                // TIMER UI
                ini.WriteFloat("TIMER_UI", "PositionX", 0.265f);
                ini.WriteFloat("TIMER_UI", "PositionY", 0.895f);
                ini.WriteFloat("TIMER_UI", "Scale", 0.40f);
                ini.WriteBool("TIMER_UI", "DropShadow", true);
                ini.WriteBool("TIMER_UI", "Background", true);
                ini.WriteFloat("TIMER_UI", "BackgroundWidth", 0.10f);
                ini.WriteFloat("TIMER_UI", "BackgroundHeight", 0.032f);
                ini.WriteFloat("TIMER_UI", "BackgroundOpacity", 0.6f);
                ini.WriteInt("TIMER_UI", "BackgroundColorR", 0);
                ini.WriteInt("TIMER_UI", "BackgroundColorG", 0);
                ini.WriteInt("TIMER_UI", "BackgroundColorB", 0);

                ini.Save();
            }

            if (!File.Exists(_storeStatePath))
            {
                SimpleIni ini = new SimpleIni(_storeStatePath);
                ini.Save();
            }

            if (!File.Exists(_stalkerIniPath))
            {
                StalkerMessageConfigCreator.CreateDefaultMessages(_stalkerIniPath);
            }

            if (!File.Exists(_debugIniPath))
            {
                SimpleIni debugIni = new SimpleIni(_debugIniPath);
                AppendDebugDefaults(debugIni);
                debugIni.Save();
            }
        }

        // ------------------------------------------------------------
        // LOAD MAIN SETTINGS
        // ------------------------------------------------------------
        public void LoadSettings()
        {
            SimpleIni ini = new SimpleIni(_mainIniPath);

            // MAIN SETTINGS
            EnableMessages = ini.ReadBool("Main Settings", "EnableMessages", true);
            EnablePolice = ini.ReadBool("Main Settings", "EnablePolice", true);
            CooldownMinutes = ini.ReadInt("Main Settings", "CooldownMinutes", 30);
            RobberyTimeLimit = ini.ReadInt("Main Settings", "RobberyTimeLimit", 180);
            EscapeDistance = ini.ReadInt("Main Settings", "EscapeDistance", 100);

            // STORE SETTINGS
            RegisterMinAmount = ini.ReadInt("Store Settings", "RegisterMinAmount", 1000);
            RegisterMaxAmount = ini.ReadInt("Store Settings", "RegisterMaxAmount", 6000);
            SafeMinAmount = ini.ReadInt("Store Settings", "SafeMinAmount", 20000);
            SafeMaxAmount = ini.ReadInt("Store Settings", "SafeMaxAmount", 60000);
            EnableCameras = ini.ReadBool("Store Settings", "EnableCameras", true);
            UseStoreNames = ini.ReadBool("Store Settings", "UseStoreNames", true);
            CameraGraceSeconds = ini.ReadInt("Store Settings", "CameraGraceSeconds", 30);
            SafeCrackTimeSeconds = ini.ReadInt("Store Settings", "SafeCrackTimeSeconds", 60);

            string multStr = ini.ReadString("Store Settings", "PayoutMultiplier", "2.0");
            if (!float.TryParse(multStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float mult))
                mult = 2.0f;
            PayoutMultiplier = mult;

            // ⭐ PHASE 3 LOAD
            SilentAlarmDelaySeconds = ini.ReadInt("Police", "SilentAlarmDelaySeconds", 4);
            ClerkCallDelaySeconds = ini.ReadInt("Police", "ClerkCallDelaySeconds", 7);
            TimeEscalationSeconds = ini.ReadInt("Police", "TimeEscalationSeconds", 25);

            // STALKER SETTINGS
            EnableStalkerMsg = ini.ReadBool("Stalker", "EnableStalkerMsg", true);
            EnableStalkerCall = ini.ReadBool("Stalker", "EnableStalkerCall", true);
            StalkerCallChance = ini.ReadInt("Stalker", "StalkerCallChance", 25);
            StalkerCallerImage = ini.ReadString("Stalker", "CallerImage", "CHAR_ARTHUR");
            StalkerCallerName = ini.ReadString("Stalker", "CallerName", "NO CALLER ID");
            MaxMessagesPerRobbery = ini.ReadInt("Stalker", "MaxMessagesPerRobbery", 5);
            MessageCooldownSeconds = ini.ReadInt("Stalker", "MessageCooldownSeconds", 20);
            StalkerMessageType = ini.ReadInt("Stalker", "MessageType", 1);

            // TIMER UI
            TimerPosX = ini.ReadFloat("TIMER_UI", "PositionX", 0.265f);
            TimerPosY = ini.ReadFloat("TIMER_UI", "PositionY", 0.895f);
            TimerScale = ini.ReadFloat("TIMER_UI", "Scale", 0.40f);
            TimerDropShadow = ini.ReadBool("TIMER_UI", "DropShadow", true);
            TimerBackground = ini.ReadBool("TIMER_UI", "Background", true);
            TimerBgWidth = ini.ReadFloat("TIMER_UI", "BackgroundWidth", 0.10f);
            TimerBgHeight = ini.ReadFloat("TIMER_UI", "BackgroundHeight", 0.032f);
            TimerBgOpacity = ini.ReadFloat("TIMER_UI", "BackgroundOpacity", 0.6f);
            TimerBgR = ini.ReadInt("TIMER_UI", "BackgroundColorR", 0);
            TimerBgG = ini.ReadInt("TIMER_UI", "BackgroundColorG", 0);
            TimerBgB = ini.ReadInt("TIMER_UI", "BackgroundColorB", 0);

            // DEBUG SETTINGS
            SimpleIni debugIni = new SimpleIni(_debugIniPath);
            LoadDebugSettings(debugIni);
            WriteBackDebugSettings(debugIni);
            debugIni.Save();

            WriteBackSettings(ini);

            ini.Save();

            LoadStalkerMessages();
        }

        // ------------------------------------------------------------
        // WRITE BACK MAIN SETTINGS
        // ------------------------------------------------------------
        private void WriteBackSettings(SimpleIni ini)
        {
            // MAIN SETTINGS
            ini.WriteBool("Main Settings", "EnableMessages", EnableMessages);
            ini.WriteBool("Main Settings", "EnablePolice", EnablePolice);
            ini.WriteInt("Main Settings", "CooldownMinutes", CooldownMinutes);
            ini.WriteInt("Main Settings", "RobberyTimeLimit", RobberyTimeLimit);
            ini.WriteInt("Main Settings", "EscapeDistance", EscapeDistance);

            // STORE SETTINGS
            ini.WriteInt("Store Settings", "RegisterMinAmount", RegisterMinAmount);
            ini.WriteInt("Store Settings", "RegisterMaxAmount", RegisterMaxAmount);
            ini.WriteInt("Store Settings", "SafeMinAmount", SafeMinAmount);
            ini.WriteInt("Store Settings", "SafeMaxAmount", SafeMaxAmount);
            ini.WriteBool("Store Settings", "EnableCameras", EnableCameras);
            ini.WriteBool("Store Settings", "UseStoreNames", UseStoreNames);
            ini.WriteInt("Store Settings", "CameraGraceSeconds", CameraGraceSeconds);
            ini.WriteInt("Store Settings", "SafeCrackTimeSeconds", SafeCrackTimeSeconds);

            // ⭐ PHASE 3 LOAD
            ini.WriteInt("Police", "SilentAlarmDelaySeconds", SilentAlarmDelaySeconds);
            ini.WriteInt("Police", "ClerkCallDelaySeconds", ClerkCallDelaySeconds);
            ini.WriteInt("Police", "TimeEscalationSeconds", TimeEscalationSeconds);

            // STALKER SETTINGS
            ini.WriteBool("Stalker", "EnableStalkerMsg", EnableStalkerMsg);
            ini.WriteBool("Stalker", "EnableStalkerCall", EnableStalkerCall);
            ini.WriteInt("Stalker", "StalkerCallChance", StalkerCallChance);
            ini.WriteString("Stalker", "CallerImage", StalkerCallerImage);
            ini.WriteString("Stalker", "CallerName", StalkerCallerName);
            ini.WriteInt("Stalker", "MaxMessagesPerRobbery", MaxMessagesPerRobbery);
            ini.WriteInt("Stalker", "MessageCooldownSeconds", MessageCooldownSeconds);
            ini.WriteInt("Stalker", "MessageType", StalkerMessageType);

            // TIMER UI
            ini.WriteFloat("TIMER_UI", "PositionX", TimerPosX);
            ini.WriteFloat("TIMER_UI", "PositionY", TimerPosY);
            ini.WriteFloat("TIMER_UI", "Scale", TimerScale);
            ini.WriteBool("TIMER_UI", "DropShadow", TimerDropShadow);
            ini.WriteBool("TIMER_UI", "Background", TimerBackground);
            ini.WriteFloat("TIMER_UI", "BackgroundWidth", TimerBgWidth);
            ini.WriteFloat("TIMER_UI", "BackgroundHeight", TimerBgHeight);
            ini.WriteFloat("TIMER_UI", "BackgroundOpacity", TimerBgOpacity);
            ini.WriteInt("TIMER_UI", "BackgroundColorR", TimerBgR);
            ini.WriteInt("TIMER_UI", "BackgroundColorG", TimerBgG);
            ini.WriteInt("TIMER_UI", "BackgroundColorB", TimerBgB);
        }

        // ------------------------------------------------------------
        // LOAD STALKER MESSAGES
        // ------------------------------------------------------------
        private void LoadStalkerMessages()
        {
            SimpleIni ini = new SimpleIni(_stalkerIniPath);

            StalkerRobberyMsgs = ReadLines(ini, "Robbery");
            StalkerEscapeMsgs = ReadLines(ini, "Escape");
            StalkerKnockoutMsgs = ReadLines(ini, "Knockout");
            StalkerGunKillMsgs = ReadLines(ini, "GunKill");
            StalkerMeleeKillMsgs = ReadLines(ini, "MeleeKill");
            StalkerCallAnsweredMsgs = ReadLines(ini, "CallAnswered");
            StalkerCallIgnoredMsgs = ReadLines(ini, "CallIgnored");
        }

        private List<string> ReadLines(SimpleIni ini, string section)
        {
            List<string> list = new List<string>();
            int index = 1;

            while (true)
            {
                string key = "Line" + index;
                string value = ini.ReadString(section, key, null);

                if (value == null)
                    break;

                if (!string.IsNullOrWhiteSpace(value))
                    list.Add(value);

                index++;
            }

            return list;
        }

        // ------------------------------------------------------------
        // WRITE DEFAULT STORE STATE (used when new stores are added)
        // ------------------------------------------------------------
        public void WriteDefaultStoreState(SimpleIni ini, TrackedStore store)
        {
            string sec = "Store" + store.Id;

            ini.WriteString(sec, "StoreName", store.Name);
            ini.WriteBool(sec, "IsRobbed", false);
            ini.WriteBool(sec, "CooldownActive", false);
            ini.WriteBool(sec, "SafeCracked", false);
            ini.WriteBool(sec, "AlarmTriggered", false);
            ini.WriteBool(sec, "ClerkDeathHandled", false);
            ini.WriteBool(sec, "ClerkKilledWithGun", false);
            ini.WriteString(sec, "LastRobbedUtc", "");
        }

        // ------------------------------------------------------------
        // AUTO‑MERGE: ADD MISSING KEYS WITHOUT OVERWRITING EXISTING DATA
        // ------------------------------------------------------------
        public bool AddMissingStoreKeys(SimpleIni ini, TrackedStore store)
        {
            bool modified = false;
            string sec = "Store" + store.Id;

            void Ensure(string key, string defaultValue)
            {
                if (!ini.KeyExists(sec, key))
                {
                    ini.WriteString(sec, key, defaultValue);
                    modified = true;
                }
            }

            Ensure("StoreName", store.Name);
            Ensure("IsRobbed", "false");
            Ensure("CooldownActive", "false");
            Ensure("SafeCracked", "false");
            Ensure("AlarmTriggered", "false");
            Ensure("ClerkDeathHandled", "false");
            Ensure("ClerkKilledWithGun", "false");
            Ensure("LastRobbedUtc", "");

            return modified;
        }

        // ------------------------------------------------------------
        // SAVE STORE STATE
        // ------------------------------------------------------------
        public void SaveStoreState(TrackedStore store)
        {
            SimpleIni ini = new SimpleIni(_storeStatePath);

            string sec = "Store" + store.Id;

            ini.WriteString(sec, "StoreName", store.Name);
            ini.WriteBool(sec, "IsRobbed", store.IsRobbed);
            ini.WriteBool(sec, "CooldownActive", store.CooldownActive);
            ini.WriteBool(sec, "SafeCracked", store.SafeCracked);
            ini.WriteBool(sec, "AlarmTriggered", store.AlarmTriggered);

            ini.WriteBool(sec, "ClerkDeathHandled", store.ClerkDeathHandled);
            ini.WriteBool(sec, "ClerkKilledWithGun", store.ClerkKilledWithGun);

            ini.WriteString(
                sec,
                "LastRobbedUtc",
                store.LastRobbedUtc == DateTime.MinValue
                    ? ""
                    : store.LastRobbedUtc.ToString("o")
            );

            ini.Save();
        }

        // ------------------------------------------------------------
        // LOAD STORE STATE
        // ------------------------------------------------------------
        public void LoadStoreState(TrackedStore store)
        {
            if (!File.Exists(_storeStatePath))
                return;

            SimpleIni ini = new SimpleIni(_storeStatePath);
            string sec = "Store" + store.Id;

            store.Name = ini.ReadString(sec, "StoreName", "");
            store.IsRobbed = ini.ReadBool(sec, "IsRobbed", false);
            store.CooldownActive = ini.ReadBool(sec, "CooldownActive", false);
            store.SafeCracked = ini.ReadBool(sec, "SafeCracked", false);
            store.AlarmTriggered = ini.ReadBool(sec, "AlarmTriggered", false);

            store.ClerkDeathHandled = ini.ReadBool(sec, "ClerkDeathHandled", false);
            store.ClerkKilledWithGun = ini.ReadBool(sec, "ClerkKilledWithGun", false);

            string robbedStr = ini.ReadString(sec, "LastRobbedUtc", "");
            if (DateTime.TryParse(robbedStr, null, DateTimeStyles.RoundtripKind, out DateTime robbed))
                store.LastRobbedUtc = robbed;

        }

        // ------------------------------------------------------------
        // PREPOPULATE STORESTATE.INI WITH ALL STORES (initial creation)
        // ------------------------------------------------------------
        public void PrepopulateStoreState(List<TrackedStore> stores)
        {
            SimpleIni ini = new SimpleIni(_storeStatePath);

            foreach (TrackedStore store in stores)
                WriteDefaultStoreState(ini, store);

            ini.Save();
        }

        // =====================================================================
        // DEBUG SECTION (NOW IN DebugSettings.ini)
        // =====================================================================

        // DEBUG SETTINGS (FIELDS)
        public bool EnableDebug { get; set; }
        public bool EnableDebugTimer { get; set; }

        public bool Debug_RobberyStart { get; set; }
        public bool Debug_SafeCrack { get; set; }
        public bool Debug_CameraAlarm { get; set; }
        public bool Debug_Escape { get; set; }
        public bool Debug_Payout { get; set; }
        public bool Debug_Cooldown { get; set; }
        public bool Debug_Stalker { get; set; }
        public bool Debug_UI { get; set; }
        public bool Debug_Banner { get; set; }
        public bool Debug_Timer { get; set; }
        public bool Debug_StalkerCall { get; set; }
        public bool Debug_StoreDiag { get; set; }
        public bool Debug_MultiPos { get; set; }
        public bool Debug_MultiActions { get; set; }
        public bool Debug_CameraDebug { get; set; }

        // NEW DEBUG PLATFORM SETTINGS
        public bool OverlayVisible { get; set; }
        public int DebugLevel { get; set; }

        // Keybinds
        public int ModifierKey { get; set; }
        public int Action_RobberyStart { get; set; }
        public int Action_SafeCrack { get; set; }
        public int Action_SafeCrackMini { get; set; }
        public int Action_CameraAlarm { get; set; }
        public int Action_Escape { get; set; }
        public int Action_Payout { get; set; }
        public int Action_Cooldown { get; set; }
        public int Action_Stalker { get; set; }
        public int Action_StalkerCall { get; set; }
        public int Action_UI { get; set; }
        public int Action_Banner { get; set; }
        public int Action_Timer { get; set; }
        public int Action_StoreDiag { get; set; }
        public int Action_MultiPos { get; set; }
        public int Action_MiscActions { get; set; }
        public int Action_CameraDebug { get; set; }
        public int ToggleKey { get; set; }

        // Scenarios
        public int Scenario_FullRobbery { get; set; }
        public int Scenario_QuickLoot { get; set; }

        // Profiler
        public bool EnableProfiler { get; set; }
        public bool Profiler_AutoDump { get; set; }
        public int Profiler_DumpInterval { get; set; }

        // File Manager
        public bool EnableLogging { get; set; }
        public bool EnableFileManager { get; set; }
        public bool EnableEvents { get; set; }
        public bool AutoSnapshotOnScenario { get; set; }

        // ------------------------------------------------------------
        // APPEND DEBUG DEFAULTS (FOR DebugSettings.ini)
        // ------------------------------------------------------------
        private void AppendDebugDefaults(SimpleIni ini)
        {
            // Comments (these will be written before [Debug] in DebugSettings.ini)
            ini.WriteComment("Debug", "############################################################");
            ini.WriteComment("Debug", "DEBUG SETTINGS");
            ini.WriteComment("Debug", "Real Windows Virtual-Key Codes Reference");
            ini.WriteComment("Debug", "############################################################");
            ini.WriteComment("Debug", "");
            ini.WriteComment("Debug", "NUMPAD KEYS");
            ini.WriteComment("Debug", "96  = NumPad0");
            ini.WriteComment("Debug", "97  = NumPad1");
            ini.WriteComment("Debug", "98  = NumPad2");
            ini.WriteComment("Debug", "99  = NumPad3");
            ini.WriteComment("Debug", "100 = NumPad4");
            ini.WriteComment("Debug", "101 = NumPad5");
            ini.WriteComment("Debug", "102 = NumPad6");
            ini.WriteComment("Debug", "103 = NumPad7");
            ini.WriteComment("Debug", "104 = NumPad8");
            ini.WriteComment("Debug", "105 = NumPad9");
            ini.WriteComment("Debug", "");
            ini.WriteComment("Debug", "FUNCTION KEYS");
            ini.WriteComment("Debug", "112 = F1");
            ini.WriteComment("Debug", "113 = F2");
            ini.WriteComment("Debug", "114 = F3");
            ini.WriteComment("Debug", "115 = F4");
            ini.WriteComment("Debug", "116 = F5");
            ini.WriteComment("Debug", "117 = F6");
            ini.WriteComment("Debug", "118 = F7");
            ini.WriteComment("Debug", "119 = F8");
            ini.WriteComment("Debug", "120 = F9");
            ini.WriteComment("Debug", "121 = F10");
            ini.WriteComment("Debug", "122 = F11");
            ini.WriteComment("Debug", "123 = F12");
            ini.WriteComment("Debug", "");
            ini.WriteComment("Debug", "MODIFIER KEYS");
            ini.WriteComment("Debug", "160 = Left SHIFT");
            ini.WriteComment("Debug", "161 = Right SHIFT");
            ini.WriteComment("Debug", "162 = Left CTRL");
            ini.WriteComment("Debug", "163 = Right CTRL");
            ini.WriteComment("Debug", "164 = Left ALT");
            ini.WriteComment("Debug", "165 = Right ALT");
            ini.WriteComment("Debug", "");

            // Core debug flags
            ini.WriteBool("Debug", "EnableDebug", false);
            ini.WriteBool("Debug", "OverlayVisible", true);
            ini.WriteInt("Debug", "DebugLevel", 2);

            // Keybinds
            ini.WriteInt("Debug", "ModifierKey", 162);   // Left CTRL
            ini.WriteInt("Debug", "ToggleKey", 120);     // F9

            ini.WriteInt("Debug", "Action_RobberyStart", 97);  // NumPad1
            ini.WriteInt("Debug", "Action_SafeCrack", 98);     // NumPad2
            ini.WriteInt("Debug", "Action_SafeCrackMini", 107);// NumPad Add
            ini.WriteInt("Debug", "Action_CameraAlarm", 99);   // NumPad3
            ini.WriteInt("Debug", "Action_Escape", 100);       // NumPad4
            ini.WriteInt("Debug", "Action_Payout", 101);       // NumPad5
            ini.WriteInt("Debug", "Action_Cooldown", 102);     // NumPad6
            ini.WriteInt("Debug", "Action_Stalker", 103);      // NumPad7
            ini.WriteInt("Debug", "Action_StalkerCall", 109);  // NumPad Subtract
            ini.WriteInt("Debug", "Action_UI", 104);           // NumPad8
            ini.WriteInt("Debug", "Action_Banner", 105);       // NumPad9
            ini.WriteInt("Debug", "Action_Timer", 96);         // NumPad0
            ini.WriteInt("Debug", "Action_StoreDiag", 121);    // F10
            ini.WriteInt("Debug", "Action_MultiPos", 111);     // Divide 
            ini.WriteInt("Debug", "Action_MiscActions", 106);  // Multiply
            ini.WriteInt("Debug", "Action_CameraDebug", 114);  // F3

            // Scenarios
            ini.WriteInt("Debug", "Scenario_FullRobbery", 112); // F1
            ini.WriteInt("Debug", "Scenario_QuickLoot", 113);   // F2

            // Profiler
            ini.WriteBool("Debug", "EnableProfiler", true);
            ini.WriteBool("Debug", "Profiler_AutoDump", false);
            ini.WriteInt("Debug", "Profiler_DumpInterval", 30);

            // File Manager
            ini.WriteBool("Debug", "EnableLogging", true);
            ini.WriteBool("Debug", "EnableFileManager", true);
            ini.WriteBool("Debug", "EnableEvents", true);
            ini.WriteBool("Debug", "AutoSnapshotOnScenario", true);
        }

        // ------------------------------------------------------------
        // LOAD DEBUG SETTINGS (FROM DebugSettings.ini)
        // ------------------------------------------------------------
        private void LoadDebugSettings(SimpleIni ini)
        {
            EnableDebug = ini.ReadBool("Debug", "EnableDebug", false);
            OverlayVisible = ini.ReadBool("Debug", "OverlayVisible", true);
            DebugLevel = ini.ReadInt("Debug", "DebugLevel", 2);

            ModifierKey = ini.ReadInt("Debug", "ModifierKey", 162);
            ToggleKey = ini.ReadInt("Debug", "ToggleKey", 120);

            Action_RobberyStart = ini.ReadInt("Debug", "Action_RobberyStart", 97);
            Action_SafeCrack = ini.ReadInt("Debug", "Action_SafeCrack", 98);
            Action_SafeCrackMini = ini.ReadInt("Debug", "Action_SafeCrackMini", 107);
            Action_CameraAlarm = ini.ReadInt("Debug", "Action_CameraAlarm", 99);
            Action_Escape = ini.ReadInt("Debug", "Action_Escape", 100);
            Action_Payout = ini.ReadInt("Debug", "Action_Payout", 101);
            Action_Cooldown = ini.ReadInt("Debug", "Action_Cooldown", 102);
            Action_Stalker = ini.ReadInt("Debug", "Action_Stalker", 103);
            Action_StalkerCall = ini.ReadInt("Debug", "Action_StalkerCall", 109);
            Action_UI = ini.ReadInt("Debug", "Action_UI", 104);
            Action_Banner = ini.ReadInt("Debug", "Action_Banner", 105);
            Action_Timer = ini.ReadInt("Debug", "Action_Timer", 96); 
            Action_StoreDiag = ini.ReadInt("Debug", "Action_StoreDiag", 121);
            Action_MultiPos = ini.ReadInt("Debug", "Action_MultiPos", 111);      
            Action_MiscActions= ini.ReadInt("Debug", "Action_MiscActions", 106);
            Action_CameraDebug = ini.ReadInt("Debug", "Action_CameraDebug", 114);

            Scenario_FullRobbery = ini.ReadInt("Debug", "Scenario_FullRobbery", 112);
            Scenario_QuickLoot = ini.ReadInt("Debug", "Scenario_QuickLoot", 113);

            EnableProfiler = ini.ReadBool("Debug", "EnableProfiler", true);
            Profiler_AutoDump = ini.ReadBool("Debug", "Profiler_AutoDump", false);
            Profiler_DumpInterval = ini.ReadInt("Debug", "Profiler_DumpInterval", 30);

            EnableLogging = ini.ReadBool("Debug", "EnableLogging", true);
            EnableFileManager = ini.ReadBool("Debug", "EnableFileManager", true);
            EnableEvents = ini.ReadBool("Debug", "EnableEvents", true);
            AutoSnapshotOnScenario = ini.ReadBool("Debug", "AutoSnapshotOnScenario", true);
        }

        // ------------------------------------------------------------
        // WRITE BACK DEBUG SETTINGS (TO DebugSettings.ini)
        // ------------------------------------------------------------
        private void WriteBackDebugSettings(SimpleIni ini)
        {
            // Master switches
            ini.WriteBool("Debug", "EnableDebug", EnableDebug);
            ini.WriteBool("Debug", "OverlayVisible", OverlayVisible);
            ini.WriteInt("Debug", "DebugLevel", DebugLevel);

            // Keybinds
            ini.WriteInt("Debug", "ModifierKey", ModifierKey);
            ini.WriteInt("Debug", "ToggleKey", ToggleKey);

            ini.WriteInt("Debug", "Action_RobberyStart", Action_RobberyStart);
            ini.WriteInt("Debug", "Action_SafeCrack", Action_SafeCrack);
            ini.WriteInt("Debug", "Action_SafeCrackMini", Action_SafeCrackMini);
            ini.WriteInt("Debug", "Action_CameraAlarm", Action_CameraAlarm);
            ini.WriteInt("Debug", "Action_Escape", Action_Escape);
            ini.WriteInt("Debug", "Action_Payout", Action_Payout);
            ini.WriteInt("Debug", "Action_Cooldown", Action_Cooldown);
            ini.WriteInt("Debug", "Action_Stalker", Action_Stalker);
            ini.WriteInt("Debug", "Action_StalkerCall", Action_StalkerCall);
            ini.WriteInt("Debug", "Action_UI", Action_UI);
            ini.WriteInt("Debug", "Action_Banner", Action_Banner);
            ini.WriteInt("Debug", "Action_Timer", Action_Timer);
            ini.WriteInt("Debug", "Action_StoreDiag", Action_StoreDiag);
            ini.WriteInt("Debug", "Action_MultiPos", Action_MultiPos);
            ini.WriteInt("Debug", "Action_MiscActions", Action_MiscActions);
            ini.WriteInt("Debug", "Action_CameraDebug", Action_CameraDebug);

            // Scenarios
            ini.WriteInt("Debug", "Scenario_FullRobbery", Scenario_FullRobbery);
            ini.WriteInt("Debug", "Scenario_QuickLoot", Scenario_QuickLoot);

            // Profiler
            ini.WriteBool("Debug", "EnableProfiler", EnableProfiler);
            ini.WriteBool("Debug", "Profiler_AutoDump", Profiler_AutoDump);
            ini.WriteInt("Debug", "Profiler_DumpInterval", Profiler_DumpInterval);

            // File Manager
            ini.WriteBool("Debug", "EnableLogging", EnableLogging);
            ini.WriteBool("Debug", "EnableFileManager", EnableFileManager);
            ini.WriteBool("Debug", "EnableEvents", EnableEvents);
            ini.WriteBool("Debug", "AutoSnapshotOnScenario", AutoSnapshotOnScenario);
        }
    }
}
