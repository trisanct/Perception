using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Perception.Models.Record;

namespace Perception.Models
{
    public class Record
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } 
        public RecordMode Mode { get; set; }
        public RecordState State { get; set; }
        public int Fps { get; set; }
        public int TestInterval { get; set; }
        public bool Cuda { get; set; }
        public float Confidence { get; set; }
        public DateTime Time { get; set; }
        public List<Result> Results { get; set; } = new List<Result>();
        public enum RecordMode { Predict, Video, Fps, Directory }
        public enum RecordState { Waiting, Completed, Error}
        public Record() { }
        public Record(RecordForm record)
        {
            GUID= record.GUID;
            Mode= record.Mode;
            Fps= record.Fps;
            TestInterval= record.TestInterval;
            Cuda= record.Cuda;
            Confidence= record.Confidence;
            State = RecordState.Waiting;
        }
    }
    public class RecordForm
    {
        public Guid GUID { get; set; }
        public RecordMode Mode { get; set; }
        public int Fps { get; set; }
        public int TestInterval { get; set; }
        public bool Cuda { get; set; }
        public float Confidence { get; set; }
    }
}
