using Common.DTO;
using Common.Helper;
using Common.Model;
using WebAPI.Controllers;
using WebAPI.Infrastructure;
using WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace WebAPI.Tests
{
    [TestFixture]
    public class TranscriptionsControllerTests
    {
        private TranscriptionService _transcriptionService;
        private TranscriptionsController _controller;

        [SetUp]
        public void Setup()
        {
            IOptions<MyConfig> myConfig = Options.Create(new MyConfig { BucketName = "meeting2textbucket", Path = "..\\Recordings\\" });
            _transcriptionService = new TranscriptionService(GetContextWithData());
            _controller = new TranscriptionsController(_transcriptionService);
        }

        [Test]
        public void GetAllTranscriptions_WhenCalled_ShouldReturnAllTranscriptions()
        {
            var result = _controller.GetAllTranscriptions();

            Assert.That(result, Is.EquivalentTo(GetContextWithData().Transcription.ToListAsync().Result));
        }

        [Test]
        [TestCase(1)]
        public void GetTranscriptionById_ValidTranscriptionId_ShouldReturnOk(int id)
        {
            var result = _controller.GetTranscriptionById(id);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            OkObjectResult objectResult = (OkObjectResult)result;

            Assert.That(objectResult.Value, Has.Property("Id").EqualTo(GetContextWithData().Transcription.SingleOrDefaultAsync(x => x.Id == 1).Result.Id)
                                  & Has.Property("TranscriptionARN").EqualTo(GetContextWithData().Transcription.SingleOrDefaultAsync(x => x.Id == 1).Result.TranscriptionARN)
                                  & Has.Property("MeetingPlatform").EqualTo(GetContextWithData().Transcription.SingleOrDefaultAsync(x => x.Id == 1).Result.MeetingPlatform));
        }

        [Test]
        [TestCase(4)]
        public void GetTranscriptionById_InvalidTranscriptionId_ShouldReturnNotFound(int id)
        {
            var result = _controller.GetTranscriptionById(id);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }


        [Test]
        [TestCase("Skype")]
        public void CreateTranscription_ValidParameters_ShouldReturnCreatedAtRoute(string meetingPlatform)
        {
            _transcriptionService.DeleteTranscription(1);

            var transcriptionDTO = new TranscriptionDTO
            {
                MeetingPlatform = meetingPlatform,
                TranscriptionUsers = new List<TranscriptionUser>()
            };

            var result = _controller.CreateTranscription(transcriptionDTO);

            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());

            CreatedAtActionResult objectResult = (CreatedAtActionResult)result;

            Assert.That(objectResult.Value, Has.Property("Id").EqualTo(GetContextWithData().Transcription.SingleOrDefaultAsync(x => x.Id == 1).Result.Id)
                                  & Has.Property("MeetingPlatform").EqualTo(GetContextWithData().Transcription.SingleOrDefaultAsync(x => x.Id == 1).Result.MeetingPlatform));
        }

        [Test]
        [TestCase(1, "arn:aws:s3:::meeting2textbucket/test.zip", "Teams")]
        public void UpdateTranscription_ValidTranscriptionId_ShouldChangeMeetingPlatformAndReturnOk(int id, string transcriptionARN, string meetingPlatform)
        {
            var transcriptionDTO = new TranscriptionDTO
            {
                TranscriptionARN = transcriptionARN,
                MeetingPlatform = meetingPlatform,
                TranscriptionUsers = new List<TranscriptionUser>()
            };

            var result = _controller.UpdateTranscription(id, transcriptionDTO);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            OkObjectResult objectResult = (OkObjectResult)result;

            Assert.That(objectResult.Value, Has.Property("Id").EqualTo(GetContextWithData().Transcription.SingleOrDefaultAsync(x => x.Id == 1).Result.Id)
                                  & Has.Property("TranscriptionARN").EqualTo(GetContextWithData().Transcription.SingleOrDefaultAsync(x => x.Id == 1).Result.TranscriptionARN)
                                  & Has.Property("MeetingPlatform").EqualTo("Teams"));
        }

        [Test]
        [TestCase(5, "arn:aws:s3:::meeting2textbucket/test.zip", "Teams")]
        public void UpdateTranscription_InvalidTranscriptionId_ShouldReturnNotFound(int id, string transcriptionARN, string meetingPlatform)
        {
            var transcriptionDTO = new TranscriptionDTO
            {
                TranscriptionARN = transcriptionARN,
                MeetingPlatform = meetingPlatform,
                TranscriptionUsers = new List<TranscriptionUser>()
            };

            var result = _controller.UpdateTranscription(id, transcriptionDTO);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        [TestCase(1)]
        public void DeleteTranscription_ValidTranscriptionId_ShouldReturnOk(int id)
        {
            var result = _controller.DeleteTranscription(id);

            Assert.That(result, Is.TypeOf<OkResult>());
        }

        [Test]
        [TestCase(4)]
        public void DeleteTranscription_InvalidTranscriptionId_ShouldReturnNotFound(int id)
        {
            var result = _controller.DeleteTranscription(id);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        private AppDbContext GetContextWithData()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                              .UseInMemoryDatabase(Guid.NewGuid().ToString())
                              .Options;
            var context = new AppDbContext(options);

            context.Transcription.Add(new Transcription { Id = 1, TranscriptionARN = "arn:aws:s3:::meeting2textbucket/test.zip", MeetingPlatform = "Skype", DateOfMeeting = DateTime.Today });
            context.Transcription.Add(new Transcription { Id = 2, TranscriptionARN = "arn:aws:s3:::meeting2textbucket/test.zip", MeetingPlatform = "Slack", DateOfMeeting = DateTime.Today });
            context.Transcription.Add(new Transcription { Id = 3, TranscriptionARN = "arn:aws:s3:::meeting2textbucket/test.zip", MeetingPlatform = "Teams", DateOfMeeting = DateTime.Today });
            context.SaveChanges();
            return context;
        }
    }
}
