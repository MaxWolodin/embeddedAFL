using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace embeddedAFL.InterfaceModules
{
    public static class IfFuzzer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static Task SendDataAsync(byte[] data,CancellationToken token, int sessioncounter)
        {
            return Task.Factory.StartNew(() => SendData(data,token, sessioncounter));
        }

        public static void SendData(byte[] data, CancellationToken token, int sessioncounter)
        {
            try
            {
                string fileName =Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Inputs",+sessioncounter%100+".bin");
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                using (BinaryWriter binWriter = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    // Write string   
                    binWriter.Write(data);
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e.Message);
            }
            while (!token.IsCancellationRequested)
            {
                // Connect to a remote device.  
                try
                {
                    // Establish the remote endpoint for the socket
                    IPAddress ipAddress = IPAddress.Parse(Parameters.TargetIpAddress);
                    IPEndPoint remoteEp = new IPEndPoint(ipAddress, Parameters.TargetPort);

                    // Create a TCP/IP  socket.  
                    Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    // Connect the socket to the remote endpoint. Catch any errors.  
                    try
                    {
                        sender.Connect(remoteEp);

                        // Send the data through the socket.  
                        int bytesSent = sender.Send(data);

                        if (bytesSent == data.Length)
                        {
                            Logger.Info("Length: {0,5} | {1}", data.Length,Encoding.ASCII.GetString(data, 0, data.Length));
                            return;
                        }
                        Logger.Error("ERROR: Message not send completely!");

                        // Release the socket.  
                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();

                    }
                    catch (ArgumentNullException ane)
                    {
                        Logger.Error("ArgumentNullException : {0}", ane.ToString());
                    }
                    catch (SocketException se)
                    {
                        Logger.Error("SocketException : {0}", se.ToString());
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Unexpected exception : {0}", e.ToString());
                    }

                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
                Thread.Sleep(Parameters.TimeoutWaitBeforeFuzzingAgain);
            }
        }
    }
}