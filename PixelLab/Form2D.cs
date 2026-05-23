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
        private ComboBox cmbColorSpace;
        private Label[] rgbLabels, hsvLabels, labLabels, yuvLabels, ycbcrLabels, cmykLabels;
        private Panel colorPreview;
        private int currentValue = 128; // 0-255 (for luminance/lightness/value)
        private string currentSpace = "HSV";
        private Label axisLabel; // class field

        public Form2D()
        {
            InitializeComponent();
            SetupUI();
            GenerateMap();
        }

        private void InitializeComponent()
        {
            this.Text = "2D Color Picker – Choose a color space";
            this.Size = new Size(950, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupUI()
        {
            // Left panel with color map and controls
            Panel leftPanel = new Panel { Dock = DockStyle.Left, Width = 500, Padding = new Padding(10) };
            colorMap = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            colorMap.MouseDown += (s, e) =>
            {
                if (colorMap.Image == null) return;
                Bitmap bmp = (Bitmap)colorMap.Image;
                if (e.X >= 0 && e.X < bmp.Width && e.Y >= 0 && e.Y < bmp.Height)
                {
                    Color picked = bmp.GetPixel(e.X, e.Y);
                    UpdateColorInfo(picked);
                }
            };
            leftPanel.Controls.Add(colorMap);

            // Control panel at bottom of left panel
            Panel controlPanel = new Panel { Dock = DockStyle.Bottom, Height = 100 };
            cmbColorSpace = new ComboBox();
            cmbColorSpace.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbColorSpace.Items.AddRange(new object[] { "RGB", "HSV", "LAB", "YUV", "YCbCr", "CMYK" });
            cmbColorSpace.SelectedItem = "HSV";
            cmbColorSpace.Dock = DockStyle.Top;
            cmbColorSpace.SelectedIndexChanged += (s, e) =>
            {
                currentSpace = cmbColorSpace.SelectedItem.ToString();
                GenerateMap();
            };
            controlPanel.Controls.Add(cmbColorSpace);

            valueSlider = new TrackBar { Minimum = 0, Maximum = 255, Value = 128, Dock = DockStyle.Top, TickFrequency = 32 };
            valueSlider.ValueChanged += (s, e) =>
            {
                currentValue = valueSlider.Value;
                valueLabel.Text = GetValueLabel();
                GenerateMap();
            };
            controlPanel.Controls.Add(valueSlider);

            valueLabel = new Label { Text = GetValueLabel(), Dock = DockStyle.Top, Height = 20, TextAlign = ContentAlignment.MiddleCenter };
            controlPanel.Controls.Add(valueLabel);

            leftPanel.Controls.Add(controlPanel);

            // Axis label (class field)
            axisLabel = new Label();
            axisLabel.Dock = DockStyle.Bottom;
            axisLabel.Height = 20;
            axisLabel.TextAlign = ContentAlignment.MiddleCenter;
            axisLabel.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            leftPanel.Controls.Add(axisLabel);

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

        private string GetValueLabel()
        {
            switch (currentSpace)
            {
                case "RGB": return $"Blue (B): {currentValue}";
                case "HSV": return $"Value (V): {currentValue}";
                case "LAB": return $"L*: {currentValue * 100 / 255:F0}";
                case "YUV": return $"Y (Luma): {currentValue}";
                case "YCbCr": return $"Y (Luma): {currentValue}";
                case "CMYK": return $"Black (K): {currentValue * 100 / 255:F0}%";
                default: return "Value";
            }
        }

        private void GenerateMap()
        {
            int w = 400, h = 400;
            Bitmap bmp = new Bitmap(w, h);
            double val = currentValue / 255.0;
            string axisText = "";

            for (int y = 0; y < h; y++)
            {
                double v = 1.0 - (double)y / h; // vertical axis (0..1)
                for (int x = 0; x < w; x++)
                {
                    double u = (double)x / w; // horizontal axis (0..1)
                    Color col = Color.Black;
                    switch (currentSpace)
                    {
                        case "RGB":
                            col = Color.FromArgb((int)(u * 255), (int)(v * 255), currentValue);
                            axisText = "← Red →   |   ← Green →   |   Blue fixed (slider below)";
                            break;
                        case "HSV":
                            col = HsvToRgb(u * 360, v, val);
                            axisText = "← Hue (0-360°) →   |   ← Saturation →   |   Value fixed (slider below)";
                            break;
                        case "LAB":
                            double a = (u - 0.5) * 256;
                            double bb = (v - 0.5) * 256;
                            col = LabToRgb(val * 100, a, bb);
                            axisText = "← a* →   |   ← b* →   |   L* fixed (slider below)";
                            break;
                        case "YUV":
                            double U = (u - 0.5) * 256;
                            double V = (v - 0.5) * 256;
                            col = YuvToRgb(val * 255, U, V);
                            axisText = "← U →   |   ← V →   |   Y fixed (slider below)";
                            break;
                        case "YCbCr":
                            double Cb = u * 255;
                            double Cr = v * 255;
                            col = YCbCrToRgb(val * 255, Cb, Cr);
                            axisText = "← Cb →   |   ← Cr →   |   Y fixed (slider below)";
                            break;
                        case "CMYK":
                            double C = u;
                            double M = v;
                            col = CmykToRgb(C, M, 0, val); // Yellow = 0, Black = slider
                            axisText = "← Cyan →   |   ← Magenta →   |   Yellow=0, Black fixed (slider below)";
                            break;
                    }
                    bmp.SetPixel(x, y, col);
                }
            }
            colorMap.Image = bmp;
            axisLabel.Text = axisText;
        }

        // ---- Conversion utilities ----
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

        private Color LabToRgb(double L, double a, double b_)
        {
            // Simplified placeholder – replace with full conversion if needed
            return Color.FromArgb((int)(L * 2.55), 128, 128);
        }

        private Color YuvToRgb(double Y, double U, double V)
        {
            double r = Y + 1.13983 * V;
            double g = Y - 0.39465 * U - 0.58060 * V;
            double b = Y + 2.03211 * U;
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            return Color.FromArgb((int)r, (int)g, (int)b);
        }

        private Color YCbCrToRgb(double Y, double Cb, double Cr)
        {
            double r = Y + 1.402 * (Cr - 128);
            double g = Y - 0.344136 * (Cb - 128) - 0.714136 * (Cr - 128);
            double b = Y + 1.772 * (Cb - 128);
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            return Color.FromArgb((int)r, (int)g, (int)b);
        }

        private Color CmykToRgb(double C, double M, double Y, double K)
        {
            double r = (1 - C) * (1 - K);
            double g = (1 - M) * (1 - K);
            double b = (1 - Y) * (1 - K);
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        // ---- UpdateColorInfo (calls ColorConversions) ----
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