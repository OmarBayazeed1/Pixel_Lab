using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Runtime.InteropServices;
namespace PixelLab
{
    public partial class Form1 : Form
    {
        private Mat originalMat;
        private Mat currentMat;
        private string currentColorSpace;
        private PictureBox pictureBox1;
        private Label infoLabel;
        private string currentFilePath;

        private float[][] originalChannels;
        private int width, height;
        private int numChannels;

        private Panel channelPanel;
        private TrackBar[] trackBars;
        private CheckBox[] channelEnableCheckboxes;
        private Label[] channelValueLabels;
        private string[] channelNames;
        private int[] channelMaxValues;
        private Button resetChannelsBtn;

        public Form1()
        {
            InitializeForm();
            SetupDragDrop();
            CreateTopPanel();
            CreateChannelPanel();
        }

        private void InitializeForm()
        {
            pictureBox1 = new PictureBox();
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.LightGray;
            this.Controls.Add(pictureBox1);

            Panel infoPanel = new Panel();
            infoPanel.Dock = DockStyle.Bottom;
            infoPanel.Height = 60;
            infoPanel.BackColor = Color.WhiteSmoke;
            infoLabel = new Label();
            infoLabel.Dock = DockStyle.Fill;
            infoLabel.Text = "No image loaded";
            infoLabel.Font = new Font("Segoe UI", 9F);
            infoPanel.Controls.Add(infoLabel);
            this.Controls.Add(infoPanel);

            this.Text = "PixelLab";
            this.WindowState = FormWindowState.Maximized;
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            this.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) LoadImage(files[0]);
            };
            pictureBox1.AllowDrop = true;
            pictureBox1.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            pictureBox1.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) LoadImage(files[0]);
            };
        }

        private void LoadImage(string filePath)
        {
            try
            {
                currentFilePath = filePath;
                Bitmap bmp = new Bitmap(filePath);
                originalMat = bmp.ToMat();
                currentMat = originalMat.Clone();
                pictureBox1.Image = currentMat.ToBitmap();
                this.Text = $"PixelLab - {Path.GetFileName(filePath)}";
                UpdateImageInfo();
                currentColorSpace = "RGB";
                channelPanel.Visible = false;
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
            long size = fi.Length;
            string sizeStr = size >= 1048576 ? $"{size / 1048576.0:F2} MB" : $"{size / 1024.0:F2} KB";
            string info = $"📄 {Path.GetFileName(currentFilePath)}  |  📏 {currentMat.Width}×{currentMat.Height} px  |  💾 {sizeStr}  |  🔖 {Path.GetExtension(currentFilePath).ToUpper()}";
            infoLabel.Text = info;
        }

        private void CreateTopPanel()
        {
            FlowLayoutPanel topPanel = new FlowLayoutPanel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 50;
            topPanel.BackColor = Color.WhiteSmoke;
            topPanel.Padding = new Padding(5);
            topPanel.FlowDirection = FlowDirection.LeftToRight;

            string[] spaces = { "RGB", "HSV", "LAB", "YUV", "YCbCr", "CMYK" };
            foreach (string cs in spaces)
            {
                Button btn = new Button();
                btn.Text = cs;
                btn.Width = 80;
                btn.Height = 40;
                btn.Margin = new Padding(5);
                btn.Click += (s, e) => ConvertToColorSpace(cs);
                topPanel.Controls.Add(btn);
            }
            Button openBtn = new Button();
            openBtn.Text = "📂 Open";
            openBtn.Width = 80;
            openBtn.Height = 40;
            openBtn.BackColor = Color.LightGray;
            openBtn.Click += (s, e) => OpenImage();
            topPanel.Controls.Add(openBtn);

            Button saveBtn = new Button();
            saveBtn.Text = "💾 Save";
            saveBtn.Width = 80;
            saveBtn.Height = 40;
            saveBtn.BackColor = Color.LightGreen;
            saveBtn.Click += (s, e) => SaveImage();
            topPanel.Controls.Add(saveBtn);

            Button resetBtn = new Button();
            resetBtn.Text = "⟳ Reset";
            resetBtn.Width = 80;
            resetBtn.Height = 40;
            resetBtn.BackColor = Color.LightCoral;
            resetBtn.Click += (s, e) => ResetToOriginal();
            topPanel.Controls.Add(resetBtn);

            Button quantizeBtn = new Button();
            quantizeBtn.Text = "🎨 Quantize";
            quantizeBtn.Width = 80;
            quantizeBtn.Height = 40;
            quantizeBtn.BackColor = Color.LightBlue;
            quantizeBtn.Click += (s, e) => QuantizeImage();
            topPanel.Controls.Add(quantizeBtn);

            this.Controls.Add(topPanel);
        }

        private void CreateChannelPanel()
        {
            channelPanel = new Panel();
            channelPanel.Dock = DockStyle.Right;
            channelPanel.Width = 320;
            channelPanel.BackColor = Color.FromArgb(240, 240, 240);
            channelPanel.AutoScroll = true;
            channelPanel.Padding = new Padding(10);
            channelPanel.Visible = false;
            this.Controls.Add(channelPanel);
        }

        private void ConvertToColorSpace(string targetSpace)
        {
            if (originalMat == null)
            {
                MessageBox.Show("Load an image first.", "PixelLab");
                return;
            }

            if (targetSpace == "CMYK")
            {
                ConvertToCmyk(currentMat);
                return;
            }

            Mat convertedMat = new Mat();
            try
            {
                switch (targetSpace)
                {
                    case "RGB":
                        convertedMat = currentMat.Clone();
                        break;
                    case "HSV":
                        CvInvoke.CvtColor(currentMat, convertedMat, ColorConversion.Bgr2Hsv);
                        break;
                    case "LAB":
                        CvInvoke.CvtColor(currentMat, convertedMat, ColorConversion.Bgr2Lab);
                        break;
                    case "YUV":
                        CvInvoke.CvtColor(currentMat, convertedMat, ColorConversion.Bgr2Yuv);
                        break;
                    case "YCbCr":
                        CvInvoke.CvtColor(currentMat, convertedMat, ColorConversion.Bgr2YCrCb);
                        break;
                    default:
                        return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Conversion error: {ex.Message}", "PixelLab");
                return;
            }

            width = convertedMat.Width;
            height = convertedMat.Height;
            currentMat = convertedMat;
            pictureBox1.Image = currentMat.ToBitmap();
            this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)} [{targetSpace}]";
            currentColorSpace = targetSpace;

            ExtractOriginalChannels(convertedMat, targetSpace);
            BuildChannelUI(targetSpace);
            channelPanel.Visible = true;
        }

        private void ExtractOriginalChannels(Mat mat, string space)
        {
            numChannels = 3;
            originalChannels = new float[3][];
            for (int i = 0; i < 3; i++)
                originalChannels[i] = new float[width * height];

            Mat[] mats = mat.Split();
            if (space == "RGB")
            {
                // mats order: [Blue, Green, Red]
                using (Image<Gray, byte> bImg = mats[0].ToImage<Gray, byte>())
                using (Image<Gray, byte> gImg = mats[1].ToImage<Gray, byte>())
                using (Image<Gray, byte> rImg = mats[2].ToImage<Gray, byte>())
                {
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            originalChannels[0][idx] = (float)rImg[y, x].Intensity; // Red
                            originalChannels[1][idx] = (float)gImg[y, x].Intensity; // Green
                            originalChannels[2][idx] = (float)bImg[y, x].Intensity; // Blue
                        }
                }
            }
            else
            {
                // For HSV, LAB, YUV, YCbCr: keep natural order (H,S,V; L,A,B; Y,U,V; Y,Cr,Cb)
                for (int c = 0; c < 3; c++)
                {
                    using (Image<Gray, byte> img = mats[c].ToImage<Gray, byte>())
                    {
                        for (int y = 0; y < height; y++)
                            for (int x = 0; x < width; x++)
                                originalChannels[c][y * width + x] = (float)img[y, x].Intensity;
                    }
                }
            }
        }

        private void BuildChannelUI(string space)
        {
            channelPanel.Controls.Clear();

            if (space == "HSV")
            {
                channelNames = new[] { "Hue Shift (°)", "Saturation (%)", "Value (%)" };
                trackBars = new TrackBar[3];
                channelEnableCheckboxes = new CheckBox[3];
                channelValueLabels = new Label[3];

                for (int i = 0; i < 3; i++)
                {
                    GroupBox gb = new GroupBox();
                    gb.Text = channelNames[i];
                    gb.Dock = DockStyle.Top;
                    gb.Height = 130;                    // enough space
                    gb.Margin = new Padding(0, 0, 0, 8);
                    gb.Padding = new Padding(6);

                    CheckBox chk = new CheckBox();
                    chk.Text = "Enable";
                    chk.Checked = true;
                    chk.Dock = DockStyle.Top;
                    chk.Height = 25;                    // fixed height
                    chk.Margin = new Padding(3);
                    chk.CheckedChanged += (s, e) => ApplyAdjustment();
                    channelEnableCheckboxes[i] = chk;
                    gb.Controls.Add(chk);

                    TrackBar tb = new TrackBar();
                    tb.Minimum = 0;
                    tb.Maximum = (i == 0) ? 360 : 200;
                    tb.Value = (i == 0) ? 0 : 100;
                    tb.Dock = DockStyle.Top;
                    tb.Height = 45;                     // fixed height
                    tb.Scroll += (s, e) => ApplyAdjustment();
                    trackBars[i] = tb;
                    gb.Controls.Add(tb);

                    Label lbl = new Label();
                    lbl.Text = tb.Value.ToString();
                    lbl.Dock = DockStyle.Top;
                    lbl.Height = 25;                    // fixed height
                    lbl.TextAlign = ContentAlignment.MiddleCenter;
                    channelValueLabels[i] = lbl;
                    tb.ValueChanged += (s, e) => lbl.Text = tb.Value.ToString();
                    gb.Controls.Add(lbl);

                    channelPanel.Controls.Add(gb);
                }
            }
            else if (space == "RGB")
            {
                channelNames = new[] { "Red (%)", "Green (%)", "Blue (%)" };
                trackBars = new TrackBar[3];
                channelEnableCheckboxes = new CheckBox[3];
                channelValueLabels = new Label[3];

                for (int i = 0; i < 3; i++)
                {
                    GroupBox gb = new GroupBox();
                    gb.Text = channelNames[i];
                    gb.Dock = DockStyle.Top;
                    gb.Height = 130;
                    gb.Margin = new Padding(0, 0, 0, 8);
                    gb.Padding = new Padding(6);

                    CheckBox chk = new CheckBox();
                    chk.Text = "Enable";
                    chk.Checked = true;
                    chk.Dock = DockStyle.Top;
                    chk.Height = 25;
                    chk.Margin = new Padding(3);
                    chk.CheckedChanged += (s, e) => ApplyAdjustment();
                    channelEnableCheckboxes[i] = chk;
                    gb.Controls.Add(chk);

                    TrackBar tb = new TrackBar();
                    tb.Minimum = 0;
                    tb.Maximum = 200;
                    tb.Value = 100;
                    tb.Dock = DockStyle.Top;
                    tb.Height = 45;
                    tb.Scroll += (s, e) => ApplyAdjustment();
                    trackBars[i] = tb;
                    gb.Controls.Add(tb);

                    Label lbl = new Label();
                    lbl.Text = tb.Value.ToString();
                    lbl.Dock = DockStyle.Top;
                    lbl.Height = 25;
                    lbl.TextAlign = ContentAlignment.MiddleCenter;
                    channelValueLabels[i] = lbl;
                    tb.ValueChanged += (s, e) => lbl.Text = tb.Value.ToString();
                    gb.Controls.Add(lbl);

                    channelPanel.Controls.Add(gb);
                }
            }
            else // LAB, YUV, YCbCr
            {
                switch (space)
                {
                    case "LAB": channelNames = new[] { "L* (%)", "a* (%)", "b* (%)" }; break;
                    case "YUV": channelNames = new[] { "Y (%)", "U (%)", "V (%)" }; break;
                    default: channelNames = new[] { "Y (%)", "Cb (%)", "Cr (%)" }; break;
                }
                trackBars = new TrackBar[3];
                channelEnableCheckboxes = new CheckBox[3];
                channelValueLabels = new Label[3];

                for (int i = 0; i < 3; i++)
                {
                    GroupBox gb = new GroupBox();
                    gb.Text = channelNames[i];
                    gb.Dock = DockStyle.Top;
                    gb.Height = 130;
                    gb.Margin = new Padding(0, 0, 0, 8);
                    gb.Padding = new Padding(6);

                    CheckBox chk = new CheckBox();
                    chk.Text = "Enable";
                    chk.Checked = true;
                    chk.Dock = DockStyle.Top;
                    chk.Height = 25;
                    chk.Margin = new Padding(3);
                    chk.CheckedChanged += (s, e) => ApplyAdjustment();
                    channelEnableCheckboxes[i] = chk;
                    gb.Controls.Add(chk);

                    TrackBar tb = new TrackBar();
                    tb.Minimum = 0;
                    tb.Maximum = 200;
                    tb.Value = 100;
                    tb.Dock = DockStyle.Top;
                    tb.Height = 45;
                    tb.Scroll += (s, e) => ApplyAdjustment();
                    trackBars[i] = tb;
                    gb.Controls.Add(tb);

                    Label lbl = new Label();
                    lbl.Text = tb.Value.ToString();
                    lbl.Dock = DockStyle.Top;
                    lbl.Height = 25;
                    lbl.TextAlign = ContentAlignment.MiddleCenter;
                    channelValueLabels[i] = lbl;
                    tb.ValueChanged += (s, e) => lbl.Text = tb.Value.ToString();
                    gb.Controls.Add(lbl);

                    channelPanel.Controls.Add(gb);
                }
            }

            resetChannelsBtn = new Button();
            resetChannelsBtn.Text = "Reset Adjustments";
            resetChannelsBtn.Dock = DockStyle.Top;
            resetChannelsBtn.Height = 40;
            resetChannelsBtn.Margin = new Padding(0, 10, 0, 0);
            resetChannelsBtn.Click += (s, e) => ResetSlidersToNeutral();
            channelPanel.Controls.Add(resetChannelsBtn);
        }
        private void ResetSlidersToNeutral()
        {
            if (currentColorSpace == "HSV")
            {
                trackBars[0].Value = 0;
                trackBars[1].Value = 100;
                trackBars[2].Value = 100;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    trackBars[i].Value = 100;
            }
            for (int i = 0; i < channelEnableCheckboxes.Length; i++)
                channelEnableCheckboxes[i].Checked = true;
            ApplyAdjustment();
        }

        private void ApplyAdjustment()
        {
            if (originalChannels == null) return;

            float[][] adjusted = new float[numChannels][];
            for (int c = 0; c < numChannels; c++)
                adjusted[c] = new float[width * height];

            if (currentColorSpace == "HSV")
            {
                float hueShift = trackBars[0].Value;
                float satScale = trackBars[1].Value / 100f;
                float valScale = trackBars[2].Value / 100f;
                bool hueEnabled = channelEnableCheckboxes[0].Checked;
                bool satEnabled = channelEnableCheckboxes[1].Checked;
                bool valEnabled = channelEnableCheckboxes[2].Checked;

                for (int p = 0; p < width * height; p++)
                {
                    float h = hueEnabled ? originalChannels[0][p] + hueShift : 0;
                    if (hueEnabled) { while (h >= 179) h -= 179; while (h < 0) h += 179; }
                    float s = satEnabled ? originalChannels[1][p] * satScale : 0;
                    float v = valEnabled ? originalChannels[2][p] * valScale : 0;
                    s = Math.Max(0, Math.Min(255, s));
                    v = Math.Max(0, Math.Min(255, v));
                    adjusted[0][p] = h;
                    adjusted[1][p] = s;
                    adjusted[2][p] = v;
                }
            }
            else if (currentColorSpace == "RGB")
            {
                // UI sliders: [0]=Red, [1]=Green, [2]=Blue
                // originalChannels stored as [Red, Green, Blue]
                for (int c = 0; c < 3; c++)
                {
                    float scale = trackBars[c].Value / 100f;
                    bool enabled = channelEnableCheckboxes[c].Checked;
                    for (int p = 0; p < width * height; p++)
                    {
                        float val = enabled ? originalChannels[c][p] * scale : 0;
                        val = Math.Max(0, Math.Min(255, val));
                        adjusted[c][p] = val;
                    }
                }
            }
            else // LAB, YUV, YCbCr
            {
                for (int c = 0; c < 3; c++)
                {
                    float scale = trackBars[c].Value / 100f;
                    bool enabled = channelEnableCheckboxes[c].Checked;
                    for (int p = 0; p < width * height; p++)
                    {
                        float val = enabled ? originalChannels[c][p] * scale : 0;
                        val = Math.Max(0, Math.Min(255, val));
                        adjusted[c][p] = val;
                    }
                }
            }

            RebuildImageFromChannels(adjusted);
        }

        private void RebuildImageFromChannels(float[][] channelData)
        {
            if (currentColorSpace == "RGB")
            {
                using (Image<Bgr, byte> img = new Image<Bgr, byte>(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte r = (byte)channelData[0][idx];
                            byte g = (byte)channelData[1][idx];
                            byte b = (byte)channelData[2][idx];
                            img[y, x] = new Bgr(b, g, r);
                        }
                    }
                    currentMat = img.Mat.Clone();
                }
            }
            else
            {
                using (Image<Gray, byte> ch0 = new Image<Gray, byte>(width, height))
                using (Image<Gray, byte> ch1 = new Image<Gray, byte>(width, height))
                using (Image<Gray, byte> ch2 = new Image<Gray, byte>(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            ch0[y, x] = new Gray(channelData[0][idx]);
                            ch1[y, x] = new Gray(channelData[1][idx]);
                            ch2[y, x] = new Gray(channelData[2][idx]);
                        }
                    }

                    using (VectorOfMat vec = new VectorOfMat(ch0.Mat, ch1.Mat, ch2.Mat))
                    {
                        Mat merged = new Mat();
                        CvInvoke.Merge(vec, merged);
                        Mat bgrMat = new Mat();
                        switch (currentColorSpace)
                        {
                            case "HSV":
                                CvInvoke.CvtColor(merged, bgrMat, ColorConversion.Hsv2Bgr);
                                break;
                            case "LAB":
                                CvInvoke.CvtColor(merged, bgrMat, ColorConversion.Lab2Bgr);
                                break;
                            case "YUV":
                                CvInvoke.CvtColor(merged, bgrMat, ColorConversion.Yuv2Bgr);
                                break;
                            case "YCbCr":
                                CvInvoke.CvtColor(merged, bgrMat, ColorConversion.YCrCb2Bgr);
                                break;
                            default:
                                bgrMat = merged;
                                break;
                        }
                        currentMat = bgrMat;
                    }
                }
            }

            pictureBox1.Image = currentMat.ToBitmap();
            this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)} [{currentColorSpace} - adjusted]";
        }

        // -----------------------------------------------------------------
        // CMYK Handling (unchanged, works correctly)
        // -----------------------------------------------------------------
        private float[][] originalCmyk;

        private void ConvertToCmyk(Mat bgrMat)
        {
            width = bgrMat.Width;
            height = bgrMat.Height;
            originalCmyk = new float[4][];
            for (int i = 0; i < 4; i++) originalCmyk[i] = new float[width * height];

            using (Image<Bgr, byte> img = bgrMat.ToImage<Bgr, byte>())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Bgr c = img[y, x];
                        double r = c.Red / 255.0;
                        double g = c.Green / 255.0;
                        double b = c.Blue / 255.0;
                        double k = 1 - Math.Max(Math.Max(r, g), b);
                        int idx = y * width + x;
                        if (k == 1.0)
                        {
                            originalCmyk[0][idx] = 0;
                            originalCmyk[1][idx] = 0;
                            originalCmyk[2][idx] = 0;
                            originalCmyk[3][idx] = 100;
                            continue;
                        }
                        double c_ = (1 - r - k) / (1 - k);
                        double m = (1 - g - k) / (1 - k);
                        double y_ = (1 - b - k) / (1 - k);
                        originalCmyk[0][idx] = (float)(c_ * 100);
                        originalCmyk[1][idx] = (float)(m * 100);
                        originalCmyk[2][idx] = (float)(y_ * 100);
                        originalCmyk[3][idx] = (float)(k * 100);
                    }
                }
            }

            currentColorSpace = "CMYK";
            BuildCmykUI();
            channelPanel.Visible = true;
            ApplyCmykAdjustment();
            this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)} [CMYK]";
        }

        private void BuildCmykUI()
        {
            channelPanel.Controls.Clear();
            channelNames = new[] { "Cyan (%)", "Magenta (%)", "Yellow (%)", "Black (%)" };
            trackBars = new TrackBar[4];
            channelEnableCheckboxes = new CheckBox[4];
            channelValueLabels = new Label[4];

            for (int i = 0; i < 4; i++)
            {
                GroupBox gb = new GroupBox();
                gb.Text = channelNames[i];
                gb.Dock = DockStyle.Top;
                gb.Height = 130;
                gb.Margin = new Padding(0, 0, 0, 8);
                gb.Padding = new Padding(6);

                CheckBox chk = new CheckBox();
                chk.Text = "Enable";
                chk.Checked = true;
                chk.Dock = DockStyle.Top;
                chk.Height = 25;
                chk.Margin = new Padding(3);
                chk.CheckedChanged += (s, e) => ApplyCmykAdjustment();
                channelEnableCheckboxes[i] = chk;
                gb.Controls.Add(chk);

                TrackBar tb = new TrackBar();
                tb.Minimum = 0;
                tb.Maximum = 200;
                tb.Value = 100;
                tb.Dock = DockStyle.Top;
                tb.Height = 45;
                tb.Scroll += (s, e) => ApplyCmykAdjustment();
                trackBars[i] = tb;
                gb.Controls.Add(tb);

                Label lbl = new Label();
                lbl.Text = tb.Value.ToString();
                lbl.Dock = DockStyle.Top;
                lbl.Height = 25;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                channelValueLabels[i] = lbl;
                tb.ValueChanged += (s, e) => lbl.Text = tb.Value.ToString();
                gb.Controls.Add(lbl);

                channelPanel.Controls.Add(gb);
            }

            resetChannelsBtn = new Button();
            resetChannelsBtn.Text = "Reset Adjustments";
            resetChannelsBtn.Dock = DockStyle.Top;
            resetChannelsBtn.Height = 40;
            resetChannelsBtn.Margin = new Padding(0, 10, 0, 0);
            resetChannelsBtn.Click += (s, e) =>
            {
                for (int i = 0; i < 4; i++)
                {
                    trackBars[i].Value = 100;
                    channelEnableCheckboxes[i].Checked = true;
                }
                ApplyCmykAdjustment();
            };
            channelPanel.Controls.Add(resetChannelsBtn);
        }
        private void ApplyCmykAdjustment()
        {
            if (originalCmyk == null) return;
            float[][] adjusted = new float[4][];
            for (int i = 0; i < 4; i++)
            {
                adjusted[i] = new float[width * height];
                float scale = trackBars[i].Value / 100f;
                bool enabled = channelEnableCheckboxes[i].Checked;
                for (int p = 0; p < width * height; p++)
                {
                    float val = enabled ? originalCmyk[i][p] * scale : 0;
                    val = Math.Max(0, Math.Min(100, val));
                    adjusted[i][p] = val;
                }
            }
            using (Image<Bgr, byte> img = new Image<Bgr, byte>(width, height))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = y * width + x;
                        float c = adjusted[0][idx] / 100f;
                        float m = adjusted[1][idx] / 100f;
                        float y_ = adjusted[2][idx] / 100f;
                        float k = adjusted[3][idx] / 100f;
                        double r = (1 - c) * (1 - k);
                        double g = (1 - m) * (1 - k);
                        double b = (1 - y_) * (1 - k);
                        double gray = (r + g + b) / 3.0;
                        double desat = 0.65;
                        r = r * desat + gray * (1 - desat);
                        g = g * desat + gray * (1 - desat);
                        b = b * desat + gray * (1 - desat);
                        byte rr = (byte)(Math.Max(0, Math.Min(1, r)) * 255);
                        byte gg = (byte)(Math.Max(0, Math.Min(1, g)) * 255);
                        byte bb = (byte)(Math.Max(0, Math.Min(1, b)) * 255);
                        img[y, x] = new Bgr(bb, gg, rr);
                    }
                }
                currentMat = img.Mat.Clone();
                pictureBox1.Image = currentMat.ToBitmap();
                this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)} [CMYK - adjusted]";
            }
        }

        private void SaveImage()
        {
            if (currentMat == null) return;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|BMP Image|*.bmp";
                sfd.FileName = currentFilePath != null ? Path.GetFileNameWithoutExtension(currentFilePath) + "_edited" : "pixelab_output";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        currentMat.ToBitmap().Save(sfd.FileName);
                        MessageBox.Show("Image saved!", "PixelLab");
                    }
                    catch (Exception ex) { MessageBox.Show($"Save error: {ex.Message}"); }
                }
            }
        }

        private void QuantizeImage()
        {
            if (currentMat == null || currentMat.IsEmpty)
            {
                MessageBox.Show("No image loaded.", "PixelLab");
                return;
            }

            using (Form dialog = new Form())
            {
                dialog.Text = "Color Quantization";
                dialog.Width = 300;
                dialog.Height = 150;
                dialog.StartPosition = FormStartPosition.CenterParent;

                Label lbl = new Label();
                lbl.Text = "Number of colors:";
                lbl.Location = new Point(20, 20);
                lbl.AutoSize = true;

                ComboBox cmb = new ComboBox();
                cmb.DropDownStyle = ComboBoxStyle.DropDownList;
                // Special entry for black & white (1-bit)
                cmb.Items.Add(new ComboItem { Text = "2 (Black & White)", Value = 2 });
                cmb.Items.AddRange(new object[] { 4, 8, 16, 32, 64, 256 });
                cmb.SelectedIndex = 5; // 64 by default
                cmb.Location = new Point(160, 18);
                cmb.Width = 100;

                Button okBtn = new Button();
                okBtn.Text = "OK";
                okBtn.DialogResult = DialogResult.OK;
                okBtn.Location = new Point(100, 70);

                dialog.Controls.Add(lbl);
                dialog.Controls.Add(cmb);
                dialog.Controls.Add(okBtn);

                if (dialog.ShowDialog() != DialogResult.OK) return;

                int targetColors = 0;
                if (cmb.SelectedItem is ComboItem ci)
                    targetColors = ci.Value;
                else
                    targetColors = (int)cmb.SelectedItem;

                Cursor.Current = Cursors.WaitCursor;

                try
                {
                    using (Bitmap srcBitmap = currentMat.ToBitmap())
                    {
                        int w = srcBitmap.Width;
                        int h = srcBitmap.Height;

                        if (targetColors == 2)
                        {
                            // --- Black & White (1-bit) using luminance threshold ---
                            Bitmap destBitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            System.Drawing.Imaging.BitmapData srcData = srcBitmap.LockBits(
                                new Rectangle(0, 0, w, h),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            System.Drawing.Imaging.BitmapData dstData = destBitmap.LockBits(
                                new Rectangle(0, 0, w, h),
                                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                            unsafe
                            {
                                byte* srcPtr = (byte*)srcData.Scan0;
                                byte* dstPtr = (byte*)dstData.Scan0;
                                int stride = srcData.Stride;

                                // Calculate average luminance for threshold
                                long totalLum = 0;
                                for (int y = 0; y < h; y++)
                                {
                                    byte* row = srcPtr + y * stride;
                                    for (int x = 0; x < w; x++)
                                    {
                                        byte b = row[x * 3];
                                        byte g = row[x * 3 + 1];
                                        byte r = row[x * 3 + 2];
                                        totalLum += (r + g + b) / 3;
                                    }
                                }
                                int threshold = (int)(totalLum / (w * h));

                                // Convert each pixel to black or white
                                for (int y = 0; y < h; y++)
                                {
                                    byte* srcRow = srcPtr + y * stride;
                                    byte* dstRow = dstPtr + y * stride;
                                    for (int x = 0; x < w; x++)
                                    {
                                        byte b = srcRow[x * 3];
                                        byte g = srcRow[x * 3 + 1];
                                        byte r = srcRow[x * 3 + 2];
                                        int lum = (r + g + b) / 3;
                                        byte val = lum > threshold ? (byte)255 : (byte)0;
                                        dstRow[x * 3] = val;     // B
                                        dstRow[x * 3 + 1] = val; // G
                                        dstRow[x * 3 + 2] = val; // R
                                    }
                                }
                            }
                            srcBitmap.UnlockBits(srcData);
                            destBitmap.UnlockBits(dstData);

                            Mat newMat = destBitmap.ToMat();
                            Mat oldMat = currentMat;
                            currentMat = newMat.Clone();
                            oldMat?.Dispose();
                            destBitmap.Dispose();

                            pictureBox1.Image = currentMat.ToBitmap();
                            this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)} [Black & White]";
                        }
                        else
                        {
                            // --- Uniform quantization for 4,8,16,32,64,256 ---
                            int levels = (int)Math.Pow(targetColors, 1.0 / 3.0);
                            if (levels < 2) levels = 2;
                            if (levels > 8) levels = 8;
                            int step = 256 / levels;
                            if (step < 1) step = 1;

                            Bitmap destBitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            System.Drawing.Imaging.BitmapData srcData = srcBitmap.LockBits(
                                new Rectangle(0, 0, w, h),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            System.Drawing.Imaging.BitmapData dstData = destBitmap.LockBits(
                                new Rectangle(0, 0, w, h),
                                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                            unsafe
                            {
                                byte* srcPtr = (byte*)srcData.Scan0;
                                byte* dstPtr = (byte*)dstData.Scan0;
                                int stride = srcData.Stride;

                                for (int y = 0; y < h; y++)
                                {
                                    byte* srcRow = srcPtr + y * stride;
                                    byte* dstRow = dstPtr + y * stride;
                                    for (int x = 0; x < w; x++)
                                    {
                                        byte b = srcRow[x * 3];
                                        byte g = srcRow[x * 3 + 1];
                                        byte r = srcRow[x * 3 + 2];
                                        b = (byte)((b / step) * step);
                                        g = (byte)((g / step) * step);
                                        r = (byte)((r / step) * step);
                                        dstRow[x * 3] = b;
                                        dstRow[x * 3 + 1] = g;
                                        dstRow[x * 3 + 2] = r;
                                    }
                                }
                            }
                            srcBitmap.UnlockBits(srcData);
                            destBitmap.UnlockBits(dstData);

                            Mat newMat = destBitmap.ToMat();
                            Mat oldMat = currentMat;
                            currentMat = newMat.Clone();
                            oldMat?.Dispose();
                            destBitmap.Dispose();

                            pictureBox1.Image = currentMat.ToBitmap();
                            this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)} [Quantized: {targetColors} colors]";
                        }

                        // Reset workspace
                        currentColorSpace = "RGB";
                        channelPanel.Visible = false;
                        originalChannels = null;
                        originalCmyk = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Quantization error: {ex.Message}", "PixelLab");
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }
        private void OpenImage()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*";
                ofd.Title = "Select an Image";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadImage(ofd.FileName);
                }
            }
        }
        private void ResetToOriginal()
        {
            if (originalMat == null) return;
            currentMat = originalMat.Clone();
            pictureBox1.Image = currentMat.ToBitmap();
            this.Text = $"PixelLab - {Path.GetFileName(currentFilePath)}";
            channelPanel.Visible = false;
            UpdateImageInfo();
            originalCmyk = null;
            originalChannels = null;
        }

        private class ComboItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public override string ToString() => Text;
        }

    }
    
}