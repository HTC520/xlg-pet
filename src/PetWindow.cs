using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    public class FloatingParticle
    {
        public EmoteType Emote;
        public float OffsetX;
        public float OffsetY;
        public float VelY;
        public int Life;
        public int MaxLife;
    }

    public class PetWindow : Form
    {
        private enum AIState
        {
            Idle,
            Walk,
            Happy,
            Sleep,
            Drag,
            Fall,
            Eat,
            KeyboardDance,
            Flee,
            FeedApproach,
            Action
        }

        public PetConfig Config { get; private set; }
        public InputStats Stats { get; private set; }

        private readonly Timer _tickTimer;
        private readonly Timer _breakTimer;
        private readonly SpeechBubbleForm _bubble;
        private readonly FoodOverlayForm _foodOverlay;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _menu;
        private readonly InputMonitor _inputMonitor;
        private StatusCardForm _statusCard;
        private InputStatsForm _statsForm;
        private SettingsForm _settingsForm;
        private FurnaceForm _furnaceForm;
        private PetAction _forcedAction = PetAction.Idle;
        private int _actionFrame;
        private readonly Random _rng = new Random();
        private readonly List<FloatingParticle> _particles = new List<FloatingParticle>();

        private AIState _state = AIState.Idle;
        private int _stateTicksRemaining = 60;
        private int _animCounter;
        private int _danceTicksRemaining;
        private int _danceBeatFrame;
        private int _activeKeySlot;
        private int _statsSaveCounter;
        private bool _facingLeft;
        private float _posX;
        private float _posY;
        private float _velY;
        private float _walkDirX = 1f;
        private float _walkDirY;
        private float _fleeDirX = 1f;
        private float _fleeDirY;

        private bool _isMouseDown;
        private bool _didDrag;
        private bool _holdTriggered;
        private int _holdTicks;
        private float _rapidDragScore;
        private int _dizzyCooldown;
        private Point _mouseDownScreen;
        private Point _lastDragScreen;
        private Point _dragStartForm;

        private Point _foodTargetCenter;
        private EmoteType _pendingFoodType = EmoteType.Hay;
        private int _feedApproachTimeout;

        private const int CanvasPadX = 24;
        private const int CanvasPadTop = 44;
        private const int CanvasPadBottom = 10;

        public PetWindow()
        {
            Config = PetConfig.Load();
            Stats = InputStats.Load();
            RetroAudio.Enabled = Config.SoundEnabled;

            _inputMonitor = new InputMonitor();
            _inputMonitor.Install();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;

            _bubble = new SpeechBubbleForm();
            _foodOverlay = new FoodOverlayForm();
            _foodOverlay.FoodPlaced += OnFoodPlaced;
            _foodOverlay.PlacementCancelled += () =>
            {
                ShowSpeech("取消放置食物啦~", 1800);
            };

            _menu = BuildContextMenu();
            _trayIcon = new NotifyIcon
            {
                Visible = true,
                Text = "星露谷桌面宠物 - " + PixelSprites.GetPetDisplayName(Config.Pet),
                ContextMenuStrip = _menu
            };
            UpdateTrayIcon();
            _trayIcon.DoubleClick += (s, e) =>
            {
                if (!Visible)
                {
                    ToggleVisibility();
                }
                else
                {
                    OpenStatusCard();
                }
            };

            // Place pet on screen
            Screen scr = Screen.PrimaryScreen;
            int spritePx = 16 * Config.Scale;
            if (Config.LastX >= scr.WorkingArea.Left - CanvasPadX &&
                Config.LastX <= scr.WorkingArea.Right - spritePx &&
                Config.LastY >= scr.WorkingArea.Top - CanvasPadTop &&
                Config.LastY <= scr.WorkingArea.Bottom - spritePx)
            {
                _posX = Config.LastX;
                _posY = Config.LastY;
            }
            else
            {
                _posX = scr.WorkingArea.Right - GetWindowWidth() - 60;
                _posY = scr.WorkingArea.Bottom - GetFloorOffsetY();
            }

            _tickTimer = new Timer();
            _tickTimer.Interval = 50; // 20 FPS
            _tickTimer.Tick += OnTick;
            _tickTimer.Start();

            _breakTimer = new Timer();
            _breakTimer.Tick += OnBreakReminder;
            ConfigureBreakTimer();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= LayeredWindowHelper.WS_EX_LAYERED | LayeredWindowHelper.WS_EX_TOOLWINDOW;
                if (Config != null && Config.TopMost)
                {
                    cp.ExStyle |= LayeredWindowHelper.WS_EX_TOPMOST;
                }
                if (Config != null && Config.ClickThrough)
                {
                    cp.ExStyle |= LayeredWindowHelper.WS_EX_TRANSPARENT;
                }
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LayeredWindowHelper.SetTopMost(Handle, Config.TopMost);
            LayeredWindowHelper.SetClickThrough(Handle, Config.ClickThrough);
            RenderFrame();
            ShowSpeech("你好呀！在星露谷的微风里一起工作吧~ ♪", 4000);
        }

        public string GetStateLabel()
        {
            switch (_state)
            {
                case AIState.Walk: return Config.FreeRoam ? "在桌面自由漫步中" : "正在任务栏散步";
                case AIState.Happy: return "开心雀跃中！";
                case AIState.Sleep: return "打盹休息中 Zzz";
                case AIState.Drag: return "被提起来啦";
                case AIState.Fall: return "自由落体中！";
                case AIState.Eat: return "大口吃零食中~";
                case AIState.KeyboardDance: return "在键盘上踩着节拍跳舞！♪";
                case AIState.Flee: return "受惊逃跑中 🏃";
                case AIState.FeedApproach: return "冲向美味食物中！";
                case AIState.Action: return PetActions.GetLabel(_forcedAction) + "中~";
                default: return "乖巧陪伴中";
            }
        }

        private int GetSpriteWidth() { return PixelSprites.GetFrameWidth(Config.Pet) * Config.Scale; }
        private int GetSpriteHeight() { return PixelSprites.GetFrameHeight(Config.Pet) * Config.Scale; }
        private int GetWindowWidth() { return GetSpriteWidth() + CanvasPadX * 2; }
        private int GetWindowHeight() { return GetSpriteHeight() + CanvasPadTop + CanvasPadBottom; }
        private int GetFloorOffsetY() { return CanvasPadTop + GetSpriteHeight(); }

        private Point GetPetCenter()
        {
            return new Point((int)_posX + GetWindowWidth() / 2, (int)_posY + CanvasPadTop + GetSpriteHeight() / 2);
        }

        private void OnTick(object sender, EventArgs e)
        {
            _animCounter++;
            _statsSaveCounter++;
            if (_dizzyCooldown > 0) _dizzyCooldown--;
            _rapidDragScore *= 0.85f;

            // Long-press 2 seconds interaction: ellipsis -> flee
            if (_isMouseDown && !_didDrag && !_holdTriggered)
            {
                _holdTicks++;
                if (_holdTicks >= 36) // ~1.8s
                {
                    _holdTriggered = true;
                    _isMouseDown = false;
                    TriggerLongPressFlee();
                }
            }

            ProcessGlobalInput();
            UpdatePhysicsAndAI();
            UpdateParticles();

            if (Visible)
            {
                RenderFrame();
            }

            if (_statsSaveCounter >= 600) // Auto-save every 30s if dirty
            {
                _statsSaveCounter = 0;
                Stats.Save();
            }

            if (_bubble.Visible)
            {
                _bubble.UpdateAnchor(GetSpeechAnchor());
            }
        }

        private void TriggerLongPressFlee()
        {
            if (Config.Pet == PetType.Furnace)
            {
                ShowSpeech("炉子是很沉的，搬不动啦~ 试试右边的烧烦恼功能吧！", 2800);
                return;
            }

            Point mouse = Control.MousePosition;
            Point center = GetPetCenter();
            float dx = center.X - mouse.X;
            float dy = center.Y - mouse.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1f)
            {
                double ang = _rng.NextDouble() * Math.PI * 2.0;
                _fleeDirX = (float)Math.Cos(ang);
                _fleeDirY = Config.FreeRoam ? (float)Math.Sin(ang) : 0f;
            }
            else
            {
                _fleeDirX = dx / len;
                _fleeDirY = Config.FreeRoam ? (dy / len) : 0f;
            }

            _facingLeft = _fleeDirX < 0;
            _state = AIState.Flee;
            _stateTicksRemaining = 36;
            SpawnParticle(EmoteType.Ellipsis);
            RetroAudio.PlayChirp();
            ShowSpeech("……？！别一直按着我呀，溜了溜了！🏃", 2600);
        }

        private void ProcessGlobalInput()
        {
            int keys, lc, rc, mc, slot;
            if (!_inputMonitor.ConsumePending(out keys, out lc, out rc, out mc, out slot))
            {
                return;
            }

            Stats.RecordActivity(keys, lc, rc, mc);

            if (!Visible || !Config.KeyboardDanceEnabled) return;
            if (_state == AIState.Drag || _state == AIState.Fall || _state == AIState.Flee ||
                _state == AIState.FeedApproach || _state == AIState.Eat || _state == AIState.Action)
            {
                return;
            }

            _state = AIState.KeyboardDance;
            _danceTicksRemaining = 18;
            _danceBeatFrame ^= 1;
            _activeKeySlot = slot;

            if (_particles.Count < 4 && _rng.Next(100) < 22)
            {
                SpawnParticle(EmoteType.Music);
            }
        }

        private void UpdatePhysicsAndAI()
        {
            Screen scr = Screen.FromPoint(GetPetCenter());
            int minX = scr.WorkingArea.Left - CanvasPadX + 6;
            int maxX = scr.WorkingArea.Right - GetWindowWidth() + CanvasPadX - 6;
            int minY = scr.WorkingArea.Top - CanvasPadTop + 6;
            int floorY = scr.WorkingArea.Bottom - GetFloorOffsetY();
            int maxY = floorY;

            if (_state == AIState.Drag)
            {
                return;
            }

            if (_state == AIState.FeedApproach)
            {
                _feedApproachTimeout--;
                Point petCenter = GetPetCenter();
                float dx = _foodTargetCenter.X - petCenter.X;
                float dy = _foodTargetCenter.Y - petCenter.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float approachSpeed = Config.MoveSpeed * 1.35f;

                if (dist <= Math.Max(10f, approachSpeed + 4f) || _feedApproachTimeout <= 0)
                {
                    _foodOverlay.ClearFood();
                    CompleteFoodEat();
                    return;
                }

                float vx = (dx / dist) * approachSpeed;
                float vy = (dy / dist) * approachSpeed;
                _posX = Clamp(_posX + vx, minX, maxX);
                _posY = Clamp(_posY + vy, minY, maxY);
                if (Math.Abs(vx) > 0.2f) _facingLeft = vx < 0;
                return;
            }

            if (_state == AIState.Flee)
            {
                _stateTicksRemaining--;
                float fleeSpeed = Config.MoveSpeed * 2.4f;
                _posX += _fleeDirX * fleeSpeed;
                if (Config.FreeRoam) _posY += _fleeDirY * fleeSpeed;

                if (_posX <= minX) { _posX = minX; _fleeDirX = Math.Abs(_fleeDirX); }
                else if (_posX >= maxX) { _posX = maxX; _fleeDirX = -Math.Abs(_fleeDirX); }

                if (Config.FreeRoam)
                {
                    if (_posY <= minY) { _posY = minY; _fleeDirY = Math.Abs(_fleeDirY); }
                    else if (_posY >= maxY) { _posY = maxY; _fleeDirY = -Math.Abs(_fleeDirY); }
                }
                else
                {
                    _posY = floorY;
                }

                _facingLeft = _fleeDirX < 0;

                if (_stateTicksRemaining <= 0)
                {
                    _state = AIState.Idle;
                    _stateTicksRemaining = 40;
                }
                return;
            }

            if (_state == AIState.KeyboardDance)
            {
                _danceTicksRemaining--;
                if (_danceTicksRemaining <= 0)
                {
                    _state = AIState.Idle;
                    _stateTicksRemaining = _rng.Next(45, 90);
                }
                return;
            }

            // Gravity applies only when FreeRoam is off (taskbar mode)
            if (!Config.FreeRoam && (_posY < floorY - 1 || _state == AIState.Fall))
            {
                _state = AIState.Fall;
                _velY += 1.6f;
                _posY += _velY;
                if (_posY >= floorY)
                {
                    _posY = floorY;
                    if (_velY > 8f)
                    {
                        _velY = -_velY * 0.28f;
                    }
                    else
                    {
                        _velY = 0;
                        _state = AIState.Happy;
                        _stateTicksRemaining = 18;
                        SpawnParticle(EmoteType.Sparkle);
                    }
                }
                return;
            }

            // Clamp inside screen bounds in FreeRoam
            _posX = Clamp(_posX, minX, maxX);
            _posY = Clamp(_posY, minY, maxY);

            _stateTicksRemaining--;

            if (_state == AIState.Action)
            {
                if (_forcedAction == PetAction.Dance || _forcedAction == PetAction.Lick)
                {
                    if (_animCounter % 5 == 0) _actionFrame ^= 1;
                }
                else if (_animCounter % 10 == 0)
                {
                    _actionFrame ^= 1;
                }
                if (_stateTicksRemaining <= 0) ChooseNextState();
                return;
            }

            if (_state == AIState.Walk)
            {
                float speed = Config.MoveSpeed * 0.75f;
                _posX += _walkDirX * speed;
                if (Config.FreeRoam)
                {
                    _posY += _walkDirY * speed;
                }

                if (_posX <= minX)
                {
                    _posX = minX;
                    _walkDirX = Math.Abs(_walkDirX);
                }
                else if (_posX >= maxX)
                {
                    _posX = maxX;
                    _walkDirX = -Math.Abs(_walkDirX);
                }

                if (Config.FreeRoam)
                {
                    if (_posY <= minY)
                    {
                        _posY = minY;
                        _walkDirY = Math.Abs(_walkDirY);
                    }
                    else if (_posY >= maxY)
                    {
                        _posY = maxY;
                        _walkDirY = -Math.Abs(_walkDirY);
                    }
                }

                if (Math.Abs(_walkDirX) > 0.05f)
                {
                    _facingLeft = _walkDirX < 0;
                }
            }

            if (_state == AIState.Sleep && _animCounter % 22 == 0)
            {
                SpawnParticle(EmoteType.Zzz);
            }

            if (_stateTicksRemaining <= 0)
            {
                if (_state == AIState.Eat)
                {
                    SpawnParticle(EmoteType.Smile);
                    SpawnParticle(EmoteType.Heart);
                }
                ChooseNextState();
            }
        }

        private PetAction StateToAction()
        {
            if (_state == AIState.Action) return _forcedAction;
            if (_state == AIState.Walk || _state == AIState.FeedApproach || _state == AIState.Flee) return PetAction.Walk;
            if (_state == AIState.Happy || _state == AIState.KeyboardDance) return PetAction.Happy;
            if (_state == AIState.Sleep) return PetAction.Sleep;
            if (_state == AIState.Eat) return PetAction.Eat;
            return PetAction.Idle;
        }

        private void ChooseNextState()
        {
            PetAction current = StateToAction();
            PetAction next = PetActions.PickNext(Config.Pet, current, _rng, Config.ActivityLevel, Config.AutoWalk);
            ApplyAction(next, false);
        }

        private void ApplyAction(PetAction action, bool manual)
        {
            // 熔炉是安静的固定伙伴，只让炉火跳动。
            if (Config.Pet == PetType.Furnace)
            {
                _forcedAction = PetAction.Idle;
                _actionFrame = 0;
                _state = AIState.Action;
                _stateTicksRemaining = 100000;
                if (manual) ShowSpeech("炉火噼啪作响…把烦恼写下来烧掉吧~", 3200);
                return;
            }

            if (!PetActions.Supports(Config.Pet, action))
            {
                action = PetAction.Idle;
            }

            _forcedAction = action;
            _actionFrame = 0;
            _stateTicksRemaining = PetActions.GetDurationTicks(action, Config.ActivityLevel);

            if (action == PetAction.Walk)
            {
                _state = AIState.Walk;
                if (Config.FreeRoam)
                {
                    int dir = _rng.Next(4);
                    if (dir == 0) { _walkDirX = 1f; _walkDirY = 0f; }
                    else if (dir == 1) { _walkDirX = -1f; _walkDirY = 0f; }
                    else if (dir == 2) { _walkDirX = (_rng.Next(2) == 0 ? 0.5f : -0.5f); _walkDirY = 0.8f; }
                    else { _walkDirX = (_rng.Next(2) == 0 ? 0.5f : -0.5f); _walkDirY = -0.8f; }
                }
                else
                {
                    _walkDirX = _rng.Next(2) == 0 ? -1f : 1f;
                    _walkDirY = 0f;
                }
            }
            else if (action == PetAction.Sleep)
            {
                _state = AIState.Sleep;
            }
            else if (action == PetAction.Happy)
            {
                _state = AIState.Happy;
                SpawnParticle(EmoteType.Music);
            }
            else if (action == PetAction.Eat)
            {
                _state = AIState.Eat;
            }
            else
            {
                _state = AIState.Action;
            }

            if (manual)
            {
                ShowSpeech(PetActions.GetSpeech(Config.Pet, action), 2800);
                if (action == PetAction.Dance || action == PetAction.Happy) SpawnParticle(EmoteType.Music);
                if (action == PetAction.Sleep) SpawnParticle(EmoteType.Zzz);
                RetroAudio.PlayChirp();
                if (_statusCard != null && _statusCard.Visible) _statusCard.RefreshCard();
            }
        }

        public void TriggerAction(PetAction action)
        {
            if (!Visible) ToggleVisibility();
            ApplyAction(action, true);
        }

        private static float Clamp(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private void UpdateParticles()
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                FloatingParticle p = _particles[i];
                p.OffsetY += p.VelY;
                p.Life--;
                if (p.Life <= 0)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        private void SpawnParticle(EmoteType emote)
        {
            _particles.Add(new FloatingParticle
            {
                Emote = emote,
                OffsetX = _rng.Next(-14, 15),
                OffsetY = 0,
                VelY = -1.2f,
                Life = 26,
                MaxLife = 26
            });
        }

        private void RenderFrame()
        {
            int w = GetWindowWidth();
            int h = GetWindowHeight();
            int spriteW = GetSpriteWidth();
            int spriteH = GetSpriteHeight();

            PetAnim anim = PetAnim.Idle;
            switch (_state)
            {
                case AIState.Action:
                    anim = PetActions.GetAnim(_forcedAction);
                    break;
                case AIState.Walk:
                case AIState.Flee:
                case AIState.FeedApproach:
                    anim = PetAnim.Walk;
                    break;
                case AIState.Happy:
                case AIState.KeyboardDance:
                    anim = PetAnim.Happy;
                    break;
                case AIState.Sleep:
                    anim = PetAnim.Sleep;
                    break;
                case AIState.Drag:
                case AIState.Fall:
                    anim = PetAnim.Drag;
                    break;
                case AIState.Eat:
                    anim = PetAnim.Eat;
                    break;
                default:
                    anim = PetAnim.Idle;
                    break;
            }

            int speedDiv = (_state == AIState.Flee) ? 3
                : ((_state == AIState.Walk || _state == AIState.FeedApproach || _state == AIState.Drag || _state == AIState.Happy || _state == AIState.Eat) ? 5 : 10);
            int frameIdx = _state == AIState.Action ? _actionFrame
                : (_state == AIState.KeyboardDance ? _danceBeatFrame : ((_animCounter / speedDiv) & 1));
            Bitmap sprite = PixelSprites.GetPetFrame(Config.Pet, anim, frameIdx);

            using (Bitmap canvas = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                // Soft shadow on ground when near floor
                if (_state != AIState.Drag && _state != AIState.Fall)
                {
                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(65, 20, 15, 10)))
                    {
                        int sw = (int)(spriteW * 0.72f);
                        int sh = Math.Max(4, Config.Scale * 2);
                        int sx = (w - sw) / 2;
                        int sy = CanvasPadTop + spriteH - sh / 2 - 2;
                        g.FillEllipse(shadow, sx, sy, sw, sh);
                    }
                }

                // Draw pixel keyboard underneath pet when dancing on keyboard
                int petOffsetX = 0;
                int petOffsetY = 0;
                if (_state == AIState.KeyboardDance)
                {
                    Bitmap kbProp = PixelSprites.GetKeyboardProp(_activeKeySlot);
                    int kbScale = Math.Max(2, Config.Scale - 1);
                    int kbW = kbProp.Width * kbScale;
                    int kbH = kbProp.Height * kbScale;
                    int kbX = (w - kbW) / 2;
                    int kbY = CanvasPadTop + spriteH - kbH + Config.Scale;
                    g.DrawImage(kbProp, kbX, kbY, kbW, kbH);

                    petOffsetX = (_danceBeatFrame == 0 ? -1 : 1) * Math.Max(1, Config.Scale / 2);
                    petOffsetY = -kbH + (_danceBeatFrame == 1 ? -Config.Scale : 0);
                }

                // Draw crisp pixel sprite (flipped if facing left)
                int drawX = CanvasPadX + petOffsetX;
                int drawY = CanvasPadTop + petOffsetY;
                Rectangle dest = new Rectangle(drawX, drawY, spriteW, spriteH);
                if (_facingLeft)
                {
                    g.DrawImage(sprite, new Rectangle(drawX + spriteW, drawY, -spriteW, spriteH));
                }
                else
                {
                    g.DrawImage(sprite, dest);
                }

                // Draw floating particles (hearts, notes, Zzz, treats, dizzy, ellipsis)
                foreach (FloatingParticle p in _particles)
                {
                    Bitmap emoteBmp = PixelSprites.GetEmote(p.Emote);
                    int emoteScale = Math.Max(2, Config.Scale - 1);
                    int ew = emoteBmp.Width * emoteScale;
                    int eh = emoteBmp.Height * emoteScale;
                    float px = (w - ew) / 2f + p.OffsetX;
                    float py = CanvasPadTop - eh / 2f + p.OffsetY;
                    g.DrawImage(emoteBmp, px, py, ew, eh);
                }

                LayeredWindowHelper.ApplyBitmap(Handle, canvas, (int)_posX, (int)_posY, 255);
            }
        }

        private Point GetSpeechAnchor()
        {
            return new Point((int)_posX + GetWindowWidth() / 2, (int)_posY + CanvasPadTop - 6);
        }

        public void ShowSpeech(string text, int durationMs = 3400)
        {
            if (!Visible) return;
            _bubble.ShowMessage(text, GetSpeechAnchor(), durationMs);
        }

        public void TriggerPetting()
        {
            if (!Visible) ToggleVisibility();
            _state = AIState.Happy;
            _stateTicksRemaining = 30;
            Config.FriendshipPoints += 5;
            Config.Save();
            SpawnParticle(EmoteType.Heart);
            RetroAudio.PlayChirp();
            ShowSpeech(StardewData.GetRandomQuote(Config.Pet), 3400);
        }

        public void StartFoodPlacement(EmoteType food)
        {
            if (!Visible) ToggleVisibility();

            Screen scr = Screen.FromPoint(GetPetCenter());
            int halfW = GetSpriteWidth() / 2 + 6;
            int halfH = GetSpriteHeight() / 2 + 6;
            Rectangle validBounds = new Rectangle(
                scr.WorkingArea.Left + halfW,
                scr.WorkingArea.Top + halfH,
                Math.Max(20, scr.WorkingArea.Width - halfW * 2),
                Math.Max(20, scr.WorkingArea.Height - halfH * 2));

            ShowSpeech("在桌面上左键点击放置食物，右键取消~", 2800);
            _foodOverlay.BeginPlacement(food, Config.Scale, validBounds);
        }

        private void OnFoodPlaced(Point center, EmoteType food)
        {
            _foodTargetCenter = center;
            _pendingFoodType = food;
            _state = AIState.FeedApproach;
            _feedApproachTimeout = 200; // max 10 seconds safety guard
            ShowSpeech("发现好吃的了！冲鸭~ 🏃", 2200);
        }

        private void CompleteFoodEat()
        {
            _state = AIState.Eat;
            _stateTicksRemaining = 24;
            int pts = StardewData.GetFoodPoints(Config.Pet, _pendingFoodType);
            Config.FriendshipPoints += pts;
            Config.Save();
            SpawnParticle(_pendingFoodType);
            RetroAudio.PlayEat();
            ShowSpeech(StardewData.GetFeedReaction(Config.Pet, _pendingFoodType), 4000);
            if (_statusCard != null && _statusCard.Visible)
            {
                _statusCard.RefreshCard();
            }
        }

        public void FeedPet(EmoteType food)
        {
            if (!Visible) ToggleVisibility();
            _pendingFoodType = food;
            _state = AIState.Eat;
            _stateTicksRemaining = 28;
            int pts = StardewData.GetFoodPoints(Config.Pet, food);
            Config.FriendshipPoints += pts;
            Config.Save();
            SpawnParticle(food);
            SpawnParticle(EmoteType.Smile);
            RetroAudio.PlayEat();
            ShowSpeech(StardewData.GetFeedReaction(Config.Pet, food), 4000);
        }

        public void ShowDailyFortune()
        {
            if (!Visible) ToggleVisibility();
            RetroAudio.PlayFortune();
            SpawnParticle(EmoteType.Sparkle);
            ShowSpeech(StardewData.GetDailyFortune(), 5200);
        }

        public void SwitchPet(PetType pet)
        {
            Config.Pet = pet;
            Config.Save();
            UpdateTrayIcon();
            _trayIcon.Text = "星露谷桌面宠物 - " + PixelSprites.GetPetDisplayName(Config.Pet);
            _actionFrame = 0;
            if (pet == PetType.Furnace)
            {
                _forcedAction = PetAction.Idle;
                _state = AIState.Action;
                _stateTicksRemaining = 100000;
                ShowSpeech("切换成熔炉啦！把烦恼丢进来烧掉吧~ 🔥", 3600);
                OpenFurnaceForm();
            }
            else
            {
                _state = AIState.Happy;
                _stateTicksRemaining = 26;
                ShowSpeech("切换成 " + PixelSprites.GetPetDisplayName(pet) + " 啦！", 3000);
            }
            SpawnParticle(EmoteType.Sparkle);
            RetroAudio.PlayChirp();
            if (_statusCard != null && _statusCard.Visible)
            {
                _statusCard.RefreshCard();
            }
        }

        public void CycleNextPet()
        {
            PetType[] all = (PetType[])Enum.GetValues(typeof(PetType));
            int idx = Array.IndexOf(all, Config.Pet);
            SwitchPet(all[(idx + 1) % all.Length]);
        }

        public void ApplyScale(int newScale)
        {
            Config.Scale = Math.Max(2, Math.Min(5, newScale));
            Config.Save();
            if (!Config.FreeRoam)
            {
                Screen scr = Screen.FromPoint(GetPetCenter());
                _posY = scr.WorkingArea.Bottom - GetFloorOffsetY();
            }
            if (Visible) RenderFrame();
        }

        public void SetFreeRoam(bool freeRoam)
        {
            Config.FreeRoam = freeRoam;
            Config.Save();
            if (!freeRoam)
            {
                _state = AIState.Fall;
                _velY = 0;
            }
        }

        public void SetTopMostEnabled(bool topMost)
        {
            Config.TopMost = topMost;
            Config.Save();
            LayeredWindowHelper.SetTopMost(Handle, Config.TopMost);
            // Re-apply click-through to guarantee Z-order changes never drop WS_EX_TRANSPARENT
            LayeredWindowHelper.SetClickThrough(Handle, Config.ClickThrough);
        }

        public void SetClickThroughEnabled(bool clickThrough)
        {
            Config.ClickThrough = clickThrough;
            Config.Save();
            LayeredWindowHelper.SetClickThrough(Handle, Config.ClickThrough);
            if (clickThrough)
            {
                ShowSpeech("已开启鼠标穿透！可右键右下角托盘图标取消~", 3400);
            }
            else
            {
                ShowSpeech("已恢复鼠标点击互动！", 2400);
            }
        }

        public void ToggleVisibility()
        {
            if (Visible)
            {
                _bubble.Hide();
                _foodOverlay.ClearFood();
                Hide();
            }
            else
            {
                Show();
                LayeredWindowHelper.SetTopMost(Handle, Config.TopMost);
                LayeredWindowHelper.SetClickThrough(Handle, Config.ClickThrough);
                RenderFrame();
            }
        }

        private void OpenStatusCard()
        {
            if (_statusCard == null || _statusCard.IsDisposed)
            {
                _statusCard = new StatusCardForm(this);
            }
            else
            {
                _statusCard.RefreshCard();
            }
            _statusCard.Show();
            _statusCard.Activate();
        }

        public void OpenFurnaceForm()
        {
            if (_furnaceForm == null || _furnaceForm.IsDisposed)
            {
                _furnaceForm = new FurnaceForm();
            }
            _furnaceForm.Show();
            _furnaceForm.Activate();
        }

        public void OpenStatsForm()
        {
            if (_statsForm == null || _statsForm.IsDisposed)
            {
                _statsForm = new InputStatsForm(Stats);
            }
            else
            {
                _statsForm.RefreshData();
            }
            _statsForm.Show();
            _statsForm.Activate();
        }

        public void OpenSettingsForm()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(this);
            }
            else
            {
                _settingsForm.RefreshSettings();
            }
            _settingsForm.Show();
            _settingsForm.Activate();
        }

        private void ConfigureBreakTimer()
        {
            _breakTimer.Stop();
            if (Config.BreakReminderMinutes > 0)
            {
                _breakTimer.Interval = Config.BreakReminderMinutes * 60 * 1000;
                _breakTimer.Start();
            }
        }

        private void OnBreakReminder(object sender, EventArgs e)
        {
            if (!Visible) return;
            RetroAudio.PlayFortune();
            SpawnParticle(EmoteType.Stardrop);
            _state = AIState.Happy;
            _stateTicksRemaining = 40;
            ShowSpeech("⚡ 体力快耗尽啦！起身喝口水、远眺休息一下吧~", 6000);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;
                _didDrag = false;
                _holdTriggered = false;
                _holdTicks = 0;
                _rapidDragScore = 0;
                _mouseDownScreen = Control.MousePosition;
                _lastDragScreen = _mouseDownScreen;
                _dragStartForm = new Point((int)_posX, (int)_posY);
            }
            else if (e.Button == MouseButtons.Right)
            {
                RefreshMenuChecks();
                _menu.Show(Control.MousePosition);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isMouseDown) return;

            Point cur = Control.MousePosition;
            int dx = cur.X - _mouseDownScreen.X;
            int dy = cur.Y - _mouseDownScreen.Y;
            if (!_didDrag && (Math.Abs(dx) > 4 || Math.Abs(dy) > 4))
            {
                _didDrag = true;
                _state = AIState.Drag;
                _velY = 0;
            }

            if (_didDrag)
            {
                float stepDist = (float)Math.Sqrt(
                    Math.Pow(cur.X - _lastDragScreen.X, 2) +
                    Math.Pow(cur.Y - _lastDragScreen.Y, 2));
                _lastDragScreen = cur;
                _rapidDragScore += stepDist;

                if (_rapidDragScore > 260f && _dizzyCooldown <= 0)
                {
                    _dizzyCooldown = 36;
                    _rapidDragScore = 0;
                    SpawnParticle(EmoteType.Dizzy);
                    RetroAudio.PlayChirp();
                    ShowSpeech("呜哇…转得头好晕，要变成一团乱麻了 😵", 2800);
                }

                _posX = _dragStartForm.X + dx;
                _posY = _dragStartForm.Y + dy;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _isMouseDown)
            {
                _isMouseDown = false;
                if (_didDrag)
                {
                    _state = Config.FreeRoam ? AIState.Idle : AIState.Fall;
                    _stateTicksRemaining = 35;
                    _velY = 0;
                    Config.LastX = (int)_posX;
                    Config.LastY = (int)_posY;
                    Config.Save();
                }
                else if (!_holdTriggered)
                {
                    TriggerPetting();
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button == MouseButtons.Left)
            {
                if (Config.Pet == PetType.Furnace) OpenFurnaceForm();
                else OpenStatusCard();
            }
        }

        private ToolStripMenuItem _miTopMost;
        private ToolStripMenuItem _miClickThrough;
        private ToolStripMenuItem _miAutoWalk;
        private ToolStripMenuItem _miFreeRoam;
        private ToolStripMenuItem _miSound;
        private ToolStripMenuItem _miKeyboardDance;
        private ToolStripMenuItem _miToggleVisible;
        private ToolStripMenuItem _actionMenu;
        private readonly Dictionary<PetType, ToolStripMenuItem> _petMenuItems = new Dictionary<PetType, ToolStripMenuItem>();
        private readonly Dictionary<int, ToolStripMenuItem> _scaleMenuItems = new Dictionary<int, ToolStripMenuItem>();
        private readonly Dictionary<int, ToolStripMenuItem> _breakMenuItems = new Dictionary<int, ToolStripMenuItem>();

        private ContextMenuStrip BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                Renderer = new StardewMenuRenderer(),
                Font = new Font("Microsoft YaHei", 9f)
            };

            _miToggleVisible = new ToolStripMenuItem("👁️ 显示 / 隐藏宠物", null, (s, e) => ToggleVisibility());
            menu.Items.Add(_miToggleVisible);
            menu.Items.Add("💕 抚摸互动", null, (s, e) => TriggerPetting());
            menu.Items.Add("🔥 打开熔炉 · 烧掉烦恼", null, (s, e) =>
            {
                if (Config.Pet != PetType.Furnace) SwitchPet(PetType.Furnace);
                else OpenFurnaceForm();
            });

            ToolStripMenuItem placeFeedMenu = new ToolStripMenuItem("🌿 放食物到桌面喂食");
            placeFeedMenu.DropDownItems.Add("🌾 放置干草（跑去吃）", null, (s, e) => StartFoodPlacement(EmoteType.Hay));
            placeFeedMenu.DropDownItems.Add("🍓 放置草莓（跑去吃）", null, (s, e) => StartFoodPlacement(EmoteType.Strawberry));
            placeFeedMenu.DropDownItems.Add("★ 放置星之果实（跑去吃）", null, (s, e) => StartFoodPlacement(EmoteType.Stardrop));
            placeFeedMenu.DropDownItems.Add("🥕 放置防风草（跑去吃）", null, (s, e) => StartFoodPlacement(EmoteType.Parsnip));
            menu.Items.Add(placeFeedMenu);

            ToolStripMenuItem feedMenu = new ToolStripMenuItem("🍓 直接投喂零食");
            feedMenu.DropDownItems.Add("🌾 干草 (+12)", null, (s, e) => FeedPet(EmoteType.Hay));
            feedMenu.DropDownItems.Add("🥕 防风草 (+10)", null, (s, e) => FeedPet(EmoteType.Parsnip));
            feedMenu.DropDownItems.Add("🍓 草莓 (+15)", null, (s, e) => FeedPet(EmoteType.Strawberry));
            feedMenu.DropDownItems.Add("★ 星之果实 (+25)", null, (s, e) => FeedPet(EmoteType.Stardrop));
            feedMenu.DropDownItems.Add("🖤 虚空蛋黄酱", null, (s, e) => FeedPet(EmoteType.VoidMayo));
            menu.Items.Add(feedMenu);

            menu.Items.Add("🔮 查看今日占卜", null, (s, e) => ShowDailyFortune());
            menu.Items.Add("📖 打开宠物日记", null, (s, e) => OpenStatusCard());
            menu.Items.Add("📊 查看键鼠统计", null, (s, e) => OpenStatsForm());
            menu.Items.Add("⚙ 详细设置...", null, (s, e) => OpenSettingsForm());
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem switchMenu = new ToolStripMenuItem("🐾 切换星露谷伙伴");
            foreach (PetType pt in Enum.GetValues(typeof(PetType)))
            {
                PetType captured = pt;
                ToolStripMenuItem item = new ToolStripMenuItem(PixelSprites.GetPetDisplayName(pt), null, (s, e) => SwitchPet(captured));
                _petMenuItems[pt] = item;
                switchMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(switchMenu);

            _actionMenu = new ToolStripMenuItem("🎬 切换动作");
            menu.Items.Add(_actionMenu);

            ToolStripMenuItem scaleMenu = new ToolStripMenuItem("📏 宠物大小");
            foreach (int sc in new[] { 2, 3, 4, 5 })
            {
                int captured = sc;
                ToolStripMenuItem item = new ToolStripMenuItem(string.Format("{0}x ({1}px)", sc, sc * 16), null, (s, e) => ApplyScale(captured));
                _scaleMenuItems[sc] = item;
                scaleMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(scaleMenu);

            _miFreeRoam = new ToolStripMenuItem("🕊️ 桌面全屏自由漫步", null, (s, e) => SetFreeRoam(!Config.FreeRoam));
            menu.Items.Add(_miFreeRoam);

            _miAutoWalk = new ToolStripMenuItem("🚶 自动随机走动", null, (s, e) =>
            {
                Config.AutoWalk = !Config.AutoWalk;
                Config.Save();
                if (!Config.AutoWalk) _state = AIState.Idle;
            });
            menu.Items.Add(_miAutoWalk);

            _miTopMost = new ToolStripMenuItem("📌 窗口置顶", null, (s, e) => SetTopMostEnabled(!Config.TopMost));
            menu.Items.Add(_miTopMost);

            _miClickThrough = new ToolStripMenuItem("👻 鼠标穿透", null, (s, e) => SetClickThroughEnabled(!Config.ClickThrough));
            menu.Items.Add(_miClickThrough);

            _miKeyboardDance = new ToolStripMenuItem("⌨️ 键鼠共舞效果", null, (s, e) =>
            {
                Config.KeyboardDanceEnabled = !Config.KeyboardDanceEnabled;
                Config.Save();
                if (!Config.KeyboardDanceEnabled && _state == AIState.KeyboardDance)
                {
                    _state = AIState.Idle;
                }
            });
            menu.Items.Add(_miKeyboardDance);

            _miSound = new ToolStripMenuItem("🔊 复古音效", null, (s, e) =>
            {
                Config.SoundEnabled = !Config.SoundEnabled;
                RetroAudio.Enabled = Config.SoundEnabled;
                Config.Save();
            });
            menu.Items.Add(_miSound);

            ToolStripMenuItem breakMenu = new ToolStripMenuItem("⏰ 休息喝水提醒");
            foreach (int mins in new[] { 0, 25, 45, 60 })
            {
                int captured = mins;
                string label = mins == 0 ? "关闭提醒" : string.Format("每 {0} 分钟提醒", mins);
                ToolStripMenuItem item = new ToolStripMenuItem(label, null, (s, e) =>
                {
                    Config.BreakReminderMinutes = captured;
                    Config.Save();
                    ConfigureBreakTimer();
                });
                _breakMenuItems[mins] = item;
                breakMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(breakMenu);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("❌ 退出宠物", null, (s, e) => ExitApp());

            menu.Opening += (s, e) => RefreshMenuChecks();
            return menu;
        }

        private void RefreshMenuChecks()
        {
            _miToggleVisible.Text = Visible ? "👁️ 隐藏宠物" : "👁️ 显示宠物";
            _miTopMost.Checked = Config.TopMost;
            _miClickThrough.Checked = Config.ClickThrough;
            _miFreeRoam.Checked = Config.FreeRoam;
            _miAutoWalk.Checked = Config.AutoWalk;
            _miSound.Checked = Config.SoundEnabled;
            _miKeyboardDance.Checked = Config.KeyboardDanceEnabled;

            foreach (var kv in _petMenuItems) kv.Value.Checked = (kv.Key == Config.Pet);
            foreach (var kv in _scaleMenuItems) kv.Value.Checked = (kv.Key == Config.Scale);
            foreach (var kv in _breakMenuItems) kv.Value.Checked = (kv.Key == Config.BreakReminderMinutes);

            if (_actionMenu != null)
            {
                _actionMenu.DropDownItems.Clear();
                PetAction current = StateToAction();
                foreach (PetAction action in PetActions.GetAvailable(Config.Pet))
                {
                    PetAction captured = action;
                    ToolStripMenuItem item = new ToolStripMenuItem(PetActions.GetLabel(action), null,
                        (s, e) => TriggerAction(captured));
                    item.Checked = action == current;
                    _actionMenu.DropDownItems.Add(item);
                }
                if (Config.Pet != PetType.Furnace)
                {
                    _actionMenu.DropDownItems.Add(new ToolStripSeparator());
                    _actionMenu.DropDownItems.Add("🎲 随机来一个动作", null,
                        (s, e) => TriggerAction(PetActions.PickDoubleClickAction(Config.Pet, _rng)));
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private void UpdateTrayIcon()
        {
            Bitmap raw = PixelSprites.GetPetFrame(Config.Pet, PetAnim.Idle, 0);
            using (Bitmap iconBmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(iconBmp))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                float fit = Math.Min(32f / raw.Width, 32f / raw.Height);
                int dw = Math.Max(1, (int)(raw.Width * fit));
                int dh = Math.Max(1, (int)(raw.Height * fit));
                g.DrawImage(raw, (32 - dw) / 2, (32 - dh) / 2, dw, dh);

                IntPtr hIcon = iconBmp.GetHicon();
                try
                {
                    using (Icon tmpIcon = Icon.FromHandle(hIcon))
                    {
                        _trayIcon.Icon = (Icon)tmpIcon.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        private void ExitApp()
        {
            Config.LastX = (int)_posX;
            Config.LastY = (int)_posY;
            Config.Save();
            Stats.Save(true);
            _trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tickTimer.Stop();
                _tickTimer.Dispose();
                _breakTimer.Stop();
                _breakTimer.Dispose();
                _inputMonitor.Dispose();
                _bubble.Dispose();
                _foodOverlay.Dispose();
                if (_statusCard != null) _statusCard.Dispose();
                if (_statsForm != null) _statsForm.Dispose();
                if (_settingsForm != null) _settingsForm.Dispose();
                if (_furnaceForm != null) _furnaceForm.Dispose();
                _trayIcon.Dispose();
                _menu.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
