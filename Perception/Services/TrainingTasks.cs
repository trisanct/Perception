using Microsoft.AspNetCore.Components.Forms;
using Perception.Data;
using Perception.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Channels;

namespace Perception.Services
{
    public class TrainingTasks
    {
        private readonly string testinputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\input";
        private readonly string inputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\upload";
        private readonly string outputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\output";
        private readonly string basedatasetpath = Directory.GetCurrentDirectory() + $@"\Neural\dataset";
        private readonly string trainingpath = Directory.GetCurrentDirectory() + @"\Neural\train.py";
        private readonly string annotationpath = Directory.GetCurrentDirectory() + @"\Neural\voc_annotation.py";
        private Channel<Task> Tasks { get; }
        private IServiceScopeFactory ScopeFactory { get; }
        public TaskQueue(IServiceScopeFactory scopeFactory)
        {
            Tasks = Channel.CreateUnbounded<Task>();
            ScopeFactory = scopeFactory;
        }
        public async Task QueueTaskAsync(Dataset dataset)
        {
            Task task;
            if (record.Mode == Record.RecordMode.Predict) task = PredictModeTask(record);
            else if (record.Mode == Record.RecordMode.Directory) task = DirectoryModeTask(record);
            else task = TestPredict(record, record.Files);
            await Tasks.Writer.WriteAsync(task);
        }

        public async Task<Task> DequeueAsync()
        {
            return await Tasks.Reader.ReadAsync();
        }
        public async Task Trainingtask(Dataset dataset)
        {
            var datasetpath=$@"{basedatasetpath}\{dataset.Id}";
            var p1=new Process()
            {
                StartInfo=new StartInfo()
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    WorkingDirectory = datasetpath,
                    FileName = "python",
                    Arguments = $@"{annotationpath}"
                }
            };
            p1.Start();
            
        }
    }
}
