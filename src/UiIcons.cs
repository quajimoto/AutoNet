using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AutoNetSwitcher
{
    /// <summary>
    /// Iconify (Lucide Icons) 디자인 기반 고해상도 벡터 아이콘 렌더러
    /// 모든 아이콘은 Iconify/Lucide 공식 SVG 벡터 지오메트리를 기준으로 수학적으로 안티앨리어싱 렌더링됩니다.
    /// </summary>
    public static class UiIcons
    {
        private static readonly Dictionary<string, Image> IconCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static readonly object CacheLock = new object();

        /// <summary>
        /// 지정된 이름, 크기, 색상의 고해상도 벡터 아이콘 Bitmap 반환 (캐싱 지원)
        /// </summary>
        public static Image Get(string iconName, int size, Color color)
        {
            string key = string.Format("{0}_{1}_{2}", iconName, size, color.ToArgb());
            lock (CacheLock)
            {
                Image cached;
                if (IconCache.TryGetValue(key, out cached))
                {
                    return cached;
                }

                Bitmap bmp = CreateVectorIcon(iconName, size, color);
                IconCache[key] = bmp;
                return bmp;
            }
        }

        private static Bitmap CreateVectorIcon(string iconName, int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // 24x24 기준 Lucide 좌표계를 요청된 픽셀 크기로 스케일링
                float scale = size / 24.0f;
                g.ScaleTransform(scale, scale);

                float strokeWidth = Math.Max(1.6f, 2.0f);
                using (Pen pen = new Pen(color, strokeWidth))
                using (SolidBrush brush = new SolidBrush(color))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;

                    string name = (iconName ?? "").Trim().ToLowerInvariant();
                    switch (name)
                    {
                        case "wifi":
                            // Lucide: wifi
                            pen.Width = 2.0f;
                            g.DrawArc(pen, 2f, 4f, 20f, 20f, 215, 110);
                            g.DrawArc(pen, 5f, 7.5f, 14f, 14f, 215, 110);
                            g.DrawArc(pen, 8.5f, 11f, 7f, 7f, 215, 110);
                            g.FillEllipse(brush, 11f, 18f, 2.2f, 2.2f);
                            break;

                        case "network":
                            // Lucide: network (서버 및 네트워크 노드)
                            pen.Width = 1.8f;
                            DrawRoundedRect(g, pen, 9f, 2f, 6f, 6f, 1f);
                            DrawRoundedRect(g, pen, 2f, 16f, 6f, 6f, 1f);
                            DrawRoundedRect(g, pen, 16f, 16f, 6f, 6f, 1f);
                            g.DrawLine(pen, 12f, 8f, 12f, 12f);
                            g.DrawLine(pen, 5f, 16f, 5f, 12f);
                            g.DrawLine(pen, 5f, 12f, 19f, 12f);
                            g.DrawLine(pen, 19f, 12f, 19f, 16f);
                            break;

                        case "pencil":
                        case "edit":
                            // Lucide: pencil
                            pen.Width = 1.8f;
                            using (GraphicsPath path = new GraphicsPath())
                            {
                                path.AddLine(17f, 3f, 21f, 7f);
                                path.AddLine(21f, 7f, 7.5f, 20.5f);
                                path.AddLine(7.5f, 20.5f, 2f, 22f);
                                path.AddLine(2f, 22f, 3.5f, 16.5f);
                                path.CloseFigure();
                                g.DrawPath(pen, path);
                            }
                            g.DrawLine(pen, 15f, 5f, 19f, 9f);
                            break;

                        case "copy":
                            // Lucide: copy
                            pen.Width = 1.8f;
                            DrawRoundedRect(g, pen, 8f, 8f, 13f, 13f, 2f);
                            using (GraphicsPath backPath = new GraphicsPath())
                            {
                                backPath.AddLine(16f, 8f, 16f, 4f);
                                backPath.AddLine(16f, 4f, 4f, 4f);
                                backPath.AddLine(4f, 4f, 4f, 16f);
                                backPath.AddLine(4f, 16f, 8f, 16f);
                                g.DrawPath(pen, backPath);
                            }
                            break;

                        case "sliders":
                        case "settings":
                            // Lucide: sliders
                            pen.Width = 1.8f;
                            g.DrawLine(pen, 4f, 21f, 4f, 14f);
                            g.DrawLine(pen, 4f, 10f, 4f, 3f);
                            g.DrawLine(pen, 12f, 21f, 12f, 12f);
                            g.DrawLine(pen, 12f, 8f, 12f, 3f);
                            g.DrawLine(pen, 20f, 21f, 20f, 16f);
                            g.DrawLine(pen, 20f, 12f, 20f, 3f);
                            g.DrawLine(pen, 1f, 14f, 7f, 14f);
                            g.DrawLine(pen, 9f, 8f, 15f, 8f);
                            g.DrawLine(pen, 17f, 16f, 23f, 16f);
                            break;

                        case "refresh-cw":
                        case "refresh":
                        case "dhcp":
                            // Lucide: refresh-cw
                            pen.Width = 1.8f;
                            g.DrawArc(pen, 3f, 3f, 18f, 18f, 200, 145);
                            g.DrawLine(pen, 21f, 3f, 21f, 8f);
                            g.DrawLine(pen, 21f, 8f, 16f, 8f);
                            g.DrawArc(pen, 3f, 3f, 18f, 18f, 20, 145);
                            g.DrawLine(pen, 3f, 21f, 3f, 16f);
                            g.DrawLine(pen, 3f, 16f, 8f, 16f);
                            break;

                        case "activity":
                        case "ping":
                            // Lucide: activity (통신 상태)
                            pen.Width = 2.0f;
                            PointF[] actPoints = new PointF[]
                            {
                                new PointF(2f, 12f),
                                new PointF(6f, 12f),
                                new PointF(9.5f, 4f),
                                new PointF(14.5f, 20f),
                                new PointF(18f, 12f),
                                new PointF(22f, 12f)
                            };
                            g.DrawLines(pen, actPoints);
                            break;

                        case "bar-chart-2":
                        case "diag":
                            // Lucide: bar-chart-2
                            pen.Width = 2.2f;
                            g.DrawLine(pen, 18f, 20f, 18f, 10f);
                            g.DrawLine(pen, 12f, 20f, 12f, 4f);
                            g.DrawLine(pen, 6f, 20f, 6f, 14f);
                            break;

                        case "list":
                        case "history":
                            // Lucide: list
                            pen.Width = 1.8f;
                            g.DrawLine(pen, 8f, 6f, 21f, 6f);
                            g.DrawLine(pen, 8f, 12f, 21f, 12f);
                            g.DrawLine(pen, 8f, 18f, 21f, 18f);
                            g.FillEllipse(brush, 3f, 5f, 2f, 2f);
                            g.FillEllipse(brush, 3f, 11f, 2f, 2f);
                            g.FillEllipse(brush, 3f, 17f, 2f, 2f);
                            break;

                        case "power":
                            // Lucide: power
                            pen.Width = 2.0f;
                            g.DrawLine(pen, 12f, 2f, 12f, 11f);
                            g.DrawArc(pen, 3f, 3f, 18f, 18f, 120, 300);
                            break;

                        case "minus":
                            // Lucide: minus
                            pen.Width = 2.0f;
                            g.DrawLine(pen, 4f, 12f, 20f, 12f);
                            break;

                        case "x":
                        case "close":
                            // Lucide: x
                            pen.Width = 2.0f;
                            g.DrawLine(pen, 6f, 6f, 18f, 18f);
                            g.DrawLine(pen, 18f, 6f, 6f, 18f);
                            break;

                        case "chevron-down":
                            // Lucide: chevron-down
                            pen.Width = 2.2f;
                            PointF[] chevPoints = new PointF[]
                            {
                                new PointF(6f, 9f),
                                new PointF(12f, 15f),
                                new PointF(18f, 9f)
                            };
                            g.DrawLines(pen, chevPoints);
                            break;

                        case "trash-2":
                        case "trash":
                            // Lucide: trash-2
                            pen.Width = 1.8f;
                            g.DrawLine(pen, 3f, 6f, 21f, 6f);
                            g.DrawLine(pen, 8f, 6f, 8f, 3f);
                            g.DrawLine(pen, 8f, 3f, 16f, 3f);
                            g.DrawLine(pen, 16f, 3f, 16f, 6f);
                            using (GraphicsPath canPath = new GraphicsPath())
                            {
                                canPath.AddLine(5f, 6f, 6f, 21f);
                                canPath.AddLine(6f, 21f, 18f, 21f);
                                canPath.AddLine(18f, 21f, 19f, 6f);
                                g.DrawPath(pen, canPath);
                            }
                            g.DrawLine(pen, 10f, 10f, 10f, 17f);
                            g.DrawLine(pen, 14f, 10f, 14f, 17f);
                            break;

                        case "play":
                            // Lucide: play (검사 실행)
                            pen.Width = 1.8f;
                            PointF[] playPts = new PointF[]
                            {
                                new PointF(7f, 5f),
                                new PointF(19f, 12f),
                                new PointF(7f, 19f)
                            };
                            g.DrawPolygon(pen, playPts);
                            break;

                        default:
                            g.DrawRectangle(pen, 4f, 4f, 16f, 16f);
                            break;
                    }
                }
            }
            return bmp;
        }

        private static void DrawRoundedRect(Graphics g, Pen pen, float x, float y, float w, float h, float r)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(x, y, r * 2, r * 2, 180, 90);
                path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
                path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }
    }
}
