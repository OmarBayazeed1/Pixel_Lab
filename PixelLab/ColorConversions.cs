using System;

using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Media;
namespace PixelLab
{
    public static class ColorConversions
    {
        public static void RgbToHsv(double r, double g, double b, out double h, out double s, out double v)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            v = max;
            double delta = max - min;
            s = max == 0 ? 0 : delta / max;
            if (delta == 0) h = 0;
            else if (r == max) h = (g - b) / delta;
            else if (g == max) h = 2 + (b - r) / delta;
            else h = 4 + (r - g) / delta;
            h *= 60;
            if (h < 0) h += 360;
        }

        public static void RgbToLab(double r, double g, double b, out double L, out double a, out double bb)
        {
            double gamma(double c) => c > 0.04045 ? Math.Pow((c + 0.055) / 1.055, 2.4) : c / 12.92;
            r = gamma(r); g = gamma(g); b = gamma(b);
            double x = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
            double y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
            double z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;
            double fx = x / 0.95047, fy = y / 1.0, fz = z / 1.08883;
            fx = fx > 0.008856 ? Math.Pow(fx, 1.0 / 3.0) : (7.787 * fx + 16.0 / 116.0);
            fy = fy > 0.008856 ? Math.Pow(fy, 1.0 / 3.0) : (7.787 * fy + 16.0 / 116.0);
            fz = fz > 0.008856 ? Math.Pow(fz, 1.0 / 3.0) : (7.787 * fz + 16.0 / 116.0);
            L = 116 * fy - 16;
            a = 500 * (fx - fy);
            bb = 200 * (fy - fz);
        }

        public static void RgbToYuv(double r, double g, double b, out double y, out double u, out double v)
        {
            y = 0.299 * r + 0.587 * g + 0.114 * b;
            u = -0.14713 * r - 0.28886 * g + 0.436 * b;
            v = 0.615 * r - 0.51499 * g - 0.10001 * b;
        }

        public static void RgbToYCbCr(double r, double g, double b, out double y, out double cb, out double cr)
        {
            y = 16 + 65.481 * r + 128.553 * g + 24.966 * b;
            cb = 128 - 37.797 * r - 74.203 * g + 112.0 * b;
            cr = 128 + 112.0 * r - 93.786 * g - 18.214 * b;
        }

        public static void RgbToCmyk(double r, double g, double b, out double c, out double m, out double y, out double k)
        {
            k = 1 - Math.Max(r, Math.Max(g, b));
            if (k == 1) { c = m = y = 0; return; }
            c = (1 - r - k) / (1 - k);
            m = (1 - g - k) / (1 - k);
            y = (1 - b - k) / (1 - k);
        }
        public static Color LabToRgb(double L, double a, double b)
        {
            // Convert LAB to XYZ to RGB (simplified, using standard D65)
            double y = (L + 16) / 116;
            double x = y + a / 500;
            double z = y - b / 200;
            x = (x > 0.206893) ? Math.Pow(x, 3) : (x - 16.0 / 116) / 7.787;
            y = (y > 0.206893) ? Math.Pow(y, 3) : (y - 16.0 / 116) / 7.787;
            z = (z > 0.206893) ? Math.Pow(z, 3) : (z - 16.0 / 116) / 7.787;
            double r = x * 3.2406 - y * 1.5372 - z * 0.4986;
            double g = -x * 0.9689 + y * 1.8758 + z * 0.0415;
            double bb = x * 0.0557 - y * 0.2040 + z * 1.0570;
            double gamma(double c) => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
            r = gamma(Math.Max(0, Math.Min(1, r)));
            g = gamma(Math.Max(0, Math.Min(1, g)));
            b = gamma(Math.Max(0, Math.Min(1, bb)));
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

    }
}