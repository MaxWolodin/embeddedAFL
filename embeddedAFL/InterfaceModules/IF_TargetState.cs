using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static embeddedAFL.DataTypes.TargetStateEnum;

namespace embeddedAFL.InterfaceModules
{
    public class IfTargetState
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #region Load C DLL
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ProgressCallback(IntPtr buffer, int length);

        [DllImport("mttty.dll")]
        static extern void ConnectToSerialPort([MarshalAs(UnmanagedType.FunctionPtr)] ProgressCallback telnetCallbackPointer,
            [MarshalAs(UnmanagedType.FunctionPtr)] ProgressCallback statusCallbackPointer,
            [MarshalAs(UnmanagedType.FunctionPtr)] ProgressCallback errorCallbackPointer);
        #endregion

        #region Types
        private DataTypes.TargetStateEnum _targetState;
        public DataTypes.TargetStateEnum TargetState
        {
            get => _targetState;
            set
            {
                _targetState = value;
                Logger.Debug("TargetState is now: {0}", value);
                _waitForStateChange = false;
            }
        }
        #endregion

        #region Static variables
        #region UDP Socket
        private static Socket _sock;
        private static IPEndPoint _endPoint;
        #endregion

        /// <summary>
        /// Advises the Send command to wait till the last command has taken effect
        /// </summary>
        private static bool _waitForStateChange;

        public static bool _fuzzingAllowed;

        /// <summary>
        /// This string contains all characters received from the serialPort since the last change of TargetState
        /// </summary>
        private string _dataReceived = "";
        #endregion

        #region Constructor and Destructor
        public IfTargetState()
        {
            TargetState = Unknown;

            // Initialize UDP socket
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress serverAddr = IPAddress.Parse("127.0.0.1");
            _endPoint = new IPEndPoint(serverAddr, 5555);
            Logger.Trace("UDP Socket initialized");

            // Start SerialPort Listener and Writer service
            Task.Factory.StartNew(() =>
            {
                ConnectToSerialPort(TelnetCallback, StatusCallback, ErrorCallback);
            });
            Logger.Trace("ConnectToSerialPort started and callbacks provided");
            Logger.Trace("IfTargetState initialized");
        }

        ~IfTargetState()
        {
            _sock?.Close();
        }
        #endregion

        #region Send / Receive
        public void Send_Command(string command)
        {
            while (_waitForStateChange)
            {
                Thread.Sleep(20);
            }
            Logger.Trace("Command: {0}",command);
            byte[] sendBuffer = Encoding.ASCII.GetBytes(command + '\n');
            _sock.SendTo(sendBuffer, _endPoint);
            _waitForStateChange = true;
        }
        #endregion

        #region Callbacks 
        private void TelnetCallback(IntPtr unsafeBuffer, int length)
        {
            byte[] safeBuffer = new byte[length];
            Marshal.Copy(unsafeBuffer, safeBuffer, 0, length);
            string bufferContent= Encoding.ASCII.GetString(safeBuffer);
            _dataReceived += bufferContent;
            if (Parameters.WriteSerialCommunicationToLog)
            {
                using (var sw = File.AppendText(".\\Output.log"))
                {
                    sw.Write(bufferContent);
                }
            }
            if (Parameters.WriteSerialCommunicationToConsole)
            {
                Console.Write(bufferContent);
            }
            DetectTargetState();
        }

        private void StatusCallback(IntPtr unsafeBuffer, int length)
        {
            byte[] safeBuffer = new byte[length];
            Marshal.Copy(unsafeBuffer, safeBuffer, 0, length);
            string text = Encoding.ASCII.GetString(safeBuffer).Replace("\0", string.Empty).Trim();
            //Logger.Info(text);
            if (text.Contains("Reader dwRes: 258"))
            {
                _fuzzingAllowed = true;
                //Logger.Info("_fuzzingAllowed");
            }
            else
            {
                //Logger.Trace(text);
                _fuzzingAllowed = false;
            }
        }

        private void ErrorCallback(IntPtr unsafeBuffer, int length)
        {
            byte[] safeBuffer = new byte[length];
            Marshal.Copy(unsafeBuffer, safeBuffer, 0, length);
            Logger.Error(Encoding.UTF8.GetString(safeBuffer));
        }
        #endregion

        #region StateHandling
        private void DetectTargetState()
        {
            #region Rebooting
            if (_dataReceived.Contains("PFail"))
            {
                TrimDataReceivedBuffer("PFail");
                TargetState = Rebooting;
            }
            #endregion
            #region Afterboot
            if (_dataReceived.Contains("Main Task... Done !"))
            {
                TrimDataReceivedBuffer("Main Task... Done !");
                TargetState = AfterBoot;
                Thread.Sleep(1000);
                ToTargetStatePIN_REQUIRED();
            }
            #endregion
            #region PinRequired
            if (_dataReceived.Contains("System Locked, enter PIN"))
            {
                TrimDataReceivedBuffer("System Locked, enter PIN");
                TargetState = PinRequired;
                Thread.Sleep(1000);
                ToTargetStateReady();
            }
            #endregion
            #region Ready
            if ((_dataReceived.Contains("Globaltrace = OFF")) && (TargetState == PinRequired))
            {
                TrimDataReceivedBuffer("Globaltrace = OFF");
                TargetState = Ready;
            }
            #endregion

            #region WaitTracingInitialized
            if (_dataReceived.Contains("REINITOK"))
            {
                TrimDataReceivedBuffer("REINITOK");
                TargetState = WaitTracingInitialized;
            }
            #endregion

            #region WaitTracingInitialized
            if (_dataReceived.Contains("INITOK"))
            {
                TrimDataReceivedBuffer("INITOK");
                TargetState = WaitTracingInitialized;
            }
            #endregion
            #region WaitTracingOn
            if (_dataReceived.Contains("TRACEON"))
            {
                TrimDataReceivedBuffer("TRACEON");
                TargetState = WaitTracingOn;
            }
            #endregion
            #region WaitTracingOff
            if (_dataReceived.Contains("TRACEOFF"))
            {
                TrimDataReceivedBuffer("TRACEOFF");
                TargetState = WaitTracingOff;
            }
            #endregion
            #region Fault
            if (_dataReceived.Contains("Fault") && ((_targetState != Rebooting) && (_targetState != Unknown)))
            {
                TrimDataReceivedBuffer("Fault");
                Logger.Fatal("FAULT occured in target!");
                StateMachine.CrashDetected = true;
            }
            if (_dataReceived.Contains("*** Fatal Error"))
            {
                TrimDataReceivedBuffer("*** Fatal Error");
                Logger.Fatal("*** Fatal Error occured in target!");
                StateMachine.CrashDetected = true;
            }
            
            #endregion
            #region AfterWait

            string readyToSendText = "#";//" Followpointers = OFF    Globaltrace = OFF\n\r\n\r#";
            if (_dataReceived.Contains(readyToSendText))
            {
                TrimDataReceivedBuffer(readyToSendText);
                if (TargetState == WaitTracingInitialized)
                    TargetState = TracingInitialized;
                if (TargetState == WaitTracingOn)
                    TargetState = TracingOn;
                if (TargetState == WaitTracingOff)
                    TargetState = TracingOff;
            }
            #endregion
        }

        private void TrimDataReceivedBuffer(string substring)
        {
            int position = _dataReceived.LastIndexOfAny(substring.ToCharArray());
            if (position > 0)
            {
                _dataReceived = _dataReceived.Substring(position);
            }
        }

        #region ToTargetState Functions
        private void ToTargetStatePIN_REQUIRED()
        {
            Send_Command("1");
        }

        private void ToTargetStateReady()
        {
            Send_Command("1909");
        }

        public void ToTargetStateTASK_INITIALIZED()
        {
            Send_Command("97");
        }

        public void ToTargetStateTRACING_INITIALIZED()
        {
            Send_Command("96");
        }

        public void ToTargetStateTRACING_ON()
        {
            Send_Command("95");
        }

        public void ToTargetStateTRACING_OFF()
        {
            Send_Command("94");
        }
        #endregion
        #endregion
    }
}
