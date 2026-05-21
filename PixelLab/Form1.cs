using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace PixelLab
{
    public partial class Form1 : Form
    {
        private Mat originalMat;
        private Bitmap currentBitmap;
        private PictureBox pictureBox1;

        public Form1()
        {
            // Create the PictureBox and set up the form
            InitializeForm();
            SetupDragDrop();
            CreateColorSpaceButtons();
        }

        private void InitializeForm()
        {
            // Create PictureBox
            pictureBox1 = new PictureBox();
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.LightGray;
            this.Controls.Add(pictureBox1);

            // Form properties
            this.Text = "PixelLab";
            this.WindowState = FormWindowState.Maximized;
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
            pictureBox1.AllowDrop = true;
            pictureBox1.DragEnter += Form1_DragEnter;
            pictureBox1.DragDrop += Form1_DragDrop;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
                LoadImage(files[0]);
        }

        private void LoadImage(string filePath)
        {
            try
            {
                currentBitmap = new Bitmap(filePath);
                originalMat = currentBitmap.ToMat();
                pictureBox1.Image = currentBitmap;
                this.Text = $"PixelLab - {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image:\n{ex.Message}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateColorSpaceButtons()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.Height = 50;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.BackColor = Color.WhiteSmoke;
            panel.AutoSize = false;

            string[] colorSpaces = { "RGB", "HSV", "YUV", "LAB", "YCbCr", "CMYK" };
            foreach (string cs in colorSpaces)
            {
                Button btn = new Button();
                btn.Text = cs;
                btn.Width = 80;
                btn.Height = 40;
                btn.Margin = new Padding(5);
                btn.Click += (sender, e) => ConvertToColorSpace(cs);
                panel.Controls.Add(btn);
            }

            this.Controls.Add(panel);
        }

        private void ConvertToColorSpace(string targetSpace)
        {
            if (originalMat == null)
            {
                MessageBox.Show("Please load an image first.", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Mat resultMat = new Mat();
            try
            {
                switch (targetSpace)
                {
                    case "HSV":
                        CvInvoke.CvtColor(originalMat, resultMat, ColorConversion.Bgr2Hsv);
                        break;
                    case "YUV":
                        CvInvoke.CvtColor(originalMat, resultMat, ColorConversion.Bgr2Yuv);
                        break;
                    case "LAB":
                        CvInvoke.CvtColor(originalMat, resultMat, ColorConversion.Bgr2Lab);
                        break;
                    case "YCbCr":
                        CvInvoke.CvtColor(originalMat, resultMat, ColorConversion.Bgr2YCrCb);
                        break;
                    case "RGB":
                        resultMat = originalMat.Clone();
                        break;
                    case "CMYK":
                        resultMat = ConvertRgbToCmyk(originalMat);
                        break;
                }

                currentBitmap = resultMat.ToBitmap();
                pictureBox1.Image = currentBitmap;
                this.Text = $"PixelLab - Converted to {targetSpace}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Conversion error: {ex.Message}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Mat ConvertRgbToCmyk(Mat bgrMat)
        {
            // Convert Mat to Image<Bgr, byte> for pixel access
            using (Image<Bgr, byte> img = bgrMat.ToImage<Bgr, byte>())
            {
                int width = img.Width;
                int height = img.Height;
                Image<Bgr, byte> resultImg = new Image<Bgr, byte>(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Bgr color = img[y, x];
                        double r = color.Red / 255.0;
                        double g = color.Green / 255.0;
                        double b = color.Blue / 255.0;

                        // Convert RGB to CMYK (standard formula)
                        double k = 1 - Math.Max(Math.Max(r, g), b);
                        if (k == 1.0)
                        {
                            resultImg[y, x] = new Bgr(0, 0, 0);
                            continue;
                        }
                        double c = (1 - r - k) / (1 - k);
                        double m = (1 - g - k) / (1 - k);
                        double y_ = (1 - b - k) / (1 - k);

                        // Simulate printed colors: convert CMYK back to RGB
                        // but apply a desaturation and darkening effect.
                        double rOut = (1 - c) * (1 - k);
                        double gOut = (1 - m) * (1 - k);
                        double bOut = (1 - y_) * (1 - k);

                        // ----- Simulation of dull printed look -----
                        // 1. Darken by K (more black = darker)
                        double darken = 1 - (k * 0.3);
                        rOut *= darken;
                        gOut *= darken;
                        bOut *= darken;

                        // 2. Desaturate by moving toward gray
                        double gray = (rOut + gOut + bOut) / 3.0;
                        double desaturate = 0.65; // 65% saturation, 35% gray
                        rOut = rOut * desaturate + gray * (1 - desaturate);
                        gOut = gOut * desaturate + gray * (1 - desaturate);
                        bOut = bOut * desaturate + gray * (1 - desaturate);

                        // 3. Optional: slight cyan cast (common in prints)
                        // bOut = bOut * 0.95;   // uncomment if needed

                        // Clamp and convert to 0-255
                        byte rr = (byte)(Math.Max(0, Math.Min(1, rOut)) * 255);
                        byte gg = (byte)(Math.Max(0, Math.Min(1, gOut)) * 255);
                        byte bb = (byte)(Math.Max(0, Math.Min(1, bOut)) * 255);

                        resultImg[y, x] = new Bgr(bb, gg, rr);
                    }
                }
                return resultImg.Mat;
            }
        }
    }
}