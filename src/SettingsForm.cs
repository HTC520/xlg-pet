using System;
using System.Drawing;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    public class SettingsForm : Form
    {
        private readonly PetWindow _ownerPet;
        private Point _dragOffset;
        private bool _dragging;

        public SettingsForm(PetWindow ownerPet)
        {
            _ownerPet = ownerPet;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(380, 420);
            BackColor = ColorTranslator.FromHtml("#FCD89D");
            DoubleBuffered = true;

            BuildControls();
        }

        private void BuildControls()
        {
            PetConfig cfg = _ownerPet.Config;

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

            Label lblHeader = new Label
            {
                Text = "⚙ 星露谷桌宠设置",
                Location = new Point(16, 11),
                Size = new Size(280, 26),
                Font = new Font("Microsoft YaHei", 10.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#592A0E"),
                BackColor = Color.Transparent
            };
            lblHeader.MouseDown += HeaderMouseDown;
            lblHeader.MouseMove += HeaderMouseMove;
            lblHeader.MouseUp += HeaderMouseUp;
            Controls.Add(lblHeader);

            int y = 52;

            // Scale slider
            Label lblScale = AddLabel(string.Format("宠物大小：{0}x ({1}px)", cfg.Scale, cfg.Scale * 16), 20, y);
            TrackBar tbScale = AddTrackBar(2, 5, cfg.Scale, 175, y - 4, 180);
            tbScale.ValueChanged += (s, e) =>
            {
                lblScale.Text = string.Format("宠物大小：{0}x ({1}px)", tbScale.Value, tbScale.Value * 16);
                _ownerPet.ApplyScale(tbScale.Value);
            };
            y += 42;

            // Speed slider
            Label lblSpeed = AddLabel(string.Format("移动速度：{0} px/帧", cfg.MoveSpeed), 20, y);
            TrackBar tbSpeed = AddTrackBar(2, 8, cfg.MoveSpeed, 175, y - 4, 180);
            tbSpeed.ValueChanged += (s, e) =>
            {
                lblSpeed.Text = string.Format("移动速度：{0} px/帧", tbSpeed.Value);
                cfg.MoveSpeed = tbSpeed.Value;
                cfg.Save();
            };
            y += 42;

            // Activity slider
            Label lblActivity = AddLabel(string.Format("活跃度：{0} / 10", cfg.ActivityLevel), 20, y);
            TrackBar tbActivity = AddTrackBar(1, 10, cfg.ActivityLevel, 175, y - 4, 180);
            tbActivity.ValueChanged += (s, e) =>
            {
                lblActivity.Text = string.Format("活跃度：{0} / 10", tbActivity.Value);
                cfg.ActivityLevel = tbActivity.Value;
                cfg.Save();
            };
            y += 48;

            // Checkboxes
            CheckBox cbFreeRoam = AddCheck("桌面全屏自由漫步（关闭则贴合任务栏）", cfg.FreeRoam, 22, y);
            cbFreeRoam.CheckedChanged += (s, e) => _ownerPet.SetFreeRoam(cbFreeRoam.Checked);
            y += 30;

            CheckBox cbAutoWalk = AddCheck("自动随机走动", cfg.AutoWalk, 22, y);
            cbAutoWalk.CheckedChanged += (s, e) =>
            {
                cfg.AutoWalk = cbAutoWalk.Checked;
                cfg.Save();
            };
            y += 30;

            CheckBox cbTopMost = AddCheck("始终置顶显示", cfg.TopMost, 22, y);
            cbTopMost.CheckedChanged += (s, e) => _ownerPet.SetTopMostEnabled(cbTopMost.Checked);
            y += 30;

            CheckBox cbClickThrough = AddCheck("鼠标穿透（开启后请用托盘图标操作）", cfg.ClickThrough, 22, y);
            cbClickThrough.CheckedChanged += (s, e) => _ownerPet.SetClickThroughEnabled(cbClickThrough.Checked);
            y += 30;

            CheckBox cbKbDance = AddCheck("敲击键盘/鼠标时在像素键盘上跳舞", cfg.KeyboardDanceEnabled, 22, y);
            cbKbDance.CheckedChanged += (s, e) =>
            {
                cfg.KeyboardDanceEnabled = cbKbDance.Checked;
                cfg.Save();
            };
            y += 30;

            CheckBox cbSound = AddCheck("复古交互音效", cfg.SoundEnabled, 22, y);
            cbSound.CheckedChanged += (s, e) =>
            {
                cfg.SoundEnabled = cbSound.Checked;
                RetroAudio.Enabled = cfg.SoundEnabled;
                cfg.Save();
            };
            y += 30;

            CheckBox cbAutoStart = AddCheck("开机自动启动（Windows 启动文件夹）", AutoStartHelper.IsEnabled(), 22, y);
            cbAutoStart.CheckedChanged += (s, e) =>
            {
                if (!AutoStartHelper.SetEnabled(cbAutoStart.Checked))
                {
                    MessageBox.Show("设置开机自启快捷方式失败。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }

        private Label AddLabel(string text, int x, int y)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(155, 24),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#4A220A"),
                BackColor = Color.Transparent
            };
            Controls.Add(lbl);
            return lbl;
        }

        private TrackBar AddTrackBar(int min, int max, int val, int x, int y, int width)
        {
            TrackBar tb = new TrackBar
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, val)),
                Location = new Point(x, y),
                Size = new Size(width, 32),
                TickStyle = TickStyle.None,
                BackColor = ColorTranslator.FromHtml("#FCD89D")
            };
            Controls.Add(tb);
            return tb;
        }

        private CheckBox AddCheck(string text, bool checkedState, int x, int y)
        {
            CheckBox cb = new CheckBox
            {
                Text = text,
                Checked = checkedState,
                Location = new Point(x, y),
                Size = new Size(340, 24),
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = ColorTranslator.FromHtml("#4A220A"),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            Controls.Add(cb);
            return cb;
        }

        public void RefreshSettings()
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
