using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static embeddedAFL.DataTypes.FuzzingStatusEnum;

namespace embeddedAFL.InterfaceModules
{
    public static class IfKelinci
    {
        #region static Variables
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Every fuzzing data block sent by AFL via Kelinci is assumed as a session and increments the _sessionCounter.
        /// </summary>
        private static int _sessionCounter;
        #endregion

        #region Types
        /// <summary>
        /// The KelinciData class stores one data block that is sent via Kelinci to embeddedAFL.
        /// </summary>
        public class KelinciData
        {
            /// <summary>
            /// Mode of operation transmitted by Kelinci
            /// </summary>
            public int Mode;
            /// <summary>
            /// Length of the data block transmitted by Kelinci
            /// </summary>
            public int Length;
            /// <summary>
            /// Data as byte array transmitted by Kelinci. Only valid if this array has the same length as previously sent with Length parameter.
            /// </summary>
            public byte[] Data;

            /// <summary>
            /// Constructor of the KelinciData class
            /// </summary>
            /// <param name="mode">Mode of operation transmitted by Kelinci</param>
            /// <param name="length">Length of the data block transmitted by Kelinci</param>
            /// <param name="data">Data as byte array transmitted by Kelinci. Only valid if this array has the same length as previously sent with Length parameter.</param>
            public KelinciData(int mode, int length, byte[] data)
            {
                if (length != data.Length)
                {
                    throw new ArgumentException(
                        $"Data[] has not the expected length. Length={length}, Data.length={data.Length}");
                }
                Mode = mode;
                Length = length;
                Data = data;
            }
        }
        #endregion

        public static async Task StartKelinciInterface(int port, IfTargetState targetState)
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            try
            {
                server.Start();
                Logger.Trace("Kelinci Server Started, waiting for session");
                while (true)
                {
                    #region Initialize

                    // Start new session with Kelinci
                    TcpClient client = server.AcceptTcpClient();
                    var stream = client.GetStream();
                    DataTypes.FuzzingResult result = new DataTypes.FuzzingResult(StatusCommError, new byte[65536]);
                    CancellationTokenSource cts = new CancellationTokenSource();
                    Logger.Trace("{0:000000} Start", _sessionCounter);

                    #endregion

                    try
                    {
                        #region ReadKelinciData

                        cts.CancelAfter(Parameters.TimeoutReadKelinciDataAsync);
                        KelinciData kelinciData = await ReadKelinciDataAsync(stream, cts.Token);
                        Logger.Trace("{0:000000} REC KELINCI: Mode: {1}, Length: {2}, Data: {3}", _sessionCounter,
                            kelinciData.Mode, kelinciData.Length, kelinciData.Data);

                        #endregion

                        if (Parameters.ShortCircuitTesting)
                        {
                            result.FuzzingStatus = StatusSuccess;
                        }
                        else
                        {
                            #region Start State Machine

                            cts.CancelAfter(Parameters.TimeoutStartStateMachineAsync);
                            result = await StateMachine.StartStateMachineAsync(_sessionCounter, kelinciData.Data,
                                targetState, cts.Token);

                            #endregion
                        }
                    }
                    #region Error Handling and Writeback to Kelinci

                    catch (OperationCanceledException e)
                    {
                        Logger.Warn("{0:000000} TIMEOUT: {1}", _sessionCounter, e.Message);
                        result.FuzzingStatus = StatusTimeout;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("{0:000000} Exception: {1}", _sessionCounter, e.Message);
                        result.FuzzingStatus = StatusCommError;
                    }

                    #region WriteKelinciStatus and close stream + client

                    try
                    {
                        cts.CancelAfter(Parameters.TimeoutWriteKelinciStatusAsync);
                        WriteKelinciStatusAsync(stream, result, cts.Token);
                        if (result.FuzzingStatus == StatusTimeout)
                        {
                            Logger.Warn("{0:000000} STATUS: {1}, BITMAP: {2}", _sessionCounter,
                                result.FuzzingStatus, result.Bitmap.Length);
                        }
                        else if (result.FuzzingStatus == StatusCommError)
                        {
                            Logger.Error("{0:000000} STATUS: {1}, BITMAP: {2}", _sessionCounter,
                                result.FuzzingStatus, result.Bitmap.Length);
                        }
                        else if (result.FuzzingStatus == StatusCrash)
                        {
                            Logger.Fatal("{0:000000} STATUS: {1}, BITMAP: {2}", _sessionCounter,
                                result.FuzzingStatus, result.Bitmap.Length);
                            Alarms.PlayAlarmInfinite();
                        }
                        else
                        {
                            Logger.Info("{0:000000} STATUS: {1}, BITMAP: {2}", _sessionCounter,
                                result.FuzzingStatus, result.Bitmap.Length);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("{0:000000} WriteKelinciException: {1}", _sessionCounter, e.Message);
                    }
                    finally
                    {
                        stream.Close();
                        client.Close();
                    }

                    #endregion

                    //if (result.FuzzingStatus == StatusCrash)
                    //{
                    //    throw new Exception("CRASH DETECTED");
                    //}

                    Logger.Trace("{0:000000} End", _sessionCounter);
                    _sessionCounter++;

                    #endregion
                }
            }
            catch (SocketException e)
            {
                Logger.Fatal("CRASH: Kelinci Interface: SocketException: {0}", e.Message);
                server.Stop();
                throw;
            }
            finally
            {
                server.Server.Dispose();
            }
        }

        #region ReadKelinciData
        private static async Task<KelinciData> ReadKelinciDataAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] mode = new byte[1];
            byte[] length = new byte[4];
            try
            {
                //Read first Byte   ==> Mode
                int bytesRead = stream.Read(mode, 0, 1);
                bool success = (bytesRead == 1);
                Logger.Trace("{0} Mode bytes read", bytesRead);
                //Read next 4 Bytes ==> Length
                bytesRead = stream.Read(length, 0, 4);
                success = success && (bytesRead == 4);
                Logger.Trace("{0} Length bytes read", bytesRead);
                //Read Bytes        ==> Data 
                byte[] data = new byte[StaticHelper.ConvertLittleEndian(length)];

                int i = 0;
                while ((i != data.Length) && (!token.IsCancellationRequested))
                {
                    i += await stream.ReadAsync(data, i, data.Length - i, token);
                }

                if (success)
                {
                    return new KelinciData(StaticHelper.ConvertLittleEndian(mode), StaticHelper.ConvertLittleEndian(length), data);
                }
                throw new IOException("ReadKelinciData failed");
            }
            catch (Exception e)
            {
                Logger.Error("Exception: {0}", e.ToString());
                return null;
            }
        }
        #endregion

        #region WriteKelinciData
        private static void WriteKelinciStatusAsync(NetworkStream stream, DataTypes.FuzzingResult fuzzingResult,
            CancellationToken token)
        {
            // Send Status
            stream.WriteAsync(fuzzingResult.FuzzingStatusByte, 0, 1, token);
            // Send Bitmap
            stream.WriteAsync(fuzzingResult.Bitmap, 0, fuzzingResult.Bitmap.Length, token);
        }
        #endregion
    }
}
