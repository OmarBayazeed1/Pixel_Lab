using System;
using System.Drawing;
using System.Windows.Forms;

namespace PixelLab
{
    public partial class Form2D : Form
    {
        private PictureBox colorMap;
        private Label[] rgbLabels, hsvLabels, labLabels, yuvLabels, ycbcrLabels, cmykLabels;
        private Panel colorPreview;

        public Form2D()
        {
            InitializeComponent();
            SetupUI();
            GenerateColorWheel();
        }

        private void InitializeComponent()
        {
            this.Text = "2D Color Map – Click to pick a color";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupUI()
        {
            Panel left = new Panel { Dock = DockStyle.Left, Width = 500, Padding = new Padding(10) };
            colorMap = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            colorMap.MouseClick += (s, e) =>
            {
                if (colorMap.Image == null) return;
                Bitmap bmp = (Bitmap)colorMap.Image;
                if (e.X >= 0 && e.X < bmp.Width && e.Y >= 0 && e.Y < bmp.Height)
                    UpdateColorInfo(bmp.GetPixel(e.X, e.Y));
            };
            left.Controls.Add(colorMap);

            Panel right = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(10) };
            colorPreview = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = Color.Black };
            right.Controls.Add(colorPreview);
            rgbLabels = AddGroup(right, "RGB", 3);
            hsvLabels = AddGroup(right, "HSV", 3);
            labLabels = AddGroup(right, "LAB", 3);
            yuvLabels = AddGroup(right, "YUV", 3);
            ycbcrLabels = AddGroup(right, "YCbCr", 3);
            cmykLabels = AddGroup(right, "CMYK", 4);

            this.Controls.Add(left);
            this.Controls.Add(right);
        }

        private Label[] AddGroup(Panel parent, string title, int count)
        {
            GroupBox gb = new GroupBox { Text = title, Dock = DockStyle.Top, Height = 70, Padding = new Padding(5) };
            Label[] labels = new Label[count];
            for (int i = 0; i < count; i++)
            {
                labels[i] = new Label { AutoSize = true, Location = new Point(10, 20 + i * 18) };
                gb.Controls.Add(labels[i]);
            }
            parent.Controls.Add(gb);
            return labels;
        }

        private void GenerateColorWheel()
        {
            int w = 400, h = 400;
            Bitmap bmp = new Bitmap(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double hue = (double)x / w * 360.0;
                    double sat = 1.0 - (double)y / h;
                    Color col = HsvToRgb(hue, sat, 1.0);
                    bmp.SetPixel(x, y, col);
                }
            colorMap.Image = bmp;
        }

        private Color HsvToRgb(double h, double s, double v)
        {
            double r = 0, g = 0, b = 0;
            int hi = (int)Math.Floor(h / 60.0) % 6;
            double f = h / 60.0 - Math.Floor(h / 60.0);
            double p = v * (1 - s);
            double q = v * (1 - f * s);
            double t = v * (1 - (1 - f) * s);
            switch (hi)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private void UpdateColorInfo(Color color)
        {
            colorPreview.BackColor = color;
            double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
            // RGB
            rgbLabels[0].Text = $"R: {color.R}";
            rgbLabels[1].Text = $"G: {color.G}";
            rgbLabels[2].Text = $"B: {color.B}";
            // HSV
            RGBToHSV(r, g, b, out double h, out double s, out double v);
            hsvLabels[0].Text = $"H: {h:F1}°";
            hsvLabels[1].Text = $"S: {s * 100:F1}%";
            hsvLabels[2].Text = $"V: {v * 100:F1}%";
            // LAB
            RGBToLAB(r, g, b, out double L, out double a, out double bb);
            labLabels[0].Text = $"L*: {L:F1}";
            labLabels[1].Text = $"a*: {a:F1}";
            labLabels[2].Text = $"b*: {bb:F1}";
            // YUV
            RGBToYUV(r, g, b, out double y, out double u, out double vv);
            yuvLabels[0].Text = $"Y: {y:F1}";
            yuvLabels[1].Text = $"U: {u:F1}";
            yuvLabels[2].Text = $"V: {vv:F1}";
            // YCbCr
            RGBToYCbCr(r, g, b, out double yc, out double cb, out double cr);
            ycbcrLabels[0].Text = $"Y: {yc:F1}";
            ycbcrLabels[1].Text = $"Cb: {cb:F1}";
            ycbcrLabels[2].Text = $"Cr: {cr:F1}";
            // CMYK
            RGBToCMYK(r, g, b, out double c, out double m, out double yk, out double k);
            cmykLabels[0].Text = $"C: {c * 100:F1}%";
            cmykLabels[1].Text = $"M: {m * 100:F1}%";
            cmykLabels[2].Text = $"Y: {yk * 100:F1}%";
            cmykLabels[3].Text = $"K: {k * 100:F1}%";
        }

        // ----- Copy the same conversion methods from Form3D -----
        private void RGBToHSV(double r, double g, double b, out double h, out double s, out double v)
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

        private void RGBToLAB(double r, double g, double b, out double L, out double a, out double bb)
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

        private void RGBToYUV(double r, double g, double b, out double y, out double u, out double v)
        {
            y = 0.299 * r + 0.587 * g + 0.114 * b;
            u = -0.14713 * r - 0.28886 * g + 0.436 * b;
            v = 0.615 * r - 0.51499 * g - 0.10001 * b;
        }

        private void RGBToYCbCr(double r, double g, double b, out double y, out double cb, out double cr)
        {
            y = 16 + 65.481 * r + 128.553 * g + 24.966 * b;
            cb = 128 - 37.797 * r - 74.203 * g + 112.0 * b;
            cr = 128 + 112.0 * r - 93.786 * g - 18.214 * b;
        }

        private void RGBToCMYK(double r, double g, double b, out double c, out double m, out double y, out double k)
        {
            k = 1 - Math.Max(r, Math.Max(g, b));
            if (k == 1) { c = m = y = 0; return; }
            c = (1 - r - k) / (1 - k);
            m = (1 - g - k) / (1 - k);
            y = (1 - b - k) / (1 - k);
        }
    }
}