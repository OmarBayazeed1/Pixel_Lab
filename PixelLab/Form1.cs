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
            Mat rgbFloat = new Mat();
            bgrMat.ConvertTo(rgbFloat, DepthType.Cv32F, 1.0 / 255.0);
            CvInvoke.CvtColor(rgbFloat, rgbFloat, ColorConversion.Bgr2Rgb);

            Mat[] channels = rgbFloat.Split();
            Mat R = channels[0];
            Mat G = channels[1];
            Mat B = channels[2];

            Mat ones = new Mat(rgbFloat.Size, DepthType.Cv32F, 1);
            ones.SetTo(new MCvScalar(1.0));

            Mat maxRG = new Mat();
            Mat maxAll = new Mat();
            CvInvoke.Max(R, G, maxRG);
            CvInvoke.Max(maxRG, B, maxAll);
            Mat K = new Mat();
            CvInvoke.Subtract(ones, maxAll, K);

            Mat oneMinusK = new Mat();
            CvInvoke.Subtract(ones, K, oneMinusK);

            Mat tmp = new Mat();

            Mat C = new Mat();
            CvInvoke.Subtract(ones, R, tmp);
            CvInvoke.Subtract(tmp, K, tmp);
            CvInvoke.Divide(tmp, oneMinusK, C);

            Mat M = new Mat();
            CvInvoke.Subtract(ones, G, tmp);
            CvInvoke.Subtract(tmp, K, tmp);
            CvInvoke.Divide(tmp, oneMinusK, M);

            Mat Y = new Mat();
            CvInvoke.Subtract(ones, B, tmp);
            CvInvoke.Subtract(tmp, K, tmp);
            CvInvoke.Divide(tmp, oneMinusK, Y);

            Mat oneMinusC = new Mat();
            Mat oneMinusM = new Mat();
            Mat oneMinusY = new Mat();
            CvInvoke.Subtract(ones, C, oneMinusC);
            CvInvoke.Subtract(ones, M, oneMinusM);
            CvInvoke.Subtract(ones, Y, oneMinusY);

            Mat newR = new Mat();
            Mat newG = new Mat();
            Mat newB = new Mat();
            CvInvoke.Multiply(oneMinusC, oneMinusK, newR);
            CvInvoke.Multiply(oneMinusM, oneMinusK, newG);
            CvInvoke.Multiply(oneMinusY, oneMinusK, newB);

            using (VectorOfMat vec = new VectorOfMat(newR, newG, newB))
            {
                Mat result = new Mat();
                CvInvoke.Merge(vec, result);
                result.ConvertTo(result, DepthType.Cv8U, 255.0);
                CvInvoke.CvtColor(result, result, ColorConversion.Rgb2Bgr);
                return result;
            }
        }
    }
}