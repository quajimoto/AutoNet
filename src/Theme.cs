using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoNetSwitcher
{
    /// <summary>
    /// AutoNet 공통 디자인 토큰 (색상 / 글꼴 / 모서리) 및 그리기 헬퍼
    /// 모든 화면이 동일한 팔레트를 사용하도록 한 곳에서 관리합니다.
    /// </summary>
    public static class Theme
    {
        // ---------- 배경 / 표면 ----------
        public static readonly Color Bg = Color.FromArgb(11, 17, 32);
        public static readonly Color Surface = Color.FromArgb(20, 29, 47);
        public static readonly Color SurfaceInset = Color.FromArgb(14, 21, 37);
        public static readonly Color SurfaceHover = Color.FromArgb(30, 42, 66);
        public static readonly Color SurfacePressed = Color.FromArgb(38, 52, 80);
        public static readonly Color Border = Color.FromArgb(36, 49, 74);
        public static readonly Color BorderStrong = Color.FromArgb(52, 66, 94);

        // ---------- 텍스트 ----------
        public static readonly Color Text = Color.FromArgb(241, 245, 249);
        public static readonly Color TextSecondary = Color.FromArgb(203, 213, 225);
        public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);
        public static readonly Color TextSubtle = Color.FromArgb(100, 116, 139);

        // ---------- 강조 ----------
        public static readonly Color Accent = Color.FromArgb(34, 211, 238);
        public static readonly Color AccentStrong = Color.FromArgb(6, 182, 212);
        public static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
        public static readonly Color AccentTint = Color.FromArgb(21, 61, 82);

        public static readonly Color Success = Color.FromArgb(52, 211, 153);
        public static readonly Color SuccessTint = Color.FromArgb(14, 52, 44);
        public static readonly Color Warning = Color.FromArgb(251, 191, 36);
        public static readonly Color Danger = Color.FromArgb(244, 63, 94);
        public static readonly Color DangerSoft = Color.FromArgb(253, 164, 175);
        public static readonly Color DangerTint = Color.FromArgb(60, 22, 34);
        public static readonly Color Indigo = Color.FromArgb(165, 180, 252);

        // ---------- 글꼴 ----------
        private const string UiFamily = "Malgun Gothic";
        private const string MonoFamily = "Consolas";
        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();
        private static readonly object FontLock = new object();

        public static Font Ui(float size, FontStyle style = FontStyle.Regular)
        {
            return GetFont(UiFamily, size, style);
        }

        public static Font Mono(float size, FontStyle style = FontStyle.Regular)
        {
            return GetFont(MonoFamily, size, style);
        }

        private static Font GetFont(string family, float size, FontStyle style)
        {
            string key = family + "|" + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + (int)style;
            lock (FontLock)
            {
                Font f;
                if (!FontCache.TryGetValue(key, out f))
                {
                    try { f = new Font(family, size, style); }
                    catch { f = new Font(FontFamily.GenericSansSerif, size, style); }
                    FontCache[key] = f;
                }
                return f;
            }
        }

        // ---------- 그리기 헬퍼 ----------
        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Blend(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>
        /// 폼에 Windows 11 둥근 모서리 + 테두리 색을 적용합니다. 성공 여부를 반환합니다.
        /// </summary>
        public static bool ApplyWindowChrome(Form form)
        {
            if (form == null || !form.IsHandleCreated) return false;
            try
            {
                int corner = NativeMethods.DWMWCP_ROUND;
                int hr = NativeMethods.DwmSetWindowAttribute(form.Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
                int border = ColorTranslator.ToWin32(BorderStrong);
                NativeMethods.DwmSetWindowAttribute(form.Handle, NativeMethods.DWMWA_BORDER_COLOR, ref border, sizeof(int));
                return hr == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 테두리 없는 창을 마우스로 끌어 이동하도록 연결합니다.
        /// </summary>
        public static void MakeDraggable(Control handle, Form target)
        {
            if (handle == null || target == null) return;
            handle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(target.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, 0);
                }
            };
        }
    }
}
