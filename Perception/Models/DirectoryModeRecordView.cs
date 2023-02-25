using static Perception.Data.Record;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Perception.Data;

namespace Perception.Models
{
    public class DirectoryModeRecordView
    {
        public int Id { get; set; }
        public string Dataset { get; set; }
        public string Mode { get; set; }
        public string State { get; set; }
        public float Confidence { get; set; }
        public string Time { get; set; }
        public List<FileResultView> FileResults { get; set; }
        public DirectoryModeRecordView(Record record)
        {
            Id = record.Id;
            Dataset = record.Dataset is null ? "" : record.Dataset.Name;
            Mode = record.Mode.ToString();
            State = record.State.ToString();
            Confidence = record.Confidence;
            Time = record.Time.ToString("F");
            FileResults = new List<FileResultView>();
            if (record.State == RecordState.Completed)
            {
                foreach (var file in record.Files)
                {
                    var fileresult = new FileResultView()
                    {
                        Filename = file.Name + file.Node.Extension,
                        InUrl = $"/work/{Id}/{file.Id}{file.Node.Extension}",
                        OutUrl = $"/work/{Id}/out/{file.Id}{file.Node.Extension}",
                        Results = new List<ResultView>()
                    };
                    if (file.Results.Count > 0)
                    {
                        for (int i = 0; i < file.Results.Count; i++)
                        {
                            fileresult.Results.Add(new ResultView()
                            {
                                Id = i + 1,
                                Class = file.Results[i].Class,
                                Score = file.Results[i].Score
                            });
                        }
                    }
                    FileResults.Add(fileresult);
                }
            }

        }
    }
    public class FileResultView
    {
        public string Filename { get; set; }
        public List<ResultView> Results { get; set; }
        public string? InUrl { get; set; }
        public string? OutUrl { get; set; }
    }
}
