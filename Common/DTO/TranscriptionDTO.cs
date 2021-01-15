using Common.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.DTO
{
    public class TranscriptionDTO
    {
        public int Id { get; set; }
        public string TranscriptionARN { get; set; }
        public string MeetingPlatform { get; set; }
        public DateTime DateOfMeeting { get; set; } = DateTime.Today;
        public List<TranscriptionUser> TranscriptionUsers { get; set; } = new List<TranscriptionUser>();
        public List<User> Users { get; set; } = new List<User>();
        public string UserIPAddress { get; set; }
        public string TranscriptionText { get; set; }
        public TranscriptionDTO()
        {

        }

        public TranscriptionDTO(string meetingPlatform, List<User> users, string IP)
        {
            MeetingPlatform = meetingPlatform;
            Users = users;
            UserIPAddress = IP;
        }
    }
}
