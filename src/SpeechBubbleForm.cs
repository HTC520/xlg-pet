using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    public class SpeechBubbleForm : Form
    {
        private readonly Timer _fadeTimer;
        private string _message = string.Empty;
        private int _lifeTicks;
        private byte _opacity = 255;
        private Point _anchorTopCenter;
        private Bitmap _cachedBitmap;

        public SpeechBubbleForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            _fadeTimer = new Timer();
            _fadeTimer.Interval = 50;
            _fadeTimer.Tick += OnFadeTick;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= LayeredWindowHelper.WS_EX_LAYERED
                            | LayeredWindowHelper.WS_EX_TOOLWINDOW
                            | LayeredWindowHelper.WS_EX_TOPMOST
                            | LayeredWindowHelper.WS_EX_NOACTIVATE
                            | LayeredWindowHelper.WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public void ShowMessage(string text, Point anchorTopCenter, int durationMs = 3600)
        {
            if (string.IsNullOrEmpty(text)) return;

            _message = text;
            _anchorTopCenter = anchorTopCenter;
            _lifeTicks = Math.Max(20, durationMs / _fadeTimer.Interval);
            _opacity = 255;

            RebuildBubbleBitmap();
            if (!Visible)
            {
                Show();
            }
            RenderAtAnchor();
            _fadeTimer.Start();
        }

        public void UpdateAnchor(Point anchorTopCenter)
        {
            _anchorTopCenter = anchorTopCenter;
            if (Visible && _cachedBitmap != null)
            {
                RenderAtAnchor();
            }
        }

        private void OnFadeTick(object sender, EventArgs e)
        {
            _lifeTicks--;
            if (_lifeTicks <= 0)
            {
                _fadeTimer.Stop();
                Hide();
                return;
            }

            if (_lifeTicks < 10)
            {
                _opacity = (byte)(_lifeTicks * 25);
                RenderAtAnchor();
            }
        }

        private void RebuildBubbleBitmap()
        {
            if (_cachedBitmap != null)
            {
                _cachedBitmap.Dispose();
                _cachedBitmap = null;
            }

            using (Font font = new Font("Microsoft YaHei", 9.5f, FontStyle.Bold))
            using (Bitmap tmp = new Bitmap(1, 1))
            using (Graphics gMeasure = Graphics.FromImage(tmp))
            {
                gMeasure.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                SizeF textSize = gMeasure.MeasureString(_message, font, 230);

                int padX = 14;
                int padY = 10;
                int tailH = 8;
                int w = Math.Max(90, (int)Math.Ceiling(textSize.Width) + padX * 2);
                int h = Math.Max(36, (int)Math.Ceiling(textSize.Height) + padY * 2) + tailH;

                Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.None;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    int boxH = h - tailH;

                    // Soft drop shadow
                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(65, 30, 15, 5)))
                    {
                        g.FillRectangle(shadow, 3, 3, w - 4, boxH - 1);
                    }

                    // Outer dark wood border
                    using (SolidBrush outerWood = new SolidBrush(ColorTranslator.FromHtml("#592A0E")))
                    {
                        g.FillRectangle(outerWood, 1, 0, w - 3, boxH);
                        g.FillRectangle(outerWood, 0, 1, w - 1, boxH - 2);
                    }

                    // Mid wood rim
                    using (SolidBrush midWood = new SolidBrush(ColorTranslator.FromHtml("#9E5524")))
                    {
                        g.FillRectangle(midWood, 2, 2, w - 5, boxH - 4);
                    }

                    // Inner warm parchment
                    using (SolidBrush parchment = new SolidBrush(ColorTranslator.FromHtml("#FCD89D")))
                    {
                        g.FillRectangle(parchment, 4, 4, w - 9, boxH - 8);
                    }

                    // Inner light bevel
                    using (Pen highlight = new Pen(ColorTranslator.FromHtml("#FFEAC2"), 1))
                    {
                        g.DrawLine(highlight, 4, 4, w - 6, 4);
                        g.DrawLine(highlight, 4, 4, 4, boxH - 5);
                    }

                    // Speech pointer tail
                    int cx = w / 2;
                    using (SolidBrush outerWood = new SolidBrush(ColorTranslator.FromHtml("#592A0E")))
                    using (SolidBrush parchment = new SolidBrush(ColorTranslator.FromHtml("#FCD89D")))
                    {
                        Point[] outerTri =
                        {
                            new Point(cx - 7, boxH - 2),
                            new Point(cx + 7, boxH - 2),
                            new Point(cx, h - 1)
                        };
                        Point[] innerTri =
                        {
                            new Point(cx - 5, boxH - 4),
                            new Point(cx + 5, boxH - 4),
                            new Point(cx, h - 4)
                        };
                        g.FillPolygon(outerWood, outerTri);
                        g.FillPolygon(parchment, innerTri);
                    }

                    // Text
                    using (SolidBrush textBrush = new SolidBrush(ColorTranslator.FromHtml("#4A220A")))
                    {
                        RectangleF rect = new RectangleF(padX, padY, w - padX * 2, boxH - padY * 2 + 2);
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(_message, font, textBrush, rect, sf);
                    }
                }

                _cachedBitmap = bmp;
            }
        }

        private void RenderAtAnchor()
        {
            if (_cachedBitmap == null || IsDisposed) return;

            Screen scr = Screen.FromPoint(_anchorTopCenter);
            int x = _anchorTopCenter.X - _cachedBitmap.Width / 2;
            int y = _anchorTopCenter.Y - _cachedBitmap.Height - 4;

            if (x < scr.WorkingArea.Left + 4) x = scr.WorkingArea.Left + 4;
            if (x + _cachedBitmap.Width > scr.WorkingArea.Right - 4)
                x = scr.WorkingArea.Right - _cachedBitmap.Width - 4;
            if (y < scr.WorkingArea.Top + 4) y = _anchorTopCenter.Y + 4;

            LayeredWindowHelper.ApplyBitmap(Handle, _cachedBitmap, x, y, _opacity);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer.Stop();
                _fadeTimer.Dispose();
                if (_cachedBitmap != null) _cachedBitmap.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
