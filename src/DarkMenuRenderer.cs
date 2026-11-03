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
            get { return Color.FromArgb(15, 23, 42); }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Color.FromArgb(15, 23, 42); }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Color.FromArgb(15, 23, 42); }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Color.FromArgb(15, 23, 42); }
        }

        public override Color MenuBorder
        {
            get { return Color.FromArgb(51, 65, 85); }
        }

        public override Color MenuItemBorder
        {
            get { return Color.Transparent; }
        }

        public override Color MenuItemSelected
        {
            get { return Color.FromArgb(30, 41, 59); }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return Color.FromArgb(30, 41, 59); }
        }

        public override Color MenuItemSelectedGradientEnd
        {
            get { return Color.FromArgb(30, 41, 59); }
        }

        public override Color MenuItemPressedGradientBegin
        {
            get { return Color.FromArgb(15, 23, 42); }
        }

        public override Color MenuItemPressedGradientMiddle
        {
            get { return Color.FromArgb(15, 23, 42); }
        }

        public override Color MenuItemPressedGradientEnd
        {
            get { return Color.FromArgb(15, 23, 42); }
        }

        public override Color SeparatorDark
        {
            get { return Color.FromArgb(51, 65, 85); }
        }

        public override Color SeparatorLight
        {
            get { return Color.FromArgb(15, 23, 42); }
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
            if (e != null)
            {
                if (e.Item != null && e.Item.Selected)
                {
                    e.TextColor = Color.FromArgb(34, 211, 238); // Cyan Accent
                }
                else if (e.Item != null && e.Item.Enabled)
                {
                    e.TextColor = Color.FromArgb(241, 245, 249);
                }
                else
                {
                    e.TextColor = Color.FromArgb(100, 116, 139);
                }
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e == null || e.Item == null)
                return;

            if (e.Item.Selected && e.Item.Enabled)
            {
                Rectangle rect = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
                using (Pen pen = new Pen(Color.FromArgb(51, 65, 85)))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
    }
}
