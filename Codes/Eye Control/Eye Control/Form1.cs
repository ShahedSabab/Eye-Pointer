using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace Eye_Control
{
    public partial class Form1 : MetroForm
    {

        //kalman initialize
        #region Variables
        float px, py, cx, cy, ix, iy;
        float ax, ay;
        #endregion

        #region Kalman Filter and Poins Lists
        PointF[] oup = new PointF[2];
        private Kalman kal;
        private SyntheticData syntheticData;
        private List<PointF> mousePoints;
        private List<PointF> kalmanPoints;
        #endregion

        #region Timers
        Timer MousePositionTaker = new Timer();
        Timer KalmanOutputDisplay = new Timer();
        #endregion
        //****kalman initialize


        private Capture cap;
        private HaarCascade haar;
        private HaarCascade eye;
        public int maxthreshold = 220;
        public int minthreshold = 100;
        public int kalman_k;
        public static string mobile_num;

        int width;   //for NAV
        int height;   //for NAV
        int xCoord;     //x coordinate to sync pointer
        int yCoord;     //y coordinate to sync pointer
        int ContTrain;  //for showing security images
        int Maxval;     //checking gmax stored pic number
        double eye_scale_rate;    //for setting eye detection rate
        int eye_detection;     //for setting eye detection rate
        double face_scale_rate;   //for setting face detection rate
        int face_detection;    //for setting face detection rate
        int prexCoord = 0;
        int preyCoord = 0;

        /// <password check timer>
        private int counter;
        private int counter_break;
        Timer t = new Timer();
        /// </password check timer>

        /// <flags>
        Boolean processed_image_flag;
        Boolean cursor_frame_flag;
        Boolean cursor_position_flag;

        public Boolean cursor_mode_flag;
        public Boolean multi_mode_flag;
        public Boolean security_mode_flag;
        public Boolean play_video_flag;
        public Boolean pause_video_flag;
        public Boolean picture_view_flag;
        public Boolean blink_control_flag ;
        public Boolean password_match_flag;
        public static Boolean sms_button_switch;

        /// </flags>

        MCvAvgComp[][] eyes;


        [DllImport("user32.dll")]
        static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int MOUSEEVENTF_SIDEUP = 0x0100;
        private const int MOUSEEVENTF_SIDEDOWN = 0x0080;

        private const int VK_RMENU = 0xA5;
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        private const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        private const int VK_LCONTROL = 0xA2; //Left Control key code
        private const int VK_LWIN = 0x5B;     //windows button

        SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        SpeechRecognitionEngine cursorModeVoiceEngine = new SpeechRecognitionEngine();
        SpeechRecognitionEngine multiModeVoiceEngine = new SpeechRecognitionEngine();
        SpeechRecognitionEngine securityModeVoiceEngine = new SpeechRecognitionEngine();
        SpeechSynthesizer speech;

        public Form1()
        {
            InitializeComponent();
            initialize();
            KalmanFilter(); //initialize kalman filter
            controlSecurity();
        }


        public void initialize()
        {

            //initialize form property
            cursor_notification.Visible = true;
            play_button.Enabled = false;
            //*****initialize form property*****

            kalman_k = 1000000;
            mobile_num = "01719151700";

            //settings 

            eye_scalerate_track.Value = 10;
            eye_detection_track.Value = 2;

            eye_scale_rate = 1 + (0.015 * eye_scalerate_track.Value);
            eye_detection = eye_detection_track.Value;

            face_scalerate_track.Value = 10;
            face_detection_track.Value = 10;

            face_scale_rate=1+(0.015 * face_scalerate_track.Value);
            face_detection = face_detection_track.Value;

            //****settings

            //flags
            Boolean processed_image_flag = false;
            Boolean cursor_frame_flag = false;
            Boolean cursor_position_flag = false;

            //mode selection
            cursor_mode_flag = true;
            multi_mode_flag = false;
            security_mode_flag = false;

            play_video_flag = true;
            pause_video_flag = false;
            picture_view_flag = false;
            blink_control_flag = false;
            sms_button_switch = false;
            password_match_flag = false;

            //***flags***

            width = 9;
            height = 9;
            pointer_movement_trackbar.Value = width;
            pointer_speed_trackbar.Value = kalman_k;
            mob_number.Text = mobile_num;
            sms_switch.Checked = sms_button_switch;
            dataset_combo.SelectedItem="Dataset 1";
            cursor_position_check.Checked = true;

            haar = new HaarCascade("haarcascade_frontalface_default.xml");
            eye = new HaarCascade("haarcascade_eye.xml");


            voice_initialize();

            Application.Idle += new EventHandler(Run);   //video manipulation 
            try
            {
                cap = new Capture(0);
            }
            catch
            {
                MessageBox.Show("No Video Intput devices found");
                Application.Exit();
            }
       
        }


        public void voice_initialize()
        {
            if (cursor_mode_flag == true)
            {
                Choices sList1 = new Choices();
                sList1.Add(new string[] { "start the process", "increase pointer movement", "decrease pointer movement", "increase pointer speed", "decrease pointer speed", "activate multi mode", "activate security access mode", "hide the application", "show the application", "freeze", "click", "open", "single click", "halt the programme", "close the programme", "go back", "stop moving cursor", "move mouse cursor", "check processed image", "check cursor frame", "uncheck processed image", "uncheck cursor frame", "activate blink control", "deactivate blink control", "right click", "show cursor coordinate", "hide cursor coordinate","close current window", "maximize current window","minimize current window"});
                Grammar gr1 = new Grammar(new GrammarBuilder(sList1));
                try
                {
                    cursorModeVoiceEngine.RequestRecognizerUpdate();
                    cursorModeVoiceEngine.LoadGrammar(gr1);
                    cursorModeVoiceEngine.SpeechRecognized += cursorRecognizeSpeech;
                    cursorModeVoiceEngine.SetInputToDefaultAudioDevice();
                    cursorModeVoiceEngine.RecognizeAsync(RecognizeMode.Multiple);

                }
                catch
                {
                    return;
                }
            }

            else if (multi_mode_flag == true)
            {
                Choices sList2 = new Choices();
                sList2.Add(new string[] { "slide", "halt the programme", "show the application", "hide the application", "move mouse cursor", "close the programme", "activate cursor control mode", "activate security access mode", "stop", "stop moving cursor", "freeze", "picture view", "deactivate picture view", "next frame", "previous frame", "upper stroke", "lower stroke", "zoom in", "zoom out", "enter", "escape", "pause the video", "start the video", "close current window", "maximize current window", "minimize current window" ,"start the process"});
                Grammar gr2 = new Grammar(new GrammarBuilder(sList2));
                try
                {
                    multiModeVoiceEngine.RequestRecognizerUpdate();
                    multiModeVoiceEngine.LoadGrammar(gr2);
                    multiModeVoiceEngine.SpeechRecognized += multiRecognizeSpeech;
                    multiModeVoiceEngine.SetInputToDefaultAudioDevice();
                    multiModeVoiceEngine.RecognizeAsync(RecognizeMode.Multiple);

                }
                catch
                {
                    return;
                }
            }
            else if (security_mode_flag==true)
            {
                Choices sList3 = new Choices();
                sList3.Add(new string[] {"eye pointer"});
                Grammar gr3 = new Grammar(new GrammarBuilder(sList3));
                try
                {
                    securityModeVoiceEngine.RequestRecognizerUpdate();
                    securityModeVoiceEngine.LoadGrammar(gr3);
                    securityModeVoiceEngine.SpeechRecognized += securityRecognizeSpeech;
                    securityModeVoiceEngine.SetInputToDefaultAudioDevice();
                    securityModeVoiceEngine.RecognizeAsync(RecognizeMode.Multiple);

                }
                catch
                {
                    return;
                }
            }

        }

        private void KalmanFilterRunner(object sender, EventArgs e)
        {
            PointF inp = new PointF(ix, iy);
            oup = new PointF[2];
            oup = filterPoints(inp);
            PointF[] pts = oup;
        }
        public PointF[] filterPoints(PointF pt)
        {
            syntheticData.state[0, 0] = pt.X;
            syntheticData.state[1, 0] = pt.Y;
            Matrix<float> prediction = kal.Predict();
            PointF predictPoint = new PointF(prediction[0, 0], prediction[1, 0]);
            PointF measurePoint = new PointF(syntheticData.GetMeasurement()[0, 0],
                syntheticData.GetMeasurement()[1, 0]);
            Matrix<float> estimated = kal.Correct(syntheticData.GetMeasurement());
            PointF estimatedPoint = new PointF(estimated[0, 0], estimated[1, 0]);
            syntheticData.GoToNextState();
            PointF[] results = new PointF[2];
            results[0] = predictPoint;
            results[1] = estimatedPoint;
            px = predictPoint.X;
            py = predictPoint.Y;
            cx = estimatedPoint.X;
            cy = estimatedPoint.Y;
            return results;
        }
        public void KalmanFilter()
        {
            mousePoints = new List<PointF>();
            kalmanPoints = new List<PointF>();
            kal = new Kalman(4, 2, 0);
            syntheticData = new SyntheticData();
            Matrix<float> state = new Matrix<float>(new float[]
            {
                0.0f, 0.0f, 0.0f, 0.0f
            });
            kal.CorrectedState = state;
            kal.TransitionMatrix = syntheticData.transitionMatrix;
            kal.MeasurementNoiseCovariance = syntheticData.measurementNoise;
            kal.ProcessNoiseCovariance = syntheticData.processNoise;
            kal.ErrorCovariancePost = syntheticData.errorCovariancePost;
            kal.MeasurementMatrix = syntheticData.measurementMatrix;
        }
        private void MousePositionRecord(object sender, EventArgs e)
        {
            Random rand = new Random();
            ix = (int)ax;
            iy = (int)ay;
            //MouseTimed_LBL.Text = "Mouse Position Timed- X:" + ix.ToString() + " Y:" + iy.ToString();
        }

        private void InitialiseTimers(int Timer_Interval = 1000)
        {
            MousePositionTaker.Interval = Timer_Interval;
            MousePositionTaker.Tick += new EventHandler(MousePositionRecord);
            MousePositionTaker.Start();
            KalmanOutputDisplay.Interval = Timer_Interval;
            KalmanOutputDisplay.Tick += new EventHandler(KalmanFilterRunner);
            KalmanOutputDisplay.Start();
        }
        private void StopTimers()
        {
            MousePositionTaker.Tick -= new EventHandler(MousePositionRecord);
            MousePositionTaker.Stop();
            KalmanOutputDisplay.Tick -= new EventHandler(KalmanFilterRunner);
            KalmanOutputDisplay.Stop();
        }

        private void cursorPosition(object sender, EventArgs arg)
        {
            cursor_position_textbox.AppendText("\nx : " + xCoord + ",  y : " + yCoord + Environment.NewLine);
            var screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            var width = screen.Width;
            var height = screen.Height;

            if (picture_view_flag == true)
            {
                if (xCoord < 200)
                {
                    KeyDown(Keys.Left);
                    KeyUp(Keys.Left);
                }
                else if (xCoord > width-200)
                {
                    KeyDown(Keys.Right);
                    KeyUp(Keys.Right);
                }

            }
           else
           {
                Point cur_point = new Point(xCoord, yCoord);
                PointF pre_point;
                PointF est_point;
                ////////////////////////
                PointF temp1;
                PointF temp2;
                double diff;

                ///////////////////////
                PointF[] results = new PointF[2];
                results = filterPoints(cur_point);
                pre_point = results[0];
                est_point = results[1];

 
                int distence = Convert.ToInt32(Math.Sqrt(((prexCoord - Convert.ToInt32(est_point.X)) * (prexCoord - Convert.ToInt32(est_point.X))) + ((preyCoord - Convert.ToInt32(est_point.Y)) * (preyCoord - Convert.ToInt32(est_point.Y)))));

                if (distence > 30)
                {
                    prexCoord = Convert.ToInt32(est_point.X);
                    preyCoord = Convert.ToInt32(est_point.Y);
                }

                Cursor.Position = new Point(prexCoord, preyCoord);
               // MoveTo(prexCoord * 40, preyCoord * 70);
               
            }
        }

        private void InitializeTimer()

        {
            counter = 0;
            t.Interval = 1000;

            DoMything();
            t.Tick += new EventHandler(passwordChecker_Tick);
            t.Enabled = true;
        }

        private void passwordChecker_Tick(object sender, EventArgs e)
        {
            if (counter >= 10)
            {
                securityModeVoiceEngine.SpeechRecognized -= securityRecognizeSpeech;
                cursorModeVoiceEngine.SpeechRecognized -= cursorRecognizeSpeech;
                multiModeVoiceEngine.SpeechRecognized -= multiRecognizeSpeech;

                multi_notification.Visible = false;
                cursor_notification.Visible = true;
                security_notification.Visible = false;
                cursor_mode_flag = true;
                security_mode_flag = false;
                multi_mode_flag = false;
                voice_initialize();
                t.Enabled = false;
                if (password_match_flag == false)
                {
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Unable to receive correct password");
                    speech.SpeakAsync("Switching back to cursor control mode");
                }
                t.Tick -= new EventHandler(passwordChecker_Tick);
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
            // stuff here
        }

        private void cursorRecognizeSpeech(object sender, SpeechRecognizedEventArgs e)
        {
            if (pause_video_flag == false)
            {
                try
                {
                    current_voice_checkbox.Text = e.Result.Text;
                    cursor_position_textbox.AppendText("x : " + xCoord + ",  y : " + yCoord + "   ###Command : " + e.Result.Text + Environment.NewLine);
                    voicelist_checkbox.AppendText(e.Result.Text + Environment.NewLine);
                }
                catch (Exception ex) { }
            }

            switch (e.Result.Text)
            {
                case "start the process":
                    timer1.Start();
                    pause_video_flag = false;
                    if (pause_video_flag == false)
                    {
                        timer1.Start();
                        play_button_Click(this, null);
                    }
                    break;

                case "halt the programme":
                    timer1.Stop();
                    eyecontrol_switch.Checked = false;
                    blinkcontrol_switch.Checked = false;
                    pause_button_Click(this, null);
                    pause_video_flag = true;
                    Application.Idle -= cursorPosition;
                    break;

                case "move mouse cursor":
                    if (pause_video_flag == false)
                    {
                        eyecontrol_switch.Checked = true;
                        eyecontrol_switch_CheckedChanged(this, null);
                    }
                    break;

                case "stop moving cursor":
                case "freeze":
                    if (pause_video_flag == false)
                    {
                        eyecontrol_switch.Checked = false;
                        eyecontrol_switch_CheckedChanged(this, null);
                    }
                    break;

                case "activate blink control":
                    blinkcontrol_switch.Checked = true;
                    blinkcontrol_switch_CheckedChanged(this, null);
                    break;

                case "deactivate blink control":
                    blinkcontrol_switch.Checked = false;
                    blinkcontrol_switch_CheckedChanged(this, null);
                    break;
   
                case "hide the application":
                    if (pause_video_flag == false)
                    {
                        this.WindowState = FormWindowState.Minimized;
                    }
                    break;
                case "show the application":
                    if (pause_video_flag == false)
                    {
                        this.WindowState = FormWindowState.Maximized;
                    }
                    break;
                case "decrease pointer movement":
                    if (pointer_movement_trackbar.Value > pointer_movement_trackbar.Minimum)
                    {
                        pointer_movement_trackbar.Value -= 1;
                        width = pointer_movement_trackbar.Value;
                        height = pointer_movement_trackbar.Value;
                    }
                    break;
                case "increase pointer movement":
                    if (pointer_movement_trackbar.Value < pointer_movement_trackbar.Maximum)
                    {
                        pointer_movement_trackbar.Value += 1;
                        width = pointer_movement_trackbar.Value;
                        height = pointer_movement_trackbar.Value;
                    }
                    break;
                case "increase pointer speed":
                    if (pointer_speed_trackbar.Value < pointer_speed_trackbar.Maximum)
                    {
                        pointer_speed_trackbar.Value += 10000;
                        kalman_k = pointer_speed_trackbar.Value;
                    }
                    break;
                case "decrease pointer speed":
                    if (pointer_speed_trackbar.Value > pointer_speed_trackbar.Minimum)
                    {
                        pointer_speed_trackbar.Value -= 10000;
                        kalman_k = pointer_speed_trackbar.Value;
                    }
                    break;

                case "show cursor coordinate":
                    cursor_position_textbox.Visible = true;
                    break;
                case "hide cursor coordinate":
                    cursor_position_textbox.Visible = false;
                    break;
                case "check cursor frame":
                    cursor_frame_check.Checked = true;
                    cursor_frame_check_CheckedChanged(this, null);
                    break;
                case "uncheck cursor frame":
                    cursor_frame_check.Checked = false;
                    cursor_frame_check_CheckedChanged(this, null);
                    break;
                case "check processed image":
                    processed_image_check.Checked = true;
                    processed_image_check_CheckedChanged(this, null);
                    break;
                case "uncheck processed image":
                    processed_image_check.Checked = false;
                    processed_image_check_CheckedChanged(this, null);
                    break;
                case "single click":
                    if (pause_video_flag == false)
                    {
                        DoMouseClick();
                        left_click_notification.Visible = true;
                        timer2.Start();
                    }
                    break;
                case "click":
                case "open":
                    if (blink_control_flag == false && pause_video_flag == false)
                    {
                        DoubleMouseClick();
                        double_click_notification.Visible = true;
                        timer2.Start();
                    }
                    break;
                case "right click":
                    if (pause_video_flag == false)
                    {
                        RightMouseClick();
                        right_click_notification.Visible = true;
                        timer2.Start();
                    }
                    break;
                case "go back":
                    if (pause_video_flag == false)
                    {
                        KeyDown(Keys.Back);
                        KeyUp(Keys.Back);
                    }
                    break;

                case "close the programme":
                    Application.Exit();
                    break;

                case "close current window":
                    AdjoinKey(VK_RMENU,Keys.F4);
                    break;

                case "maximize current window":
                    AdjoinKey(VK_LWIN,Keys.Up);
                    break;

                case "minimize current window":
                    AdjoinKey(VK_LWIN, Keys.Down);
                    AdjoinKey(VK_LWIN, Keys.Down);
                    break;

                case "activate multi mode":
                    cursorModeVoiceEngine.SpeechRecognized -= cursorRecognizeSpeech;
                    securityModeVoiceEngine.SpeechRecognized -= securityRecognizeSpeech;
                    multiModeVoiceEngine.SpeechRecognized -= multiRecognizeSpeech;
                    security_mode_flag = false;
                    cursor_mode_flag = false;
                    multi_mode_flag = true;
                    voice_initialize();
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Multi Mode Activated");
                    multi_notification.Visible = true;
                    cursor_notification.Visible = false;
                    security_notification.Visible = false;
                    blink_control_flag = false;
                    break;
                case "activate security access mode":
                    securityModeVoiceEngine.SpeechRecognized -= securityRecognizeSpeech;
                    cursorModeVoiceEngine.SpeechRecognized -= cursorRecognizeSpeech;
                    multiModeVoiceEngine.SpeechRecognized -= multiRecognizeSpeech;
                    cursor_mode_flag = false;
                    security_mode_flag = true;
                    multi_mode_flag = false;
                    voice_initialize();
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Say the password");
                    speech.SpeakAsync("You have five seconds to enter the password");
                    multi_notification.Visible = false;
                    cursor_notification.Visible = false;
                    security_notification.Visible = true;
                    blink_control_flag = false;
                    InitializeTimer();                   //password timer check
                    
                    break;
                default:
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Invalid Command. Use The Manual Words");
                    break;
            }

        }

    

        private void multiRecognizeSpeech(object sender, SpeechRecognizedEventArgs e)
        {
            if (pause_video_flag == false)
            {
                try
                {
                    current_voice_checkbox.Text = e.Result.Text;
                    cursor_position_textbox.AppendText("x : " + xCoord + ",  y : " + yCoord + "   ###Command : " + e.Result.Text + Environment.NewLine);
                    voicelist_checkbox.AppendText(e.Result.Text + Environment.NewLine);
                }
                catch (Exception ex) { }
            }

            switch (e.Result.Text)
            {                    
                case "move mouse cursor":
                    if (pause_video_flag == false && picture_view_flag==false)
                    {
                        eyecontrol_switch.Checked = true;
                        eyecontrol_switch_CheckedChanged(this, null);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                    break;

                case "stop moving cursor":
                case "freeze":
                    if (pause_video_flag == false)
                    {
                        eyecontrol_switch.Checked = false;
                        eyecontrol_switch_CheckedChanged(this, null);
                    }
                    break;

                case "halt the programme":
                    timer1.Stop();
                    eyecontrol_switch.Checked = false;
                    blinkcontrol_switch.Checked = false;
                    pause_button_Click(this, null);
                    pause_video_flag = true;
                    Application.Idle -= cursorPosition;
                    break;

                case "start the process":
                    timer1.Start();
                    pause_video_flag = false;
                    if (pause_video_flag == false)
                    {
                        timer1.Start();
                        play_button_Click(this, null);
                    }
                    break;

                case "hide the application":
                    if (pause_video_flag == false)
                    {
                        this.WindowState = FormWindowState.Minimized;
                    }
                    break;

                case "show the application":
                    if (pause_video_flag == false)
                    {
                        this.WindowState = FormWindowState.Maximized;
                    }
                    break;

                case "stop":
                    if (picture_view_flag == false)
                    {
                        DoMouseClick();
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                   
                    break;

                case "slide":
                    if (picture_view_flag == false)
                    {
                        MiddleMouseClick();
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
               
                    break;

                case "picture view":
                    Application.Idle += cursorPosition;
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Picture View Activated");
                    picture_view_flag = true;
                    break;

                case "deactivate picture view":
                    Application.Idle -= cursorPosition;
                    Application.Idle -= cursorPosition;
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Picture View Deactivated");
                    picture_view_flag =false;
                    break;

                case "next frame":
                    if (picture_view_flag == false)
                    {

                        KeyDown(Keys.Right);
                        KeyUp(Keys.Right);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                    break;

                case "previous frame":
                    if (picture_view_flag == false)
                    {

                        KeyDown(Keys.Left);
                        KeyUp(Keys.Left);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                
                    break;

                case "enter":
                    if (picture_view_flag == false)
                    {

                        KeyDown(Keys.Enter);
                        KeyUp(Keys.Enter);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }

                    break;

                case "escape":
                    if (picture_view_flag == false)
                    {

                        KeyDown(Keys.Escape);
                        KeyUp(Keys.Escape);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }

                    break;

                case "zoom in":

                    if (picture_view_flag == false)
                    {

                        KeyDown(Keys.Up);
                        KeyUp(Keys.Up);
                        KeyDown(Keys.Up);
                        KeyUp(Keys.Up);
                        KeyDown(Keys.Up);
                        KeyUp(Keys.Up);
                        KeyDown(Keys.Up);
                        KeyUp(Keys.Up);
                        KeyDown(Keys.Add);
                        KeyUp(Keys.Add);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
       
                    break;

                case "zoom out":
                    if (picture_view_flag == false)
                    {
                        KeyDown(Keys.Down);
                        KeyUp(Keys.Down);
                        KeyDown(Keys.Down);
                        KeyUp(Keys.Down);
                        KeyDown(Keys.Down);
                        KeyUp(Keys.Down);
                        KeyDown(Keys.Down);
                        KeyUp(Keys.Down);
                        KeyDown(Keys.Subtract);
                        KeyUp(Keys.Subtract);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
           
                    break;

                case "upper stroke":
                    if (picture_view_flag == false)
                    {
                        KeyDown(Keys.Up);
                        KeyUp(Keys.Up);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }

                    break;

                case "lower stroke":
                    if (picture_view_flag == false)
                    {
                        KeyDown(Keys.Down);
                        KeyUp(Keys.Down);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }

                    break;

                case "pause the video":
                case "start the video":
                    if (picture_view_flag == false)
                    {
                        KeyDown(Keys.Space);
                        KeyUp(Keys.Space);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                    break;

                case "close the programme":
                    Application.Exit();
                    break;

                case "close current window":
                    if (picture_view_flag == false)
                    {
                        AdjoinKey(VK_RMENU, Keys.F4);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                    
                    break;

                case "maximize current window":
                    if (picture_view_flag == false)
                    {
                        AdjoinKey(VK_LWIN, Keys.Up);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }

                    break;

                case "minimize current window":
                    if (picture_view_flag == false)
                    {
                        AdjoinKey(VK_LWIN, Keys.Down);
                        AdjoinKey(VK_LWIN, Keys.Down);
                    }
                    else if (picture_view_flag == true)
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                 
                    break;

                case "activate cursor control mode":
                    if (picture_view_flag == false)
                    {
                        cursorModeVoiceEngine.SpeechRecognized -= cursorRecognizeSpeech;
                        securityModeVoiceEngine.SpeechRecognized -= securityRecognizeSpeech;
                        multiModeVoiceEngine.SpeechRecognized -= multiRecognizeSpeech;
                        multi_mode_flag = false;
                        cursor_mode_flag = true;
                        voice_initialize();
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Cursor Control Mode Activated");
                        multi_notification.Visible = false;
                        cursor_notification.Visible = true;
                        security_notification.Visible = false;
                    }
                    else
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                    break;

                case "activate security access mode":
                    if (picture_view_flag == false)
                    {
                        cursorModeVoiceEngine.SpeechRecognized -= cursorRecognizeSpeech;
                        securityModeVoiceEngine.SpeechRecognized -= securityRecognizeSpeech;
                        multiModeVoiceEngine.SpeechRecognized -= multiRecognizeSpeech;
                        cursor_mode_flag = false;
                        security_mode_flag = true;
                        multi_mode_flag = false;
                        voice_initialize();
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Say the password");
                        speech.SpeakAsync("You have five seconds to enter the password");
                        multi_notification.Visible = false;
                        cursor_notification.Visible = false;
                        security_notification.Visible = true;
                        InitializeTimer();                   //password timer check
                    }
                    else
                    {
                        speech = new SpeechSynthesizer();
                        speech.SpeakAsync("Please deactivate the picture view");
                    }
                  
                    break;

                default:
                    speech = new SpeechSynthesizer();
                    speech.SpeakAsync("Invalid Command. Use The Manual Words");
                    break;
            }
        }

        private void securityRecognizeSpeech(object sender, SpeechRecognizedEventArgs e)
        {
            switch (e.Result.Text)
            {
                case "eye pointer":
                    password_match_flag = true;
                    cursorModeVoiceEngine.SpeechRecognized -= cursorRecognizeSpeech;
                    securityModeVoiceEngine.SpeechRecognized -= securityRecognizeSpeech;
                    multiModeVoiceEngine.SpeechRecognized -= multiRecognizeSpeech;
                    Application.Idle -= cursorPosition;
                    Application.Idle -= cursorPosition;
                    t.Tick -= new EventHandler(passwordChecker_Tick);

                    pause_video_flag = true;
                    cursor_mode_flag = false;
                    multi_mode_flag = false;
                    security_mode_flag = false;

                    timer1.Stop();
                    cap.Stop();
                    cap.Dispose();
                    Application.Idle -= new EventHandler(Run);
             
                    new security_trigger().Show();
                    this.Close();
                    break;
                default:
                    break;
            }
        }
        private void Run(object sender, EventArgs e)
        {

            Image<Bgr, Byte> nextFrame = cap.QueryFrame().Flip(Emgu.CV.CvEnum.FLIP.HORIZONTAL);

            using (nextFrame = cap.QueryFrame().Flip(Emgu.CV.CvEnum.FLIP.HORIZONTAL))
            {
                if (nextFrame != null)
                {
                     
                    Image<Gray, Byte> grayframe = nextFrame.Convert<Gray, Byte>();                        //Every detection is based on
                    Image<Gray, Byte> gray = nextFrame.Convert<Gray, Byte>();

                    Image<Bgr, Byte> img2 = nextFrame.Not();
                    Image<Gray, byte> gray_image = img2.Convert<Gray, byte>();
                    gray_image = gray_image.ThresholdBinary(new Gray(minthreshold), new Gray(maxthreshold));


                    Image<Gray, Byte> smallGrayFrame = gray_image.PyrDown();
                    Image<Gray, Byte> smoothedGrayFrame = smallGrayFrame.PyrUp();
                    Image<Gray, Byte> cannyFrame = smoothedGrayFrame.Canny(100, 60);

                    grayframe._EqualizeHist();

                    MCvAvgComp[][] faces =
                            grayframe.DetectHaarCascade(
                                    haar, face_scale_rate, face_detection,
                                    HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT,
                                    new Size(nextFrame.Width / 25, nextFrame.Height / 25)
                                    );


                    if (faces[0].Length == 1)
                    {
                        MCvAvgComp face = faces[0][0];
                        Int32 yCoordStartSearchEyes = face.rect.Top + (face.rect.Height * 3 / 11);
                        Point startingPointSearchEyes = new Point(face.rect.X, yCoordStartSearchEyes);
                        Size searchEyesAreaSize = new Size(face.rect.Width, (face.rect.Height * 3 / 11));
                        Rectangle possibleROI_eyes = new Rectangle(startingPointSearchEyes, searchEyesAreaSize);

                        int widthNav = (nextFrame.Width / width * 2);
                        int heightNav = (nextFrame.Height / height * 2);

                        Rectangle nav = new Rectangle(new Point(nextFrame.Width / 2 - widthNav / 2, nextFrame.Height / 2 - heightNav / 2), new Size(widthNav, heightNav));

                        if (cursor_frame_flag == true)
                        {
                            nextFrame.Draw(nav, new Bgr(Color.Lavender), 2);
                        }
                        Point cursor = new Point(face.rect.X + searchEyesAreaSize.Width / 2, yCoordStartSearchEyes + searchEyesAreaSize.Height / 2);

                        if (processed_image_flag == true)
                        {
                            nextFrame.Draw(possibleROI_eyes, new Bgr(Color.DarkGoldenrod), 3);
                        }

                        grayframe.ROI = possibleROI_eyes;


                        eyes = grayframe.DetectHaarCascade(
                                       eye, eye_scale_rate, eye_detection,
                                       HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                                       new Size(nextFrame.Width / 20, nextFrame.Height / 20)
                                       );
                        grayframe.ROI = Rectangle.Empty;


                        if (processed_image_flag == true)
                        {
                            nextFrame.Draw(face.rect, new Bgr(Color.Yellow), 3);
                        }

                        if(blink_control_flag==true)                       //blink click option
                        {
                            if(eyes[0].Length==1)
                            {
                                timer2.Start();
                                left_click_notification.Visible = true;
                                DoMouseClick();
                            }
                        }

                        foreach (MCvAvgComp ey in eyes[0])
                        {
                            Rectangle eyeRect = ey.rect;

                            eyeRect.Offset(possibleROI_eyes.X, possibleROI_eyes.Y);
                            grayframe.ROI = eyeRect;

                            if (processed_image_flag == true)
                            {
                                nextFrame.Draw(eyeRect, new Bgr(Color.DarkRed), 3);
                                nextFrame.Draw(possibleROI_eyes, new Bgr(Color.DarkOrange), 2);
                            }
                        }
                        grayframe.ROI = possibleROI_eyes;
                        gray_image.ROI = possibleROI_eyes;


                        if (nav.Left < cursor.X && cursor.X < (nav.Left + nav.Width) && nav.Top < cursor.Y && cursor.Y < nav.Top + nav.Height)
                        {
                            LineSegment2D CursorDraw = new LineSegment2D(cursor, new Point(cursor.X, cursor.Y + 1));

                            if (cursor_frame_flag == true)
                            {
                                nextFrame.Draw(CursorDraw, new Bgr(Color.White), 3);
                            }
                            //cursor coordinate using a simple scale based on frame width and height

                             xCoord = ((Screen.PrimaryScreen.Bounds.Width + 200) * (cursor.X - nav.Left)) / nav.Width - 100;
                             yCoord = ((Screen.PrimaryScreen.Bounds.Height + 200) * (cursor.Y - nav.Top)) / nav.Height - 100;
                           
                        }

                        video_output.Image = nextFrame;

                    }
                }
            }
        }

        public static void AdjoinKey(int key1,Keys key2)
        {
            keybd_event((byte)key1, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event((byte)key2, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event((byte)key2, 0, KEYEVENTF_KEYUP, 0);
            keybd_event((byte)key1, 0, KEYEVENTF_KEYUP, 0);
        }

        public static void KeyDown(System.Windows.Forms.Keys key)
        {
            keybd_event((byte)key, 0x45, 0x0001 | 0, 0);
        }

        public static void KeyUp(System.Windows.Forms.Keys key)
        {
            keybd_event((byte)key, 0x45, 0x0001 | 0x0002, 0);
        }

        public static void DoMouseClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void mouseSideClick()
        {
            mouse_event(MOUSEEVENTF_SIDEDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_SIDEUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void RightMouseClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void DoubleMouseClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);

            mouse_event(MOUSEEVENTF_LEFTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void Move(int xDelta, int yDelta)
        {
            mouse_event(MOUSEEVENTF_MOVE, xDelta, yDelta, 0, 0);
        }


        public static void MoveTo(int x, int y)
        {
            mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, x, y, 0, 0);
        }


        public static void MiddleMouseClick()
        {
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_MIDDLEUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public void checkMode()
        {
            if (cursor_mode_flag == true)
            {
                cursor_notification.Visible = true;
                multi_notification.Visible = false;
                security_notification.Visible = false;
            }
            else if (multi_mode_flag == true)
            {
                cursor_notification.Visible = false;
                multi_notification.Visible = true;
                security_notification.Visible = false;
            }
            else if (security_mode_flag == true)
            {
                cursor_notification.Visible = false;
                multi_notification.Visible = false;
                security_notification.Visible = true;
            }
        }


        private void setting_button_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = settings_tab; 
        }

        private void cursorcontrol_button_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = cursor_tab;
            cursor_mode_flag = true;
            multi_mode_flag = false;
            security_mode_flag = false;
        }

        private void multi_button_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = multi_tab;
            cursor_mode_flag = false;
            multi_mode_flag = true;
            security_mode_flag = false;
        }

        private void security_button_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = security_tab;
            cursor_mode_flag = false;
            multi_mode_flag = false;
            security_mode_flag = true;
        }


        private void play_button_Click(object sender, EventArgs e)
        {
            Application.Idle += new EventHandler(Run);
            pause_button.Enabled = true;
            play_button.Enabled = false;
            play_video_flag = true;
            pause_video_flag = false;
        }

        private void pause_button_Click(object sender, EventArgs e)
        {
            Application.Idle -= new EventHandler(Run);
            play_button.Enabled = true;
            pause_button.Enabled = false;
            pause_video_flag = true;
            play_video_flag = false;
        }

        private void cursor_position_check_CheckedChanged(object sender, EventArgs e)
        {
            if (cursor_position_check.Checked)
            {
                cursor_position_flag=true;
                cursor_position_textbox.Visible = true;
            }
            else
            {
                cursor_position_flag= false;
                cursor_position_textbox.Visible = false;
            }
        }


        private void processed_image_check_CheckedChanged(object sender, EventArgs e)
        {
            if (processed_image_check.Checked)
            {
                processed_image_flag = true;
            }
            else
            {
                processed_image_flag = false;
            }
        }

        private void cursor_frame_check_CheckedChanged(object sender, EventArgs e)
        {
            if (cursor_frame_check.Checked)
            {
                cursor_frame_flag = true;
            }
            else
            {
                cursor_frame_flag = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
             //Application.Idle += new EventHandler(Run);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "Minimize to Tray App";
            notifyIcon1.BalloonTipText = "You have successfully minimized your form.";

            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(2000);
                //Application.Idle += new EventHandler(Run);
                //this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void eyecontrol_switch_CheckedChanged(object sender, EventArgs e)
        {
            if(eyecontrol_switch.Checked==true)
            {
                Application.Idle += cursorPosition;
                //kalman 
                InitialiseTimers(kalman_k);
                //InitialiseTimers(1000000);//change this value to control cursor speed
                //=====kalman
            }
            else if (eyecontrol_switch.Checked == false)
            {
                Application.Idle -= cursorPosition;
                Application.Idle -= cursorPosition;

                //kalman 
                StopTimers();
                //=====kalman
            }
        }

        private void blinkcontrol_switch_CheckedChanged(object sender, EventArgs e)
        {
            if (blinkcontrol_switch.Checked == true)
            {
                blink_control_flag = true;
            }
            else if (eyecontrol_switch.Checked == false)
            {
                blink_control_flag = false;
            }
        }

        private void pointer_movement_trackbar_Scroll(object sender, ScrollEventArgs e)
        {
            width = pointer_movement_trackbar.Value;
            height = pointer_movement_trackbar.Value;
        }

     

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            left_click_notification.Visible = false;
            right_click_notification.Visible = false;
            double_click_notification.Visible = false;
        }

        private void pointer_speed_trackbar_Scroll(object sender, ScrollEventArgs e)
        {
            kalman_k = pointer_speed_trackbar.Value;
        }

        private void password_timer_Tick(object sender, EventArgs e)
        {

        }

        private void mob_number_edit_Click(object sender, EventArgs e)
        {
            if(mob_number.Enabled==false)
            {
                mob_number_edit.Text = "Done";
                mob_number.Enabled = true;
                mobile_num = mob_number.Text;
            }
            else 
            {
                mob_number_edit.Text = "Edit";
                mob_number.Enabled = false;
            }
        }

        private void dataset_combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dataset_combo.SelectedIndex == 0)
            {
                haar = new HaarCascade("haarcascade_frontalface_default.xml");
            }
            else if (dataset_combo.SelectedIndex == 1)
            {
                haar = new HaarCascade("haarcascade_frontalface_alt.xml");
            }
            else if (dataset_combo.SelectedIndex == 2)
            {
                haar = new HaarCascade("haarcascade_frontalface_alt_tree.xml");
            }
            else if (dataset_combo.SelectedIndex == 3)
            {
                haar = new HaarCascade("haarcascade_frontalface_alt2.xml");
            }
        }

        private void browse_button_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            
          
            if (of.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                security_imageBox.ImageLocation = of.FileName;
                DateTime creationTime = File.GetLastWriteTime(security_imageBox.ImageLocation);
                image_details.Text = "Date:" + creationTime.ToString();
            }
            string path;
            path = Application.StartupPath + "/TrainedFaces";
            if (Directory.Exists(path))
            {
                of.InitialDirectory = path;
            }

        }
        public void controlSecurity()
        {
            string subPath = "ImagesPath"; // your code goes here

            bool exists = System.IO.Directory.Exists(Application.StartupPath + "/TrainedFaces");
            string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
            string[] Labels = Labelsinfo.Split('%');
            ContTrain = Convert.ToInt16(Labels[0]);
            Maxval = ContTrain;
            string LoadFaces;
            LoadFaces = "face" + ContTrain + ".bmp";
            Image<Bgr, byte> lastImage = new Image<Bgr, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces);
            DateTime creationTime = File.GetLastWriteTime(Application.StartupPath + "/TrainedFaces/" + LoadFaces);
            image_details.Text="Date:"+creationTime.ToString();
            security_imageBox.Image = lastImage;
        }

        private void next_photo_Click(object sender, EventArgs e)
        {
            if (ContTrain<Maxval)
            {
                ContTrain += 1;
                string LoadFaces;
                LoadFaces = "face" + ContTrain + ".bmp";
                Image<Bgr, byte> lastImage = new Image<Bgr, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces);
                DateTime creationTime = File.GetLastWriteTime(Application.StartupPath + "/TrainedFaces/" + LoadFaces);
                image_details.Text = "Date:" + creationTime.ToString();
                security_imageBox.Image = lastImage;
            }
            
            
        }

        private void prev_photo_Click(object sender, EventArgs e)
        {
            if (ContTrain != 1)
            {
                ContTrain -= 1;
                string LoadFaces;
                LoadFaces = "face" + ContTrain + ".bmp";
                Image<Bgr, byte> lastImage = new Image<Bgr, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces);
                DateTime creationTime = File.GetLastWriteTime(Application.StartupPath + "/TrainedFaces/" + LoadFaces);
                image_details.Text = "Date:" + creationTime.ToString();
                security_imageBox.Image = lastImage;
            }
           
        }

        private void detele_button_MouseEnter(object sender, EventArgs e)
        {

        }

        private void detele_button_MouseLeave(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }


        private void eye_scalerate_incr_Click(object sender, EventArgs e)
        {
            if(eye_scalerate_track.Value < eye_scalerate_track.Maximum)
            {
                eye_scalerate_track.Value = eye_scalerate_track.Value + 10;
                eye_scale_rate = 1 + (eye_scalerate_track.Value * .015);
            }
        }

        private void eye_scalerate_decr_Click(object sender, EventArgs e)
        {
            if(eye_scalerate_track.Value > eye_scalerate_track.Minimum)
            {
                eye_scalerate_track.Value = eye_scalerate_track.Value - 10;
                eye_scale_rate = 1 + (eye_scalerate_track.Value * .015);
            }
        }

        private void face_scalerate_incr_Click(object sender, EventArgs e)
        {
            if (face_scalerate_track.Value < face_scalerate_track.Maximum)
            {
                face_scalerate_track.Value = face_scalerate_track.Value + 10;
                face_scale_rate = 1 + (face_scalerate_track.Value * .015);
            }
        }

        private void face_scalerate_decr_Click(object sender, EventArgs e)
        {
            if (face_scalerate_track.Value > face_scalerate_track.Minimum)
            {
                face_scalerate_track.Value = face_scalerate_track.Value - 10;
                face_scale_rate = 1 + (face_scalerate_track.Value * .015);
            }
        }

        private void eye_detection_incr_Click(object sender, EventArgs e)
        {
            if (eye_detection_track.Value < eye_detection_track.Maximum)
            {
                eye_detection_track.Value = eye_detection_track.Value + 1;
                eye_detection = eye_detection_track.Value;
            }
        }

        private void eye_detection_decr_Click(object sender, EventArgs e)
        {
            if (eye_detection_track.Value > eye_detection_track.Minimum)
            {
                eye_detection_track.Value = eye_detection_track.Value - 1;
                eye_detection = eye_detection_track.Value;
            }
        }

        private void face_detection_incr_Click(object sender, EventArgs e)
        {
            if (face_detection_track.Value < face_detection_track.Maximum)
            {
                face_detection_track.Value = face_detection_track.Value + 1;
                face_detection = face_detection_track.Value;
            }
        }

        private void face_detection_decr_Click(object sender, EventArgs e)
        {
            if (face_detection_track.Value > face_detection_track.Minimum)
            {
                face_detection_track.Value = face_detection_track.Value - 1;
                face_detection = face_detection_track.Value;
            }
        }

        private void sms_switch_CheckedChanged(object sender, EventArgs e)
        {
            if(sms_switch.Checked==true)
            {
                sms_button_switch = true;
            }
            else
            {
                sms_button_switch = false ;
            }
        }


        private void metroButton9_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath+"/Voice Commands.pdf");
        }

        private void metroButton9_MouseHover(object sender, EventArgs e)
        {
            ToolTip ToolTip1 = new System.Windows.Forms.ToolTip();
            ToolTip1.SetToolTip(this.metroButton9, "Voice Commands help");
        }

    }
  }

