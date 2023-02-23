﻿using static Perception.Data.Record;
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
        public List<ResultView> Results { get; set; }
        public DirectoryModeRecordView(Record record)
        {
            Id = record.Id;
            Dataset = record.Dataset is null ? "" : record.Dataset.Name;
            Mode = record.Mode.ToString();
            State = record.State.ToString();
            Confidence = record.Confidence;
            Time = record.Time.ToString("F");
            Results = new List<ResultView>();
            if (record.State == RecordState.Completed)
            {
                foreach (var file in record.Files)
                {
                    if (file.Results.Count > 0)
                        Results.Add(new ResultView
                        {
                            Filename = file.Name + file.Node.Extension,
                            Class = file.Results[0].Class,
                            Score = file.Results[0].Score,
                            InUrl = $"/work/{Id}/{file.Id}{file.Node.Extension}",
                            OutUrl = $"/work/{Id}/out/{file.Id}{file.Node.Extension}"
                        });
                    else
                        Results.Add(new ResultView
                        {
                            Filename = file.Name + file.Node.Extension,
                            Class = "未检出",
                            InUrl = $"/work/{Id}/{file.Id}{file.Node.Extension}",
                            OutUrl = $"/work/{Id}/out/{file.Id}{file.Node.Extension}"
                        });

                }
            }

        }
    }
    public class ResultView
    {
        public string Filename { get; set; }
        public string Class { get; set; }
        public float? Score { get; set; }
        public string? InUrl { get; set; }
        public string? OutUrl { get; set; }
    }
}
