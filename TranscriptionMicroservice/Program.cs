using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Common.DTO;
using Common.Helper;
using Common.Model;
using WebAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace TranscriptionMicroservice
{
    class Program
    {
        private static AppDbContext _context;
        private static ServiceCollection _services;
        private static HttpClient HttpClient { get; set; }
        private static IAmazonS3 S3Client { get; set; }
        private static AmazonTranscribeServiceClient TranscribeClient { get; set; }

        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.EUCentral1;
        private static readonly ManualResetEvent _quitEvent = new ManualResetEvent(false);

        #region Configuration variables
        private static string _path = "";
        private static string _bucketName = "";
        private static string _jsonName = "";
        private static string _serverStoragePath = "";
        private static string _rabbitMQAddress = "";
        private static string _rabbitMQQueueName = "";
        private static string _mixedAudioName = "";
        private static string _finishedActionURL = "";
        private static string _transcriptionText = "";
        #endregion

        static void Main()
        {
            Setup();

            TranscriptionDTO dto = new TranscriptionDTO();
            string message = "";

            var factory = new ConnectionFactory() { HostName = _rabbitMQAddress };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: _rabbitMQQueueName,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                var consumer = new EventingBasicConsumer(channel);


                consumer.Received += async (model, eventArguments) =>
                {
                    var body = eventArguments.Body;
                    message = Encoding.UTF8.GetString(body);
                    dto = JsonConvert.DeserializeObject<TranscriptionDTO>(message);
                    channel.BasicAck(deliveryTag: eventArguments.DeliveryTag, multiple: false);
                    Console.WriteLine(" [x] Received {0}", message);
                    await MessageReceived(dto);
                };


                channel.BasicConsume(queue: _rabbitMQQueueName,
                        autoAck: false,
                        consumer: consumer);

                _quitEvent.WaitOne();
            }

        }

        public static void Setup()
        {
            _services = new ServiceCollection();

            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            _services.AddDbContext<AppDbContext>(options =>
                                                   options.UseSqlServer(ConfigurationManager.AppSettings.Get("connectionstring")));
            var serviceProvider = _services.BuildServiceProvider();
            _context = serviceProvider.GetService<AppDbContext>();
            S3Client = new AmazonS3Client(bucketRegion);
            TranscribeClient = new AmazonTranscribeServiceClient();
            HttpClient = new HttpClient();
            _path = ConfigurationManager.AppSettings.Get("path");
            _bucketName = ConfigurationManager.AppSettings.Get("bucketname");
            _jsonName = ConfigurationManager.AppSettings["jsonfilename"];
            _serverStoragePath = ConfigurationManager.AppSettings["serverstoragepath"];
            _rabbitMQAddress = ConfigurationManager.AppSettings["RabbitMQAddress"];
            _rabbitMQQueueName = ConfigurationManager.AppSettings["RabbitMQQueueName"];
            _mixedAudioName = ConfigurationManager.AppSettings["mixedaudioname"];
            _finishedActionURL = ConfigurationManager.AppSettings["finishedactionurl"];
        }

        public async static Task MessageReceived(TranscriptionDTO dto)
        {
            ZipFile.ExtractToDirectory(_serverStoragePath + _mixedAudioName + dto.Id.ToString() + ".zip", _serverStoragePath + "\\recordings");

            await UploadFileAsync(_bucketName, _serverStoragePath + "recordings\\" + _mixedAudioName + dto.Id.ToString() + ".wav");

            try
            {
                dto = await CreateTranscription(dto);

                var myContent = JsonConvert.SerializeObject(dto);
                var buffer = Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                await HttpClient.PostAsync(_finishedActionURL, byteContent); //kada se transkripcija zavrsi, obavestiti web api
            }
            catch (Exception ex)
            {
                MyLogger.LogException(ex);
            }
        }

        //Ovde vracam DTO da bih mogao da pristupim tekstu transkripcije
        public static async Task<TranscriptionDTO> CreateTranscription(TranscriptionDTO dto)
        {
            Transcription transcriptionToBeUpdated = _context.Transcription.SingleOrDefaultAsync(x => x.Id == dto.Id).Result; //retrive object from DB

            transcriptionToBeUpdated.TranscriptionARN = await ProcessTranscribe(_bucketName, _mixedAudioName + dto.Id.ToString() + ".wav"); //only change ARN because it was empty before
            dto.TranscriptionText = _transcriptionText;
            dto.TranscriptionARN = transcriptionToBeUpdated.TranscriptionARN;

            _context.Transcription.Update(transcriptionToBeUpdated);
            _context.SaveChanges();

            return dto;
        }

        public static async Task UploadFileAsync(string bucketname, string keyname)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(S3Client);

                await fileTransferUtility.UploadAsync(keyname, bucketname);
            }
            catch (AmazonS3Exception e)
            {
                MyLogger.LogException(e);
            }
            catch (Exception e)
            {
                MyLogger.LogException(e);
            }

        }

        public static async Task<string> ProcessTranscribe(string bucket, string key)
        {
            Settings settings = new Settings();

            int index = key.LastIndexOf('\\') + 1;  //vadjenje imena fajla iz putanje
            string keyname = key.Substring(index);
            string transcriptionID = keyname.Split('.')[0].Substring(10);


            Media media = new Media
            {
                MediaFileUri = string.Format("https://s3.eu-central-1.amazonaws.com/{0}/{1}", bucket, keyname)
            };
            CancellationToken token = new CancellationToken();


            StartTranscriptionJobRequest startJobRequest = new StartTranscriptionJobRequest
            {
                LanguageCode = LanguageCode.EnUS,
                Settings = settings,
                Media = media,
                MediaFormat = MediaFormat.Wav,
                TranscriptionJobName = Guid.NewGuid().ToString()
            };

            await TranscribeClient.StartTranscriptionJobAsync(startJobRequest, token);


            GetTranscriptionJobRequest getJobRequest = new GetTranscriptionJobRequest();
            GetTranscriptionJobResponse getJobResponse = new GetTranscriptionJobResponse();
            getJobRequest.TranscriptionJobName = startJobRequest.TranscriptionJobName;

            bool isComplete = false;
            while (!isComplete)
            {
                getJobResponse = await TranscribeClient.GetTranscriptionJobAsync(getJobRequest);
                if (getJobResponse.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                {
                    isComplete = true;
                }
                else if (getJobResponse.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.FAILED)
                {
                    isComplete = true;
                    MyLogger.LogException(new Exception(getJobResponse.TranscriptionJob.FailureReason));
                }
                else
                {
                    Thread.Sleep(3000);//wait 3 seconds and check again
                }
            }

            if (getJobResponse.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                return GetTranscriptionFile(getJobResponse.TranscriptionJob.Transcript.TranscriptFileUri, transcriptionID);
            else
                return null;
        }

        private static string GetTranscriptionFile(string transcriptURI, string transcriptionID)
        {
            var webRequest = WebRequest.Create(transcriptURI);
            using (var response = webRequest.GetResponse())
            using (var content = response.GetResponseStream())
            using (var reader = new StreamReader(content))
            {
                _transcriptionText = reader.ReadToEnd();
            }

            return UploadJsonToS3(_transcriptionText, transcriptionID);
        }

        private static string UploadJsonToS3(string stringContent, string transcriptionID)
        {
            JObject transcriptJSON = JObject.Parse(stringContent);
            JObject sendObject = new JObject(
                new JProperty("Text", (string)transcriptJSON["results"]["transcripts"][0]["transcript"]));

            _transcriptionText = sendObject.ToString().Split(':')[1].Replace('"',' ').Replace('}',' '); //vadjenje samo teksta iz odgovora transkripcije

            //save json to a file
            File.WriteAllText(_path + _jsonName + transcriptionID + ".json", stringContent);

            //compress json file
            using (ZipArchive zip = ZipFile.Open(_path + _jsonName + transcriptionID + ".zip", ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(_path + _jsonName + transcriptionID + ".json", Path.GetFileName(_path + _jsonName + transcriptionID + ".json"));
            }

            // upload it to s3
            try
            {
                AmazonS3Client s3 = new AmazonS3Client(bucketRegion);
                using (TransferUtility tranUtility = new TransferUtility(s3))
                {

                    tranUtility.Upload(_path + _jsonName + transcriptionID + ".zip", _bucketName);

                    return "arn:aws:s3:::" + _bucketName + "/" + _jsonName + transcriptionID + ".zip";
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogException(ex);
            }

            return "error";
        }
    }
}
