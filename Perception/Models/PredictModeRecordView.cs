using System.Text.RegularExpressions;
using Perception.Data;
using static Perception.Data.Record;
using static Perception.Data.Result;

namespace Perception.Models
{
    public class PredictModeRecordView
    {
        public int Id { get; set; }
        public string Mode { get; set; }
        public string State { get; set; }
        public string Filename { get; set; }
        public float Confidence { get; set; }
        public string Time { get; set; }
        public string? Class { get; set; }
        public float? Score { get; set; }
        public string? InUrl { get; set; }
        public string? OutUrl { get; set; }

        public PredictModeRecordView(Record record)
        {
            Id = record.Id;
            Mode = record.Mode.ToString();
            State = record.State.ToString();
            Filename = record.Files[0].Name + record.Files[0].Node.Extension;
            Confidence = record.Confidence;
            Time = record.Time.ToString("F");
            if (record.State == RecordState.Completed)
            {
                Class = record.Files[0].Results[0].Class;
                Score = record.Files[0].Results[0].Score;
                InUrl = $"/work/{Id}/{Id}{record.Files[0].Node.Extension}";
                OutUrl = $"/work/{Id}/{Id}_out{record.Files[0].Node.Extension}";
            }

        }
    }

}
