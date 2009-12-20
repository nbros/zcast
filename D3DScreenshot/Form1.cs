using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace D3DScreenshot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnScreenshot_Click(object sender, EventArgs e)
        {
            this.Text = "Waiting...";
            //System.Threading.Thread.Sleep(3000);
            this.Text = "Capturing";

            Stopwatch timer = new Stopwatch();

            //seting up a D3D device
            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Discard;
            presentParams.PresentFlag = PresentFlag.LockableBackBuffer;

            Device device = new
            Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing,
            presentParams);

            //creating a Surface the size of screen
            Surface surface =
            device.CreateOffscreenPlainSurface(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height, Format.A8R8G8B8, Pool.SystemMemory);


            timer.Start();

            for (int i = 1; i <= 100; i++)
            {
                //Surface surface = device.GetBackBuffer(0, 0, BackBufferType.Mono);
                device.GetFrontBufferData(0, surface);
                GraphicsStream myGraphicsStream = surface.LockRectangle(LockFlags.ReadOnly);
                Bitmap bitmap = new
                Bitmap(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height, SystemInformation.PrimaryMonitorSize.Width * 4, PixelFormat.Format32bppArgb, myGraphicsStream.InternalData);

                surface.UnlockRectangle();

                Bitmap bmp = new Bitmap(800, 600);
                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(bitmap, 0, 0, new Rectangle(0, 0, 800, 600), GraphicsUnit.Pixel);



                bmp.Save(@"C:\Users\Nicolas\Documents\aaa" + i + ".png");

                //Bitmap bm = new Bitmap(dxWholeScreenBitmap);


            }
            timer.Stop();
            label.Text = timer.ElapsedMilliseconds + " ms";

            device.Dispose();
            this.Text = "Idle";

        }

    }
}
