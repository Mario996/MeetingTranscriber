using System.Collections.Generic;
using System.Threading.Tasks;
using Common.DTO;
using Common.Model;
using WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    //[Authorize]
    [ApiController]
    public class TranscriptionsController : ControllerBase
    {
        private readonly ITranscriptionService _transcriptionService;

        public TranscriptionsController(ITranscriptionService transcriptionService)
        {
            _transcriptionService = transcriptionService;
        }

        [HttpGet]
        public IEnumerable<Transcription> GetAllTranscriptions()
        {
            return _transcriptionService.GetAllTranscriptions();
        }

        [HttpGet("{id}", Name = "GetTranscriptionById")]
        public IActionResult GetTranscriptionById(int id)
        {
            var transcription = _transcriptionService.GetTranscriptionById(id);
            // TO DO:
            // Implement that user can get transcriptions only from meetings that
            // he was a part of.

            if (transcription == null)
                return NotFound();

            return Ok(transcription);
        }

        [HttpPost]
        // Action creates a transcription entity in DB without the TrnascriptionARN property, which
        // is added later by the AddRecordingToTranscription action
        public IActionResult CreateTranscription([FromBody]TranscriptionDTO transcriptionDTO)
        {
            var result = _transcriptionService.CreateTranscription(transcriptionDTO);

            if (result == null)
                return BadRequest();    
            else
                return CreatedAtAction(nameof(GetTranscriptionById), new { id = result.Id }, result);
        }

        [HttpPost("{id}/recording")]
        public async Task<IActionResult> AddRecordingToTranscription([FromForm] IFormFile file, int id)
        {
            var transcriptionDTO = _transcriptionService.GetTranscriptionById(id).TransformToDTO();
            var result = await _transcriptionService.AddRecordingToTranscription(transcriptionDTO, file);

            if (!result)
                return BadRequest();
            else
                return Ok();
        }

        [HttpPut]
        public IActionResult UpdateTranscription(int id, [FromBody]TranscriptionDTO transcriptionDTO)
        {
            var updatedTranscription = _transcriptionService.UpdateTranscription(id, transcriptionDTO);
            // TO DO:
            // Implement that user can update only transcriptions of the meetings that
            // he was a part of.

            if (updatedTranscription == null)
                return NotFound();
            else
                return Ok(updatedTranscription);
        }

        [HttpDelete]
        public IActionResult DeleteTranscription(int id)
        {
            // TO DO:
            // Implement that only admin can delete transcriptions
            if (!_transcriptionService.DeleteTranscription(id))
                return NotFound();
            else
                return Ok();
        }

        [HttpPost("finished")]
        public IActionResult TranscriptionFinished([FromBody]TranscriptionDTO dto)
        {
            _transcriptionService.TranscriptionFinished(dto);
            return Ok();
        }
    }
}