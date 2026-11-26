using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    public class InputStatsForm : Form
    {
        private readonly InputStats _stats;
        private readonly Timer _refreshTimer;
        private Label _lblTitleRank;
        private Label _lblTodayKeys;
        private Label _lblTodayMouse;
        private Label _lblTotalKeys;
        private Label _lblTotalMouse;
        private Label _lblSpeed;
        private Panel _chartPanel;
        private Point _dragOffset;
        private bool _dragging;

        public InputStatsForm(InputStats stats)
        {
            _stats = stats;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(440, 410);
            BackColor = ColorTranslator.FromHtml("#FCD89D");
            DoubleBuffered = true;

            BuildControls();
            RefreshData();

            _refreshTimer = new Timer();
            _refreshTimer.Interval = 500;
            _refreshTimer.Tick += (s, e) => RefreshData();
            _refreshTimer.Start();
        }

        private void BuildControls()
        {
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
                Text = "⌨ 鹈鹕镇键鼠劳作统计面板",
                Location = new Point(16, 11),
                Size = new Size(340, 26),
                Font = new Font("Microsoft YaHei", 10.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#592A0E"),
                BackColor = Color.Transparent
            };
            lblHeader.MouseDown += HeaderMouseDown;
            lblHeader.MouseMove += HeaderMouseMove;
            lblHeader.MouseUp += HeaderMouseUp;
            Controls.Add(lblHeader);

            _lblTitleRank = new Label
            {
                Location = new Point(20, 48),
                Size = new Size(400, 24),
                Font = new Font("Microsoft YaHei", 9.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#8C3B10"),
                BackColor = Color.Transparent
            };
            Controls.Add(_lblTitleRank);

            _lblTodayKeys = CreateStatBox(20, 78, 192, 54, "#EBF7DC", "#3A7D26");
            _lblTodayMouse = CreateStatBox(228, 78, 192, 54, "#E1EFFC", "#2A62A8");
            _lblTotalKeys = CreateStatBox(20, 140, 192, 50, "#F5C982", "#4A220A");
            _lblTotalMouse = CreateStatBox(228, 140, 192, 50, "#F5C982", "#4A220A");

            _lblSpeed = new Label
            {
                Location = new Point(20, 198),
                Size = new Size(400, 22),
                Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#592A0E"),
                BackColor = Color.Transparent
            };
            Controls.Add(_lblSpeed);

            Label lblChartTitle = new Label
            {
                Text = "近 7 日按键与点击趋势（绿=键盘  蓝=鼠标）",
                Location = new Point(20, 224),
                Size = new Size(360, 20),
                Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#592A0E"),
                BackColor = Color.Transparent
            };
            Controls.Add(lblChartTitle);

            _chartPanel = new Panel
            {
                Location = new Point(20, 246),
                Size = new Size(400, 112),
                BackColor = ColorTranslator.FromHtml("#FCE6BD")
            };
            _chartPanel.Paint += PaintChart;
            Controls.Add(_chartPanel);

            Button btnReset = new Button
            {
                Text = "🗑 清空全部统计",
                Location = new Point(20, 368),
                Size = new Size(130, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#EBA952"),
                ForeColor = ColorTranslator.FromHtml("#4A220A"),
                Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#753A14");
            btnReset.Click += (s, e) =>
            {
                DialogResult res = MessageBox.Show(
                    "确定要清空全部按键与鼠标统计记录吗？",
                    "重置统计",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    _stats.ResetAll();
                    RefreshData();
                }
            };
            Controls.Add(btnReset);

            Label lblPrivacy = new Label
            {
                Text = "🔒 仅统计次数，绝不记录任何按键内容",
                Location = new Point(160, 373),
                Size = new Size(260, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = ColorTranslator.FromHtml("#7A4924"),
                BackColor = Color.Transparent
            };
            Controls.Add(lblPrivacy);
        }

        private Label CreateStatBox(int x, int y, int w, int h, string bgHex, string fgHex)
        {
            Label lbl = new Label
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = ColorTranslator.FromHtml(bgHex),
                ForeColor = ColorTranslator.FromHtml(fgHex),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 4, 8, 4),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lbl);
            return lbl;
        }

        public void RefreshData()
        {
            if (IsDisposed) return;
            _stats.EnsureCurrentDay();

            _lblTitleRank.Text = string.Format("今日称号：{0}（今日总操作 {1:N0} 次）",
                _stats.GetFarmerTitle(), _stats.TodayKeyPresses + _stats.TodayMouseClicks);

            _lblTodayKeys.Text = string.Format("⌨ 今日敲击键盘\r\n   {0:N0} 次", _stats.TodayKeyPresses);
            _lblTodayMouse.Text = string.Format("🖱 今日鼠标点击\r\n   {0:N0} 次 (左{1:N0}/右{2:N0})",
                _stats.TodayMouseClicks, _stats.TodayLeftClicks, _stats.TodayRightClicks);

            _lblTotalKeys.Text = string.Format("📚 历史累计键盘\r\n   {0:N0} 次", _stats.TotalKeyPresses);
            _lblTotalMouse.Text = string.Format("🏆 历史累计鼠标\r\n   {0:N0} 次", _stats.TotalMouseClicks);

            _lblSpeed.Text = string.Format("⚡ 当前实时手速：{0} 次/分    |    峰值记录：{1} 次/分",
                _stats.GetCurrentMinuteOps(), _stats.BestMinuteOps);

            _chartPanel.Invalidate();
        }

        private void PaintChart(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;

            using (Pen border = new Pen(ColorTranslator.FromHtml("#9E5524"), 2))
            {
                g.DrawRectangle(border, 1, 1, _chartPanel.Width - 2, _chartPanel.Height - 2);
            }

            List<DailyInputEntry> days = _stats.GetLast7Days();
            long maxVal = 10;
            foreach (DailyInputEntry d in days)
            {
                if (d.KeyPresses > maxVal) maxVal = d.KeyPresses;
                if (d.MouseClicks > maxVal) maxVal = d.MouseClicks;
            }

            int leftPad = 14;
            int topPad = 18;
            int bottomPad = 22;
            int plotH = _chartPanel.Height - topPad - bottomPad;
            int slotW = (_chartPanel.Width - leftPad * 2) / days.Count;
            int barW = 14;

            using (SolidBrush keyBrush = new SolidBrush(ColorTranslator.FromHtml("#5CC445")))
            using (SolidBrush mouseBrush = new SolidBrush(ColorTranslator.FromHtml("#5CA2F5")))
            using (SolidBrush textBrush = new SolidBrush(ColorTranslator.FromHtml("#592A0E")))
            using (Pen axisPen = new Pen(ColorTranslator.FromHtml("#C48A52"), 1))
            using (Font smallFont = new Font("Microsoft YaHei", 7.5f))
            {
                int baseY = topPad + plotH;
                g.DrawLine(axisPen, leftPad, baseY, _chartPanel.Width - leftPad, baseY);

                for (int i = 0; i < days.Count; i++)
                {
                    DailyInputEntry d = days[i];
                    int cx = leftPad + i * slotW + slotW / 2;

                    int kh = (int)(plotH * ((double)d.KeyPresses / maxVal));
                    int mh = (int)(plotH * ((double)d.MouseClicks / maxVal));
                    if (d.KeyPresses > 0 && kh < 2) kh = 2;
                    if (d.MouseClicks > 0 && mh < 2) mh = 2;

                    g.FillRectangle(keyBrush, cx - barW - 1, baseY - kh, barW, kh);
                    g.FillRectangle(mouseBrush, cx + 1, baseY - mh, barW, mh);

                    string shortDate = d.DateKey.Length >= 10 ? d.DateKey.Substring(5) : d.DateKey;
                    SizeF sz = g.MeasureString(shortDate, smallFont);
                    g.DrawString(shortDate, smallFont, textBrush, cx - sz.Width / 2f, baseY + 3);

                    long daySum = d.KeyPresses + d.MouseClicks;
                    if (daySum > 0)
                    {
                        string numLabel = daySum >= 1000 ? string.Format("{0:0.#}k", daySum / 1000.0) : daySum.ToString();
                        SizeF nsz = g.MeasureString(numLabel, smallFont);
                        int topY = Math.Min(baseY - kh, baseY - mh) - 14;
                        if (topY < 2) topY = 2;
                        g.DrawString(numLabel, smallFont, textBrush, cx - nsz.Width / 2f, topY);
                    }
                }
            }
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
