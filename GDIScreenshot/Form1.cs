using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GDIScreenshot
{
    public partial class Form1 : Form
    {

        int nImages = 200;
        int resX = 1024;
        int resY = 768;
        int interval = 250; // 8 fps

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        public static Bitmap CaptureRectangle(IntPtr handle, Rectangle rect)
        {
            // Get the hDC of the target window
            IntPtr hdcSrc = NativeMethods.GetWindowDC(handle);
            // Create a device context we can copy to
            IntPtr hdcDest = GDI.CreateCompatibleDC(hdcSrc);
            // Create a bitmap we can copy it to
            IntPtr hBitmap = GDI.CreateCompatibleBitmap(hdcSrc, rect.Width, rect.Height);
            // Select the bitmap object
            IntPtr hOld = GDI.SelectObject(hdcDest, hBitmap);
            // BitBlt over
            GDI.BitBlt(hdcDest, 0, 0, rect.Width, rect.Height, hdcSrc, rect.Left, rect.Top, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
            // Restore selection
            GDI.SelectObject(hdcDest, hOld);
            // Clean up
            GDI.DeleteDC(hdcDest);
            NativeMethods.ReleaseDC(handle, hdcSrc);
            // Get a .NET image object for it
            Bitmap bmp = Image.FromHbitmap(hBitmap);
            // Free up the Bitmap object
            GDI.DeleteObject(hBitmap);
            return bmp;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private double Now()
        {
            return (DateTime.Now - new DateTime(2000, 1, 1)).TotalMilliseconds;
        }

        private void btnScreencast_Click(object sender, EventArgs e)
        {
            Stopwatch timer = new Stopwatch();

            Bitmap bmpPrev = null;
            timer.Start();
            double last = Now();
            double betweenFrames = interval;

            for (int i = 1; i <= nImages; i++)
            {
                IntPtr desktopWindow = NativeMethods.GetDesktopWindow();
                Bitmap bmp = CaptureRectangle(desktopWindow, new Rectangle(0, 0, resX, resY));
                Bitmap diff;
                if (bmpPrev != null)
                {
                    diff = ComputeDiff(bmpPrev, bmp);
                }
                else
                {
                    diff = bmp;
                }
                diff.Save(@"C:\Users\Nicolas\Documents\AAA\" + i + ".png");
                bmpPrev = bmp;
                label.Text = "" + i;
                Application.DoEvents();


                double now = Now();
                double remaining = last + betweenFrames - now;
                last = now;
                if (remaining > 0)
                {
                    //txtLog.AppendText("frame " + i + ": sleeping " + (remaining) + "ms\n");
                    System.Threading.Thread.Sleep((int)remaining);
                }
                else
                {
                    txtLog.AppendText(String.Format("Missed frame {0} by {1:f2}ms\n", i, -remaining));
                }
            }
            timer.Stop();
            label.Text = timer.ElapsedMilliseconds + " ms";

            /*form.WindowState = FormWindowState.Maximized;
            form.FormBorderStyle = FormBorderStyle.None;
            form.TopMost = true;

            form.Paint += new PaintEventHandler(form_Paint);
            Bitmap reconstructed = new Bitmap(800, 600, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(reconstructed);
            for (int i = 1; i <= 1000; i++)
            {
                Bitmap bmp = new Bitmap(@"C:\Users\Nicolas\Documents\AAA\" + i + ".png");
                g.DrawImage(bmp, 0, 0, new Rectangle(0, 0, 800, 600), GraphicsUnit.Pixel);
                reconstructed.Save(@"C:\Users\Nicolas\Documents\AAA\reconstructed " + i + ".png");
                label.Text = "reconstructing: " + i;
                Application.DoEvents();
            }*/
        }

        public static Bitmap ComputeDiff(Bitmap bmpPrev, Bitmap bmpCur)
        {
            int width = bmpPrev.Size.Width;
            int height = bmpPrev.Size.Height;

            Bitmap resultImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            Rectangle rect = new Rectangle(new Point(0, 0), bmpPrev.Size);

            // Access the image data directly for faster image processing
            BitmapData prevImageData = bmpPrev.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData curImageData = bmpCur.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData resultImageData = resultImage.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            try
            {
                IntPtr pPrevImage = prevImageData.Scan0;
                IntPtr pCurImage = curImageData.Scan0;
                IntPtr pResultImage = resultImageData.Scan0;

                int bytes = prevImageData.Stride * prevImageData.Height;
                byte[] prevImageRGB = new byte[bytes];
                byte[] curImageRGB = new byte[bytes];
                byte[] resultImageRGB = new byte[bytes];

                Marshal.Copy(pPrevImage, prevImageRGB, 0, bytes);
                Marshal.Copy(pCurImage, curImageRGB, 0, bytes);

                int offset = 0;

                int b0, g0, r0, b1, g1, r1, resultR, resultG, resultB, resultAlpha;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // ARGB is in fact BGRA (little endian)
                        b0 = prevImageRGB[offset + 0];
                        g0 = prevImageRGB[offset + 1];
                        r0 = prevImageRGB[offset + 2];

                        b1 = curImageRGB[offset + 0];
                        g1 = curImageRGB[offset + 1];
                        r1 = curImageRGB[offset + 2];

                        if (b0 == b1 && g0 == g1 && r0 == r1)
                        {
                            resultImageRGB[offset + 0] = 0;
                            resultImageRGB[offset + 1] = 0;
                            resultImageRGB[offset + 2] = 0;
                            resultImageRGB[offset + 3] = 0;
                        }
                        else
                        {
                            resultImageRGB[offset + 0] = (byte)b1;
                            resultImageRGB[offset + 1] = (byte)g1;
                            resultImageRGB[offset + 2] = (byte)r1;
                            resultImageRGB[offset + 3] = 255;
                        }

                        offset += 4;
                    }
                }

                Marshal.Copy(resultImageRGB, 0, pResultImage, bytes);
            }
            finally
            {
                bmpPrev.UnlockBits(prevImageData);
                bmpCur.UnlockBits(curImageData);
                resultImage.UnlockBits(resultImageData);
            }
            return resultImage;
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            PlayForm form = new PlayForm(resX, resY, nImages, interval);
            form.Show();
        }
    }
}
