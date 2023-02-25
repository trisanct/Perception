using System.Text.RegularExpressions;
using Perception.Data;
using static Perception.Data.Record;
using static Perception.Data.Result;

namespace Perception.Models
{
    public class PredictModeRecordView
    {
        public int Id { get; set; }
        public string Dataset { get; set; }
        public string Mode { get; set; }
        public string State { get; set; }
        public string Filename { get; set; }
        public float Confidence { get; set; }
        public string Time { get; set; }
        public List<ResultView> Results { get; set; } 
        public string InUrl { get; set; }
        public string? OutUrl { get; set; }

        public PredictModeRecordView(Record record)
        {
            Id = record.Id;
            Dataset = record.Dataset is null ? "" : record.Dataset.Name;
            Mode = record.Mode.ToString();
            State = record.State.ToString();
            Filename = record.Files[0].Name + record.Files[0].Node.Extension;
            Confidence = record.Confidence;
            Time = record.Time.ToString("F");
            InUrl = $"/work/{Id}/{record.Files[0].Id}{record.Files[0].Node.Extension}";
            Results = new List<ResultView>();
            if (record.State == RecordState.Completed)
            {
                for(int i=0;i< record.Files[0].Results.Count;i++)
                {
                    Results.Add(new ResultView()
                    {
                        Id = i+1,
                        Class = record.Files[0].Results[i].Class,
                        Score = record.Files[0].Results[i].Score
                    });
                }
                OutUrl = $"/work/{Id}/{record.Files[0].Id}_out{record.Files[0].Node.Extension}";
            }

        }
    }

}
