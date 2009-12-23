using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace GDIScreenshot
{
    public partial class Form1 : Form
    {

        int resX = SystemInformation.PrimaryMonitorSize.Width;
        int resY = SystemInformation.PrimaryMonitorSize.Height;
        int interval = 125; // 1/fps

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

        public static string SaveFolder
        {
            get
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AAAZCast");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        private double Now()
        {
            return (DateTime.Now - new DateTime(2000, 1, 1)).TotalMilliseconds;
        }

        private bool capturing = false;
        private bool stopCapture = false;

        private void btnCapture_Click(object sender, EventArgs e)
        {
            lock (btnCapture)
            {
                if (capturing)
                {
                    btnCapture.Text = "Start capturing";
                    stopCapture = true;
                    return;
                }

                btnCapture.Text = "Stop capture";
                capturing = true;
            }

            Stopwatch timer = new Stopwatch();
            Stopwatch timer2 = new Stopwatch();

            Bitmap bmpPrev = null;

            double last = Now();
            double betweenFrames = interval;

            timer.Start();
            ScreencastData screencastData = new ScreencastData();
            screencastData.Width = resX;
            screencastData.Height = resY;
            screencastData.DelayBetweenFrames = interval;

            //for (int i = 1; i <= nImages; i++)
            int frameNumber = 0;
            while(true)
            {
                if (stopCapture)
                {
                    stopCapture = false;
                    capturing = false;
                    
                    timer.Stop();
                    label.Text = timer.Elapsed.Seconds + " s";

                    using (FileStream stream = new FileStream(Path.Combine(SaveFolder, "zcast.xml"), FileMode.Create))
                    {
                        screencastData.SerializeTo(stream);
                    }
                    return;
                }
                frameNumber++;

                IntPtr desktopWindow = NativeMethods.GetDesktopWindow();
                timer2.Reset();
                timer2.Start();
                Bitmap bmp = CaptureRectangle(desktopWindow, new Rectangle(0, 0, resX, resY));
                Debug.WriteLine("capture rectangle: " + timer2.ElapsedMilliseconds + "ms");
                //bmp.Save(@"C:\Users\Nicolas\Documents\test" + i + ".png");

                WaitCallback callBack = new WaitCallback(ComputeDiffJob);
                ThreadPool.QueueUserWorkItem(callBack, new ComputeDiffWork()
                {
                    BmpPrev = bmpPrev,
                    BmpCur = (Bitmap)bmp.Clone(),
                    FileName = "zcast" + frameNumber.ToString(),
                    ScreencastData = screencastData,
                    FrameNumber = frameNumber - 1,
                    EncodingQuality = (int)udQuality.Value
                });

                bmpPrev = bmp;
                label.Text = "" + frameNumber;
                Application.DoEvents();


                double now = Now();
                double nextFrameTime = last + betweenFrames;
                double remaining = nextFrameTime - now;
                if (remaining > 0)
                {
                    txtLog.AppendText("frame " + frameNumber + ": sleeping " + (remaining) + "ms\n");
                    while ((int)remaining > 1)
                    {
                        //txtLog.AppendText(Thread.CurrentThread.ManagedThreadId.ToString());
                        System.Threading.Thread.Sleep((int)remaining);
                        now = Now();
                        remaining = nextFrameTime - now;
                    }
                }
                else
                {
                    txtLog.AppendText(String.Format("Missed frame {0} by {1:f2}ms\n", frameNumber, -remaining));
                }
                last = now;
            }

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

        private class ComputeDiffWork
        {
            public Bitmap BmpPrev { get; set; }
            public Bitmap BmpCur { get; set; }
            public int FrameNumber { get; set; }
            public string FileName { get; set; }
            public ScreencastData ScreencastData { get; set; }
            public int EncodingQuality { get; set; }
        }

        static void ComputeDiffJob(object state)
        {
            ComputeDiffWork work = (ComputeDiffWork)state;
            Debug.WriteLine("ComputeDiff: " + Thread.CurrentThread.ToString());
            Stopwatch timer = new Stopwatch();

            Bitmap diff;
            Point offset;
            bool keyFrame;
            if (work.BmpPrev != null)
            {
                timer.Start();
                diff = ComputeDiff(work.BmpPrev, work.BmpCur, out offset, out keyFrame);
                Debug.WriteLine("compute diff: " + timer.ElapsedMilliseconds + "ms");
            }
            else
            {
                diff = work.BmpCur;
                keyFrame = true;
                offset = Point.Empty;
            }

            timer.Reset();
            timer.Start();

            string filename;

            if (keyFrame)
            {
                filename = work.FileName + ".jpeg";
                EncoderParameters parameters = new EncoderParameters(1);
                parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(Encoder.Quality, work.EncodingQuality);
                ImageCodecInfo[] ies = ImageCodecInfo.GetImageEncoders();
                diff.Save(Path.Combine(SaveFolder, filename), ies[1], parameters);
            }
            else
            {
                filename = work.FileName + ".png";
                diff.Save(Path.Combine(SaveFolder, filename));
            }

            lock (work.ScreencastData)
            {
                FrameInfo frameInfo = new FrameInfo() { FrameNumber = work.FrameNumber, Offset = offset, FileName = filename };
                work.ScreencastData.FrameInfos.Add(frameInfo);
            }

            if (diff != work.BmpCur)
                diff.Dispose();
            //Debug.WriteLine("save diff: " + timer.ElapsedMilliseconds + "ms");

            if (work.BmpPrev != null)
            {
                work.BmpPrev.Dispose();
            }

        }


        public static Bitmap ComputeDiff(Bitmap bmpPrev, Bitmap bmpCur, out Point offset, out bool keyFrame)
        {
            int width = bmpPrev.Size.Width;
            int height = bmpPrev.Size.Height;

            int minx = width;
            int miny = height;
            int maxx = 0;
            int maxy = 0;

            Bitmap resultImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(new Point(0, 0), bmpPrev.Size);

            // Access the image data directly for faster image processing
            BitmapData prevImageData = bmpPrev.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData curImageData = bmpCur.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData resultImageData = resultImage.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            uint nDifferentPixels = 0;

            int length = (prevImageData.Stride * prevImageData.Height) / 4;

            unsafe
            {

                uint* pPrev = (uint*)prevImageData.Scan0.ToPointer();
                uint* pCur = (uint*)curImageData.Scan0.ToPointer();
                uint* pResult = (uint*)resultImageData.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (*pPrev == *pCur)
                        {
                            *pResult = 0;
                        }
                        else
                        {
                            *pResult = *pCur;
                            nDifferentPixels++;
                            minx = Math.Min(minx, x);
                            maxx = Math.Max(maxx, x);
                            miny = Math.Min(miny, y);
                            maxy = Math.Max(maxy, y);
                        }

                        pPrev++;
                        pCur++;
                        pResult++;
                    }
                }
            }

            bmpPrev.UnlockBits(prevImageData);
            bmpCur.UnlockBits(curImageData);
            resultImage.UnlockBits(resultImageData);

            int croppedWidth = Math.Max(maxx - minx + 1, 1);
            int croppedHeight = Math.Max(maxy - miny + 1, 1);

            Bitmap resultImageCropped = new Bitmap(croppedWidth, croppedHeight, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(resultImageCropped);
            //g.Clear(Color.Transparent);
            keyFrame = nDifferentPixels > width * height / 10;
            Image usedImg = keyFrame ? bmpCur : resultImage;
            g.DrawImage(usedImg, 0, 0, new Rectangle(minx, miny, croppedWidth, croppedHeight), GraphicsUnit.Pixel);

            resultImage.Dispose();

            offset = new Point(minx, miny);
            return resultImageCropped;
        }


        public static Bitmap ComputeDiff2(Bitmap bmpPrev, Bitmap bmpCur)
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
            PlayForm form = new PlayForm();
            form.Show();
        }
    }
}
