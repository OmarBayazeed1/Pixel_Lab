using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace PixelLab
{
    public partial class ColorSpaceViewer : Form
    {
        private GLControl glControl;
        private List<ColorPoint> points = new List<ColorPoint>();
        private Panel colorPreviewPanel;
        private Label[] rgbLabels, hsvLabels, labLabels, yuvLabels, ycbcrLabels, cmykLabels;

        // Orbit camera
        private float rotationX = 30f;
        private float rotationY = 40f;
        private float zoom = 1.5f;
        private Point lastMousePos;
        private bool isDragging = false;

        public delegate IEnumerable<ColorPoint> PointGenerator();

        public ColorSpaceViewer(string title, PointGenerator generator)
        {
            Text = title;
            Size = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterScreen;
            SetupGLControl();
            SetupColorPickerUI();
            points.AddRange(generator());
        }

        private void SetupGLControl()
        {
            glControl = new GLControl { Dock = DockStyle.Fill };
            glControl.Load += (s, e) =>
            {
                GL.Enable(EnableCap.DepthTest);
                GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
                GL.PointSize(2.5f);
            };
            glControl.Resize += (s, e) =>
            {
                glControl.MakeCurrent();
                int w = glControl.Width, h = glControl.Height;
                GL.Viewport(0, 0, w, h);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)w / h, 0.01f, 100f);
                GL.LoadMatrix(ref perspective);
                GL.MatrixMode(MatrixMode.Modelview);
            };
            glControl.Paint += (s, e) =>
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadIdentity();
                GL.Translate(0, 0, -zoom);
                GL.Rotate(rotationX, 1, 0, 0);
                GL.Rotate(rotationY, 0, 1, 0);
                DrawAxes();
                GL.Begin(PrimitiveType.Points);
                foreach (var p in points)
                {
                    GL.Color3(p.R, p.G, p.B);
                    GL.Vertex3(p.X, p.Y, p.Z);
                }
                GL.End();
                glControl.SwapBuffers();
            };
            // Mouse drag to rotate
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
            // Mouse wheel to zoom
            glControl.MouseWheel += (s, e) =>
            {
                zoom -= e.Delta * 0.002f;
                zoom = Math.Max(0.3f, Math.Min(3.5f, zoom));
                glControl.Invalidate();
            };
            // Keyboard: M/N to rotate left/right, Space to pick color
            glControl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.M)
                {
                    rotationY += 5f;
                    glControl.Invalidate();
                }
                else if (e.KeyCode == Keys.N)
                {
                    rotationY -= 5f;
                    glControl.Invalidate();
                }
                else if (e.KeyCode == Keys.Space)
                {
                    ColorDialog cd = new ColorDialog();
                    if (cd.ShowDialog() == DialogResult.OK)
                        UpdateColorInfo(cd.Color);
                }
            };
            glControl.Focus();
            this.Controls.Add(glControl);
        }

        private void DrawAxes()
        {
            GL.LineWidth(1f);
            GL.Color3(0.5f, 0.5f, 0.5f);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(-0.6f, 0, 0); GL.Vertex3(0.6f, 0, 0);
            GL.Vertex3(0, -0.6f, 0); GL.Vertex3(0, 0.6f, 0);
            GL.Vertex3(0, 0, -0.6f); GL.Vertex3(0, 0, 0.6f);
            GL.End();
        }

        private void SetupColorPickerUI()
        {
            Panel rightPanel = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(10) };
            colorPreviewPanel = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = Color.Black };
            rightPanel.Controls.Add(colorPreviewPanel);
            Button info = new Button { Text = "Drag: rotate | Scroll: zoom | M/N: rotate | Space: pick color", Dock = DockStyle.Top, Height = 35, Enabled = false };
            rightPanel.Controls.Add(info);
            rgbLabels = AddGroup(rightPanel, "RGB", 3);
            hsvLabels = AddGroup(rightPanel, "HSV", 3);
            labLabels = AddGroup(rightPanel, "LAB", 3);
            yuvLabels = AddGroup(rightPanel, "YUV", 3);
            ycbcrLabels = AddGroup(rightPanel, "YCbCr", 3);
            cmykLabels = AddGroup(rightPanel, "CMYK", 4);
            Controls.Add(rightPanel);
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

        public class ColorPoint
        {
            public float X, Y, Z, R, G, B;
            public ColorPoint(float x, float y, float z, float r, float g, float b)
            {
                X = x; Y = y; Z = z; R = r; G = g; B = b;
            }
        }
    }
}