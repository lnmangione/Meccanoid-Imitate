using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//for Kinect (duh)
using Microsoft.Kinect;
//for serial com with Arduino
using System.IO.Ports;
//for timer usage
using System.Timers;

namespace FirstWPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private BodyFrameReader bodyReader;
        private SerialPort sp = new SerialPort();
        private int framesRead;

        // Array for the bodies, index for the currently tracked body, flag to assess if a body is currently tracked
        private Body[] bodies = null;
        private int bodyIndex;
        private bool bodyTracked = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.sensor = KinectSensor.GetDefault();
            this.sensor.Open();

            this.bodyReader = this.sensor.BodyFrameSource.OpenReader();
            this.bodyReader.FrameArrived += OnFrameArrived;

            //Arduino serial com set up
            try
            {
                sp.PortName = "COM3";
                sp.BaudRate = 9600;
                sp.Open();

                framesRead = 0;

                Timer serialTimer = new Timer(50);
                serialTimer.Elapsed += new ElapsedEventHandler(ResetSerial);
                serialTimer.Enabled = true;
                serialTimer.Start();

            }
            catch (Exception)
            {
                MessageBox.Show("Please give a valid port number or check your connection");
            }
        }

        //Reset the serial out buffer every 50 ms
        private void ResetSerial(object source, ElapsedEventArgs e)
        {
            sp.DiscardOutBuffer();
        }

        void OnFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                Body body = null;
                if (this.bodyTracked)
                {
                    if (this.bodies[this.bodyIndex].IsTracked)
                    {
                        body = this.bodies[this.bodyIndex];
                    }
                    else
                    {
                        bodyTracked = false;
                    }
                }
                if (!bodyTracked)
                {
                    for (int i = 0; i < this.bodies.Length; ++i)
                    {
                        if (this.bodies[i].IsTracked)
                        {
                            this.bodyIndex = i;
                            this.bodyTracked = true;
                            break;
                        }
                    }
                }

                if (body != null && this.bodyTracked && body.IsTracked)
                {
                    // body is currently being tracked
                    framesRead++;

                    //prepare data for output every 10 frames
                    if (framesRead % 10 == 0)
                    {
                        String data = "";

                        data += trackNeck(body);
                        data += trackLeftArm(body);

                        //send data to Arduino
                        sp.WriteLine(data);
                    }
                }
            }
        }

        //interpret neck movement
        String trackNeck(Body body)
        {
            //string to return angle data
            String data = "H";

            Joint head = body.Joints[JointType.Head];
            Joint neck = body.Joints[JointType.Neck];

            //Kinect too inaccurate for exact neck angle, so set angle to 1 of 3 set values
            double dx = head.Position.X - neck.Position.X;
            double dy = head.Position.Y - neck.Position.Y;
            double angleRadians = Math.Atan(dy / dx);
            double angle = angleRadians / Math.PI * 180;

            if (angle > 0 && angle < 65){
                data += "120";
            }
            else if(angle < 0 && angle > -65)
            {
                data += "60";
            }
            else
            {
                data += "90";
            }

            data += "/";
            return data;
        }

        //interpret left arm movement
        String trackLeftArm(Body body)
        {
            //string to return angle data
            String data = "L";

            Joint leftShoulder = body.Joints[JointType.ShoulderLeft];
            Joint leftElbow = body.Joints[JointType.ElbowLeft];

            double deltaX = leftShoulder.Position.X - leftElbow.Position.X;
            double deltaY = leftShoulder.Position.Y - leftElbow.Position.Y;
            double thetaRadians = Math.Atan(deltaX / deltaY);
            double theta = thetaRadians / Math.PI * 180;

            //convert theta to more friendly angle
            double angle;
            if (theta < 0)
            {
                angle = -1 * theta;
            }
            else
            {
                angle = ((1.0 - (theta / 90.0)) * 90) + 90;
            }

            data += Math.Truncate(angle);
            data += "/";
            return data;
        }
    }
}