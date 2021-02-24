using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using embeddedAFL.InterfaceModules;
using NLog;
using NLog.Targets;

namespace embeddedAFL
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // ReSharper disable once UnusedParameter.Local
#pragma warning disable IDE0060 // Remove unused parameter
        private static async Task Main(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            #region REMOVE
            //byte[] data = File.ReadAllBytes(@"C:\Users\DEU208215\Desktop\Crashinput.txt");
            //CancellationTokenSource cts=new CancellationTokenSource();
            //IfFuzzer.SendData(data, cts.Token);
            //return;
            #endregion

            #region NLog Configuration

            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new FileTarget("logfile") { FileName = "Logbook.log" };
            // Log targets
            var coloredConsoleTarget = new ColoredConsoleTarget { UseDefaultRowHighlightingRules = true };
            coloredConsoleTarget.Layout = "${longdate}|${level:uppercase=true:padding=-6}|${logger:shortName=true:padding=-16} [${threadid:padding=-2}] | ${message}";
            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, coloredConsoleTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

            // Apply config           
            LogManager.Configuration = config;

            #endregion

            Logger.Info("embeddedAFL Started...!");
            IfTargetState targetState = new IfTargetState();
            int sessioncounter = 0;

            while (true)
            {
                #region Print parameters
                Logger.Info("Debug:          {0}", Parameters.Debug);
                Logger.Info("AggressiveMode: {0}", Parameters.EnableAggressiveMode);
                Logger.Info("SkipPrepare:    {0}", Parameters.SkipPreparation);
                Logger.Info("ShortCircuit:   {0}", Parameters.ShortCircuitTesting);
                #endregion


                CancellationTokenSource cts = new CancellationTokenSource();
                if (Parameters.Debug)
                {
                    byte[] data = Encoding.ASCII.GetBytes("TEST");
                    IfKelinci.KelinciData kelinciData = new IfKelinci.KelinciData(0, data.Length, data);
                    Logger.Trace("{0:000000} REC KELINCI: Mode: {1}, Length: {2}, Data: {3}", 0,
                        kelinciData.Mode, kelinciData.Length, kelinciData.Data);
                    cts.CancelAfter(Parameters.TimeoutStartStateMachineAsync);
                    try
                    {
                        StateMachine.StartStateMachineAsync(sessioncounter, kelinciData.Data, targetState, cts.Token).Wait(cts.Token);
                    }
                    catch (Exception e)
                    {
                        // This area should never be reached
                        Logger.Fatal(e);
                        Logger.Fatal("embeddedAFL Restarted after crash!");
                        Alarms.PlayAlarmInfinite();
                    }
                    sessioncounter++;
                }
                else
                {
                    try
                    {
                        Logger.Trace("Start IfKelinci");
                        cts.CancelAfter(Parameters.TimeoutInterfaceKelinciAsync);
                        await IfKelinci.StartKelinciInterface(Parameters.KelinciInterfacePort, targetState);
                    }
                    catch (Exception e)
                    {
                        // This area should never be reached
                        Logger.Fatal(e);
                        Logger.Fatal("embeddedAFL Restarted after crash!");
                        Alarms.PlayAlarmInfinite();
                    }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }
    }
}