using System;
using System.Drawing;
using System.Windows.Forms;

namespace PixelLab
{
    public partial class Form2D : Form
    {
        private PictureBox colorMap;
        private TrackBar valueSlider;
        private Label valueLabel;
        private Label[] rgbLabels, hsvLabels, labLabels, yuvLabels, ycbcrLabels, cmykLabels;
        private Panel colorPreview;
        private int currentValue = 255; // 0-255

        public Form2D()
        {
            InitializeComponent();
            SetupUI();
            GenerateColorWheel();
        }

        private void InitializeComponent()
        {
            this.Text = "2D Color Picker – Full HSV (Hue/Saturation/Value)";
            this.Size = new Size(900, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupUI()
        {
            // Left panel with color map and Value slider
            Panel leftPanel = new Panel { Dock = DockStyle.Left, Width = 500, Padding = new Padding(10) };
            colorMap = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            colorMap.MouseDown += (s, e) =>
            {
                if (colorMap.Image == null) return;
                Bitmap bmp = (Bitmap)colorMap.Image;
                if (e.X >= 0 && e.X < bmp.Width && e.Y >= 0 && e.Y < bmp.Height)
                {
                    Color picked = bmp.GetPixel(e.X, e.Y);
                    // The map is generated with full saturation, but the actual color's Value is taken from the slider.
                    // However, GetPixel returns the color with Value = currentValue because we generate the map with that Value.
                    // So we can just use picked directly.
                    UpdateColorInfo(picked);
                }
            };
            leftPanel.Controls.Add(colorMap);

            // Value slider at bottom of left panel
            Panel sliderPanel = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            valueSlider = new TrackBar { Minimum = 0, Maximum = 255, Value = 255, Dock = DockStyle.Fill, TickFrequency = 32 };
            valueSlider.ValueChanged += (s, e) =>
            {
                currentValue = valueSlider.Value;
                valueLabel.Text = $"Value (V): {currentValue}";
                GenerateColorWheel();
            };
            valueLabel = new Label { Text = $"Value (V): {currentValue}", Dock = DockStyle.Bottom, Height = 20, TextAlign = ContentAlignment.MiddleCenter };
            sliderPanel.Controls.Add(valueSlider);
            sliderPanel.Controls.Add(valueLabel);
            leftPanel.Controls.Add(sliderPanel);

            // Right panel with color information
            Panel rightPanel = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(10) };
            colorPreview = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = Color.Black };
            rightPanel.Controls.Add(colorPreview);
            rgbLabels = AddGroup(rightPanel, "RGB", 3);
            hsvLabels = AddGroup(rightPanel, "HSV", 3);
            labLabels = AddGroup(rightPanel, "LAB", 3);
            yuvLabels = AddGroup(rightPanel, "YUV", 3);
            ycbcrLabels = AddGroup(rightPanel, "YCbCr", 3);
            cmykLabels = AddGroup(rightPanel, "CMYK", 4);

            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);
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
            float val = currentValue / 255f;
            for (int y = 0; y < h; y++)
            {
                double sat = 1.0 - (double)y / h; // saturation from top (1) to bottom (0)
                for (int x = 0; x < w; x++)
                {
                    double hue = (double)x / w * 360.0;
                    Color col = HsvToRgb(hue, sat, val);
                    bmp.SetPixel(x, y, col);
                }
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

            rgbLabels[0].Text = $"R: {color.R}";
            rgbLabels[1].Text = $"G: {color.G}";
            rgbLabels[2].Text = $"B: {color.B}";

            ColorConversions.RgbToHsv(r, g, b, out double h, out double s, out double v);
            hsvLabels[0].Text = $"H: {h:F1}°";
            hsvLabels[1].Text = $"S: {s * 100:F1}%";
            hsvLabels[2].Text = $"V: {v * 100:F1}%";

            ColorConversions.RgbToLab(r, g, b, out double L, out double a, out double bb);
            labLabels[0].Text = $"L*: {L:F1}";
            labLabels[1].Text = $"a*: {a:F1}";
            labLabels[2].Text = $"b*: {bb:F1}";

            ColorConversions.RgbToYuv(r, g, b, out double y, out double u, out double vv);
            yuvLabels[0].Text = $"Y: {y:F1}";
            yuvLabels[1].Text = $"U: {u:F1}";
            yuvLabels[2].Text = $"V: {vv:F1}";

            ColorConversions.RgbToYCbCr(r, g, b, out double yc, out double cb, out double cr);
            ycbcrLabels[0].Text = $"Y: {yc:F1}";
            ycbcrLabels[1].Text = $"Cb: {cb:F1}";
            ycbcrLabels[2].Text = $"Cr: {cr:F1}";

            ColorConversions.RgbToCmyk(r, g, b, out double c, out double m, out double yk, out double k);
            cmykLabels[0].Text = $"C: {c * 100:F1}%";
            cmykLabels[1].Text = $"M: {m * 100:F1}%";
            cmykLabels[2].Text = $"Y: {yk * 100:F1}%";
            cmykLabels[3].Text = $"K: {k * 100:F1}%";
        }
    }
}