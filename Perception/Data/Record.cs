using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Perception.Data.Record;

namespace Perception.Data
{
    public class Record
    {
        public int Id { get; set; }
        public int DatasetId { get; set; }
        public RecordMode Mode { get; set; }
        public RecordState State { get; set; }
        public int Fps { get; set; }
        public int TestInterval { get; set; }
        public bool Cuda { get; set; }
        public float Confidence { get; set; }
        public DateTime Time { get; set; }
        public List<FileMap> Files { get; set; } = new List<FileMap>();
        public Dataset? Dataset { get; set; }
        public enum RecordMode { Predict, Video, Fps, Directory }
        public enum RecordState { Waiting, Completed, Error }
        public Record() { }
        public Record(RecordForm record)
        {
            Mode = record.Mode;
            Fps = record.Fps;
            TestInterval = record.TestInterval;
            Cuda = record.Cuda;
            Confidence = record.Confidence;
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
