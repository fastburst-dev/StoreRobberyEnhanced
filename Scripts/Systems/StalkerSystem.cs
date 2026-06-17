using GTA;
using GTA.Math;
using GTA.Native;
using iFruitAddon2;
using StoreRobberyEnhanced.Data;
using StoreRobberyEnhanced.Debug;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Xml.Linq;

namespace StoreRobberyEnhanced.Systems
{
    internal class StalkerSystem
    {
        private readonly StoreContext _ctx;
        private readonly Random _rng;

        private List<string> _robberyMsgs;
        private List<string> _escapeMsgs;
        private List<string> _knockoutMsgs;
        private List<string> _gunKillMsgs;
        private List<string> _meleeKillMsgs;
        private List<string> _callAnsweredMsgs;
        private List<string> _callIgnoredMsgs;

        // Internal call/message state (used by ResetAfterDeath)
        private bool _callActive = false;
        private DateTime _callStartUtc = DateTime.MinValue;
        private Queue<string> _messageQueue = new Queue<string>();
        private DateTime _lastMessageUtc = DateTime.MinValue;

        private Queue<StalkerEvent> _eventQueue;

        private string _callerImage;
        private string _callerName; 
        private int _messageType;

        private int _messagesSentThisRobbery = 0;
        private DateTime _nextAllowedMessageTime = DateTime.MinValue;

        // ⭐ NEW: Call limiting + cooldown
        private int _callsThisRobbery = 0;
        private const int MAX_CALLS_PER_ROBBERY = 3;
        private int _nextAllowedCallTimeMs = 0;
        private bool _callInProgress = false;

        // iFruit phone
        private readonly CustomiFruit _phone;
        private readonly iFruitContact _stalkerContact;
        private readonly iFruitContactCollection _stalkerContactCollection;
        private static readonly Dictionary<string, ContactIcon> ContactIcons = new Dictionary<string, ContactIcon>(StringComparer.OrdinalIgnoreCase)
        {
            { "generic", ContactIcon.Generic },
            { "abigail", ContactIcon.Abigail },
            { "allcharacters", ContactIcon.AllCharacters },
            { "amanda", ContactIcon.Amanda },
            { "ammunation", ContactIcon.Ammunation },
            { "andreas", ContactIcon.Andreas },
            { "antonia", ContactIcon.Antonia },
            { "arthur", ContactIcon.Arthur },
            { "ashley", ContactIcon.Ashley },
            { "bankofliberty", ContactIcon.BankOfLiberty },
            { "fleecabank", ContactIcon.FleecaBank },
            { "mazebank", ContactIcon.MazeBank },
            { "barry", ContactIcon.Barry },
            { "beverly", ContactIcon.Beverly },
            { "bikesite", ContactIcon.Bikesite },
            { "blank", ContactIcon.Blank },
            { "blimp", ContactIcon.Blimp },
            { "blocked", ContactIcon.Blocked },
            { "boatsite", ContactIcon.Boatsite },
            { "brokendowngirl", ContactIcon.BrokenDownGirl },
            { "bugstars", ContactIcon.Bugstars },
            { "emergency", ContactIcon.Emergency },
            { "legendarymotorsport", ContactIcon.LegendaryMotorsport },
            { "ssasuperautos", ContactIcon.SSASuperAutos },
            { "castro", ContactIcon.Castro },
            { "chaticon", ContactIcon.ChatIcon },
            { "chef", ContactIcon.Chef },
            { "cheng", ContactIcon.Cheng },
            { "chengsr", ContactIcon.ChengSr },
            { "chop", ContactIcon.Chop },
            { "creatorportraits", ContactIcon.CreatorPortraits },
            { "cris", ContactIcon.Cris },
            { "dave", ContactIcon.Dave },
            { "denise", ContactIcon.Denise },
            { "detonatebomb", ContactIcon.DetonateBomb },
            { "detonatephone", ContactIcon.DetonatePhone },
            { "devin", ContactIcon.Devin },
            { "dialasub", ContactIcon.DialASub },
            { "dom", ContactIcon.Dom },
            { "domesticgirl", ContactIcon.DomesticGirl },
            { "dreyfuss", ContactIcon.Dreyfuss },
            { "drfriedlander", ContactIcon.DrFriedlander },
            { "epsilon", ContactIcon.Epsilon },
            { "estateagent", ContactIcon.EstateAgent },
            { "facebook", ContactIcon.Facebook },
            { "filmnoir", ContactIcon.Filmnoir },
            { "floyd", ContactIcon.Floyd },
            { "franklin", ContactIcon.Franklin },
            { "franklintrevor", ContactIcon.FranklinTrevor },
            { "gaymilitary", ContactIcon.GayMilitary },
            { "hao", ContactIcon.Hao },
            { "hitchergirl", ContactIcon.HitcherGirl },
            { "human", ContactIcon.Human },
            { "hunter", ContactIcon.Hunter },
            { "jimmy", ContactIcon.Jimmy },
            { "jimmyboston", ContactIcon.JimmyBoston },
            { "joe", ContactIcon.Joe },
            { "josef", ContactIcon.Josef },
            { "josh", ContactIcon.Josh },
            { "lamar", ContactIcon.Lamar },
            { "lazlow", ContactIcon.Lazlow },
            { "lester", ContactIcon.Lester },
            { "skull", ContactIcon.Skull },
            { "lesterfranklin", ContactIcon.LesterFranklin },
            { "lestermichael", ContactIcon.LesterMichael },
            { "lifeinvader", ContactIcon.Lifeinvader },
            { "lscustoms", ContactIcon.LSCustoms },
            { "lstouristboard", ContactIcon.LSTouristBoard },
            { "manuel", ContactIcon.Manuel },
            { "marnie", ContactIcon.Marnie },
            { "martin", ContactIcon.Martin },
            { "maryann", ContactIcon.MaryAnn },
            { "maude", ContactIcon.Maude },
            { "mechanic", ContactIcon.Mechanic },
            { "michael", ContactIcon.Michael },
            { "michaelfranklin", ContactIcon.MichaelFranklin },
            { "michaeltrevor", ContactIcon.MichaelTrevor },
            { "milsite", ContactIcon.Milsite },
            { "minotaur", ContactIcon.Minotaur },
            { "molly", ContactIcon.Molly },

            // MP Contacts
            { "mp_armycontact", ContactIcon.MP_ArmyContact },
            { "mp_bikerboss", ContactIcon.MP_BikerBoss },
            { "mp_bikermechanic", ContactIcon.MP_BikerMechanic },
            { "mp_brucie", ContactIcon.MP_Brucie },
            { "mp_detonatephone", ContactIcon.MP_Detonatephone },
            { "mp_famboss", ContactIcon.MP_FamBoss },
            { "mp_fibcontact", ContactIcon.MP_FibContact },
            { "mp_fmcontact", ContactIcon.MP_FmContact },
            { "mp_gerald", ContactIcon.MP_Gerald },
            { "mp_julio", ContactIcon.MP_Julio },
            { "mp_mechanic", ContactIcon.MP_Mechanic },
            { "mp_merryweather", ContactIcon.MP_Merryweather },
            { "mp_mexboss", ContactIcon.MP_MexBoss },
            { "mp_mexdocks", ContactIcon.MP_MexDocks },
            { "mp_mexlt", ContactIcon.MP_MexLt },
            { "mp_morsmutual", ContactIcon.MP_MorsMutual },
            { "mp_profboss", ContactIcon.MP_ProfBoss },
            { "mp_raylavoy", ContactIcon.MP_RayLavoy },
            { "mp_roberto", ContactIcon.MP_Roberto },
            { "mp_snitch", ContactIcon.MP_Snitch },
            { "mp_stretch", ContactIcon.MP_Stretch },
            { "mp_stripclubpr", ContactIcon.MP_StripclubPr },

            { "mrsthornhill", ContactIcon.MrsThornhill },
            { "multiplayer", ContactIcon.Multiplayer },
            { "nigel", ContactIcon.Nigel },
            { "omega", ContactIcon.Omega },
            { "oneil", ContactIcon.Oneil },
            { "ortega", ContactIcon.Ortega },
            { "oscar", ContactIcon.Oscar },
            { "patricia", ContactIcon.Patricia },
            { "pegasus", ContactIcon.Pegasus },
            { "planesite", ContactIcon.Planesite },

            // Property Icons
            { "property_armstrafficking", ContactIcon.Property_ArmsTrafficking },
            { "property_barairport", ContactIcon.Property_BarAirport },
            { "property_barbayview", ContactIcon.Property_BarBayview },
            { "property_barcaferojo", ContactIcon.Property_BarCafeRojo },
            { "property_barcockotoos", ContactIcon.Property_BarCockotoos },
            { "property_bareclipse", ContactIcon.Property_BarEclipse },
            { "property_barfes", ContactIcon.Property_BarFes },
            { "property_barhenhouse", ContactIcon.Property_BarHenHouse },
            { "property_barhimen", ContactIcon.Property_BarHiMen },
            { "property_barhookies", ContactIcon.Property_BarHookies },
            { "property_barirish", ContactIcon.Property_BarIrish },
            { "property_barlesbianco", ContactIcon.Property_BarLesBianco },
            { "property_barmirrorpark", ContactIcon.Property_BarMirrorPark },
            { "property_barpitchers", ContactIcon.Property_BarPitchers },
            { "property_barsingletons", ContactIcon.Property_BarSingletons },
            { "property_bartequilala", ContactIcon.Property_BarTequilala },
            { "property_barunbranded", ContactIcon.Property_BarUnbranded },

            { "property_carmodshop", ContactIcon.Property_CarModShop },
            { "property_carscrapyard", ContactIcon.Property_CarScrapYard },
            { "property_cinemadowntown", ContactIcon.Property_CinemaDowntown },
            { "property_cinemamorningwood", ContactIcon.Property_CinemaMorningwood },
            { "property_cinemavinewood", ContactIcon.Property_CinemaVinewood },
            { "property_golfclub", ContactIcon.Property_GolfClub },
            { "property_planescrapyard", ContactIcon.Property_PlaneScrapYard },
            { "property_sonarcollections", ContactIcon.Property_SonarCollections },
            { "property_taxilot", ContactIcon.Property_TaxiLot },
            { "property_towingimpound", ContactIcon.Property_TowingImpound },
            { "property_weedshop", ContactIcon.Property_WeedShop },

            { "ron", ContactIcon.Ron },
            { "saeeda", ContactIcon.Saeeda },
            { "sasquatch", ContactIcon.Sasquatch },
            { "simeon", ContactIcon.Simeon },
            { "socialclub", ContactIcon.SocialClub },
            { "solomon", ContactIcon.Solomon },
            { "steve", ContactIcon.Steve },
            { "stevemichael", ContactIcon.SteveMichael },
            { "stevetrevor", ContactIcon.SteveTrevor },
            { "stretch", ContactIcon.Stretch },

            // Strippers
            { "stripperchastity", ContactIcon.StripperChastity },
            { "strippercheetah", ContactIcon.StripperCheetah },
            { "stripperfufu", ContactIcon.StripperFufu },
            { "stripperinfernus", ContactIcon.StripperInfernus },
            { "stripperjuliet", ContactIcon.StripperJuliet },
            { "strippernikki", ContactIcon.StripperNikki },
            { "stripperpeach", ContactIcon.StripperPeach },
            { "strippersapphire", ContactIcon.StripperSapphire },

            { "tanisha", ContactIcon.Tanisha },
            { "taxi", ContactIcon.Taxi },
            { "taxiliz", ContactIcon.TaxiLiz },
            { "tenniscoach", ContactIcon.TennisCoach },
            { "tonya", ContactIcon.Tonya },
            { "tracey", ContactIcon.Tracey },
            { "trevor", ContactIcon.Trevor },
            { "wade", ContactIcon.Wade },
            { "youtube", ContactIcon.Youtube }
        };

        public StalkerSystem(StoreContext ctx)
        {
            try
            {
                _ctx = ctx;
                _rng = new Random();
                _eventQueue = new Queue<StalkerEvent>();

                // iFruit setup
                _phone = new CustomiFruit();

                _stalkerContact = new iFruitContact(_callerName ?? "Unknown Caller")
                {
                    Active = true,
                    DialTimeout = 8000,
                    Icon = ResolveIcon(_callerImage)
                };

                _phone.Contacts.Add(_stalkerContact);

                // Event wiring (use On-prefixed names)
                _stalkerContact.Answered += OnStalkerCallAnswered;

                DebugLogger.Info("StalkerSystem initialized (iFruit enabled)");

            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.ctor", ex);
            }
        }

        // Resets internal state after player death
        public void ResetAfterDeath()
        {
            _callActive = false;
            _callStartUtc = DateTime.MinValue;
            _messageQueue.Clear();
            _lastMessageUtc = DateTime.MinValue;
        }

        // Resolves icon name from INI to ContactIcon enum, defaults to Blank if invalid
        private ContactIcon ResolveIcon(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName))
                return ContactIcon.Blank;

            iconName = iconName.ToLowerInvariant();

            return ContactIcons.TryGetValue(iconName, out var icon)
                ? icon
                : ContactIcon.Blank;
        }

        // ------------------------------------------------------------
        // DEBUG FORCE PHONE CALL
        // ------------------------------------------------------------
        public void DebugForceStalkerCall()
        {
            try
            {
                DebugLogger.Info("DebugForceStalkerCall() invoked");
                _ctx.Ui.ShowNotification("~y~Debug: Forcing stalker phone call");
                StartCall();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.DebugForceCall", ex);
            }
        }

        // ------------------------------------------------------------
        // DEBUG FORCE MESSAGE
        // ------------------------------------------------------------
        public void DebugForceStalker()
        {
            try
            {
                DebugLogger.Info("DebugForceStalker() called");

                List<string> pool = _robberyMsgs;

                if (pool == null || pool.Count == 0)
                {
                    DebugLogger.Info("No stalker messages loaded");
                    _ctx.Ui.ShowNotification("~r~No stalker messages loaded");
                    return;
                }

                string msg = pool[_rng.Next(pool.Count)];

                _ctx.Ui.TextNotification(
                    _callerImage,
                    _callerName,
                    "UNKNOWN NUMBER",
                    msg, _messageType
                );

                DebugLogger.Info("Forced stalker message sent");
                _ctx.Ui.ShowNotification("~r~Stalker message forced (debug)");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.DebugForceStalker", ex);
            }
        }

        // ------------------------------------------------------------
        // LOAD FROM INI
        // ------------------------------------------------------------
        public void LoadFromIni()
        {
            try
            {
                DebugLogger.Info("Loading stalker messages from INI");

                IniConfig ini = _ctx.Config;

                _callerImage = ini.StalkerCallerImage;
                _callerName = ini.StalkerCallerName;

                _robberyMsgs = ini.StalkerRobberyMsgs;
                _escapeMsgs = ini.StalkerEscapeMsgs;
                _knockoutMsgs = ini.StalkerKnockoutMsgs;
                _gunKillMsgs = ini.StalkerGunKillMsgs;
                _meleeKillMsgs = ini.StalkerMeleeKillMsgs;
                _callAnsweredMsgs = ini.StalkerCallAnsweredMsgs;
                _callIgnoredMsgs = ini.StalkerCallIgnoredMsgs;
                _messageType = ini.StalkerMessageType;

                // Sync iFruit contact name with INI
                if (!string.IsNullOrWhiteSpace(_callerName))
                    _stalkerContact.Name = _callerName;
                else
                    _stalkerContact.Name = "Unknown Caller";

                DebugLogger.Info("Stalker messages loaded");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.LoadFromIni", ex);
            }
        }

        // ------------------------------------------------------------
        // MESSAGE QUEUEING
        // ------------------------------------------------------------
        private void QueueMessage(List<string> pool)
        {
            try
            {
                if (!_ctx.AnyRobberyActive)
                    return;

                if (!_ctx.Config.EnableStalkerMsg)
                    return;

                if (pool == null || pool.Count == 0)
                    return;

                if (_messagesSentThisRobbery >= _ctx.Config.MaxMessagesPerRobbery)
                    return;

                if (DateTime.UtcNow < _nextAllowedMessageTime)
                    return;

                int delay = _rng.Next(2000, 10000);

                StalkerEvent evt = new StalkerEvent
                {
                    TriggerTime = Game.GameTime + delay,
                    Pool = pool
                };

                _eventQueue.Enqueue(evt);

                DebugLogger.Trace($"Queued stalker message (delay={delay}ms)");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.QueueMessage", ex);
            }
        }

        public void QueueRobberyMessage() { QueueMessage(_robberyMsgs); }
        public void QueueEscapeMessage() { QueueMessage(_escapeMsgs); }
        public void QueueKnockoutMessage() { QueueMessage(_knockoutMsgs); }
        public void QueueGunKillMessage() { QueueMessage(_gunKillMsgs); }
        public void QueueMeleeKillMessage() { QueueMessage(_meleeKillMsgs); }

        // ------------------------------------------------------------
        // PROCESS QUEUED EVENTS (PATCHED FOR NEW SURRENDER LOGIC)
        // ------------------------------------------------------------
        public void ProcessEvents()
        {
            try
            {
                var player = Game.Player.Character;

                // ------------------------------------------------------------
                // ⭐ NEW: Detect surrender-in-progress so we DON'T shut down
                // ------------------------------------------------------------
                TrackedStore activeStore = null;

                // Find the active store (robbery in progress)
                foreach (var s in _ctx.Stores)
                {
                    if (s.IsRobberyActive)
                    {
                        activeStore = s;
                        break;
                    }
                }

                // If clerk is in surrender flow:
                // ClerkFleeing = true + SurrenderStage >= 1
                // → DO NOT clear queue, DO NOT treat as robbery ended
                if (activeStore != null)
                {
                    if (activeStore.ClerkFleeing && activeStore.ClerkSurrenderStage >= 1)
                    {
                        // Surrender in progress — treat as active robbery
                    }
                    else if (activeStore.ClerkFleeing && activeStore.ClerkSurrenderStage == 0)
                    {
                        // Actual fleeing (old behavior) → end robbery
                        DebugLogger.Trace("Clerk actually fled — clearing stalker queue");
                        _eventQueue.Clear();
                        return;
                    }
                }

                // ------------------------------------------------------------
                // ORIGINAL LOGIC
                // ------------------------------------------------------------
                if (!_ctx.AnyRobberyActive)
                {
                    DebugLogger.Trace("No robbery active — clearing stalker queue");
                    _eventQueue.Clear();
                    return;
                }

                if (player.IsDead || Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player))
                {
                    if (_ctx.AnyRobberyActive && player.IsDead)
                    {
                        //DebugLogger.Info("Player died during robbery — sending death message");
                        SendRandomMessage(_meleeKillMsgs);
                        _messagesSentThisRobbery = _ctx.Config.MaxMessagesPerRobbery;
                    }

                    //DebugLogger.Trace("Player dead/arrested — clearing stalker queue");
                    _eventQueue.Clear();
                    return;
                }

                if (_eventQueue.Count == 0)
                    return;

                StalkerEvent evt = _eventQueue.Peek();

                if (Game.GameTime >= evt.TriggerTime)
                {
                    DebugLogger.Trace("Processing queued stalker message");
                    SendRandomMessage(evt.Pool);
                    _eventQueue.Dequeue();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.ProcessEvents", ex);
            }
        }

        // Sends a random message from the specified pool, respecting limits and cooldowns
        private void SendRandomMessage(List<string> pool)
        {
            try
            {
                if (pool == null || pool.Count == 0)
                    return;

                if (_messagesSentThisRobbery >= _ctx.Config.MaxMessagesPerRobbery)
                    return;

                if (DateTime.UtcNow < _nextAllowedMessageTime)
                    return;

                string msg = pool[_rng.Next(pool.Count)];

                DebugLogger.Info($"Sending stalker message: {msg}");

                _ctx.Ui.TextNotification(
                    _callerImage,
                    _callerName,
                    "NO CALLER ID",
                    msg, _messageType
                );

                _messagesSentThisRobbery++;
                _nextAllowedMessageTime = DateTime.UtcNow.AddSeconds(_ctx.Config.MessageCooldownSeconds);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.SendRandomMessage", ex);
            }
        }

        // ------------------------------------------------------------
        // CALL SYSTEM (iFruit)
        // ------------------------------------------------------------
        public void TryTriggerCall()
        {
            try
            {
                if (!_ctx.AnyRobberyActive)
                    return;

                if (!_ctx.Config.EnableStalkerCall)
                    return;

                // ⭐ Prevent overlapping calls
                if (_callInProgress)
                {
                    DebugLogger.Trace("Stalker call suppressed — call already in progress");
                    return;
                }

                // ⭐ Hard cap per robbery
                if (_callsThisRobbery >= MAX_CALLS_PER_ROBBERY)
                {
                    DebugLogger.Trace("Stalker call suppressed — max calls per robbery reached");
                    return;
                }

                // ⭐ Cooldown between calls
                if (Game.GameTime < _nextAllowedCallTimeMs)
                {
                    DebugLogger.Trace("Stalker call suppressed — call cooldown active");
                    return;
                }

                // ⭐ PURE CHANCE — THIS IS THE PART YOU WANTED
                int chance = _ctx.Config.StalkerCallChance;
                if (_rng.Next(0, 100) >= chance)
                    return; // chance failed → do nothing

                // ⭐ Chance succeeded → attempt call
                _callsThisRobbery++;
                _nextAllowedCallTimeMs = Game.GameTime + 15000; // 15s cooldown

                DebugLogger.Info($"Stalker call triggered (iFruit), call #{_callsThisRobbery}");
                StartCall();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.TryTriggerCall", ex);
            }
        }

        // Starts the iFruit call and sets up timeout handling
        private void StartCall()
        {
            try
            {
                DebugLogger.Info("Starting stalker iFruit call");

                if (_stalkerContact == null || !_stalkerContact.Active)
                {
                    DebugLogger.Info("Stalker contact not active or null");
                    return;
                }

                // Destroy any existing phone first
                CleanupPhone();
                Function.Call(Hash.SET_MOBILE_PHONE_SCALE, 250.0f);
                // ⭐ Position the phone bottom-right (GTA Online style)
                //Function.Call(Hash.SET_MOBILE_PHONE_POSITION, 0.12f, -0.02f, 0.0f);
                Function.Call(Hash.CREATE_MOBILE_PHONE, 0);

                // Trigger the call
                _stalkerContact.Call();

                _ctx.Ui.TextNotification(_callerImage, _callerName, "NO CALLER ID", "INCOMING CALL");

                int timeoutMs = _stalkerContact.DialTimeout > 0 ? _stalkerContact.DialTimeout + 2000 : 10000;

                // Run a non‑blocking timeout check using Script.Tick
                Script.Wait(timeoutMs);

                // If still ringing after timeout, end the call manually
                if (_stalkerContact != null && _stalkerContact.Active)
                {
                    DebugLogger.Info("Stalker call timed out — ending call");
                    _stalkerContact.EndCall();
                    CleanupPhone();
                }

                // ⭐ Clear in-progress flag after timeout
                _callInProgress = false;
            }
            catch (Exception ex)
            {
                _callInProgress = false;
                DebugLogger.LogException("StalkerSystem.StartCall", ex);
            }
        }

        // iFruit event handlers
        private void OnStalkerCallAnswered(iFruitContact contact)
        {
            try
            {
                DebugLogger.Info("Stalker call answered (iFruit)");
                QueueMessage(_callAnsweredMsgs);

                // Cleanup phone model + camera
                CleanupPhone();

                // ⭐ Call finished
                _callInProgress = false;
            }
            catch (Exception ex)
            {
                _callInProgress = false;
                DebugLogger.LogException("StalkerSystem.OnStalkerCallAnswered", ex);
            }
        }

        // Called from Main.OnTick
        public void UpdatePhone()
        {
            try
            {
                _phone?.Update();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.UpdatePhone", ex);
            }
        }

        // Clean up phone model and camera to prevent glitches
        public void CleanupPhone()
        {
            try
            {
                Function.Call(Hash.DESTROY_MOBILE_PHONE);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.CleanupPhone", ex);
            }
        }

        // ------------------------------------------------------------
        // RESET ON ROBBERY END
        // ------------------------------------------------------------
        public void ResetForNewRobbery()
        {
            try
            {
                DebugLogger.Info("Resetting stalker system for new robbery");

                _messagesSentThisRobbery = 0;
                _nextAllowedMessageTime = DateTime.MinValue;
                _eventQueue.Clear();

                // ⭐ Reset call state
                _callsThisRobbery = 0;
                _nextAllowedCallTimeMs = 0;
                _callInProgress = false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("StalkerSystem.ResetForNewRobbery", ex);
            }
        }
    }
}
