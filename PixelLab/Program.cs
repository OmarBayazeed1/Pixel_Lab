using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace PixelLab
{
    static class Program
    {
        
        [STAThread]
        static void Main()
        {
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SplashForm());
        }
    }
}
