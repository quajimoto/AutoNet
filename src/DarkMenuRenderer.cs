using System.Drawing;
using System.Windows.Forms;

namespace AutoNetSwitcher
{
    /// <summary>
    /// 모던 다크 테마 컨텍스트 메뉴 컬러 테이블
    /// </summary>
    public class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Theme.Surface; }
        }

        public override Color MenuBorder
        {
            get { return Theme.Border; }
        }

        public override Color MenuItemBorder
        {
            get { return Color.Transparent; }
        }

        public override Color MenuItemSelected
        {
            get { return Theme.SurfaceHover; }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return Theme.SurfaceHover; }
        }

        public override Color MenuItemSelectedGradientEnd
        {
            get { return Theme.SurfaceHover; }
        }

        public override Color MenuItemPressedGradientBegin
        {
            get { return Theme.Surface; }
        }

        public override Color MenuItemPressedGradientMiddle
        {
            get { return Theme.Surface; }
        }

        public override Color MenuItemPressedGradientEnd
        {
            get { return Theme.Surface; }
        }

        public override Color SeparatorDark
        {
            get { return Theme.Border; }
        }

        public override Color SeparatorLight
        {
            get { return Theme.Surface; }
        }
    }

    /// <summary>
    /// 모던 다크 테마 렌더러 (자원 누수 방지 using 패턴 적용 - CWE-404)
    /// </summary>
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColorTable())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e != null && !(e.Item is ToolStripLabel))
            {
                if (e.Item != null && e.Item.Selected)
                {
                    e.TextColor = Theme.Accent; // Cyan Accent
                }
                else if (e.Item != null && e.Item.Enabled)
                {
                    e.TextColor = Theme.Text;
                }
                else
                {
                    e.TextColor = Theme.TextSubtle;
                }
            }
            base.OnRenderItemText(e);
        }

        /// <summary>
        /// 체크 표시를 다크 테마에 맞게 강조색 체크 아이콘으로 그림
        /// </summary>
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            if (e == null) return;
            Rectangle r = e.ImageRectangle;
            Image check = UiIcons.Get("check", 14, Theme.Success);
            e.Graphics.DrawImage(check, r.X + (r.Width - check.Width) / 2, r.Y + (r.Height - check.Height) / 2, check.Width, check.Height);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e == null || e.Item == null)
                return;

            if (e.Item.Selected && e.Item.Enabled)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(4f, 1f, e.Item.Width - 8f, e.Item.Height - 2f);
                using (System.Drawing.Drawing2D.GraphicsPath path = Theme.RoundedRect(rect, 6f))
                using (SolidBrush brush = new SolidBrush(Theme.SurfaceHover))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
    }
}
