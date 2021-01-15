using Common.DTO;
using Common.Model;
using Newtonsoft.Json;
using ServiceStack.Host.HttpListener;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Windows;

namespace RecordingApp
{
    public class HttpServer
    {
        private readonly string wavpath = ConfigurationManager.AppSettings["wavpath"];
        private static HttpListener MyHttpListener;
        private bool IsListenerStarted = false;
        private event DelReceiveWebRequest ReceiveWebRequest;

        public HttpServer()
        {
        }

        public void Start(string UrlBase)
        {
            if (IsListenerStarted)
                return;

            if (MyHttpListener == null)
            {
                MyHttpListener = new HttpListener();
            }

            MyHttpListener.Prefixes.Add("http://" + UrlBase + "/");
            //MyHttpListener.Prefixes.Add("http://localhost:4444/");  without admin privileges

            IsListenerStarted = true;

            MyHttpListener.Start();

            IAsyncResult result = MyHttpListener.BeginGetContext(new AsyncCallback(WebRequestCallback), MyHttpListener);
        }

        public void Stop()
        {
            if (MyHttpListener != null)
            {
                MyHttpListener.Close();
                MyHttpListener = null;
                IsListenerStarted = false;
            }
        }

        protected void WebRequestCallback(IAsyncResult result)
        {
            if (MyHttpListener == null)
                return;

            HttpListenerContext context = MyHttpListener.EndGetContext(result);

            MyHttpListener.BeginGetContext(new AsyncCallback(WebRequestCallback), MyHttpListener);

            ReceiveWebRequest?.Invoke(context);

            ProcessRequest(context);
        }

        protected virtual void ProcessRequest(HttpListenerContext Context)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                string json;
                string allUsernames = "";
                using (var reader = new StreamReader(Context.Request.InputStream, Context.Request.ContentEncoding))
                {
                    json = reader.ReadToEnd();
                }
                var transcriptionDTO = JsonConvert.DeserializeObject<TranscriptionDTO>(json);

                foreach(User u in transcriptionDTO.Users)
                {
                    allUsernames += u.ToString() + ", ";
                }

                TranscriptionViewWindow newWindow = new TranscriptionViewWindow();
                newWindow.DateOfMeetingTextBox.Text = transcriptionDTO.DateOfMeeting.ToShortDateString();
                newWindow.TranscriptionARNTextBox.Text = transcriptionDTO.TranscriptionARN;
                newWindow.MeetingParticipantsTextBox.Text = allUsernames.Substring(0, allUsernames.Length - 2);
                newWindow.MeetingPlatformTextBox.Text = transcriptionDTO.MeetingPlatform;
                newWindow.TranscriptionTextTextBox.Text = transcriptionDTO.TranscriptionText;

                newWindow.Show();
                newWindow.Activate();
            });

            Array.ForEach(Directory.GetFiles(wavpath), File.Delete);
        }
    }
}
