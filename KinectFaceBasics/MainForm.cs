using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.IO;
using System.Drawing.Imaging;

namespace KinectFaceBasics
{
    public partial class MainForm : Form
    {
        private KinectSensor kinectSensor = null;
        private ColorFrameReader colorFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        IList<Body> bodies = null;

        private FaceFrameSource faceFrameSource = null;
        private FaceFrameReader faceFrameReader = null;

        private enum SensorStatus { Opened, Closed };
        private SensorStatus sensorStatus = SensorStatus.Closed;

        private enum SensorOperation { Open, Close };
        private SensorOperation sensorOperation = SensorOperation.Open;

        private string capFolder = "Capture/";

        private Bitmap bmp = null;

        private int eyeLX = 0;
        private int eyeLY = 0;
        private int eyeRX = 0;
        private int eyeRY = 0;

        private int noseX = 0;
        private int noseY = 0;

        private int mouthLX = 0;
        private int mouthLY = 0;
        private int mouthRX = 0;
        private int mouthRY = 0;

        int idx = 0;
        bool faceDetected = false;

        public MainForm()
        {
            InitializeComponent();

            kinectSensor = KinectSensor.GetDefault();

            if (kinectSensor == null)
            {
                throw new Exception("Failed to open KinectSensor!");
            }

            if (!Directory.Exists(capFolder))
            {
                Directory.CreateDirectory(capFolder);
            }

            textBox1.Text = "READY";
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
            catch (Exception ex)
            {
                textBox1.Text = ex.Message;
            }
        }

        private void btnCaptureImage_Click(object sender, EventArgs e)
        {
            if (bmp == null)
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
            if (sensorStatus == SensorStatus.Closed)
            {
                kinectSensor.Open();

                bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];

                colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
                colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

                bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

                FaceFrameFeatures fff = FaceFrameFeatures.BoundingBoxInColorSpace |
                    FaceFrameFeatures.FaceEngagement |
                    FaceFrameFeatures.Glasses |
                    FaceFrameFeatures.Happy |
                    FaceFrameFeatures.LeftEyeClosed |
                    FaceFrameFeatures.MouthOpen |
                    FaceFrameFeatures.PointsInColorSpace |
                    FaceFrameFeatures.RightEyeClosed;

                faceFrameSource = new FaceFrameSource(kinectSensor, 0, fff);

                faceFrameReader = faceFrameSource.OpenReader();
                faceFrameReader.FrameArrived += faceFrameReader_FrameArrived;

                sensorStatus = SensorStatus.Opened;
            }
        }

        private void SafeCloseSensor()
        {
            if (sensorStatus == SensorStatus.Opened)
            {
                kinectSensor.Close();

                colorFrameReader.Dispose();
                faceFrameSource.Dispose();
                faceFrameReader.Dispose();

                if (bmp != null)
                {
                    bmp.Dispose();
                    bmp = null;
                }
                pictureBox1.Image = null;
                pictureBox1.Refresh();

                sensorStatus = SensorStatus.Closed;
            }
        }

        private void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame == null)
                {
                    return;
                }

                using (KinectBuffer buffer = frame.LockRawImageBuffer())
                {
                    int width = frame.FrameDescription.Width;
                    int height = frame.FrameDescription.Height;
                    Rectangle rect = new Rectangle(0, 0, width, height);
                    uint size = (uint)(width * height * 4);

                    if (bmp == null)
                    {
                        bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    }

                    BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    frame.CopyConvertedFrameDataToIntPtr(bmpData.Scan0, size, ColorImageFormat.Bgra);
                    bmp.UnlockBits(bmpData);
                    if (faceDetected)
                    {
                        Graphics g = Graphics.FromImage(bmp);
                        //Pen pen = new Pen(Brushes.Red, 3.0f);
                        g.FillEllipse(Brushes.Red, eyeLX - 30, eyeLY - 20, 60, 40);
                        g.FillEllipse(Brushes.Red, eyeRX - 30, eyeRY - 20, 60, 40);
                        g.FillEllipse(Brushes.Red, noseX - 20, noseY - 30, 40, 40);
                        g.FillEllipse(Brushes.Red, mouthLX, mouthLY-20, mouthRX-mouthLX, 40);
                        g.Save();
                    }
                }
            }

            if (bmp != null)
            {
                pictureBox1.Image = bmp;
                pictureBox1.Refresh();
            }
        }

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (!faceFrameSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            faceFrameSource.TrackingId = body.TrackingId;
                        }
                    }
                }
            }
        }

        void faceFrameReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    ++idx;

                    FaceFrameResult result = faceFrame.FaceFrameResult;

                    if (result != null)
                    {
                        var eyeLeft = result.FacePointsInColorSpace[FacePointType.EyeLeft];
                        var eyeRight = result.FacePointsInColorSpace[FacePointType.EyeRight];
                        var nose = result.FacePointsInColorSpace[FacePointType.Nose];
                        var mouthLeft = result.FacePointsInColorSpace[FacePointType.MouthCornerLeft];
                        var mouthRight = result.FacePointsInColorSpace[FacePointType.MouthCornerRight];

                        eyeLX = (int)eyeLeft.X;
                        eyeLY = (int)eyeLeft.Y;
                        eyeRX = (int)eyeRight.X;
                        eyeRY = (int)eyeRight.Y;

                        noseX = (int)nose.X;
                        noseY = (int)nose.Y;

                        mouthLX = (int)mouthLeft.X;
                        mouthLY = (int)mouthLeft.Y;
                        mouthRX = (int)mouthRight.X;
                        mouthRY = (int)mouthRight.Y;

                        faceDetected = true;
                    }
                    else
                    {
                        faceDetected = false;
                    }
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sensorStatus == SensorStatus.Opened)
            {
                if (bmp != null)
                {
                    bmp.Dispose();
                }

                if (colorFrameReader != null)
                {
                    colorFrameReader.Dispose();
                    colorFrameReader = null;
                }

                if (bodyFrameReader != null)
                {
                    bodyFrameReader.Dispose();
                    bodyFrameReader = null;
                }

                if (faceFrameReader != null)
                {
                    faceFrameReader.Dispose();
                    faceFrameReader = null;
                }

                if (faceFrameSource != null)
                {
                    faceFrameSource.Dispose();
                    faceFrameSource = null;
                }

                if (kinectSensor != null)
                {
                    kinectSensor.Close();
                }
            }
        }
    }
}
