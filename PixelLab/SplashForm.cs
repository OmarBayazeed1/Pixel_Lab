using System;
using System.Drawing;
using System.Windows.Forms;

namespace PixelLab
{
    public partial class SplashForm : Form
    {
        public SplashForm()
        {
            InitializeComponent();
            SetupUI();
        }

        private void InitializeComponent()
        {
            this.Text = "Welcome to PixelLab";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void SetupUI()
        {
            // Title
            Label titleLabel = new Label();
            titleLabel.Text = "AJOYA PixelLab";
            titleLabel.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(70, 130, 200);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 80;
            this.Controls.Add(titleLabel);

            // Description
            Label descLabel = new Label();
            descLabel.Text = "An interactive image processing laboratory.\n\n" +
                             "Features:\n" +
                             "• Open, edit, and save images\n" +
                             "• Convert between RGB, HSV, LAB, YUV, YCbCr, CMYK\n" +
                             "• Adjust color channels independently\n" +
                             "• Quantize colors (reduce number of colors)\n" +
                             "• Interactive 2D & 3D color space visualizers\n" +
                             "• Real-time color picking with cross-space synchronization";
            descLabel.Font = new Font("Segoe UI", 10);
            descLabel.TextAlign = ContentAlignment.MiddleLeft;
            descLabel.Dock = DockStyle.Fill;
            descLabel.Padding = new Padding(20, 40, 20, 20);
            this.Controls.Add(descLabel);

            // Enter button
            Button enterBtn = new Button();
            enterBtn.Text = "Enter PixelLab";
            enterBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            enterBtn.BackColor = Color.LightGreen;
            enterBtn.ForeColor = Color.Black;
            enterBtn.FlatStyle = FlatStyle.Flat;
            enterBtn.FlatAppearance.BorderColor = Color.DarkGreen;
            enterBtn.Size = new Size(180, 50);
            enterBtn.Location = new Point((this.Width - enterBtn.Width) / 2, this.Height - 80);
            enterBtn.Dock = DockStyle.Bottom;
            

            enterBtn.Click += (s, e) =>
            {
                Form1 mainForm = new Form1();
                mainForm.Show();
                this.Hide(); // hide splash, not close (so app doesn't exit)
                // optional: when main form closes, close the splash too
                mainForm.FormClosed += (_, __) => this.Close();
            };
            this.Controls.Add(enterBtn);
        }
    }
}