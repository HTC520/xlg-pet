using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    public class StatusCardForm : Form
    {
        private readonly PetWindow _ownerPet;
        private Point _dragOffset;
        private bool _dragging;

        public StatusCardForm(PetWindow ownerPet)
        {
            _ownerPet = ownerPet;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(370, 360);
            BackColor = ColorTranslator.FromHtml("#FCD89D");
            DoubleBuffered = true;

            BuildControls();
        }

        private void BuildControls()
        {
            // Close button
            Button btnClose = new Button
            {
                Text = "✕",
                Size = new Size(28, 26),
                Location = new Point(Width - 38, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#D96B43"),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#592A0E");
            btnClose.Click += (s, e) => Hide();
            Controls.Add(btnClose);

            // Title
            Label lblTitle = new Label
            {
                Text = "★ 星露谷桌面伙伴日记 ★",
                AutoSize = false,
                Size = new Size(280, 26),
                Location = new Point(16, 11),
                Font = new Font("Microsoft YaHei", 10.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#592A0E"),
                BackColor = Color.Transparent
            };
            lblTitle.MouseDown += HeaderMouseDown;
            lblTitle.MouseMove += HeaderMouseMove;
            lblTitle.MouseUp += HeaderMouseUp;
            Controls.Add(lblTitle);

            // Portrait Box
            PictureBox pic = new PictureBox
            {
                Size = new Size(76, 76),
                Location = new Point(20, 48),
                BackColor = ColorTranslator.FromHtml("#F3C27A"),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            pic.Paint += (s, e) =>
            {
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                Bitmap sprite = PixelSprites.GetPetFrame(_ownerPet.Config.Pet, PetAnim.Happy, 0);
                e.Graphics.DrawImage(sprite, new Rectangle(6, 6, 64, 64));
                using (Pen border = new Pen(ColorTranslator.FromHtml("#8C4A1F"), 2))
                {
                    e.Graphics.DrawRectangle(border, 1, 1, pic.Width - 3, pic.Height - 3);
                }
            };
            Controls.Add(pic);

            // Name & Friendship
            Label lblName = new Label
            {
                Text = PixelSprites.GetPetDisplayName(_ownerPet.Config.Pet),
                Location = new Point(108, 50),
                Size = new Size(240, 24),
                Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#4A220A"),
                BackColor = Color.Transparent
            };
            Controls.Add(lblName);

            int hearts = Math.Min(10, _ownerPet.Config.FriendshipPoints / 20);
            string heartBar = new string('♥', hearts) + new string('♡', 10 - hearts);
            Label lblHearts = new Label
            {
                Text = string.Format("好感度：{0} ({1} pt)", heartBar, _ownerPet.Config.FriendshipPoints),
                Location = new Point(108, 76),
                Size = new Size(246, 22),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#C4303C"),
                BackColor = Color.Transparent
            };
            Controls.Add(lblHearts);

            Label lblMood = new Label
            {
                Text = "状态：" + _ownerPet.GetStateLabel(),
                Location = new Point(108, 100),
                Size = new Size(240, 22),
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = ColorTranslator.FromHtml("#6B3B16"),
                BackColor = Color.Transparent
            };
            Controls.Add(lblMood);

            // Daily Fortune box
            Label lblFortune = new Label
            {
                Text = StardewData.GetDailyFortune(),
                Location = new Point(20, 136),
                Size = new Size(Width - 40, 46),
                Padding = new Padding(8, 6, 8, 6),
                Font = new Font("Microsoft YaHei", 8.5f),
                ForeColor = ColorTranslator.FromHtml("#4A220A"),
                BackColor = ColorTranslator.FromHtml("#F5C982")
            };
            Controls.Add(lblFortune);

            // Feed Section Label
            Label lblFeed = new Label
            {
                Text = "投喂星露谷特产：",
                Location = new Point(20, 192),
                Size = new Size(200, 20),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#592A0E"),
                BackColor = Color.Transparent
            };
            Controls.Add(lblFeed);

            AddFeedButton("防风草", EmoteType.Parsnip, 20, 218);
            AddFeedButton("草莓", EmoteType.Strawberry, 105, 218);
            AddFeedButton("星之果实", EmoteType.Stardrop, 190, 218);
            AddFeedButton("虚空蛋黄酱", EmoteType.VoidMayo, 275, 218);

            // Quick Actions
            Button btnPet = CreateWoodenButton("💕 摸摸头", 20, 268, 104);
            btnPet.Click += (s, e) =>
            {
                _ownerPet.TriggerPetting();
                RefreshCard();
            };
            Controls.Add(btnPet);

            Button btnNextPet = CreateWoodenButton("🐾 换个伙伴", 133, 268, 104);
            btnNextPet.Click += (s, e) =>
            {
                _ownerPet.CycleNextPet();
                RefreshCard();
            };
            Controls.Add(btnNextPet);

            Button btnFortune = CreateWoodenButton("🔮 运势播报", 246, 268, 104);
            btnFortune.Click += (s, e) => _ownerPet.ShowDailyFortune();
            Controls.Add(btnFortune);

            long todayOps = _ownerPet.Stats.TodayKeyPresses + _ownerPet.Stats.TodayMouseClicks;
            Button btnStats = CreateWoodenButton(
                string.Format("📊 查看键鼠统计（今日 {0:N0} 次）", todayOps), 20, 310, 330);
            btnStats.Click += (s, e) => _ownerPet.OpenStatsForm();
            Controls.Add(btnStats);
        }

        private void AddFeedButton(string label, EmoteType food, int x, int y)
        {
            Button btn = CreateWoodenButton(label, x, y, 76);
            btn.Height = 36;
            btn.Font = new Font("Microsoft YaHei", 8f, FontStyle.Bold);
            btn.Click += (s, e) =>
            {
                _ownerPet.FeedPet(food);
                RefreshCard();
            };
            Controls.Add(btn);
        }

        private Button CreateWoodenButton(string text, int x, int y, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#EBA952"),
                ForeColor = ColorTranslator.FromHtml("#4A220A"),
                Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#753A14");
            btn.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#F5C374");
            return btn;
        }

        public void RefreshCard()
        {
            Controls.Clear();
            BuildControls();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen outer = new Pen(ColorTranslator.FromHtml("#592A0E"), 4))
            using (Pen mid = new Pen(ColorTranslator.FromHtml("#A65B26"), 2))
            using (SolidBrush headerBar = new SolidBrush(ColorTranslator.FromHtml("#F0BC6B")))
            {
                e.Graphics.FillRectangle(headerBar, 4, 4, Width - 8, 36);
                e.Graphics.DrawRectangle(outer, 2, 2, Width - 4, Height - 4);
                e.Graphics.DrawRectangle(mid, 5, 5, Width - 10, Height - 10);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            HeaderMouseDown(this, e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            HeaderMouseMove(this, e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            HeaderMouseUp(this, e);
        }

        private void HeaderMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragOffset = Control.MousePosition;
                _dragOffset.Offset(-Location.X, -Location.Y);
            }
        }

        private void HeaderMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                Point p = Control.MousePosition;
                Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y);
            }
        }

        private void HeaderMouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }
    }
}
