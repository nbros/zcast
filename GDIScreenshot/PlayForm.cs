using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace GDIScreenshot
{
    public partial class PlayForm : Form
    {
        private int resX, resY, nFrames, interval;

        public PlayForm(int resX, int resY, int nFrames, int interval)
        {
            InitializeComponent();
            this.resX = resX;
            this.resY = resY;
            this.nFrames = nFrames;
            this.interval = interval;
        }

        int currentFrame = 1;

        Bitmap reconstructed;
        Graphics g;

        private void PlayForm_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;

            Paint += new PaintEventHandler(PlayForm_Paint);

            Timer timer = new Timer();
            timer.Interval = interval;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            reconstructed = new Bitmap(resX, resY, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(reconstructed);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Refresh();
        }

        void PlayForm_Paint(object sender, PaintEventArgs e)
        {
            if (currentFrame > nFrames)
                currentFrame = 1;

            Bitmap bmp = new Bitmap(@"C:\Users\Nicolas\Documents\AAA\" + currentFrame + ".png");
            g.DrawImage(bmp, 0, 0, new Rectangle(0, 0, resX, resY), GraphicsUnit.Pixel);
            e.Graphics.DrawImage(reconstructed, 0, 0);
            currentFrame++;
        }
    }
}
