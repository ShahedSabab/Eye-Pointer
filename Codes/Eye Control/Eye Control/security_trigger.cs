using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Eye_Control
{
    public partial class security_trigger : Form
    {
        SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        SpeechSynthesizer speech;
        private Boolean check;
        private Capture cap;
        private int counter;
        public Boolean corr_flag=false;
        public int key = 0;

        Timer t = new Timer();
        private HaarCascade haar = new HaarCascade("haarcascade_frontalface_default.xml");


        public security_trigger()
        {
            InitializeComponent();
            initialization();
            VoiceInitialize();
        }

        public void initialization()
        {
            check = false;
        }
        public void VoiceInitialize()
        {
            Choices sList = new Choices();
            sList.Add(new string[] { "terminate security access mode", "eye pointer" });
            Grammar gr = new Grammar(new GrammarBuilder(sList));


            try
            {
                sRecognize.RequestRecognizerUpdate();
                sRecognize.LoadGrammar(gr);
                sRecognize.SpeechRecognized += password_Speechrecognized;
                sRecognize.SetInputToDefaultAudioDevice();
                sRecognize.RecognizeAsync(RecognizeMode.Multiple);

            }
            catch
            {
                return;
            }
        }

        private void Run(object sender, EventArgs e)
        {
            int ContTrain;
            int temp;
            Image<Gray, byte> gray = null;
            Image<Bgr, byte> TrainedFace = null;
            Image<Bgr, byte> newFrame;
            newFrame = cap.QueryFrame().Resize(200, 200, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            gray = cap.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            //currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
            string[] Labels = Labelsinfo.Split('%');
            ContTrain = Convert.ToInt16(Labels[0]);
            temp = ContTrain;

            //Face Detector
            MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
            haar,
            1.015,
            10,
            Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
            new Size(20, 20));

            //Action for each element detected
            try
            {
                foreach (MCvAvgComp f in facesDetected[0])
                {
                    TrainedFace = newFrame.Convert<Bgr, byte>();
                    ContTrain++;
                    break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (ContTrain > temp)
            {
                cap.Stop();
                cap.Dispose();

                File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", ContTrain.ToString() + "%");
                TrainedFace.Save(Application.StartupPath + "/TrainedFaces/face" + ContTrain + ".bmp");

                string s;
                Image im = Image.FromFile(Application.StartupPath + "/TrainedFaces/face" + ContTrain + ".bmp");
                ImageConverter _imageConverter = new ImageConverter();
                byte[] xByte = (byte[])_imageConverter.ConvertTo(im, typeof(byte[]));
                s = Convert.ToBase64String(xByte);


                MemoryStream mem = new MemoryStream(xByte, 0, xByte.Length);

                using (WebClient client = new WebClient())
                {
                    System.Collections.Specialized.NameValueCollection reqparm = new System.Collections.Specialized.NameValueCollection();
                    reqparm.Add("param1", "<any> kinds & of = ? strings");
                    reqparm.Add("param2", s);
                    try
                    {
                        byte[] responsebytes = client.UploadValues("http://www.eyepointer.tk/sabab/connection/insert_pic.php", "POST", reqparm);
                        string responsebody = Encoding.UTF8.GetString(responsebytes);
                        Console.WriteLine(responsebody);
                        client.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                }


                Application.Idle -= new EventHandler(Run);
         

                if(Form1.sms_button_switch==true)
                {
                    sms(); //send message
                }
                runOnBackground();
            }
        }

        private void security_trigger_MouseClick(object sender, MouseEventArgs e)
        {
            cap = new Capture(0);
            Application.Idle += new EventHandler(Run);
        }

        private void security_trigger_KeyUp(object sender, KeyEventArgs e)
        {
            cap = new Capture(0);
            Application.Idle += new EventHandler(Run);
        }

        public void runOnBackground()
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void password_Speechrecognized(object sender, SpeechRecognizedEventArgs e)
        {
 
            if (e.Result.Text == "terminate security access mode")
            {
                speech = new SpeechSynthesizer();
                speech.SpeakAsync("Say The Password");
                check = true;
                InitializeTimer();
            }

            else if (e.Result.Text == "eye pointer")
            {
                if (check == true)
                {
                    corr_flag = true;
                    sRecognize.SpeechRecognized -= password_Speechrecognized;
                    this.WindowState = FormWindowState.Maximized;
                    new Form1().Show();
                    this.Close();
                }
            }
            else
            {
                if (check == true)
                {
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Password is not matched. Action Denied");
                }
                else
                {
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Invalid Command. Use The Manual Words");
                }
            }
        }

        private void InitializeTimer()
        {
            counter = 0;
            t.Interval = 1000;

            DoMything();
            t.Tick += new EventHandler(securityPasswordChecker_Tick);
            t.Enabled = true;
        }

        private void securityPasswordChecker_Tick(object sender, EventArgs e)
        {
            if (counter >= 5 && corr_flag==false)
            {
                t.Enabled = false;
                speech = new SpeechSynthesizer();
                speech.SpeakAsync("Unable to receive correct password within time limit");
                check = false;
                t.Tick -= new EventHandler(securityPasswordChecker_Tick);
                counter = 0;
            }
            else
            {
                //do something here 
                counter++;
                DoMything();
            }
        }


        private void DoMything()
        {
            //Do you stuff here
        }

        public void sms()
        {
            string sendSMS = "Warning!!! Unauthorized access is found on your computer. Please check 'Pointer Security' app and take necessary action.";
            SerialPort SP = new SerialPort();
            string ph_no;
            string[] ports = SerialPort.GetPortNames();


            foreach (string port in ports)
            {
                try
                {
                    SP.PortName = port;
                    SP.Open();
                    //string ph_no;
                    ph_no = Char.ConvertFromUtf32(34) + Form1.mobile_num + Char.ConvertFromUtf32(34);
                    SP.Write("AT+CMGF=1" + Char.ConvertFromUtf32(13));
                    SP.Write("AT+CMGS=" + ph_no + Char.ConvertFromUtf32(13));
                    SP.Write(sendSMS + Char.ConvertFromUtf32(26) + Char.ConvertFromUtf32(13));
                    SP.Close();
                }
                catch (Exception ex) { }


            }  
        }

        private void password_check_Tick(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }


        private void security_trigger_Resize(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "!!!";
            notifyIcon1.BalloonTipText = "Alert";
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                //notifyIcon1.ShowBalloonTip(30);
                //Application.Idle += new EventHandler(Run);
                //this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void security_trigger_Load(object sender, EventArgs e)
        {
            Icon.Dispose();
        }
    }
}
