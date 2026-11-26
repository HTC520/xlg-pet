using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    public class FoodOverlayForm : Form
    {
        private readonly Timer _followTimer;
        private Rectangle _validCenterBounds;
        private EmoteType _foodType = EmoteType.Hay;
        private int _scale = 3;
        private bool _following;
        private Point _currentCenter;

        public event Action<Point, EmoteType> FoodPlaced;
        public event Action PlacementCancelled;

        public bool IsPlaced { get; private set; }
        public EmoteType FoodType { get { return _foodType; } }
        public Point PlacedCenter { get { return _currentCenter; } }

        public FoodOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            _followTimer = new Timer();
            _followTimer.Interval = 25;
            _followTimer.Tick += OnFollowTick;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= LayeredWindowHelper.WS_EX_LAYERED
                            | LayeredWindowHelper.WS_EX_TOOLWINDOW
                            | LayeredWindowHelper.WS_EX_TOPMOST
                            | LayeredWindowHelper.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public void BeginPlacement(EmoteType food, int petScale, Rectangle validCenterBounds)
        {
            _foodType = food;
            _scale = Math.Max(2, petScale - 1);
            _validCenterBounds = validCenterBounds;
            _following = true;
            IsPlaced = false;

            if (!Visible) Show();
            LayeredWindowHelper.SetClickThrough(Handle, false);
            LayeredWindowHelper.SetTopMost(Handle, true);

            UpdateToCursor();
            _followTimer.Start();
        }

        public void ClearFood()
        {
            _following = false;
            IsPlaced = false;
            _followTimer.Stop();
            if (Visible) Hide();
        }

        private void OnFollowTick(object sender, EventArgs e)
        {
            if (_following)
            {
                UpdateToCursor();
            }
        }

        private void UpdateToCursor()
        {
            Point mouse = Control.MousePosition;
            int cx = Math.Max(_validCenterBounds.Left, Math.Min(_validCenterBounds.Right, mouse.X));
            int cy = Math.Max(_validCenterBounds.Top, Math.Min(_validCenterBounds.Bottom, mouse.Y));
            _currentCenter = new Point(cx, cy);
            RenderFood();
        }

        private void RenderFood()
        {
            Bitmap src = PixelSprites.GetEmote(_foodType);
            int w = src.Width * _scale + 8;
            int h = src.Height * _scale + 10;

            using (Bitmap canvas = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                // Soft shadow on ground
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(75, 25, 18, 10)))
                {
                    g.FillEllipse(shadow, 4, h - 8, w - 8, 6);
                }

                g.DrawImage(src, 4, 2, src.Width * _scale, src.Height * _scale);
                LayeredWindowHelper.ApplyBitmap(Handle, canvas, _currentCenter.X - w / 2, _currentCenter.Y - h / 2,
                    (byte)(_following ? 220 : 255));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!_following) return;

            if (e.Button == MouseButtons.Left)
            {
                _following = false;
                IsPlaced = true;
                _followTimer.Stop();
                LayeredWindowHelper.SetClickThrough(Handle, true);
                RenderFood();
                if (FoodPlaced != null) FoodPlaced(_currentCenter, _foodType);
            }
            else if (e.Button == MouseButtons.Right)
            {
                ClearFood();
                if (PlacementCancelled != null) PlacementCancelled();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _followTimer.Stop();
                _followTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
