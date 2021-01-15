using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Model
{
    public class TranscriptionUser
    {
        public int TranscriptionId { get; set; }
        public Transcription Transcription { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
