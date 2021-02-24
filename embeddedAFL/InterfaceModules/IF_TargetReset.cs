
using System;
using System.Net;
using System.Threading;

namespace embeddedAFL.InterfaceModules
{
    public static class IfTargetReset
    {
        #region Static variables
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #region On/Off Commands
        public static string OnCommand = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SMARTPLUG id=""edimax"">
    <CMD id=""setup"">
       <Device.System.Power.State>ON</Device.System.Power.State>
    </CMD>
</SMARTPLUG>";

        public static string OffCommand = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SMARTPLUG id=""edimax"">
    <CMD id=""setup"">
       <Device.System.Power.State>OFF</Device.System.Power.State>
    </CMD>
</SMARTPLUG>";
        #endregion
        #endregion

        /// <summary>
        /// This value tries to authenticate to the target reset device and returns if it was successful
        /// </summary>
        public static bool TargetResetIsAvailable
        {
            get
            {
                bool success = false;
                try
                {
                    Uri myUri = new Uri(Parameters.TargetResetLoginUrl);
                    WebRequest myWebRequest = WebRequest.Create(myUri);

                    HttpWebRequest myHttpWebRequest = (HttpWebRequest)myWebRequest;

                    NetworkCredential myNetworkCredential = new NetworkCredential(Parameters.TargetResetUsername, Parameters.TargetResetPassword);

                    CredentialCache myCredentialCache = new CredentialCache { { myUri, "Digest", myNetworkCredential } };

                    myHttpWebRequest.PreAuthenticate = true;
                    myHttpWebRequest.Credentials = myCredentialCache;

                    HttpWebResponse resp = (HttpWebResponse)myWebRequest.GetResponse();
                    success = resp.StatusCode == HttpStatusCode.OK;
                    resp.Close();
                }
                catch (Exception e)
                {
                    Logger.Error((e.Message));
                }
                return success;
            }
        }

        public static void ResetTarget()
        {
            POST_Command(OffCommand);
            Thread.Sleep(200);
            POST_Command(OnCommand);
        }

        private static void POST_Command(string command)
        {
            try
            {
                Uri myUri = new Uri(Parameters.TargetResetUrl);
                WebRequest webRequest = WebRequest.Create(myUri);

                HttpWebRequest req = (HttpWebRequest)webRequest;

                NetworkCredential myNetworkCredential = new NetworkCredential(Parameters.TargetResetUsername, Parameters.TargetResetPassword);

                CredentialCache myCredentialCache = new CredentialCache { { myUri, "Digest", myNetworkCredential } };

                req.PreAuthenticate = true;
                req.Credentials = myCredentialCache;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                using (var reqStream = req.GetRequestStream())
                {
                    reqStream.Write(System.Text.Encoding.UTF8.GetBytes(command));
                }
                HttpWebResponse resp = (HttpWebResponse)webRequest.GetResponse();
                resp.Close();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }
}
