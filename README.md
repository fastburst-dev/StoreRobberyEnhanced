# 🏪 Store Robbery Enhanced — Bringing GTA Online’s Robbery Achievements to Single Player
### Bringing GTA Online’s Robbery Achievements to Single Player — Rebuilt, Expanded, and Modernized

Store Robbery Enhanced is a complete re-imagining of the classic GTA Online store robbery loop — rebuilt from the ground up for **GTA V Single Player** Enhanced to bring a different engaging experience that upon completion, gives you a Mission Sytle completed feel, something that the GTAO never offered. It also offers a crazy twist with the option "Stalker System" that makes you feel during the Robbery Process if you will, feel like you are being watched and if you really got away clean or not... If it is enabled (default true), there are over 350+ preprogrammed messages and you can edit or add your own messages, change the name and charater icon, to make it even more emmersive and thrilling.
<img width="1536" height="1024" alt="banner1" src="https://github.com/user-attachments/assets/b6e02de3-ce13-4442-b25f-7cc5e8e2c727" />

<img width="1536" height="1024" alt="banner3" src="https://github.com/user-attachments/assets/01354a8c-9782-459c-9205-3245730958d3" />

In GTA Online, robbing all convenience stores is a structured progression path:

- Each store has its own state  
- Cooldowns prevent farming  
- Clerks react dynamically  
- Safes offer bonus payouts  
- Subtitles guide the player  
- Achievements track your progress
- Stalker System intregrated and customizible
- Safe Cracking Minigame
- Shop Menu System

**Store Robbery Enhanced brings that entire experience into Single Player — and then pushes it further.**

---

# 🎯 What Makes This Mod Different?

This isn’t a simple “point gun, get money” script.  
It’s a **full robbery ecosystem**, built to feel like Rockstar designed it for Story Mode:
🔥 FEATURES
🏪 Dynamic Store Robberies
20 fully supported stores (19 Online stores + Ace Liquor)
- 26 Stores if DLC stores are added.

- Every store is tracked, saved, and managed  
  - Robbery states persist across sessions  
  - Cooldowns mirror GTA Online’s anti‑farm system  
  - Clerks behave with fear, surrender, and escalation logic  
  - Safes can be cracked for bonus cash  
  - Subtitles recreate the Online-style robbery prompts  
- Minimap-only blips match the Online UI  
- A psychological **Stalker System** reacts to your behavior  
- A full **debug suite** lets developers inspect every system in real time  

The goal was simple:  
**Bring the GTA Online robbery achievement system into Single Player — with the polish, depth, and reliability it always deserved.**

---

# 🔥 Features (Player Overview)

## 🏪 Dynamic Store Robberies
- 20 fully supported stores (19 Online stores + Ace Liquor)  
- Persistent store states:
  - Robbed / Not Robbed  
  - Cooldown active  
  - Safe cracked  
  - Alarm triggered  
  - Clerk killed (gun/melee)  
- Store states save across sessions  
- Cooldowns prevent farming  
- Register payouts scale per store  
- Optional safe cracking for bonus cash  

---

## 🔧 Enhanced AI & Systems
- Clerk fear, surrender, and flee logic  
- Alarm escalation and police response hooks  
- Silent alarm triggers  
- Clerk phone calls to police  
- Time‑based escalation  
- Camera system for immersion and debugging  

---

## 💰 Safe Cracking Minigame
<img width="1500" height="922" alt="SafeCrackMinigame2" src="https://github.com/user-attachments/assets/0f748995-f39d-4db3-a553-3f2f799ce5f4" />

A fully interactive, skill‑based safe cracking system:

- Dial rotation with sweet‑spot detection  
- Difficulty scaling per store (safes can be 3-4 combos to unlock)
- Visual feedback with glowing style unlocks
- Audio queues for when unlocked in stages
- Dynamic Timer for Minigame
- Bonus payout on success  
- Configurable safe crack time  
- Configurable cooldown  
- Optional controller vibration  
- Optional loading of “optional safes”  
- Clean UI with timer, dial, and feedback  

---

## 🔪 Stalker System — Psychological Threat Layer
A dynamic psychological system that reacts to your behavior:

- **450+ reactive stalker messages**  
- Categories include:
  - Knockout  
  - MeleeKill  
  - GunKill  
  - Robbery  
  - Escape  
  - CallAnswered  
  - CallIgnored  
- Stalker phone calls with custom caller ID  
- Behavior‑based reactions:
  - Violence level  
  - Escape style  
  - Sloppiness vs precision  
  - Morality patterns  
- Adds a thriller‑style narrative layer to every robbery  

For a list and visual of for possible Stalker Icons for the Stalker System can be view and seen at this link below. Default Stalker Image used is "CHAR_ARTHUR"
👉 https://wiki.rage.mp/wiki/Notification_Pictures

You can edit the StoreRobberyTracker.ini file and update the Icon and Name in these settings
[Stalker]
- EnableStalkerMsg=true
- EnableStalkerCall=true
- StalkerCallChance=25
- CallerImage=CHAR_ARTHUR <-- where you can change the icon
- CallerName=NO CALLER ID	<-- where you can change the caller ID name if desired.
- MaxMessagesPerRobbery=5
- MessageCooldownSeconds=20
<img width="1500" height="807" alt="Stalker1" src="https://github.com/user-attachments/assets/700618a2-8d22-43fe-b6a0-9c59e4c21013" />

---

## 🗺️ Clean Minimap Integration
- Blips appear **only on the minimap**, not the world map  
- Dynamic color changes based on robbery state  
- Matches GTA Online’s UI style  

---

## 🛒 Shop Menu System

<img width="1500" height="849" alt="ClerkStore1" src="https://github.com/user-attachments/assets/1b717fef-6411-4e11-8f8a-7015cccf6957" />

A fully integrated in‑store shopping experience that activates when the player approaches a clerk inside any supported convenience store interior. Each shop automatically loads a themed LemonUI menu with the correct Rockstar banner (24/7, LTD, Rob’s Liquor, or Liquor Ace) and a matching subtitle. Items display with right‑aligned prices and detailed descriptions, mirroring GTA V’s native shop layout. Purchases trigger the Shop Consume System, which handles animations, props, cooldowns, and item effects for a seamless, lore‑friendly shopping flow.

---

## 🛠️ Full Debug Suite (Developer Tools)
If the Debug System is enabled in the MainSettings.ini, to active and use features you must Holddown Left CTRL + Hotkey(s) to activate. All HotKeys as well as ModifierKey (Left CTRL) can be changed via the DebugSettings.ini a full list of useable HotKeys can be found at the link below
👉 https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=netframework-4.8.1

### Debug Overlay
Shows:
- Store ID  
- Store name  
- Robbery state  
- Clerk state  
- Cooldown timers  
- Safe status  
- Alarm state  
- Camera state  
- System flags  

### Camera Debug
- Toggleable camera overlay  
- Real‑time camera vectors  
- Interior validation  
- Store alignment tools  

### Profiler
- Optional performance profiler  
- Auto‑dump mode  
- Interval‑based dumps  

### Scenario Runner
- Full robbery simulation  
- Quick loot simulation  
- Auto snapshot on scenario  

### Debug Hotkeys and the pre-assigned values
- Start robbery                    // 97  NumPad1
- Trigger safe crack               // 98  NumPad2
- Trigger safe crack minigame      // 107 NumPad Add
- Trigger alarm                    // 99  NumPad3
- trigger escape                   // 100 NumPad4
- Trigger payout                   // 101 NumPad5
- Trigger cooldown                 // 102 NumPad6
- Trigger stalker event            // 103 NumPad7
- Trigger stalker call event       // 109 NumPad Subtrack
- Toggle UI                        // 104 NumPad8
- Toggle banner                    // 105 NumPad9
- Toggle timer                     // 96  NumPad0
- Store diagnostics                // 121 F10
- Multi‑position debug             // 111 NumPad Divide
- Misc actions                     // 106 NumPad Multiply
- Camera debug                     // 114 F3

---

# ⚙️ INI Settings (Full Breakdown)

## 📄 DebugSettings.ini

### **[Debug]** and pre-assugned valus for HotKeys (if any)
- EnableDebug — master debug toggle                  // true/false
- OverlayVisible — show/hide debug overlay           // true/false
- DebugLevel — verbosity (0–3)                       // 2 is the default
- ModifierKey — modifier for debug actions           // 162 Left CTRL
- ToggleKey — toggle overlay                         // 120 F9                
- Action_* — hotkeys for all debug actions           // SEE ABOVE SECTION
- Scenario_FullRobbery — run full robbery simulation // 112 F1 
- Scenario_QuickLoot — run quick loot simulation     // 113 F2
- EnableProfiler — enable profiler                   // true/false
- Profiler_AutoDump — auto‑dump profiler data        // true/false
- Profiler_DumpInterval — dump interval              // 30
- EnableLogging — enable debug logging               // true/false
- EnableFileManager — enable debug file manager      // true/false
- EnableEvents — enable debug events                 // true/false
- AutoSnapshotOnScenario — auto snapshot             // true/false

---

## 📄 StalkerMessages.ini
Contains **350+ lines** across categories:

- Knockout  
- MeleeKill  
- GunKill  
- Robbery  
- Escape  
- CallAnswered  
- CallIgnored  

Each category contains 35 unique lines.

---

## 📄 MainSettings.ini (Main Settings)

### **[Main Settings]**
- EnableMessages  
- EnablePolice  
- EnableBlips
- CooldownMinutes  
- RobberyTimeLimit  
- EscapeDistance  

### **[Police]**
- SilentAlarmDelaySeconds  
- ClerkCallDelaySeconds  
- TimeEscalationSeconds  

### **[Stalker]**
- EnableStalkerMsg  
- EnableStalkerCall  
- StalkerCallChance  
- CallerImage  
- CallerName  
- MaxMessagesPerRobbery  
- MessageCooldownSeconds  
- MessageType = 1 chat box, 2 Email, 3 Add Friend Request or 0 None Right corner

### **[Store Settings]**
- RegisterMinAmount / MaxAmount  
- SafeMinAmount / MaxAmount  
- EnableCameras  
- UseStoreNames  
- CameraGraceSeconds  
- SafeCrackTimeSeconds  
- PayoutMultiplier  
- SafeCrackCooldownMs  
- SafeCrackPadShake  
- SafeCrackLoadOptionalSafes  

### **[TIMER_UI]**
- PositionX / PositionY  
- Scale  
- DropShadow  
- Background  
- BackgroundWidth / Height  
- BackgroundOpacity  
- BackgroundColorR/G/B  

---

## 📄 StoreState.ini
Tracks persistent store data:

- IsRobbed  
- CooldownActive  
- SafeCracked  
- AlarmTriggered  
- ClerkDeathHandled  
- ClerkKilledWithGun  
- LastRobbedUtc  

All 20 stores included.

---

# 📦 Installation

### Requirements
- **GTA V (Latest Version)**  
- **ScriptHookV**  
- **ScriptHookVDotNet 3.9.0 Enhanced** for Enhanced Version of Game
- **ScriptHookVDotNet 3.7.0 Nightly Build or Newer** for Legacy Version of Game
- **.NET Framework 4.8**

### Install Steps
1. Install ScriptHookV  
2. Install ScriptHookVDotNet 3.9.0 Enhanced for Enhanced Version
   - Install ScriptHookVDotNet 3.7.0 Nightly for Legacy Version
4. Drag `StoreRobberyEnhanced.dll` and `StoreRobberyEnhanced.pdb`into:

   ```
   Grand Theft Auto V/scripts/
   ```
5. Launch the game.

---

## 🧩 Folder Structure (Developer Overview)

```
StoreRobberyTracker/
├── Properties/
│   └── AssemblyInfo.cs
├── Scripts/
│   ├── Config/
│   │   ├── DefaultConfigCreator.cs
│   │   ├── IniConfig.cs
│   │   ├── SafeCrackConfigLoader.cs
│   │   ├── SafeCrackSettings.cs
│   │   ├── SimpleIni.cs
│   │   └── StalkerMessageConfigCreator.cs
│   ├── Data/
│   │   ├── CameraData.cs
│   │   └── ShopItemData.cs
│   │   └── TrackStore.cs
│   ├── Debug/
│   │   ├── DebugActions.cs
│   │   ├── DebugCameraRender.cs
│   │   ├── DebugController.cs
│   │   ├── DebugEvents.cs
│   │   ├── DebugFileManager.cs
│   │   ├── DebugKeybinds.cs
│   │   ├── DebugLogger.cs
│   │   ├── DebugOverlay.cs
│   │   ├── DebugProfiler.cs
│   │   ├── DebugScenarios.cs
│   │   ├── DebugState.cs
│   │   └── DebugStoreOverlay.cs
│   ├── Initialization/
│   │   └── StoreInitializer.cs
│   ├── Minigame/
│   │   ├── ISafeCrackAnimation.cs
│   │   ├── ISafeCrackInput.cs
│   │   ├── ISafeCrackLogic.cs
│   │   ├── SafeCrackAnimations.cs
│   │   ├── SafeCrackController.cs
│   │   ├── SafeCrackEvents.cs
│   │   ├── SafeCrackInput.cs
│   │   ├── SafeCrackLogic.cs
│   │   ├── SafeCrackState.cs
│   │   └── SafeCrackUI.cs
│   ├── Systems/
│   │   ├── BlipSystem.cs
│   │   ├── CameraSystem.cs
│   │   ├── ClerkHelperSystem.cs
│   │   ├── ClerkReplacementSystem.cs
│   │   ├── ClerkSystem.cs
│   │   ├── CooldownSystem.cs
│   │   ├── PoliceSystem.cs
│   │   ├── RobberySystem.cs
│   │   ├── SafeSystem.cs
│   │   ├── ShopConsumeSystem.cs
│   │   ├── ShopSystem.cs
│   │   ├── StalkerSystem.cs
│   │   └── TimerSystem.cs
│   ├── UI/
│   │   ├── PlayerHelper.cs
│   │   ├── ShopMenuUI.cs
│   │   └── UiHelpers.cs
│   ├── Main.cs
│   └── StoreContext.cs
└── StoreRobberyEnhanced/
├── .gitattributes
├── .gitignore
├── LICENSE.txt
├── README.md
├── StoreRobberyEnhanced.csproj
└── StoreRobberyEnhanced.sln

```

# 🧠 System Architecture

### Initialization Layer
- Script startup  
- Store registration  
- Context creation  
- Debug system bootstrapping  

### Core Systems
- RobberySystem  
- ClerkSystem  
- CooldownSystem  
- AlarmSystem  
- SubtitleSystem  
- CameraSystem  

### Minigame Layer
- Safe cracking logic  
- UI rendering  
- Difficulty scaling  

### Stalker Layer
- Behavioral tracking  
- Message selection  
- Phone call system  
- Psychological profiling  

### Debug Layer
- Overlay  
- Camera debug  
- Profiler  
- Logging  

### UI Layer
- Notifications  
- Menus  
- Minimap blips  

---

## 🔧 Restoring Project References

Due to licensing restrictions, ScriptHookV and ScriptHookVDotNet DLLs cannot be included in this repository.

To build the project:

1. Download ScriptHookVDotNet 3.9.0 Enhanced:
   https://www.gta5-mods.com/tools/script-hook-v-net-enhanced
   - ScriptHookVDotNet Nighly Build for Legacy Installations:
   - https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases/tag/v3.7.0-nightly.48

3. Create a folder named `lib` in the project root.

4. Place the following files inside `/lib/`:
   - ScriptHookVDotNet.dll
   - ScriptHookVDotNet2.dll
   - ScriptHookVDotNet3.dll

5. Open the solution and build in Release mode.
   
---

# 🛠️ Building the Project

1. Open `StoreRobberyEnhanced.sln` in Visual Studio  
2. Ensure **.NET Framework 4.8** is installed  
3. Build in **Release** mode  
4. Output DLL appears in:
   ```
   /bin/Release/
   ```

---

## 📄 License
This project is licensed under the terms of the included `LICENSE.txt`.

---

## 🤝 Contributing
Pull requests are welcome.  
For major changes, open an issue first to discuss what you’d like to modify.

---

## ⭐ Credits
Created by **James Neeley**  AKA **FastBurst** Discord ID **FastBurst#7708**
GTA V Mod Developer & Systems Architect
