using Common.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Model
{
    public class Transcription
    {
        public int Id { get; set; }
        public string TranscriptionARN { get; set; }
        public string MeetingPlatform { get; set; }
        public DateTime DateOfMeeting { get; set; } = DateTime.Today;
        public List<TranscriptionUser> TranscriptionUsers { get; set; } = new List<TranscriptionUser>();
        public string UserIPAddress { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Id == ((Transcription)obj).Id && TranscriptionARN == ((Transcription)obj).TranscriptionARN;
        }

        public override int GetHashCode()
        {
            var hashCode = 939636486;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TranscriptionARN);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MeetingPlatform);
            hashCode = hashCode * -1521134295 + DateOfMeeting.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<List<TranscriptionUser>>.Default.GetHashCode(TranscriptionUsers);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserIPAddress);
            return hashCode;
        }

        public TranscriptionDTO TransformToDTO()
        {
            TranscriptionDTO returnValue = new TranscriptionDTO
            {
                Id = Id,
                TranscriptionARN = TranscriptionARN,
                MeetingPlatform = MeetingPlatform,
                DateOfMeeting = DateOfMeeting,
                TranscriptionUsers = TranscriptionUsers,
                UserIPAddress = UserIPAddress
            };
            return returnValue;
        }

    }
}
