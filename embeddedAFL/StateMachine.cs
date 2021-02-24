using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using embeddedAFL.InterfaceModules;

namespace embeddedAFL
{
    public static class StateMachine
    {
        #region Static variables
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        #endregion

        public static volatile bool CrashDetected;

        public static async Task<DataTypes.FuzzingResult> StartStateMachineAsync(int sessionCounter, byte[] fuzzingData, IfTargetState targetState, CancellationToken token)
        {
            #region Initialize
            long tmPrepare = 0, tmTraceOn = 0, tmFuzz = 0, tmTraceOff = 0, tmBitmap = 0, tmReinit = 0;
            Stopwatch sw = new Stopwatch();
            DataTypes.FuzzingResult fuzzingResult = new DataTypes.FuzzingResult(DataTypes.FuzzingStatusEnum.StatusSuccess, new byte[65536]);
            Logger.Trace("{0:000000} StateMachine started", sessionCounter);
            #endregion

            while (!token.IsCancellationRequested)
            {

                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                try
                {
                    #region Prepare with exception handling
                    try
                    {
                        try
                        {
                            #region Prepare

                            sw.Restart();
                            if (!Parameters.SkipPreparation)
                            {
                                if (targetState.TargetState == DataTypes.TargetStateEnum.Unknown)
                                {
                                    //Wait for target reset to be available
                                    while (!IfTargetReset.TargetResetIsAvailable)
                                    {
                                        Thread.Sleep(Parameters.TimeoutWaitForResetAvailability);
                                    }

                                    //Reboot the target
                                    Logger.Info("{0:000000} Target will be rebooted", sessionCounter);
                                    cts.CancelAfter(Parameters.TimeoutWaitAfterReboot);
                                    ExecuteAndWaitForTargetState(IfTargetReset.ResetTarget,
                                        targetState,
                                        DataTypes.TargetStateEnum.Ready,
                                        cts.Token);
                                    Logger.Info("{0:000000} Wait {1}ms after rebooting the target", sessionCounter,
                                        Parameters.SleepAfterDeviceRebooted);
                                    Thread.Sleep(Parameters.SleepAfterDeviceRebooted);
                                    ExecuteAndWaitForTargetState(targetState.ToTargetStateTASK_INITIALIZED,
                                        targetState,
                                        DataTypes.TargetStateEnum.TracingInitialized,
                                        cts.Token);
                                }
                            }

                            tmPrepare = sw.ElapsedMilliseconds;

                            #endregion
                        }
                        #region Handle InnerExceptions

                        catch (AggregateException ae)
                        {
                            // This may contain multiple exceptions, which you can iterate with a foreach
                            foreach (var exception in ae.InnerExceptions)
                            {
                                throw exception;
                            }
                        }

                        #endregion
                    }
                    // if a timeout is exceeded, loop again if it is not related to a crash
                    catch (OperationCanceledException oce)
                    {
                        Logger.Trace("Current State: {0}", targetState.TargetState);
                        Logger.Fatal(oce);
                        //Reset the target state to unknown so the target will be reset and start over
                        targetState.TargetState = DataTypes.TargetStateEnum.Unknown;
                        if (CrashDetected)
                        {
                            Logger.Fatal("CRASH detected");
                            fuzzingResult.FuzzingStatus = DataTypes.FuzzingStatusEnum.StatusCrash;
                            CrashDetected = false;
                            Alarms.PlayAlarm(1.0f);
                            return fuzzingResult;
                        }
                        continue;
                    }
                    #endregion

                    if (sessionCounter < 40)
                    {
                        return fuzzingResult;
                    }

                    try
                    {
                        #region TraceOn

                        if (!Parameters.EnableAggressiveMode)
                        {
                            sw.Restart();
                            Logger.Trace("{0:000000} START TRACING", sessionCounter);
                            cts.CancelAfter(12000);
                            ExecuteAndWaitForTargetState(targetState.ToTargetStateTRACING_ON,
                                targetState,
                                DataTypes.TargetStateEnum.TracingOn,
                                cts.Token);
                            tmTraceOn = sw.ElapsedMilliseconds;
                        }

                        #endregion

                        #region Fuzz

                        sw.Restart();
                        Thread.Sleep(Parameters.TimeoutWaitBetweenFuzzingSteps);
                        Logger.Trace("{0:000000} FUZZ", sessionCounter);
                        cts.CancelAfter(Parameters.TimeoutSendFuzzingToTarget);
                        await IfFuzzer.SendDataAsync(fuzzingData, cts.Token, sessionCounter);
                        tmFuzz = sw.ElapsedMilliseconds;

                        #endregion

                        #region TraceOff

                        if (!Parameters.EnableAggressiveMode)
                        {
                            sw.Restart();
                            Thread.Sleep(Parameters.TimeoutWaitBetweenFuzzingSteps);
                            Logger.Trace("{0:000000} STOP TRACING", sessionCounter);
                            cts.CancelAfter(Parameters.TimeoutStopTracing);
                            ExecuteAndWaitForTargetState(targetState.ToTargetStateTRACING_OFF,
                                targetState,
                                DataTypes.TargetStateEnum.TracingOff,
                                cts.Token);
                            tmTraceOff = sw.ElapsedMilliseconds;
                        }

                        #endregion

                        #region GetBitmap

                        sw.Restart();
                        Thread.Sleep(Parameters.TimeoutWaitBetweenFuzzingSteps);
                        Logger.Trace("{0:000000} GET BITMAP", sessionCounter);
                        cts.CancelAfter(Parameters.TimeoutGetBitmap);
                        do
                        {
                            fuzzingResult.Bitmap = await IfBitmap.Get_BitmapAsync(Parameters.TargetBitmapIpAddress,
                            Parameters.TargetBitmapPort, cts.Token);
                            //Finish even though not the complete bitmap is loaded for the test cases
                        } while ((fuzzingResult.Bitmap.Length != 65536)&&(!Parameters.EnableAggressiveMode));

                        tmBitmap = sw.ElapsedMilliseconds;

                        #endregion

                        #region ReInit

                        sw.Restart();
                        Thread.Sleep(Parameters.TimeoutWaitBetweenFuzzingSteps);
                        Logger.Trace("{0:000000} REINIT TRACING", sessionCounter);
                        cts.CancelAfter(Parameters.TimeoutReinitTracing);
                        ExecuteAndWaitForTargetState(targetState.ToTargetStateTRACING_INITIALIZED,
                            targetState,
                            DataTypes.TargetStateEnum.TracingInitialized,
                            cts.Token);
                        tmReinit = sw.ElapsedMilliseconds;

                        #endregion

                        #region Write Stopwatch
                        if (Parameters.WriteStopwatchResultToLog)
                        {
                            using (var timeWriter = File.AppendText(".\\Stopwatch.log"))
                            {
                                timeWriter.WriteLine("{0},{1},{2},{3},{4},{5}", tmPrepare, tmTraceOn, tmFuzz, tmTraceOff, tmBitmap, tmReinit);
                            }
                        }
                        #endregion

                        #region Crash detection
                        if (CrashDetected)
                        {
                            Logger.Fatal("CRASH detected");
                            fuzzingResult.FuzzingStatus = DataTypes.FuzzingStatusEnum.StatusCrash;
                            CrashDetected = false;
                            Alarms.PlayAlarm(1.0f);
                        }
                        #endregion

                        return fuzzingResult;
                    }

                    #region Handle InnerExceptions

                    catch (AggregateException ae)
                    {
                        // This may contain multiple exceptions, which you can iterate with a foreach
                        foreach (var exception in ae.InnerExceptions)
                        {
                            throw exception;
                        }
                    }

                    #endregion
                }
                // if a timeout is exceeded, loop again if it is not related to a crash
                catch (OperationCanceledException)
                {
                    Logger.Trace("Current State: {0}", targetState.TargetState);
                    //Reset the target state to unknown so the target will be reset and start over
                    targetState.TargetState = DataTypes.TargetStateEnum.Unknown;
                    // if a crash is detected return with crash signal to kelinci
                    if (CrashDetected)
                    {
                        Logger.Fatal("CRASH detected");
                        fuzzingResult.FuzzingStatus = DataTypes.FuzzingStatusEnum.StatusCrash;
                        CrashDetected = false;
                        return fuzzingResult;
                    }

                    fuzzingResult.FuzzingStatus = DataTypes.FuzzingStatusEnum.StatusTimeout;
                    Alarms.PlayAlarm(0.2f);
                    return fuzzingResult;
                }
                // if a normal exception occured, log exception and loop again
                catch (Exception e)
                {
                    Logger.Error("{0:000000} ERROR: {1}", sessionCounter, e.Message);
                }
                // cancel all running tasks
                finally
                {
                    cts.Cancel();
                }
            }
            fuzzingResult.FuzzingStatus = DataTypes.FuzzingStatusEnum.StatusQueueFull;
            Logger.Warn("{0:000000} QUEUE FULL -> Kelinci will try again", sessionCounter);
            Alarms.PlayAlarm(1.0f);
            return fuzzingResult;
        }

        #region ExecuteAndWaitForTargetState
        private static void ExecuteAndWaitForTargetState(Action function, IfTargetState targetStateObject, DataTypes.TargetStateEnum condition, CancellationToken token)
        {
            Task.Factory.StartNew(() =>
                {
                    function();
                    while (condition != targetStateObject.TargetState)
                    {
                        Thread.Sleep(Parameters.TimeoutWaitForTargetStateChange);
                    }
                }).Wait(token);
        }
        #endregion
    }
}
