using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace embeddedAFL.InterfaceModules
{
    public static class IfBitmap
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static Task<byte[]> Get_BitmapAsync(string host, int port,CancellationToken token)
        {
            return Task<byte[]>.Factory.StartNew(() => Get_Bitmap(host, port),token);
        }

        public static byte[] Get_Bitmap(string host, int port)
        {
            byte[] trimmedBytes = null;
            try
            {
                StringBuilder geTrequest = new StringBuilder();
                geTrequest.Append("GET /write.gif HTTP/1.1\r\n");
                geTrequest.Append("Host: " + host + "\r\n");
                geTrequest.Append("User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:71.0) Gecko/20100101 Firefox/71.0\r\n");
                geTrequest.Append("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n");
                geTrequest.Append("Accept-Language: de,en-US;q=0.7,en;q=0.3\r\n");
                geTrequest.Append("Accept-Encoding: gzip, deflate\r\n");
                geTrequest.Append("DNT: 1\r\n");
                geTrequest.Append("Connection: keep-alive\r\n");
                geTrequest.Append("Upgrade-Insecure-Requests: 1\r\n");
                geTrequest.Append("Cache-Control: max-age=0\r\n");
                geTrequest.Append("\r\n");
                byte[] getBytes = Encoding.ASCII.GetBytes(geTrequest.ToString());

                var tcpClient = new TcpClient(host, port);
                var networkStream = tcpClient.GetStream();

                networkStream.Write(getBytes, 0, getBytes.Length);
                Thread.Sleep(Parameters.TimeoutWaitForBitmapResponse);

                // IF_Kelinci Reply
                if (networkStream.CanRead)
                {
                    // Buffer to store the response bytes.
                    byte[] readBuffer = new byte[80000];
                    var writer = new MemoryStream();
                    using (writer)
                    {
                        while (networkStream.DataAvailable)
                        {
                            int currentBuffer = networkStream.Read(readBuffer, 0, readBuffer.Length);
                            if (currentBuffer <= 0)
                            {
                                break;
                            }
                            writer.Write(readBuffer, 0, currentBuffer);
                            Thread.Sleep(Parameters.TimeoutWaitForNextBitmapPackage);
                        }
                        //Skip Header (86 bytes)
                        trimmedBytes = writer.ToArray().Skip(86).ToArray();
                        //string fullServerReply = Encoding.UTF8.GetString(trimmedBytes.ToArray());
                        Logger.Trace("{0} Bytes received", trimmedBytes.Length);
                    }
                }


            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            return trimmedBytes;
        }
    }
}