using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perception.Data;
using Perception.Models;
using Perception.Services;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using static Perception.Data.Record;

namespace Perception.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PerceptionController : ControllerBase
    {
        private readonly PerceptionContext context;
        private PredictTaskQueue Tasks { get; }
        private readonly TrainService trainService;
        public PerceptionController(PerceptionContext context, PredictTaskQueue tasks, TrainService trainService)
        {
            this.context = context;
            Tasks = tasks;
            this.trainService = trainService;
        }
        private byte[] HexToByte(string hex) { return Enumerable.Range(0, 32).Select(i => Convert.ToByte(hex.Substring(i << 1, 2), 16)).ToArray(); }
        private (string name, string ext) Apart(string fullname)
        {
            var exti = fullname.LastIndexOf('.');
            if (exti == -1) return (fullname, "");
            else return (fullname.Substring(0, exti), fullname.Substring(exti));
        }
        //for Submit.vue
        [HttpGet("/[Controller]/[Action]")]
        public IActionResult Hello()
        {
            return Ok("Hello");
        }
        [HttpGet("/[Controller]/[Action]/{id}")]
        public async Task<IActionResult> TestDaskAsync(int id)
        {
            var r = await context.Records.Where(r => r.Id == id).Include(r => r.Files).ThenInclude(f => f.Node).FirstAsync();
            _ = Tasks.QueuePredictTaskAsync(r);
            //if (r != null)
            //{
            //    var fs = context.Files.Where(f => f.GUID == r.GUID).Include(f => f.Node).ToList(); 
            //    _ = Tasks.QueueTaskAsync(r, fs);
            //}
            return Ok();
        }
        [HttpGet("/[Controller]/[Action]")]
        public IActionResult GetGUID()
        {
            var guid = Guid.NewGuid();
            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{guid}");
            return Ok(guid);
        }

        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> GetGUID([FromBody] FileVessel vessel)
        {
            try
            { 
                if (vessel.Filename == null || vessel.Sha == null) return BadRequest();
                var guid = Guid.NewGuid();
                var (name, ext) = Apart(vessel.Filename);
                var sha = HexToByte(vessel.Sha);
                var node = await context.Nodes.Where(f => f.SHA == sha).FirstOrDefaultAsync();
                var start = 0;
                var existed = false;
                if (node == null)
                {
                    node = new FileNode(ext, sha, false);
                    await context.Nodes.AddAsync(node);
                    await context.SaveChangesAsync();
                    var path = Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{node.Id}";
                    Directory.CreateDirectory(path);
                }
                else
                {
                    if (node.Ready) existed = true;
                    else
                    {
                        var path = Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{node.Id}";
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        //断点续传 else start = Directory.GetFiles(Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{node.Id}").Count();
                    }
                }
                var f = new FileMap(guid, name, node.Id);
                await context.Files.AddAsync(f);
                await context.SaveChangesAsync();
                return Ok(new { guid, existed , start });
            }
            catch { return BadRequest(); }
        }

        [HttpPost("/[Controller]/[Action]/{mode}")]
        public async Task<IActionResult> UploadFile(string mode, [FromForm(Name = "file")] IFormFile file, [FromForm(Name = "sha")] string shastring, [FromForm(Name = "guid")] string? guidstring)
        {
            try
            {
                if (file != null)
                {
                    if (file.Length > 0)
                    {
                        Guid guid;
                        if (mode == "predict") guid = Guid.NewGuid();
                        else if (mode == "directory")
                        {
                            if (guidstring == null) return BadRequest();
                            else guid = new Guid(guidstring);
                        }
                        else return BadRequest();
                        var sha = HexToByte(shastring);
                        var node = await context.Nodes.Where(f => f.SHA == sha).FirstOrDefaultAsync();
                        var (name, ext) = Apart(file.FileName);
                        if (node == null)
                        {
                            node = new FileNode(ext, sha, true);
                            await context.Nodes.AddAsync(node);
                            await context.SaveChangesAsync();
                            var path = Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{node.Id}{node.Extension}";
                            using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream);
                        }
                        var f = new FileMap(guid, name, node.Id);
                        await context.Files.AddAsync(f);
                        await context.SaveChangesAsync();
                        return Ok(guid);
                    }
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("/[Controller]/[Action]/{end}")]
        public async Task<IActionResult> UploadSlice(bool end, [FromForm(Name = "file")] IFormFile file, [FromForm(Name = "guid")] string guidstring, [FromForm(Name = "id")] string id)
        {
            try
            {
                if (file != null)
                {
                    if (file.Length > 0)
                    {
                        var guid = new Guid(guidstring);
                        var f = await context.Files.Where(f => f.GUID == guid).SingleAsync();
                        var filePath = Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{f.NodeId}\{id}.slice";
                        using (var stream = System.IO.File.Create(filePath)) await file.CopyToAsync(stream);
                        if (end) return await MergeSlices(guid, Int32.Parse(id));
                        return Ok();
                    }
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        private async Task<IActionResult> MergeSlices(Guid guid, int id)
        {
            try
            {
                var vessel = await context.Files.Where(f => f.GUID == guid).Include(f => f.Node).SingleAsync();
                using (var vesselstream = System.IO.File.Create(Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{vessel.NodeId}{vessel.Node.Extension}"))
                {
                    for (int i = 0; i < id; i++)
                    {
                        using (var slicestream = System.IO.File.OpenRead(Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{vessel.NodeId}\{i}.slice"))
                        {
                            await slicestream.CopyToAsync(vesselstream);
                        }
                    }
                }
                vessel.Node.Ready = true;
                await context.SaveChangesAsync();
                Directory.Delete(Directory.GetCurrentDirectory() + $@"\wwwroot\upload\{vessel.NodeId}", true);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> Submit([FromBody] RecordForm recordf)
        {
            try
            {
                var record = new Record(recordf);
                await context.Records.AddAsync(record);
                var files = await context.Files.Where(f => f.GUID == recordf.GUID).Include(f=>f.Node).ToListAsync();
                await context.SaveChangesAsync();
                foreach (var f in files) f.RecordId = record.Id;
                await context.SaveChangesAsync();
                record.Files = files;
                _ = Tasks.QueuePredictTaskAsync(record);
                return Ok(record.Id);
            }
            catch { return BadRequest(); }
        }
        //for Record(s).vue
        [HttpGet("/[Controller]/[Action]/{direction}/{lastid}/{step}")]
        public async Task<IActionResult> Record(bool direction, int lastid, int step)
        {
            var datasets =  await context.Datasets.ToDictionaryAsync(d => d.Id, d => d.Name);
            var count = await context.Records.CountAsync();
            if (lastid == 0)
                return Ok(new
                {
                    count,
                    recordlist = await context.Records
                    .OrderByDescending(r => r.Id)
                    .Take(10)
                    .Select(r => new { id = r.Id, mode = r.Mode.ToString(), state = r.State.ToString(), time = r.Time.ToString("F"), dataset = datasets[r.DatasetId] })
                    .ToArrayAsync()
                });
            else
                if (direction) return Ok(new
                {
                    count,
                    recordlist = await context.Records
                    .OrderByDescending(r => r.Id)
                    .Where(r => r.Id < lastid)
                    .Skip(10 * step)
                    .Take(10)
                    .Select(r => new { id = r.Id, mode = r.Mode.ToString(), state = r.State.ToString(), time = r.Time.ToString("F"), dataset = datasets[r.DatasetId] })
                    .ToArrayAsync()
                });
            else return Ok(new
            {
                count,
                recordlist = await context.Records
                .OrderBy(r => r.Id)
                .Where(r => r.Id > lastid)
                .Skip(10 * step)
                .Take(10)
                .Select(r => new { id = r.Id, mode = r.Mode.ToString(), state = r.State.ToString(), time = r.Time.ToString("F"), dataset = datasets[r.DatasetId] })
                .Reverse()
                .ToArrayAsync()
            });
        }
        [HttpGet("/[Controller]/[Action]")]
        public async Task<IActionResult> Dataset()
        {
            return Ok(await context.Datasets.ToArrayAsync());
        }
        [HttpGet("/[Controller]/[Action]")]
        public async Task<IActionResult> TrainedDataset()
        {
            return Ok(await context.Datasets.Where(d=>d.Ready==true).Select(d=>new {d.Id,d.Name}).ToArrayAsync());
        }
        [HttpGet("/[Controller]/[Action]/{id}")]
        public async Task<IActionResult> Record(int id)
        {
            var record = await context.Records
                .Where(r => r.Id == id)
                .Include(r => r.Files)
                  .ThenInclude(f=>f.Node)
                .Include(r => r.Files)
                  .ThenInclude(f => f.Results)
                  .OrderBy(r => r.Id)
                .Include(r=>r.Dataset)
                .FirstAsync();
            object recordview;
            if (record.Mode == RecordMode.Predict) recordview = new PredictModeRecordView(record);
            else if (record.Mode == RecordMode.Directory) recordview = new DirectoryModeRecordView(record);
            else return BadRequest();
            return Ok(recordview);
        }

        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> UploadDatasetFile([FromForm(Name = "file")] IFormFile file, [FromForm(Name = "guid")] string guidstring)
        {
            try
            {
                if (file != null)
                {
                    if (file.Length > 0)
                    {
                        var basepath = Directory.GetCurrentDirectory() + $@"\Neural\datasets\{guidstring}";
                        if(!Directory.Exists(basepath)) Directory.CreateDirectory(basepath);
                        string path;
                        if (file.FileName == "classes.txt") path = basepath + $@"\classes.txt";
                        else
                        { 
                            basepath = basepath + $@"\images";
                            if (!Directory.Exists(basepath)) Directory.CreateDirectory(basepath);
                            path = basepath + $@"\{file.FileName}";
                        }
                        using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream);
                        return Ok();
                    }
                }
                return BadRequest("空文件");
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> UploadDatasetXMLFiles([FromForm(Name = "file")] List<IFormFile> xmlfiles, [FromForm(Name = "guid")] string guidstring)
        {
            try
            {
                if (xmlfiles == null) throw new Exception("空文件");
                if (xmlfiles.Count == 0) throw new Exception("空文件");
                var basepath = Directory.GetCurrentDirectory() + $@"\Neural\datasets\{guidstring}";
                if (!Directory.Exists(basepath)) Directory.CreateDirectory(basepath);
                basepath = basepath + $@"\annotations";
                if (!Directory.Exists(basepath)) Directory.CreateDirectory(basepath);
                foreach (var file in xmlfiles)
                {
                    if(file.Length==0) throw new Exception("空文件");
                    var path = basepath + $@"\{file.FileName}";
                    using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream);
                }
                return Ok();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> SubmitDataset([FromBody] DatasetModel model)
        {
            var dataset = new Dataset()
            {
                Name = model.Name,
                Epoch = model.Epoch,
                Ready = false,
                Augmentation = model.Augmentation
            };
            await context.Datasets.AddAsync(dataset);
            await context.SaveChangesAsync();
            var basepath = Directory.GetCurrentDirectory() + $@"\Neural\datasets";
            var dir = new DirectoryInfo($@"{basepath}\{model.Guid}");
            dir.MoveTo($@"{basepath}\{dataset.Id}");
            _ = trainService.QueueTaskAsync(dataset);
            return Ok(dataset.Id);
        }

        [HttpGet("/[Controller]/[Action]/{id}")]
        public async Task<IActionResult> TestTrain([FromRoute]int id)
        {
            var d =await context.Datasets.FirstAsync(x => x.Id == id);
            _ = trainService.QueueTaskAsync(d);
            return Ok();
        }
        [HttpGet("/[Controller]/[Action]")]
        public IActionResult StopCurrentTrainTask()
        {
            trainService.StopCurrent();
            return Ok();
        }
        [HttpGet("/[Controller]/[Action]/{filename}")]
        public IActionResult MKVFileTest(string filename)
        {
            return PhysicalFile($@"C:\Users\sherl\source\repos\Perception\Perception\wwwroot\{filename}", "application/octet-stream", true);
        }
    }
}
