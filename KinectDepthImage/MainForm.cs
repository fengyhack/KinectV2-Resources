using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Kinect; //!!!
using System.Runtime.InteropServices; // Marshal.Copy()

namespace KinectDepthImage
{
    public partial class MainForm : Form
    {
        #region Members

        private KinectSensor kinectSensor = null;
        private DepthFrameReader depthFrameReader = null;
        private Bitmap bmp = null;

        private enum SensorStatus { Opened, Closed };
        private SensorStatus sensorStatus = SensorStatus.Closed;
        
        private enum SensorOperation{ Open, Close };
        private SensorOperation sensorOperation = SensorOperation.Open;

        private string capFolder = "Capture/";

        #endregion Members
        
        public MainForm()
        {
            kinectSensor = KinectSensor.GetDefault();

            if (kinectSensor == null)
            {
                throw new Exception("Failed to open KinectSensor!");
            }

            depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

            InitializeComponent();

            if(!Directory.Exists(capFolder))
            {
                Directory.CreateDirectory(capFolder);
            }
            textBox1.Text = "READY";

            StartPosition = FormStartPosition.CenterScreen;
        }

        private void btnSensorSwitch_Click(object sender, EventArgs e)
        {
            try
            {
                if (sensorOperation == SensorOperation.Open)
                {
                    SafeOpenSensor();

                    sensorOperation = SensorOperation.Close;
                    btnSensorSwitch.Text = "Close";
                    textBox1.Text = "KinectSensor opened, now working...";
                }
                else
                {
                    SafeCloseSensor();

                    sensorOperation = SensorOperation.Open;
                    btnSensorSwitch.Text = "Open";
                    textBox1.Text = "KinectSensor closed.";
                }
            }
            catch(Exception ex)
            {
                textBox1.Text = ex.Message;
            }
            
        }

        private void btnCaptureImage_Click(object sender, EventArgs e)
        {
            if(bmp==null)
            {
                textBox1.Text = "NO image available!";
                return;          
            }

            string sfName = capFolder + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg";
            bmp.Save(sfName, ImageFormat.Jpeg);
            textBox1.Text = string.Format("Image saved as file \'{0}\'", sfName);
        }

        private void SafeOpenSensor()
        {
            if(sensorStatus==SensorStatus.Closed)
            {
                kinectSensor.Open();
                sensorStatus = SensorStatus.Opened;
            }
        }

        private void SafeCloseSensor()
        {
            if(sensorStatus==SensorStatus.Opened)
            {
                kinectSensor.Close();
                sensorStatus = SensorStatus.Closed;

                if(bmp!=null)
                {
                    bmp.Dispose();
                    bmp = null;
                }
                pictureBox1.Image = null;
                pictureBox1.Refresh();                
            }
        }

        private void depthFrameReader_FrameArrived(object sender,DepthFrameArrivedEventArgs e)
        {
            using(DepthFrame frame=e.FrameReference.AcquireFrame())
            {
                if (frame == null)
                {
                    return;
                }

                using(KinectBuffer buffer=frame.LockImageBuffer())
                {
                    int width = frame.FrameDescription.Width;
                    int height = frame.FrameDescription.Height;
                    ushort minDepth = frame.DepthMinReliableDistance;
                    ushort maxDepth = frame.DepthMaxReliableDistance;
                    int d = (maxDepth - minDepth) / 4;
                    int ratio = (maxDepth-minDepth) / 256;         
                    int size = width * height;

                    if(bmp==null)
                    {         
                        bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    }

                    unsafe
                    {
                        BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                            ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                        byte* bgrPtr = (byte*)(bmpData.Scan0.ToPointer());
                        ushort* dataPtr = (ushort*)(buffer.UnderlyingBuffer);
                        int uv, gv;
                        const byte b0 = 0, bx = 255;
                        int rowOffset = bmpData.Stride - 3 * width;

                        for (int row = 0; row < height;++row )
                        {
                            for(int col=0;col<width;++col)
                            {
                                uv = dataPtr[row * width + col];
                                gv = uv / ratio;
                                if(uv<minDepth)
                                {
                                    bgrPtr[2] = b0;
                                    bgrPtr[1] = b0;
                                    bgrPtr[0] = b0;
                                }
                                else if (uv < minDepth+d)
                                {
                                    bgrPtr[2] = b0;
                                    bgrPtr[1] = (byte)(4*gv);
                                    bgrPtr[0] = bx;
                                }
                                else if (uv < minDepth+2*d)
                                {
                                    bgrPtr[2] = b0;
                                    bgrPtr[1] = bx;
                                    bgrPtr[0] = (byte)(-4 * gv + 2 * 255);
                                }
                                else if (uv < minDepth+3*d)
                                {
                                    bgrPtr[2] = (byte)(4 * gv - 2 * 255);
                                    bgrPtr[1] = bx;
                                    bgrPtr[0] = b0;
                                }
                                else if(uv<maxDepth)
                                {
                                    bgrPtr[2] = bx;
                                    bgrPtr[1] = (byte)(-4 * gv + 4 * 255);
                                    bgrPtr[0] = b0;
                                }
                                else
                                {
                                    bgrPtr[2] = bx;
                                    bgrPtr[1] = bx;
                                    bgrPtr[0] = bx;
                                }
                                bgrPtr += 3;
                            }
                            bgrPtr += rowOffset;
                        }
                        
                        bmp.UnlockBits(bmpData);
                    }
                }
            }

            if (bmp != null)
            {
                pictureBox1.Image = bmp;
                pictureBox1.Refresh();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(sensorStatus==SensorStatus.Opened)
            {
                if(bmp!=null)
                {
                    bmp.Dispose();
                }

                if(depthFrameReader!=null)
                {
                    depthFrameReader.Dispose();
                }

                if(kinectSensor!=null)
                {
                    kinectSensor.Close();
                }
            }
        }

    }
}
