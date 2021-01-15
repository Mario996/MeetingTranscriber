using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using Common.Model;
using System.Collections.ObjectModel;
using Common.DTO;
using Common.Helper;
using System.Configuration;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace RecordingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveFileWriter waveFileSpeakers = null;
        private WaveFileWriter waveFileMic = null;
        private WasapiLoopbackCapture waveSourceSpeakers = null;
        private WaveIn waveSourceMic = null;
        private static readonly HttpClient client = new HttpClient();
        private static readonly WebClient webClient = new WebClient();
        private HttpServer HttpServer = null;
        private static IPAddress IPAddress = null;

        #region Configuration variables
        private readonly string wavpath = ConfigurationManager.AppSettings["wavpath"];
        private readonly string zippath = ConfigurationManager.AppSettings["zippath"];
        private readonly string micfilename = ConfigurationManager.AppSettings["micfilename"];
        private readonly string speakerfilename = ConfigurationManager.AppSettings["speakerfilename"];
        private readonly string mixedfilefullname = ConfigurationManager.AppSettings["mixedfilefullname"];
        private readonly string mixedfilename = ConfigurationManager.AppSettings["mixedfilename"];
        private readonly string TranscriptionsAPIEndpoint = ConfigurationManager.AppSettings["TranscriptionsAPIEndpoint"];
        private readonly string UsersAPIEndpoint = ConfigurationManager.AppSettings["UsersAPIEndpoint"];
        private readonly string[] meetingplatforms = ConfigurationManager.AppSettings["meetingplatforms"].Split(',');
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            TranscribeButton.IsEnabled = false;
            StopRecordingButton.IsEnabled = false;
            MeetingPlatformComboBox.ItemsSource = meetingplatforms;
            HttpServer = new HttpServer();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IPAddress = GetIPAddress();
            HttpServer.Start(IPAddress.ToString());
            //HttpServer.Start("http://localhost:4444/");
            ListBoxAddedUsers.DisplayMemberPath = "Username";
            MeetingPlatformComboBox.SelectedItem = "Skype";
            FilteredComboBox1.ItemsSource = await GetUsersAsync();
            FilteredComboBox1.DisplayMemberPath = "Username";
            FilteredComboBox1.IsEditable = true;
            FilteredComboBox1.IsTextSearchEnabled = false;
        }
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            HttpServer.Stop();
        }

        private async Task<ObservableCollection<User>> GetUsersAsync()
        {
            ObservableCollection<User> users = new ObservableCollection<User>();
            HttpResponseMessage response = await client.GetAsync(UsersAPIEndpoint);
            if (response.IsSuccessStatusCode)
            {
                users = await response.Content.ReadAsAsync<ObservableCollection<User>>();
            }
            return users;
        }

        void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            var senderName = sender.GetType();
            if (waveFileMic != null && senderName.Name == "WaveIn")
            {
                waveFileMic.Write(e.Buffer, 0, e.BytesRecorded);
                waveFileMic.Flush();
            }
            if (waveFileSpeakers != null && senderName.Name == "WasapiLoopbackCapture")
            {
                waveFileSpeakers.Write(e.Buffer, 0, e.BytesRecorded);
                waveFileSpeakers.Flush();
            }
        }

        void WaveSource_RecordingStoppedAsync(object sender, StoppedEventArgs e)
        {
            DisposeWaveFileSources(new IWaveIn[] { waveSourceSpeakers, waveSourceMic });

            DisposeWaveFileWriters(new[] { waveFileMic, waveFileSpeakers });

            using (var speakerReader = new AudioFileReader(wavpath + speakerfilename))
            using (var micReader = new AudioFileReader(wavpath + micfilename))
            {
                var mixer = new MixingSampleProvider(new[] { speakerReader, micReader });
                WaveFileWriter.CreateWaveFile16(wavpath + mixedfilefullname, mixer);
            }

            this.TranscribeButton.IsEnabled = true;
        }

        public void DisposeWaveFileWriters(WaveFileWriter[] fileWriters)
        {
            for (int i = 0; i < fileWriters.Length; i++)
            {
                if (fileWriters[i] != null)
                {
                    fileWriters[i].Dispose();
                    fileWriters[i] = null;
                }
            }
        }

        public void DisposeWaveFileSources(IWaveIn[] waveSources)
        {
            for (int i = 0; i < waveSources.Length; i++)
            {
                if (waveSources[i] != null)
                {
                    waveSources[i].Dispose();
                    waveSources[i] = null;
                }
            }
        }

        private void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            waveSourceSpeakers = new WasapiLoopbackCapture();

            waveFileSpeakers = new WaveFileWriter(wavpath + speakerfilename, waveSourceSpeakers.WaveFormat);

            waveSourceSpeakers.DataAvailable += new EventHandler<WaveInEventArgs>(WaveSource_DataAvailable);
            waveSourceSpeakers.RecordingStopped += new EventHandler<StoppedEventArgs>(WaveSource_RecordingStoppedAsync);

            waveSourceSpeakers.StartRecording();

            waveSourceMic = new WaveIn
            {
                WaveFormat = waveSourceSpeakers.WaveFormat
            };

            waveFileMic = new WaveFileWriter(wavpath + micfilename, waveSourceMic.WaveFormat);

            waveSourceMic.DataAvailable += new EventHandler<WaveInEventArgs>(WaveSource_DataAvailable);
            waveSourceMic.RecordingStopped += new EventHandler<StoppedEventArgs>(WaveSource_RecordingStoppedAsync);

            waveSourceMic.StartRecording();

            StartRecordingButton.IsEnabled = false;
            StopRecordingButton.IsEnabled = true;
        }


        private void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            waveSourceSpeakers.StopRecording();
            waveSourceMic.StopRecording();

            StartRecordingButton.IsEnabled = false;
            StopRecordingButton.IsEnabled = false;
            Application.Current.MainWindow.Height = 450;
        }

        private async void TranscribeButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Height = 135;
            StartRecordingButton.IsEnabled = true;

            TranscriptionDTO dto = new TranscriptionDTO(MeetingPlatformComboBox.SelectedItem.ToString(),
                                                       ListBoxAddedUsers.Items.Cast<User>().ToList(),
                                                       IPAddress.ToString());

            try
            {
                var response = await client.PostAsJsonAsync(TranscriptionsAPIEndpoint, dto);
                var jsonDTO = JsonConvert.SerializeObject(dto);
                var responseObject = (Transcription)await response.Content.ReadAsAsync(typeof(Transcription));
                using (var webclient = new WebClient())
                {
                    File.Move(wavpath + mixedfilefullname, wavpath + mixedfilename + responseObject.Id + ".wav");

                    using (ZipArchive zip = ZipFile.Open(zippath + responseObject.Id + ".zip", ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(wavpath + mixedfilename + responseObject.Id + ".wav",
                            Path.GetFileName(wavpath + mixedfilename + responseObject.Id + ".wav"));
                    }
                    webClient.UploadFileAsync(new Uri(String.Format(TranscriptionsAPIEndpoint + "/{0}/recording", responseObject.Id))
                    , "POST"
                    , zippath + responseObject.Id.ToString() + ".zip");
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogException(ex);
            }
            ResetUI();
        }

        private void DeleteAudioButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you want to delete audio?", "Confirmation", MessageBoxButton.YesNo);
            if (result.Equals(MessageBoxResult.Yes))
            {
                ResetUI();
                Application.Current.MainWindow.Height = 135;
                StartRecordingButton.IsEnabled = true;
                StopRecordingButton.IsEnabled = false;
                Array.ForEach(Directory.GetFiles(wavpath), File.Delete);
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            User addedUser = new User();
            if (FilteredComboBox1.SelectedItem is User && !ListBoxAddedUsers.Items.Contains(FilteredComboBox1.SelectedItem))
            {
                addedUser = (User)FilteredComboBox1.SelectedItem;
                ListBoxAddedUsers.Items.Add(addedUser);
            }
        }

        private void RemoveUserButton_Click(object sender, RoutedEventArgs e)
        {
            User removedUser = new User();
            if (ListBoxAddedUsers.SelectedItem is User && ListBoxAddedUsers.Items.Contains(ListBoxAddedUsers.SelectedItem))
            {
                ListBoxAddedUsers.Items.Remove((User)ListBoxAddedUsers.SelectedItem);
            }
        }

        public static IPAddress GetIPAddress()
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses("");

            foreach (IPAddress hostAddress in hostAddresses)
            {
                if (hostAddress.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(hostAddress) &&  // ignore loopback addresses
                    !hostAddress.ToString().StartsWith("169.254."))  // ignore link-local addresses
                    return hostAddress;
            }
            return IPAddress.None;
        }

        public void ResetUI()
        {
            MeetingPlatformComboBox.SelectedItem = "Skype";
            ListBoxAddedUsers.Items.Clear();
            FilteredComboBox1.SelectedIndex = -1; //clears combobox selection
        }
    }
}
