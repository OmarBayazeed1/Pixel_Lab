using System;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;

namespace PixelLab
{
    public partial class Form3D : Form
    {
        private GLControl glControl;
        private float rotationX = 30f, rotationY = 40f, zoom = 1.5f;
        private Point lastMousePos;
        private bool isDragging = false;
        private List<ColorPoint> points = new List<ColorPoint>();

        private Label[] rgbLabels, hsvLabels, labLabels, yuvLabels, ycbcrLabels, cmykLabels;
        private Panel colorPreviewPanel;

        public Form3D(Mat imageMat)
        {
            InitializeComponent();
            SetupGLControl();
            SetupColorPickerUI();
            BuildPointCloud(imageMat);
        }

        private void InitializeComponent()
        {
            this.Text = "3D RGB Cube – Rotate & Zoom (click for color picker)";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SetupGLControl()
        {
            glControl = new GLControl();
            glControl.Dock = DockStyle.Fill;
            glControl.Load += (s, e) =>
            {
                GL.Enable(EnableCap.DepthTest);
                GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
                GL.PointSize(2.5f);
            };
            glControl.Paint += (s, e) =>
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadIdentity();
                GL.Translate(0, 0, -zoom);
                GL.Rotate(rotationX, 1.0f, 0.0f, 0.0f);
                GL.Rotate(rotationY, 0.0f, 1.0f, 0.0f);
                DrawCubeWireframe();
                GL.Begin(PrimitiveType.Points);
                foreach (var p in points)
                {
                    GL.Color3(p.R, p.G, p.B);
                    GL.Vertex3(p.X, p.Y, p.Z);
                }
                GL.End();
                glControl.SwapBuffers();
            };
            glControl.MouseDown += (s, e) => { isDragging = true; lastMousePos = e.Location; };
            glControl.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    rotationY += (e.X - lastMousePos.X) * 0.5f;
                    rotationX += (e.Y - lastMousePos.Y) * 0.5f;
                    lastMousePos = e.Location;
                    glControl.Invalidate();
                }
            };
            glControl.MouseUp += (s, e) => isDragging = false;
            glControl.MouseWheel += (s, e) =>
            {
                zoom -= e.Delta * 0.001f;
                zoom = Math.Max(0.5f, Math.Min(3.0f, zoom));
                glControl.Invalidate();
            };
            glControl.MouseClick += (s, e) =>
            {
                ColorDialog cd = new ColorDialog();
                if (cd.ShowDialog() == DialogResult.OK)
                    UpdateColorInfo(cd.Color);
            };
            this.Controls.Add(glControl);
        }

        private void DrawCubeWireframe()
        {
            GL.LineWidth(1.5f);
            GL.Color3(1.0f, 1.0f, 1.0f);
            GL.Begin(PrimitiveType.Lines);
            float s = 0.5f;
            GL.Vertex3(-s, -s, -s); GL.Vertex3(s, -s, -s);
            GL.Vertex3(s, -s, -s); GL.Vertex3(s, -s, s);
            GL.Vertex3(s, -s, s); GL.Vertex3(-s, -s, s);
            GL.Vertex3(-s, -s, s); GL.Vertex3(-s, -s, -s);
            GL.Vertex3(-s, s, -s); GL.Vertex3(s, s, -s);
            GL.Vertex3(s, s, -s); GL.Vertex3(s, s, s);
            GL.Vertex3(s, s, s); GL.Vertex3(-s, s, s);
            GL.Vertex3(-s, s, s); GL.Vertex3(-s, s, -s);
            GL.Vertex3(-s, -s, -s); GL.Vertex3(-s, s, -s);
            GL.Vertex3(s, -s, -s); GL.Vertex3(s, s, -s);
            GL.Vertex3(s, -s, s); GL.Vertex3(s, s, s);
            GL.Vertex3(-s, -s, s); GL.Vertex3(-s, s, s);
            GL.End();
        }

        private void BuildPointCloud(Mat image)
        {
            points.Clear();
            if (image == null || image.IsEmpty) return;
            int w = image.Width, h = image.Height;
            int maxPoints = 15000;
            int step = Math.Max(1, (w * h) / maxPoints);
            Random rand = new Random();
            using (Image<Bgr, byte> img = image.ToImage<Bgr, byte>())
            {
                for (int y = 0; y < h; y += step)
                    for (int x = 0; x < w; x += step)
                    {
                        int rx = x + rand.Next(step);
                        int ry = y + rand.Next(step);
                        if (rx >= w) rx = w - 1;
                        if (ry >= h) ry = h - 1;
                        Bgr c = img[ry, rx];
                        float r = (float)c.Red / 255f;
                        float g = (float)c.Green / 255f;
                        float b = (float)c.Blue / 255f;
                        points.Add(new ColorPoint(r - 0.5f, g - 0.5f, b - 0.5f, r, g, b));
                        if (points.Count >= maxPoints) break;
                    }
            }
        }

        private void SetupColorPickerUI()
        {
            Panel rightPanel = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(10) };
            colorPreviewPanel = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = Color.Black };
            rightPanel.Controls.Add(colorPreviewPanel);
            Button infoBtn = new Button { Text = "Click any point → Color Picker", Dock = DockStyle.Top, Height = 35, Enabled = false };
            rightPanel.Controls.Add(infoBtn);
            rgbLabels = AddGroup(rightPanel, "RGB", 3);
            hsvLabels = AddGroup(rightPanel, "HSV", 3);
            labLabels = AddGroup(rightPanel, "LAB", 3);
            yuvLabels = AddGroup(rightPanel, "YUV", 3);
            ycbcrLabels = AddGroup(rightPanel, "YCbCr", 3);
            cmykLabels = AddGroup(rightPanel, "CMYK", 4);
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

        private void UpdateColorInfo(Color color)
        {
            colorPreviewPanel.BackColor = color;
            double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
            rgbLabels[0].Text = $"R: {color.R}";
            rgbLabels[1].Text = $"G: {color.G}";
            rgbLabels[2].Text = $"B: {color.B}";
            RGBToHSV(r, g, b, out double h, out double s, out double v);
            hsvLabels[0].Text = $"H: {h:F1}°";
            hsvLabels[1].Text = $"S: {s * 100:F1}%";
            hsvLabels[2].Text = $"V: {v * 100:F1}%";
            RGBToLAB(r, g, b, out double L, out double a, out double bb);
            labLabels[0].Text = $"L*: {L:F1}";
            labLabels[1].Text = $"a*: {a:F1}";
            labLabels[2].Text = $"b*: {bb:F1}";
            RGBToYUV(r, g, b, out double y, out double u, out double vv);
            yuvLabels[0].Text = $"Y: {y:F1}";
            yuvLabels[1].Text = $"U: {u:F1}";
            yuvLabels[2].Text = $"V: {vv:F1}";
            RGBToYCbCr(r, g, b, out double yc, out double cb, out double cr);
            ycbcrLabels[0].Text = $"Y: {yc:F1}";
            ycbcrLabels[1].Text = $"Cb: {cb:F1}";
            ycbcrLabels[2].Text = $"Cr: {cr:F1}";
            RGBToCMYK(r, g, b, out double c, out double m, out double yk, out double k);
            cmykLabels[0].Text = $"C: {c * 100:F1}%";
            cmykLabels[1].Text = $"M: {m * 100:F1}%";
            cmykLabels[2].Text = $"Y: {yk * 100:F1}%";
            cmykLabels[3].Text = $"K: {k * 100:F1}%";
        }

        // ---- Color space conversions ----
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

        private class ColorPoint
        {
            public float X, Y, Z, R, G, B;
            public ColorPoint(float x, float y, float z, float r, float g, float b)
            {
                X = x; Y = y; Z = z; R = r; G = g; B = b;
            }
        }
        
    }
}