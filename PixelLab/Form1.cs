using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace PixelLab
{
    public partial class Form1 : Form
    {
        private Mat originalMat;
        private Bitmap currentBitmap;
        private PictureBox pictureBox1;
        private Label infoLabel;
        private string currentFilePath;

        public Form1()
        {
            InitializeForm();
            SetupDragDrop();
            CreateTopPanel(); // Now includes Save button
        }

        private void InitializeForm()
        {
            // PictureBox
            pictureBox1 = new PictureBox();
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.LightGray;
            this.Controls.Add(pictureBox1);

            // Info panel at bottom
            Panel infoPanel = new Panel();
            infoPanel.Dock = DockStyle.Bottom;
            infoPanel.Height = 60;
            infoPanel.BackColor = Color.WhiteSmoke;
            infoPanel.Padding = new Padding(10);

            infoLabel = new Label();
            infoLabel.Dock = DockStyle.Fill;
            infoLabel.Text = "No image loaded";
            infoLabel.Font = new Font("Segoe UI", 9F);
            infoLabel.ForeColor = Color.DarkGray;
            infoPanel.Controls.Add(infoLabel);
            this.Controls.Add(infoPanel);

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
                currentFilePath = filePath;
                currentBitmap = new Bitmap(filePath);
                originalMat = currentBitmap.ToMat();
                pictureBox1.Image = currentBitmap;
                this.Text = $"PixelLab - {Path.GetFileName(filePath)}";
                UpdateImageInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image:\n{ex.Message}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateImageInfo()
        {
            if (currentFilePath == null) return;

            FileInfo fi = new FileInfo(currentFilePath);
            long fileSizeBytes = fi.Length;
            string fileSizeStr = fileSizeBytes >= 1048576
                ? $"{fileSizeBytes / 1048576.0:F2} MB"
                : $"{fileSizeBytes / 1024.0:F2} KB";

            string info = $"📄 {Path.GetFileName(currentFilePath)}  |  " +
                          $"📏 {currentBitmap.Width} × {currentBitmap.Height} px  |  " +
                          $"💾 {fileSizeStr}  |  " +
                          $"🔖 {Path.GetExtension(currentFilePath).ToUpper()}";
            infoLabel.Text = info;
            infoLabel.ForeColor = Color.Black;
        }

        private void CreateTopPanel()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.Height = 50;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.BackColor = Color.WhiteSmoke;
            panel.AutoSize = false;
            panel.Padding = new Padding(5);

            // Color space buttons
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

            // Save button (separator)
            Label sep = new Label();
            sep.Text = "     ";
            panel.Controls.Add(sep);

            Button saveBtn = new Button();
            saveBtn.Text = "💾 Save Image";
            saveBtn.Width = 100;
            saveBtn.Height = 40;
            saveBtn.Margin = new Padding(5);
            saveBtn.BackColor = Color.LightGreen;
            saveBtn.Click += SaveImage;
            panel.Controls.Add(saveBtn);

            this.Controls.Add(panel);
        }

        private void SaveImage(object sender, EventArgs e)
        {
            if (currentBitmap == null)
            {
                MessageBox.Show("No image to save. Please load an image first.", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|BMP Image|*.bmp|GIF Image|*.gif";
                sfd.Title = "Save Image As";
                sfd.FileName = currentFilePath != null ? Path.GetFileNameWithoutExtension(currentFilePath) : "untitled";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ImageFormat format = ImageFormat.Png;
                        string ext = Path.GetExtension(sfd.FileName).ToLower();
                        switch (ext)
                        {
                            case ".jpg":
                            case ".jpeg":
                                format = ImageFormat.Jpeg;
                                break;
                            case ".bmp":
                                format = ImageFormat.Bmp;
                                break;
                            case ".gif":
                                format = ImageFormat.Gif;
                                break;
                            default:
                                format = ImageFormat.Png;
                                break;
                        }

                        currentBitmap.Save(sfd.FileName, format);
                        MessageBox.Show($"Image saved successfully to:\n{sfd.FileName}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Save failed: {ex.Message}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
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
                this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)} [{targetSpace}]";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Conversion error: {ex.Message}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // CMYK simulation (duller print look)
        private Mat ConvertRgbToCmyk(Mat bgrMat)
        {
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

                        double k = 1 - Math.Max(Math.Max(r, g), b);
                        if (k == 1.0)
                        {
                            resultImg[y, x] = new Bgr(0, 0, 0);
                            continue;
                        }
                        double c = (1 - r - k) / (1 - k);
                        double m = (1 - g - k) / (1 - k);
                        double y_ = (1 - b - k) / (1 - k);

                        double rOut = (1 - c) * (1 - k);
                        double gOut = (1 - m) * (1 - k);
                        double bOut = (1 - y_) * (1 - k);

                        double darken = 1 - (k * 0.3);
                        rOut *= darken;
                        gOut *= darken;
                        bOut *= darken;

                        double gray = (rOut + gOut + bOut) / 3.0;
                        double desaturate = 0.65;
                        rOut = rOut * desaturate + gray * (1 - desaturate);
                        gOut = gOut * desaturate + gray * (1 - desaturate);
                        bOut = bOut * desaturate + gray * (1 - desaturate);

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