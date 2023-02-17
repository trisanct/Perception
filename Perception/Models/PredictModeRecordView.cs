using System.Text.RegularExpressions;
using static Perception.Models.Record;
using static Perception.Models.Result;

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

        public PredictModeRecordView(Record record, FileMap file)
        {
            Id = record.Id;
            Mode = record.Mode.ToString();
            State = record.State.ToString();
            Filename = file.Name + file.Node.Extension;
            Confidence = record.Confidence;
            Time = record.Time.ToString("F");
            var reg = @"(?<!^)(?=[A-Z])";
            if (record.State == RecordState.Completed)
            {
                Class = Regex.Split(record.Results[0].Class.ToString(), reg).Aggregate("", (res, next) => res == "" ? next : res + " " + next);
                Score = record.Results[0].Score;
                InUrl = $"/work/{Id}/{Id}{file.Node.Extension}";
                OutUrl = $"/work/{Id}/{Id}_out{file.Node.Extension}";
            }

        }
    }

}
