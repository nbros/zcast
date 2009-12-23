using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;

namespace GDIScreenshot
{
    public partial class PlayForm : Form
    {
        //private int resX, resY, nFrames, interval;

        public PlayForm()
        {
            InitializeComponent();
        }

        int currentFrame = 1;

        Bitmap reconstructed;
        Graphics g;
        ScreencastData screencastData;

        private void PlayForm_Load(object sender, EventArgs e)
        {
            screencastData = ScreencastData.DeSerializeFrom(new FileStream(Path.Combine(Form1.SaveFolder, "zcast.xml"), FileMode.Open));
            screencastData.FrameInfos.Sort((x, y) => x.FrameNumber - y.FrameNumber);

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            //TopMost = true;

            Paint += new PaintEventHandler(PlayForm_Paint);

            Timer timer = new Timer();
            timer.Interval = screencastData.DelayBetweenFrames;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            reconstructed = new Bitmap(screencastData.Width, screencastData.Height, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(reconstructed);

        }

        void timer_Tick(object sender, EventArgs e)
        {
            Refresh();
        }

        void PlayForm_Paint(object sender, PaintEventArgs e)
        {
            if (currentFrame > screencastData.FrameInfos.Count)
                currentFrame = 1;

            FrameInfo frame = screencastData.FrameInfos.ElementAt(currentFrame - 1);
            Bitmap bmp = new Bitmap(Path.Combine(Form1.SaveFolder, frame.FileName));
            Point offset = frame.Offset;
            g.DrawImage(bmp, offset.X, offset.Y); //, new Rectangle(0, 0, resX, resY), GraphicsUnit.Pixel);
            e.Graphics.DrawImage(reconstructed, Point.Empty);
            bmp.Dispose();
            currentFrame++;
        }
    }
}
