using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoNetSwitcher
{
    /// <summary>
    /// 둥근 모서리 카드 패널. BackColor 가 카드 채움색이며, 모서리 바깥은 부모 배경색으로 칠합니다.
    /// </summary>
    public class RoundedPanel : Panel
    {
        private int radius = 12;
        private Color borderColor = Theme.Border;
        private Color gradientEnd = Color.Empty;

        public RoundedPanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            this.BackColor = Theme.Surface;
        }

        public int Radius { get { return radius; } set { radius = value; Invalidate(); } }
        public Color BorderColor { get { return borderColor; } set { borderColor = value; Invalidate(); } }
        /// <summary>지정 시 좌상단→우하단 그라데이션 채움</summary>
        public Color GradientEnd { get { return gradientEnd; } set { gradientEnd = value; Invalidate(); } }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color outer = (this.Parent != null) ? this.Parent.BackColor : Theme.Bg;
            g.Clear(outer);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF r = new RectangleF(0.5f, 0.5f, this.Width - 1.5f, this.Height - 1.5f);
            using (GraphicsPath path = Theme.RoundedRect(r, radius))
            {
                if (gradientEnd != Color.Empty)
                {
                    using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), this.BackColor, gradientEnd, 45f))
                    {
                        g.FillPath(lb, path);
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(this.BackColor))
                    {
                        g.FillPath(b, path);
                    }
                }

                if (borderColor != Color.Empty && borderColor != Color.Transparent)
                {
                    using (Pen p = new Pen(borderColor, 1f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 둥근 모서리 · hover/press 상태 · 아이콘을 지원하는 플랫 버튼
    /// </summary>
    public class ModernButton : Button
    {
        private bool hovered;
        private bool pressed;

        private int radius = 8;
        private Color hoverBackColor = Color.Empty;
        private Color pressBackColor = Color.Empty;
        private Color borderColor = Color.Empty;
        private Color gradientEnd = Color.Empty;
        private string iconName;
        private int iconSize = 14;
        private Color iconColor = Color.Empty;
        private Color iconHoverColor = Color.Empty;
        private string trailingIcon;
        private int contentPadding = 12;

        public ModernButton()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.TextAlign = ContentAlignment.MiddleCenter;
            this.Font = Theme.Ui(9f, FontStyle.Bold);
            this.ForeColor = Theme.Text;
            this.BackColor = Theme.SurfaceHover;
        }

        public int Radius { get { return radius; } set { radius = value; Invalidate(); } }
        public Color HoverBackColor { get { return hoverBackColor; } set { hoverBackColor = value; Invalidate(); } }
        public Color PressBackColor { get { return pressBackColor; } set { pressBackColor = value; Invalidate(); } }
        public Color BorderColor { get { return borderColor; } set { borderColor = value; Invalidate(); } }
        /// <summary>지정 시 BackColor → GradientEnd 가로 그라데이션</summary>
        public Color GradientEnd { get { return gradientEnd; } set { gradientEnd = value; Invalidate(); } }
        public string IconName { get { return iconName; } set { iconName = value; Invalidate(); } }
        public int IconSize { get { return iconSize; } set { iconSize = value; Invalidate(); } }
        public Color IconColor { get { return iconColor; } set { iconColor = value; Invalidate(); } }
        public Color IconHoverColor { get { return iconHoverColor; } set { iconHoverColor = value; Invalidate(); } }
        /// <summary>오른쪽 끝에 표시할 보조 아이콘 (예: chevron)</summary>
        public string TrailingIcon { get { return trailingIcon; } set { trailingIcon = value; Invalidate(); } }
        public int ContentPadding { get { return contentPadding; } set { contentPadding = value; Invalidate(); } }

        protected override bool ShowFocusCues { get { return false; } }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { if (mevent.Button == MouseButtons.Left) { pressed = true; Invalidate(); } base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { pressed = false; Invalidate(); base.OnMouseUp(mevent); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Color outer = (this.Parent != null) ? this.Parent.BackColor : Theme.Bg;
            g.Clear(outer);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            bool transparent = this.BackColor.A == 0;
            Color fill = transparent ? outer : this.BackColor;
            Color fillEnd = gradientEnd;

            if (this.Enabled)
            {
                if (pressed)
                {
                    fill = pressBackColor != Color.Empty ? pressBackColor : Theme.Blend(fill, Color.White, transparent ? 0.10f : 0.14f);
                    if (fillEnd != Color.Empty) fillEnd = Theme.Blend(fillEnd, Color.Black, 0.10f);
                }
                else if (hovered)
                {
                    fill = hoverBackColor != Color.Empty ? hoverBackColor : Theme.Blend(fill, Color.White, transparent ? 0.07f : 0.10f);
                    if (fillEnd != Color.Empty) fillEnd = Theme.Blend(fillEnd, Color.White, 0.12f);
                }
            }
            else
            {
                fill = Theme.Blend(fill, outer, 0.55f);
                if (fillEnd != Color.Empty) fillEnd = Theme.Blend(fillEnd, outer, 0.55f);
            }

            RectangleF r = new RectangleF(0.5f, 0.5f, this.Width - 1.5f, this.Height - 1.5f);
            bool drawFill = !transparent || hovered || pressed;
            using (GraphicsPath path = Theme.RoundedRect(r, radius))
            {
                if (drawFill)
                {
                    if (fillEnd != Color.Empty)
                    {
                        using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), fill, fillEnd, 0f))
                        {
                            g.FillPath(lb, path);
                        }
                    }
                    else
                    {
                        using (SolidBrush b = new SolidBrush(fill))
                        {
                            g.FillPath(b, path);
                        }
                    }
                }

                if (borderColor != Color.Empty)
                {
                    Color bc = this.Enabled ? (hovered ? Theme.Blend(borderColor, Color.White, 0.15f) : borderColor) : Theme.Blend(borderColor, outer, 0.5f);
                    using (Pen p = new Pen(bc, 1f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }

            // ---- 내용(아이콘 + 텍스트) 배치 ----
            Color textColor = this.Enabled ? this.ForeColor : Theme.Blend(this.ForeColor, outer, 0.55f);
            Image icon = null;
            if (!string.IsNullOrEmpty(iconName))
            {
                Color ic = iconColor != Color.Empty ? iconColor : this.ForeColor;
                if (this.Enabled && hovered && iconHoverColor != Color.Empty) ic = iconHoverColor;
                if (!this.Enabled) ic = Theme.Blend(ic, outer, 0.55f);
                icon = UiIcons.Get(iconName, iconSize, ic);
            }
            else if (this.Image != null)
            {
                icon = this.Image;
            }

            string text = (this.Text ?? string.Empty).Trim();
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
            Size textSize = text.Length > 0 ? TextRenderer.MeasureText(g, text, this.Font, new Size(int.MaxValue, int.MaxValue), flags) : Size.Empty;
            int gap = (icon != null && text.Length > 0) ? 7 : 0;
            int iconW = icon != null ? icon.Width : 0;

            int trailingW = 0;
            if (!string.IsNullOrEmpty(trailingIcon)) trailingW = 14;

            int contentW = iconW + gap + textSize.Width;
            int x;
            bool leftAligned = this.TextAlign == ContentAlignment.MiddleLeft || this.TextAlign == ContentAlignment.TopLeft || this.TextAlign == ContentAlignment.BottomLeft;
            if (leftAligned)
            {
                x = contentPadding;
            }
            else
            {
                x = (this.Width - contentW - (trailingW > 0 ? trailingW + 4 : 0)) / 2;
            }

            int maxTextW = Math.Max(0, this.Width - x - iconW - gap - contentPadding - (trailingW > 0 ? trailingW + 6 : 0));

            if (icon != null)
            {
                g.DrawImage(icon, x, (this.Height - icon.Height) / 2, icon.Width, icon.Height);
                x += iconW + gap;
            }

            if (text.Length > 0)
            {
                Rectangle tr = new Rectangle(x, 0, Math.Min(textSize.Width + 2, maxTextW), this.Height);
                TextRenderer.DrawText(g, text, this.Font, tr, textColor, flags | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            if (trailingW > 0)
            {
                Color tc = this.Enabled ? Theme.TextSubtle : Theme.Blend(Theme.TextSubtle, outer, 0.5f);
                if (hovered && this.Enabled) tc = Theme.TextMuted;
                Image ti = UiIcons.Get(trailingIcon, 12, tc);
                g.DrawImage(ti, this.Width - contentPadding - ti.Width, (this.Height - ti.Height) / 2, ti.Width, ti.Height);
            }
        }
    }

    /// <summary>
    /// 연결 상태 표시용 원형 점 (바깥 은은한 링 포함)
    /// </summary>
    public class StatusDot : Control
    {
        private Color dotColor = Theme.TextSubtle;

        public StatusDot()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            this.Size = new Size(10, 10);
        }

        public Color DotColor { get { return dotColor; } set { dotColor = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color outer = (this.Parent != null) ? this.Parent.BackColor : Theme.Bg;
            e.Graphics.Clear(outer);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush halo = new SolidBrush(Theme.Blend(outer, dotColor, 0.30f)))
            {
                e.Graphics.FillEllipse(halo, 0, 0, this.Width - 1, this.Height - 1);
            }
            using (SolidBrush b = new SolidBrush(dotColor))
            {
                e.Graphics.FillEllipse(b, 2, 2, this.Width - 5, this.Height - 5);
            }
        }
    }

    /// <summary>
    /// 둥근 배지(Pill) 라벨
    /// </summary>
    public class PillLabel : Label
    {
        private Color pillColor = Theme.AccentTint;

        public PillLabel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            this.Font = Theme.Ui(7.5f, FontStyle.Bold);
            this.TextAlign = ContentAlignment.MiddleCenter;
        }

        public Color PillColor { get { return pillColor; } set { pillColor = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color outer = (this.Parent != null) ? this.Parent.BackColor : Theme.Bg;
            e.Graphics.Clear(outer);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Theme.RoundedRect(new RectangleF(0, 0, this.Width - 1, this.Height - 1), this.Height / 2f))
            using (SolidBrush b = new SolidBrush(pillColor))
            {
                e.Graphics.FillPath(b, path);
            }
            TextRenderer.DrawText(e.Graphics, this.Text, this.Font, this.ClientRectangle, this.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>
    /// 둥근 테두리 입력창 (포커스 시 강조색 테두리)
    /// </summary>
    public class ThemedInput : RoundedPanel
    {
        private readonly TextBox textBox;
        private Color stateBorder = Color.Empty;

        public ThemedInput(bool monospace)
        {
            this.Radius = 8;
            this.BackColor = Theme.SurfaceInset;
            this.BorderColor = Theme.Border;
            this.Height = 34;
            this.Cursor = Cursors.IBeam;

            textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.SurfaceInset,
                ForeColor = Theme.Text,
                Font = monospace ? Theme.Mono(10f) : Theme.Ui(9.5f)
            };
            this.Controls.Add(textBox);

            textBox.GotFocus += (s, e) => { RefreshBorder(); };
            textBox.LostFocus += (s, e) => { RefreshBorder(); };
            this.Click += (s, e) => { textBox.Focus(); };
        }

        public TextBox TextBox { get { return textBox; } }

        /// <summary>유효성 상태 테두리 (Color.Empty 이면 기본)</summary>
        public Color StateBorder { get { return stateBorder; } set { stateBorder = value; RefreshBorder(); } }

        private void RefreshBorder()
        {
            if (stateBorder != Color.Empty) this.BorderColor = stateBorder;
            else this.BorderColor = textBox.Focused ? Theme.AccentStrong : Theme.Border;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (textBox != null)
            {
                textBox.Location = new Point(11, Math.Max(0, (this.Height - textBox.Height) / 2));
                textBox.Width = Math.Max(10, this.Width - 22);
            }
        }
    }

    /// <summary>
    /// 공통 다이얼로그 셸: 둥근 창 · 그림자 · 제목 바 · 닫기 버튼 · 드래그 이동
    /// </summary>
    public class ThemedDialog : Form
    {
        public const int HeaderHeight = 50;
        private bool nativeChrome;

        public ThemedDialog(string title, string iconName, int width, int height)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.Size = new Size(width, height);
            this.BackColor = Theme.Bg;
            this.ForeColor = Theme.Text;
            this.Font = Theme.Ui(9f);
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            Panel header = new Panel { Location = new Point(0, 0), Size = new Size(width, HeaderHeight), BackColor = Theme.Bg };
            Theme.MakeDraggable(header, this);

            RoundedPanel iconBadge = new RoundedPanel
            {
                Location = new Point(16, 13),
                Size = new Size(26, 26),
                Radius = 7,
                BackColor = Theme.AccentTint,
                BorderColor = Color.Empty
            };
            Label iconLbl = new Label
            {
                Dock = DockStyle.Fill,
                Image = UiIcons.Get(iconName ?? "list", 14, Theme.Accent),
                ImageAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.AccentTint
            };
            iconBadge.Controls.Add(iconLbl);
            Theme.MakeDraggable(iconLbl, this);

            Label lblTitle = new Label
            {
                Text = title ?? string.Empty,
                Font = Theme.Ui(10.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                Location = new Point(50, 14),
                AutoSize = true
            };
            Theme.MakeDraggable(lblTitle, this);

            ModernButton btnClose = new ModernButton
            {
                IconName = "x",
                IconSize = 13,
                IconColor = Theme.TextMuted,
                IconHoverColor = Theme.Text,
                BackColor = Color.Transparent,
                HoverBackColor = Theme.DangerTint,
                Size = new Size(30, 28),
                Location = new Point(width - 42, 11),
                TabStop = false
            };
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            header.Controls.Add(iconBadge);
            header.Controls.Add(lblTitle);
            header.Controls.Add(btnClose);
            this.Controls.Add(header);

            this.Paint += (s, e) =>
            {
                if (!nativeChrome)
                {
                    using (Pen p = new Pen(Theme.BorderStrong, 1f))
                    {
                        e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
                    }
                }
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            nativeChrome = Theme.ApplyWindowChrome(this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && this.CancelButton == null)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        /// <summary>라벨 + 입력창을 추가하고 입력창을 반환합니다.</summary>
        public ThemedInput AddField(string label, string value, int x, int y, int width, bool monospace)
        {
            Label lbl = new Label
            {
                Text = label,
                Font = Theme.Ui(8.5f),
                ForeColor = Theme.TextMuted,
                Location = new Point(x, y),
                AutoSize = true
            };
            ThemedInput input = new ThemedInput(monospace)
            {
                Location = new Point(x, y + 20),
                Width = width
            };
            input.TextBox.Text = value ?? string.Empty;
            this.Controls.Add(lbl);
            this.Controls.Add(input);
            return input;
        }

        public static ModernButton PrimaryButton(string text)
        {
            return new ModernButton
            {
                Text = text,
                Font = Theme.Ui(9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Theme.AccentStrong,
                GradientEnd = Theme.AccentBlue,
                Radius = 9
            };
        }

        public static ModernButton SecondaryButton(string text)
        {
            return new ModernButton
            {
                Text = text,
                Font = Theme.Ui(9f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Surface,
                BorderColor = Theme.Border,
                Radius = 9
            };
        }
    }
}
