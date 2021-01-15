using Common.DTO;
using Common.Helper;
using Common.Model;
using WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services
{
    public interface ITranscriptionService
    {
        IEnumerable<Transcription> GetAllTranscriptions();
        Transcription GetTranscriptionById(int id);
        Transcription CreateTranscription(TranscriptionDTO transcriptionDTO);
        Transcription UpdateTranscription(int id, TranscriptionDTO transcriptionDTO);
        Task<bool> AddRecordingToTranscription(TranscriptionDTO transcriptionDTO, IFormFile file);
        bool DeleteTranscription(int id);
        void TranscriptionFinished(TranscriptionDTO transcriptionDTO);
    }

    public class TranscriptionService : ITranscriptionService
    {
        private readonly AppDbContext _context;
        private readonly IOptions<MyConfig> _config;
        private HttpClient HttpClient = new HttpClient();

        public TranscriptionService(AppDbContext context)
        {
            _context = context;
        }

        public TranscriptionService(AppDbContext context, IOptions<MyConfig> config)
        {
            _context = context;
            _config = config;
        }

        public void TranscriptionFinished(TranscriptionDTO transcriptionDTO)
        {
            List<TranscriptionUser> transcriptionUsers = _context.TranscriptionUser.Include(x => x.User).Where(x => x.TranscriptionId == transcriptionDTO.Id).ToList();

            foreach(TranscriptionUser tu in transcriptionUsers)
            {
                transcriptionDTO.Users.Add(tu.User);
            }         
            var myContent = JsonConvert.SerializeObject(transcriptionDTO);
            var buffer = Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpClient.PostAsync("http://" + transcriptionDTO.UserIPAddress + "/", byteContent); //proper way to do it
            //HttpClient.PostAsync("http://localhost:4444/", byteContent); //because of missing admin rights
        }

        public IEnumerable<Transcription> GetAllTranscriptions()
        {
            return _context.Transcription.ToList();
        }

        public Transcription GetTranscriptionById(int id)
        {
            return _context.Transcription.SingleOrDefault(x => x.Id == id);
        }

        public Transcription CreateTranscription(TranscriptionDTO transcriptionDTO)
        {
            Transcription newTranscription = new Transcription();
            try
            {
                newTranscription.DateOfMeeting = transcriptionDTO.DateOfMeeting;
                newTranscription.MeetingPlatform = transcriptionDTO.MeetingPlatform;
                newTranscription.UserIPAddress = transcriptionDTO.UserIPAddress;
                
                foreach (User user in transcriptionDTO.Users)
                {
                    TranscriptionUser tu = new TranscriptionUser
                    {
                        UserId = user.Id,
                        TranscriptionId = newTranscription.Id
                    };

                    newTranscription.TranscriptionUsers.Add(tu);
                }

                _context.Transcription.Add(newTranscription);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                MyLogger.LogException(ex);
                return null;
            }

            return newTranscription;
        }

        public async Task<bool> AddRecordingToTranscription(TranscriptionDTO transcriptionDTO, IFormFile file)
        {
            try
            {
                await SaveFileToStorage(file);
            }
            catch (Exception ex)
            {
                MyLogger.LogException(ex);
                return false;
            }

            try
            {
                SendMessageToQueue(transcriptionDTO);
            }
            catch (Exception ex)
            {
                MyLogger.LogException(ex);
                return false;
            }

            return true;
        }

        public async Task SaveFileToStorage(IFormFile file)
        {
            var recordingpath = _config.Value.ServerStoragePath;

            if (file.Length > 0)
            {
                var filePath = Path.Combine(recordingpath, file.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
        }

        public void SendMessageToQueue(TranscriptionDTO transcriptionDTO)
        {
            var factory = new ConnectionFactory() { HostName = _config.Value.RabbitMQAddress };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: _config.Value.RabbitMQQueueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = JsonConvert.SerializeObject(transcriptionDTO);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "",
                                     routingKey: _config.Value.RabbitMQQueueName,
                                     basicProperties: properties,
                                     body: body);
            }
        }

        public Transcription UpdateTranscription(int id, TranscriptionDTO transcriptionDTO)
        {
            var transcriptionToBeUpdated = _context.Transcription.SingleOrDefault(x => x.Id == id);

            if (transcriptionToBeUpdated == null)
                return null;

            transcriptionToBeUpdated.MeetingPlatform = transcriptionDTO.MeetingPlatform;
            transcriptionToBeUpdated.TranscriptionUsers = transcriptionDTO.TranscriptionUsers;

            _context.SaveChanges();

            return transcriptionToBeUpdated;
        }

        public Transcription AddFileToTranscription(int id, TranscriptionDTO transcriptionDTO)
        {
            var transcriptionToBeUpdated = _context.Transcription.SingleOrDefault(x => x.Id == id);

            if (transcriptionToBeUpdated == null)
                return null;

            transcriptionToBeUpdated.TranscriptionARN = transcriptionDTO.TranscriptionARN;

            _context.SaveChanges();

            return transcriptionToBeUpdated;
        }

        public bool DeleteTranscription(int id)
        {
            var transcriptionToBeDeleted = _context.Transcription.SingleOrDefault(x => x.Id == id);

            if (transcriptionToBeDeleted == null)
                return false;

            _context.Transcription.Remove(transcriptionToBeDeleted);
            _context.SaveChanges();

            return true;
        }
    }
}
