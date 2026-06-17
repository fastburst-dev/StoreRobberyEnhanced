using GTA;
using GTA.Native;
using LemonUI.Scaleform;
using NativeUI;
using StoreRobberyEnhanced.Debug;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace StoreRobberyEnhanced.UI
{
    internal class UiHelpers
    {
        private readonly IniConfig _config;

        // ------------------------------------------------------------
        // BANNER CORE
        // ------------------------------------------------------------

        public enum BannerType
        {
            HeistPassed,
            HeistFailed,
            LevelUp,
            Achievement,
            Purchase,
            Generic
        }

        private class ActiveBanner
        {
            public BannerType Type;
            public string Title;
            public string Subtitle;
            public int StartTime;
            public int DurationMs;
            public Scaleform Main;        // MP_BIG_MESSAGE_FREEMODE
            public Scaleform Background;  // MP_RESULTS_PANEL
            public bool PlaySound;
            public bool AllowManualClose = false;
            public int MinDisplayMs = 3000; // time before button appears
            public string ContinueText = "Press ~INPUT_FRONTEND_ACCEPT~ to Continue";
        }

        private readonly Queue<ActiveBanner> _bannerQueue = new Queue<ActiveBanner>();
        private ActiveBanner _currentBanner;
        private Scaleform _instructionalButtons;
        private float _instructionalAlpha = 0f;

        private bool _suppressUiUntilBannerDone = false;

        public bool IsBannerActive =>
            _currentBanner != null &&
            Game.GameTime <= _currentBanner.StartTime + _currentBanner.DurationMs;

        // ------------------------------------------------------------
        // LEGACY FIELDS (TIMER + OLD BANNER)
        // ------------------------------------------------------------

        private string _activeTimerText = null;
        private int _activeTimerSeconds = 0;

        private float _timerAlpha = 0f;
        private bool _timerVisible = false;
        private int _timerFadeSpeed = 5;
        private bool _useScaleformTimer = false;

        private Scaleform _timerScaleform = null;

        // Backwards-compat fields
        private Scaleform _heistScaleform;
        private Scaleform _celebration;
        private int _heistBannerEndTime = 0;

        public UiHelpers(IniConfig config)
        {
            try
            {
                _config = config;
                DebugLogger.Info("UiHelpers initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ctor", ex);
            }
        }

        // ------------------------------------------------------------
        // NOTIFICATION
        // ------------------------------------------------------------
        public void ShowNotification(string msg)
        {
            try
            {
                DebugLogger.Trace($"ShowNotification: {msg}");
                GTA.UI.Notification.PostTicker(msg, true);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowNotification", ex);
            }
        }

        // ------------------------------------------------------------
        // SUBTITLE
        // ------------------------------------------------------------
        public void ShowSubtitle(string msg, int duration = 3000)
        {
            try
            {
                DebugLogger.Trace($"ShowSubtitle: {msg}");
                GTA.UI.Screen.ShowSubtitle(msg, duration);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowSubtitle", ex);
            }
        }

        // ------------------------------------------------------------
        // SHOW HELP TEXT (TOP-LEFT INSTRUCTIONAL)
        // ------------------------------------------------------------
        public void ShowHelpText(string text)
        {
            try
            {
                DebugLogger.Trace($"ShowHelpText: {text}");

                Function.Call((Hash)0x8509B634FBE7DA11, "STRING");
                Function.Call((Hash)0x6C188BE134E074AA, text);
                Function.Call((Hash)0x238FFE5C7B0498A6, 0, false, true, -1);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowHelpText", ex);
            }
        }

        // ------------------------------------------------------------
        // PUBLIC BANNER API
        // ------------------------------------------------------------
        public void ShowHeistPassedBanner(string title, string subtitle, string storeName = null)
        {
            try
            {
                DebugLogger.Info($"ShowHeistPassedBanner: {title} / {subtitle} / {storeName}");

                // If storeName is provided, build the two-line subtitle
                string finalSubtitle;
                if (!string.IsNullOrEmpty(storeName))
                {
                    // Example: "24/7 Supermarket Robbery Complete\nTotal amount earned: $500000"
                    finalSubtitle = $"{storeName} - Robbery Completed\nTotal amount earned: ${subtitle}";
                }
                else
                {
                    // Fallback: keep original behavior (just payout)
                    finalSubtitle = $"Total amount earned: ${subtitle}";
                }

                EnqueueBanner(new ActiveBanner
                {
                    Type = BannerType.HeistPassed,
                    Title = title,
                    Subtitle = finalSubtitle,
                    DurationMs = 600000,
                    PlaySound = true,
                    AllowManualClose = true,
                    MinDisplayMs = 3000
                });

            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ShowHeistPassedBanner", ex);
            }
        }

        private void EnqueueBanner(ActiveBanner banner)
        {
            if (_currentBanner == null)
            {
                StartBanner(banner);
            }
            else
            {
                _bannerQueue.Enqueue(banner);
                DebugLogger.Trace($"Banner queued: {_bannerQueue.Count} in queue");
            }
        }

        // ------------------------------------------------------------
        // ⭐ StartBanner — Rockstar Composite Banner
        // ------------------------------------------------------------
        private void StartBanner(ActiveBanner banner)
        {
            try
            {
                DebugLogger.Trace($"StartBanner: {banner.Type} / {banner.Title}");

                banner.StartTime = Game.GameTime;

                // Heist-style banners use Rockstar composite scaleforms
                if (banner.Type == BannerType.HeistPassed ||
                    banner.Type == BannerType.HeistFailed ||
                    banner.Type == BannerType.LevelUp)
                {
                    banner.Background = new Scaleform("MP_RESULTS_PANEL");
                    banner.Main = new Scaleform("MP_BIG_MESSAGE_FREEMODE");

                    Script.Wait(100); // allow initialization

                    // Background: payout + icon
                    banner.Background.CallFunction(
                        "SHOW_MISSION_PASSED_MESSAGE",
                        banner.Title,
                        banner.Subtitle,
                        100,     // fade speed
                        true     // show background
                    );

                    // Foreground: gold shard
                    banner.Main.CallFunction(
                        "SHOW_SHARD_CENTERED_MP_MESSAGE",
                        banner.Title,
                        banner.Subtitle,
                        21,      // gold shard style
                        true,
                        false
                    );

                    if (banner.PlaySound)
                    {
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1,
                            "Mission_Pass_Notify",
                            "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS");
                    }
                }
                else
                {
                    // Non-heist banners use original scaleform
                    banner.Main = new Scaleform("MP_BIG_MESSAGE_FREEMODE");

                    banner.Main.CallFunction(
                        "SHOW_SHARD_CENTERED_MP_MESSAGE",
                        banner.Title,
                        banner.Subtitle,
                        0, true, false);

                    if (banner.PlaySound)
                    {
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1,
                            "CONFIRM_BEEP",
                            "HUD_MINI_GAME_SOUNDSET");
                    }
                }

                _currentBanner = banner;
                _suppressUiUntilBannerDone = true;

                _heistScaleform = banner.Main;
                _celebration = banner.Background;
                _heistBannerEndTime = banner.StartTime + banner.DurationMs;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.StartBanner", ex);
            }
        }

        private void EndCurrentBanner()
        {
            try
            {
                if (_currentBanner != null)
                {
                    DebugLogger.Trace($"EndCurrentBanner: {_currentBanner.Type} / {_currentBanner.Title}");
                }

                // ⭐ Properly clear instructional buttons so they disappear
                if (_instructionalButtons != null)
                {
                    try
                    {
                        _instructionalButtons.CallFunction("CLEAR_ALL");
                        _instructionalButtons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
                    }
                    catch { }
                }

                _instructionalButtons = null;
                _instructionalAlpha = 0f;

                _currentBanner = null;
                _heistScaleform = null;
                _celebration = null;
                _suppressUiUntilBannerDone = false;

                if (_bannerQueue.Count > 0)
                {
                    var next = _bannerQueue.Dequeue();
                    StartBanner(next);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.EndCurrentBanner", ex);
            }
        }

        // ------------------------------------------------------------
        // TEXT NOTIFICATIONS (STALKER)
        // ------------------------------------------------------------
        public void TextNotification(string avatar, string author, string title, string message, int iconTyle = 0)
        {
            try
            {
                DebugLogger.Info($"TextNotification: {title} / {message}");

                while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, avatar))
                {
                    Script.Wait(10);
                    Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, avatar, 0);
                }

                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "CONFIRM_BEEP", "HUD_MINI_GAME_SOUNDSET");
                Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, message);

                Function.Call<int>(
                    Hash.END_TEXT_COMMAND_THEFEED_POST_MESSAGETEXT,
                    avatar, avatar, true, iconTyle, title, author
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.TextNotification", ex);
            }
        }

        // ------------------------------------------------------------
        // DRAW LOOP
        // ------------------------------------------------------------
        public void Draw()
        {
            try
            {
                // ⭐ ALWAYS draw UiHelpers ABOVE SafeCrackUI and all other script UI
                Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 1000);

                // ⭐ If UI is suppressed because a banner is active, skip everything
                if (_suppressUiUntilBannerDone)
                {
                    DrawCurrentBanner();
                    return;
                }

                if (_currentBanner != null && IsBannerActive)
                {
                    DrawCurrentBanner();
                    return;
                }

                if (_activeTimerText != null)
                    DrawTimer(_activeTimerText, _activeTimerSeconds);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.Draw", ex);
            }
        }

        // ------------------------------------------------------------
        // ⭐ DrawCurrentBanner — Composite Rendering (With Instructional Buttons)
        // ------------------------------------------------------------
        private void DrawCurrentBanner()
        {
            try
            {
                if (_currentBanner == null)
                    return;

                int now = Game.GameTime;
                int endTime = _currentBanner.StartTime + _currentBanner.DurationMs;

                DebugLogger.Trace($"[BannerDraw] Type={_currentBanner.Type}, Time={now}/{endTime}");

                // Render banner
                if (_currentBanner.Type == BannerType.HeistPassed ||
                    _currentBanner.Type == BannerType.HeistFailed ||
                    _currentBanner.Type == BannerType.LevelUp)
                {
                    DrawCompositeMissionPassed(_currentBanner, now);
                }
                else
                {
                    if (_currentBanner.Background != null && _currentBanner.Background.IsValid)
                    {
                        Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 999);
                        _currentBanner.Background.Render2D();
                    }

                    if (_currentBanner.Main != null && _currentBanner.Main.IsValid)
                    {
                        Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 1000);
                        _currentBanner.Main.Render2D();
                    }
                }

                // Manual close logic
                if (_currentBanner.AllowManualClose)
                {
                    int elapsed = now - _currentBanner.StartTime;

                    // If the banner's own lifetime is over, kill everything (banner + buttons)
                    if (elapsed >= _currentBanner.DurationMs)
                    {
                        DebugLogger.Trace("Banner duration elapsed (manual close enabled) -> forcing EndCurrentBanner()");
                        EndCurrentBanner();
                        return;
                    }

                    // Only show buttons after MinDisplayMs, but before DurationMs
                    if (elapsed >= _currentBanner.MinDisplayMs)
                    {
                        bool usingController = Game.IsControlPressed(Control.LookUpOnly);

                        if (_instructionalButtons == null)
                        {
                            if (usingController)
                            {
                                BuildInstructionalButtons(
                                    ("Continue", Control.FrontendAccept),
                                    //("Replay", Control.FrontendX),
                                    ("Exit", Control.FrontendCancel)
                                );
                            }
                            else
                            {
                                BuildInstructionalButtons(
                                    ("Continue (Enter)", Control.FrontendAccept),
                                    //("Replay (R)", Control.Reload),
                                    ("Exit (Esc)", Control.FrontendCancel)
                                );
                            }
                        }

                        _instructionalAlpha = Math.Min(_instructionalAlpha + 0.02f, 1f);
                        Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 1100);
                        _instructionalButtons.Render2D();

                        bool pressedContinue =
                            Game.IsControlJustPressed(Control.FrontendAccept) ||
                            Game.IsKeyPressed(System.Windows.Forms.Keys.Enter);

                        //bool pressedReplay =
                        //    Game.IsControlJustPressed(Control.FrontendX);

                        bool pressedExit =
                            Game.IsControlJustPressed(Control.FrontendCancel) ||
                            Game.IsKeyPressed(System.Windows.Forms.Keys.Escape);

                        if (pressedContinue || pressedExit)
                        {
                            DebugLogger.Info("[Banner] Manual close triggered");
                            EndCurrentBanner();
                            return;
                        }
                    }

                    return; // do NOT auto-expire
                }

                // Auto-expire
                if (now > endTime)
                {
                    DebugLogger.Trace("Banner expired");
                    EndCurrentBanner();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawCurrentBanner", ex);
            }
        }

        // ------------------------------------------------------------
        // ⭐ Rockstar Composite Banner Renderer
        // ------------------------------------------------------------
        private void DrawCompositeMissionPassed(ActiveBanner banner, int now)
        {
            try
            {
                int elapsed = now - banner.StartTime;
                const int fadeInMs = 600;
                const int fadeOutMs = 600;

                float alpha = 1f;

                if (elapsed < fadeInMs)
                    alpha = elapsed / (float)fadeInMs;
                else if (elapsed > banner.DurationMs - fadeOutMs)
                    alpha = (banner.DurationMs - elapsed) / (float)fadeOutMs;

                if (alpha <= 0f)
                    return;

                int a = (int)(alpha * 255);

                // ⭐ Add this line here — black transparent background
                Function.Call(Hash.DRAW_RECT, 0.5f, 0.5f, 1.0f, 1.0f, 0, 0, 0, 120);

                // ⭐ Add this line here — green transparent background
                Function.Call(Hash.DRAW_RECT, 0.5f, 0.5f, 1.0f, 1.0f, 30, 180, 60, 40);


                // Background panel (money icon + payout)
                Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 999);
                if (banner.Background != null && banner.Background.IsValid)
                    banner.Background.Render2D();

                // Foreground shard (gold text)
                Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 1000);
                if (banner.Main != null && banner.Main.IsValid)
                    banner.Main.Render2D();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawCompositeMissionPassed", ex);
            }
        }

        // ------------------------------------------------------------
        // ⭐ Rockstar Composite Banner Button Prompt (appears after MinDisplayMs has passed)
        // ------------------------------------------------------------
        private void BuildInstructionalButtons(params (string Label, Control Control)[] buttons)
        {
            try
            {
                _instructionalButtons = new Scaleform("INSTRUCTIONAL_BUTTONS");

                _instructionalButtons.CallFunction("CLEAR_ALL");
                _instructionalButtons.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);

                int slot = 0;

                foreach (var b in buttons)
                {
                    // SHVDN 3.9.0: call native by hash
                    string glyph = Function.Call<string>(
                        (Hash)0x0499D7B09FC9B407,
                        2, // input group
                        (int)b.Control,
                        true
                    );

                    _instructionalButtons.CallFunction(
                        "SET_DATA_SLOT",
                        slot,
                        glyph,
                        b.Label
                    );

                    slot++;
                }

                // Online-style bottom placement
                _instructionalButtons.CallFunction("SET_BACKGROUND_COLOUR", 0, 0, 0, 80);
                _instructionalButtons.CallFunction("SET_POSITION", 0.5f, 0.95f);
                _instructionalButtons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);

            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.BuildInstructionalButtons", ex);
            }
        }

        // ------------------------------------------------------------
        // TIMER CONTROLS
        // ------------------------------------------------------------
        public void SetTimerText(string text, int secondsRemaining = 0)
        {
            try
            {
                DebugLogger.Trace($"SetTimerText: {text} ({secondsRemaining}s)");
                _activeTimerText = text;
                _activeTimerSeconds = secondsRemaining;
                _timerVisible = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.SetTimerText", ex);
            }
        }

        public void ClearTimer()
        {
            try
            {
                DebugLogger.Trace("ClearTimer()");
                _activeTimerText = null;
                _timerVisible = false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.ClearTimer", ex);
            }
        }

        // ------------------------------------------------------------
        // TIMER RENDERING
        // ------------------------------------------------------------
        private void DrawTimer(string text, int secondsRemaining)
        {
            try
            {
                if (_timerVisible)
                {
                    if (_timerAlpha < 1f)
                        _timerAlpha += 0.01f * _timerFadeSpeed;

                    if (_timerAlpha > 1f)
                        _timerAlpha = 1f;
                }
                else
                {
                    if (_timerAlpha > 0f)
                        _timerAlpha -= 0.01f * _timerFadeSpeed;

                    if (_timerAlpha <= 0f)
                        return;
                }

                if (_useScaleformTimer)
                {
                    DrawScaleformTimer(text);
                    return;
                }

                DrawTimerInternal(text, secondsRemaining);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawTimer", ex);
            }
        }

        // ------------------------------------------------------------
        // SCALEFORM TIMER
        // ------------------------------------------------------------
        private void DrawScaleformTimer(string text)
        {
            try
            {
                if (_timerScaleform == null)
                {
                    DebugLogger.Trace("Creating scaleform timer");
                    _timerScaleform = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
                    _timerScaleform.CallFunction("SHOW_SHARD_WASTED_MP_MESSAGE", text, "", 5);
                }

                _timerScaleform.Render2D();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawScaleformTimer", ex);
            }
        }

        // ------------------------------------------------------------
        // NORMAL TIMER (NATIVES)
        // ------------------------------------------------------------
        private void DrawTimerInternal(string text, int secondsRemaining)
        {
            try
            {
                var cfg = _config;

                bool flash = secondsRemaining <= 10 && (Game.GameTime % 500 < 250);

                int r = flash ? 255 : 255;
                int g = flash ? 50 : 255;
                int b = flash ? 50 : 255;
                int a = (int)(_timerAlpha * 255);

                float x = cfg.TimerPosX;
                float y = cfg.TimerPosY;

                float boxWidth = cfg.TimerBgWidth;
                float boxHeight = cfg.TimerBgHeight;

                if (cfg.TimerBackground)
                {
                    DrawTimerBackground(x, y, boxWidth, boxHeight,
                        a, cfg.TimerBgOpacity,
                        cfg.TimerBgR, cfg.TimerBgG, cfg.TimerBgB);
                }

                Function.Call(Hash.SET_TEXT_FONT, 0);
                Function.Call(Hash.SET_TEXT_SCALE, 0.0f, cfg.TimerScale);
                Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, a);
                Function.Call(Hash.SET_TEXT_CENTRE, false);

                if (cfg.TimerDropShadow)
                    Function.Call(Hash.SET_TEXT_DROPSHADOW, 2, 0, 0, 0, 255);

                Function.Call(Hash.SET_TEXT_OUTLINE);

                Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
                Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawTimerInternal", ex);
            }
        }

        private void DrawTimerBackground(float x, float y, float width, float height,
            int alpha, float opacity, int r, int g, int b)
        {
            try
            {
                Function.Call(Hash.DRAW_RECT,
                    x + width / 2f,
                    y + height / 2f,
                    width, height,
                    r, g, b,
                    (int)(alpha * opacity));
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("UiHelpers.DrawTimerBackground", ex);
            }
        }
    }
}
