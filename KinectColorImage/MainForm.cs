using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Kinect; //!!!
 
namespace KinectColorImage
{
    public partial class MainForm : Form
    {
        #region Members
 
        private KinectSensor kinectSensor = null;
        private ColorFrameReader colorFrameReader = null;
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
 
            colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
 
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
 
        private void colorFrameReader_FrameArrived(object sender,ColorFrameArrivedEventArgs e)
        {
            using(ColorFrame frame=e.FrameReference.AcquireFrame())
            {
                if (frame == null)
                {
                    return;
                }
 
                using(KinectBuffer buffer=frame.LockRawImageBuffer())
                {
                    int width = frame.FrameDescription.Width;
                    int height = frame.FrameDescription.Height;
                    Rectangle rect = new Rectangle(0, 0, width, height);
                    uint size = (uint)(width * height * 4);
 
                    if(bmp==null)
                    {         
                        bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    }
                    
                    BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);            
                    frame.CopyConvertedFrameDataToIntPtr(bmpData.Scan0, size, ColorImageFormat.Bgra);
                    bmp.UnlockBits(bmpData);
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
 
                if(colorFrameReader!=null)
                {
                    colorFrameReader.Dispose();
                }
 
                if(kinectSensor!=null)
                {
                    kinectSensor.Close();
                }
            }
        }
 
    }
}