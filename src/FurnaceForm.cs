using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    // 参考 kele-desktop-pet 的熔炉：写下烦恼，点火烧掉。
    public class FurnaceForm : Form
    {
        private class Ash
        {
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public int Life;
            public int MaxLife;
            public Color Color;
        }

        private readonly TextBox _text;
        private readonly Label _hint;
        private readonly Button _burnButton;
        private readonly Timer _timer;
        private readonly List<Ash> _ashes = new List<Ash>();
        private readonly Random _rng = new Random();
        private bool _burning;
        private int _tick;
        private int _flameFrame;
        private bool _dragging;
        private Point _dragOffset;

        public FurnaceForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(460, 430);
            BackColor = ColorTranslator.FromHtml("#F3D6A0");
            DoubleBuffered = true;

            Label header = new Label
            {
                Text = "🔥 熔炉 · 烧掉烦恼",
                Location = new Point(18, 12),
                Size = new Size(300, 30),
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#592A0E"),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            header.MouseDown += HeaderMouseDown;
            header.MouseMove += HeaderMouseMove;
            header.MouseUp += HeaderMouseUp;
            Controls.Add(header);

            Button close = new Button
            {
                Text = "✕",
                Location = new Point(Width - 42, 12),
                Size = new Size(28, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#D96B43"),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            close.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#592A0E");
            close.Click += (s, e) => Hide();
            Controls.Add(close);

            _hint = new Label
            {
                Text = "写下今天不开心的事，让炉火替你吞掉它（最多 200 字）",
                Location = new Point(30, 188),
                Size = new Size(400, 22),
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = ColorTranslator.FromHtml("#6B3D19"),
                BackColor = Color.Transparent
            };
            Controls.Add(_hint);

            _text = new TextBox
            {
                Location = new Point(32, 216),
                Size = new Size(396, 92),
                Multiline = true,
                MaxLength = 200,
                ScrollBars = ScrollBars.Vertical,
                BackColor = ColorTranslator.FromHtml("#FFF7DE"),
                ForeColor = ColorTranslator.FromHtml("#4A220A"),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei", 10f)
            };
            Controls.Add(_text);

            _burnButton = new Button
            {
                Text = "🔥 烧掉烦恼",
                Location = new Point(152, 332),
                Size = new Size(156, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#C65325"),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _burnButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#592A0E");
            _burnButton.Click += BurnButtonClick;
            Controls.Add(_burnButton);

            _timer = new Timer();
            _timer.Interval = 50;
            _timer.Tick += OnTick;

            MouseDown += HeaderMouseDown;
            MouseMove += HeaderMouseMove;
            MouseUp += HeaderMouseUp;
        }

        private void BurnButtonClick(object sender, EventArgs e)
        {
            if (_burning) return;
            if (string.IsNullOrWhiteSpace(_text.Text))
            {
                _hint.Text = "先写下烦恼，再点燃炉火哦~";
                return;
            }

            _burning = true;
            _tick = 0;
            _text.ReadOnly = true;
            _burnButton.Enabled = false;
            _hint.Text = "炉火噼啪作响，正在吞掉烦恼……";
            RetroAudio.PlayFortune();
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            _tick++;
            _flameFrame = (_tick / 4) % 4;

            if (_burning)
            {
                for (int i = 0; i < 2; i++)
                {
                    _ashes.Add(new Ash
                    {
                        X = 200 + _rng.Next(0, 90),
                        Y = 120 + _rng.Next(0, 40),
                        VX = (_rng.Next(-10, 11)) / 10f,
                        VY = -1.4f - (float)_rng.NextDouble(),
                        Life = 28 + _rng.Next(24),
                        MaxLife = 52,
                        Color = _rng.Next(2) == 0
                            ? ColorTranslator.FromHtml("#F5A93C")
                            : ColorTranslator.FromHtml("#7B6A63")
                    });
                }

                if (_tick >= 66)
                {
                    _timer.Stop();
                    _burning = false;
                    _text.ReadOnly = false;
                    _text.Text = "";
                    _burnButton.Enabled = true;
                    _hint.Text = "✨ 烦恼已经化成灰烬啦！深呼吸，继续今天吧~";
                }
            }

            for (int i = _ashes.Count - 1; i >= 0; i--)
            {
                Ash a = _ashes[i];
                a.X += a.VX;
                a.Y += a.VY;
                a.VX += (_rng.Next(-3, 4)) / 30f;
                a.Life--;
                if (a.Life <= 0) _ashes.RemoveAt(i);
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            using (SolidBrush bg = new SolidBrush(ColorTranslator.FromHtml("#F3D6A0")))
            using (SolidBrush header = new SolidBrush(ColorTranslator.FromHtml("#F0BC6B")))
            using (SolidBrush paper = new SolidBrush(ColorTranslator.FromHtml("#FFF0CC")))
            using (Pen outer = new Pen(ColorTranslator.FromHtml("#592A0E"), 4))
            using (Pen inner = new Pen(ColorTranslator.FromHtml("#A65B26"), 2))
            {
                g.FillRectangle(bg, 0, 0, Width, Height);
                g.FillRectangle(header, 4, 4, Width - 8, 38);

                for (int y = 56; y < Height - 6; y += 26)
                {
                    using (Pen plank = new Pen(Color.FromArgb(35, 150, 90, 40)))
                    {
                        g.DrawLine(plank, 8, y, Width - 8, y);
                    }
                }

                g.FillRectangle(paper, 26, 212, 408, 100);
                g.DrawRectangle(inner, 26, 212, 408, 100);
                g.DrawRectangle(outer, 2, 2, Width - 4, Height - 4);
                g.DrawRectangle(inner, 5, 5, Width - 10, Height - 10);
            }

            Bitmap furnace = PixelSprites.GetPetFrame(PetType.Furnace, PetAnim.Idle, _flameFrame);
            int scale = 4;
            int fw = furnace.Width * scale;
            int fh = furnace.Height * scale;
            int fx = (Width - fw) / 2;
            int fy = 54;
            g.DrawImage(furnace, fx, fy, fw, fh);

            foreach (Ash a in _ashes)
            {
                int alpha = Math.Max(0, Math.Min(255, (int)(255f * a.Life / a.MaxLife)));
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, a.Color)))
                {
                    g.FillRectangle(brush, a.X, a.Y, 3, 3);
                }
            }
        }

        private void HeaderMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragOffset = Control.MousePosition;
            _dragOffset.Offset(-Location.X, -Location.Y);
        }

        private void HeaderMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            Point p = Control.MousePosition;
            Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y);
        }

        private void HeaderMouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
