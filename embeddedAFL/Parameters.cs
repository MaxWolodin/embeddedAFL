using System.IO.Ports;

namespace embeddedAFL
{
    public static class Parameters
    {
        #region Timeouts
        /// <summary>
        /// Timeout (ms) to read the data from Kelinci via tcp
        /// </summary>
        public const int TimeoutReadKelinciDataAsync = 10000;

        /// <summary>
        /// Timeout (ms) to write data to Kelinci via tcp
        /// </summary>
        public const int TimeoutWriteKelinciStatusAsync = 10000;

        /// <summary>
        /// Timeout (ms) to handle everything in the Kelinci interface
        /// </summary>
        public const int TimeoutInterfaceKelinciAsync = 60000;

        /// <summary>
        /// Timeout (ms) to handle everything in the State Machine
        /// </summary>
        public const int TimeoutStartStateMachineAsync = 40000;

        /// <summary>
        /// Timeout (ms) that defines how long the program should sleep till the KelinciInterface thread is watched again.
        /// </summary>
        public const int TimeoutKelinciInterfaceWatchdog = 200;

        /// <summary>
        /// Timeout (ms) that defines how long the IF_Bitmap will wait for an response after sending the bitmap request.
        /// </summary>
        public const int TimeoutWaitForBitmapResponse = 40;

        /// <summary>
        /// Timeout (ms) that defines how long the IF_Bitmap will wait to process the next tcp package. 
        /// </summary>
        public const int TimeoutWaitForNextBitmapPackage = 20;

        /// <summary>
        /// Timeout (ms) that defines how long the State Machine will wait to get the bitmap.
        /// </summary>
        public const int TimeoutGetBitmap = 10000;

        /// <summary>
        /// Timeout (ms) that defines how long the State Machine will wait after requesting the Target Status after checking again
        /// </summary>
        public const int TimeoutWaitForTargetStateChange = 100;

        /// <summary>
        /// Timeout (ms) that defines how long the State Machine will wait to check again if the Reset module is available
        /// </summary>
        public const int TimeoutWaitForResetAvailability = 400;

        /// <summary>
        /// Timeout (ms) that defines how long the Fuzzer will wait before trying to send fuzzing data to the target after the previous try failed
        /// </summary>
        public const int TimeoutWaitBeforeFuzzingAgain = 100;

        /// <summary>
        /// Timeout (ms) that defines how long the Fuzzer has time to send fuzzing data to the target
        /// </summary>
        public const int TimeoutSendFuzzingToTarget = 10000;

        /// <summary>
        /// Timeout (ms) that defines how long the Fuzzer should wait between its different steps (TraceOn/Fuzz/TraceOff/GetBitmap/ReInit)
        /// </summary>
        public const int TimeoutWaitBetweenFuzzingSteps = 20;

        /// <summary>
        /// Timeout (ms) that defines how long the Fuzzer should wait before aborting StopTracing function
        /// </summary>
        public const int TimeoutStopTracing = 12000;

        /// <summary>
        /// Timeout (ms) that defines how long the Fuzzer should wait before aborting Reinit Tracing function
        /// </summary>
        public const int TimeoutReinitTracing = 12000;

       /// <summary>
        /// Timeout (ms) that defines how long the Fuzzer should wait after rebooting the target
        /// </summary>
        public const int TimeoutWaitAfterReboot = 60000;
        #endregion

        /// <summary>
        /// SleepTime (ms) that defines how long the Fuzzer should wait after rebooting the target before sending fuzzing data
        /// </summary>
        public const int SleepAfterDeviceRebooted = 10000;

        #region Communication
        /// <summary>
        /// Port where Kelinci connects to
        /// </summary>
        public const int KelinciInterfacePort = 7007;

        /// <summary>
        /// IP address of the target which will be fuzzed
        /// </summary>
        public const string TargetIpAddress = "172.16.1.5";

        /// <summary>
        /// Port of the target which will be fuzzed
        /// </summary>
        public const int TargetPort = 9991;

        /// <summary>
        /// IP address of the target where the bitmap is retrieved
        /// </summary>
        public const string TargetBitmapIpAddress = "172.16.1.5";

        /// <summary>
        /// Port of the target where the bitmap is retrieved
        /// </summary>
        public const int TargetBitmapPort = 80;

        /// <summary>
        /// URL to check if the Target can be reset
        /// </summary>
        public const string TargetResetLoginUrl = "http://192.168.0.235:10000/";

        /// <summary>
        /// URL where the reset command is sent to
        /// </summary>
        public const string TargetResetUrl = "http://192.168.0.235:10000/smartplug.cgi";
        #endregion

        #region Credentials
        /// <summary>
        /// Username to operate the reset plug
        /// </summary>
        public const string TargetResetUsername = "admin";

        /// <summary>
        /// Password to operate the reset plug
        /// </summary>
        public const string TargetResetPassword = "plugimilian";
        #endregion

        #region Telnet
        public const string TelnetCoMport = "COM7";
        public const int TelnetBaudrate = 9600;
        public const Parity TelnetParity = Parity.None;
        public const int TelnetDataBits = 8;
        public const StopBits TelnetStopBits = StopBits.One;
        #endregion

        #region Advanced Settings
        /// <summary>
        /// Switch to decide if the Serial communication should be logged to a file (.//Output.log)
        /// </summary>
        public static bool WriteSerialCommunicationToLog = true;

        /// <summary>
        /// Switch to decide if the Serial communication should be logged to the console window
        /// </summary>
        public static bool WriteSerialCommunicationToConsole = false;

        /// <summary>
        /// Switch to decide if the Stopwatch result will be written to a file
        /// </summary>
        public static bool WriteStopwatchResultToLog = true;

        /// <summary>
        /// Switch to decide if the state machine is running in aggressive mode. If this parameter is true the tracing is not disabled before retrieving the bitmap.
        /// </summary>
        public static bool EnableAggressiveMode = true;

        /// <summary>
        /// Switch to decide if the state machine should skip the preparation of the fuzzing target.
        /// </summary>
        public static bool SkipPreparation = false;

        /// <summary>
        /// Switch to decide if the program should run in debug mode, where there is no need to run AFL.
        /// </summary>
        public static bool Debug = false;

        /// <summary>
        /// Switch to decide if the program should directly respond to Kelinci with a null bitmap to test performance.
        /// </summary>
        public static bool ShortCircuitTesting = false;

        #endregion
    }
}
