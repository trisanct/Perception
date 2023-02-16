using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perception.Models;
using Perception.Services;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using static Perception.Models.Record;

namespace Perception.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PerceptionController : ControllerBase
    {
        private readonly PerceptionContext context;
        private TaskQueue Tasks { get; }

        public PerceptionController(PerceptionContext context, TaskQueue tasks)
        {
            this.context = context;
            Tasks = tasks;
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
        public IActionResult TestTask(int id)
        {
            var record = new Record() { Id = 1, Mode = RecordMode.Predict };
            _ = Tasks.QueueTaskAsync(record);
            return Ok();
        }
        [HttpGet("/[Controller]/[Action]")]
        public IActionResult GetGUID()
        {
            var guid = Guid.NewGuid();
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + $@"\wwwroot\input\{guid}");
            return Ok(guid);
        }

        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> GetGUID([FromBody] FileVessel vessel)
        {
            if (vessel.Filename == null || vessel.Sha == null) return BadRequest();
            var guid = Guid.NewGuid();
            var (name, ext) = Apart(vessel.Filename);
            var sha = HexToByte(vessel.Sha);
            var node = await context.Nodes.Where(f => f.SHA == sha).FirstOrDefaultAsync();
            if (node == null)
            {
                node = new FileNode(ext, sha, false);
                await context.Nodes.AddRangeAsync(node);
                await context.SaveChangesAsync();
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + $@"\wwwroot\input\{node.Id}");
            }
            var f = new FileMap(guid, name, false, node.Id);
            await context.Files.AddAsync(f);
            await context.SaveChangesAsync();
            return Ok(guid);
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
                            var path = Directory.GetCurrentDirectory() + $@"\wwwroot\input\{node.Id}{node.Extension}";
                            using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream);
                        }
                        var f = new FileMap(guid, name, false, node.Id);
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
                        var filePath = Directory.GetCurrentDirectory() + $@"\wwwroot\input\{f.NodeId}\{id}.slice";
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
                using (var vesselstream = System.IO.File.Create(Directory.GetCurrentDirectory() + $@"\wwwroot\input\{vessel.NodeId}{vessel.Node.Extension}"))
                {
                    for (int i = 0; i < id; i++)
                    {
                        using (var slicestream = System.IO.File.OpenRead(Directory.GetCurrentDirectory() + $@"\wwwroot\input\{vessel.NodeId}\{i}.slice"))
                        {
                            await slicestream.CopyToAsync(vesselstream);
                        }
                    }
                }
                vessel.Node.Ready = true;
                await context.SaveChangesAsync();
                Directory.Delete(Directory.GetCurrentDirectory() + $@"\wwwroot\input\{vessel.NodeId}", true);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> Submit([FromBody] RecordView recordv/* , [FromServices] IServiceScopeFactory serviceScopeFactory*/)
        {
            var record = new Record(recordv);
            await context.Records.AddAsync(record);
            var files = await context.Files.Where(f => f.GUID == record.GUID).ToListAsync();
            foreach (var f in files) f.IsSubmitted = true;
            await context.SaveChangesAsync();
            //_ = Task.Run(() => {
            //    var inputpath = Directory.GetCurrentDirectory() + $@"\wwwroot\input";
            //    var networkpath = Directory.GetCurrentDirectory() + @"\Neural\predict.py";
            //    var p = new Process()
            //    {
            //        StartInfo = new ProcessStartInfo()
            //        {
            //            RedirectStandardOutput = true,
            //            RedirectStandardError = true,
            //            CreateNoWindow = false,
            //            UseShellExecute = false,
            //            WorkingDirectory = inputpath,
            //            FileName = "python",
            //            Arguments = $"{networkpath} predict 1.jpg"
            //        }
            //    };
            //    p.Start();
            //    p.WaitForExit();
            //    if (p.ExitCode == 0)
            //    {
            //        var outstrings = p.StandardOutput.ReadToEnd().Split(',');
            //        var pclass = (PredictedClass)Enum.Parse(typeof(PredictedClass), outstrings[0].Replace(" ", ""));
            //        var score = float.Parse(outstrings[1]);
            //        var res = context.Results.Where(r => r.Id == record.Id).FirstOrDefault();

            //        if (res != null)
            //        {
            //            res.Class = pclass;
            //            res.Score = score;
            //            context.SaveChanges();
            //        }
            //    }
            //    else
            //    {

            //    }
            //});
            //运行python程序进行预测(Task)
            //prediction.QueueTask(record.Id);
            return Ok(record.Id);
        }
        //for History(s).vue
        [HttpGet("/[Controller]/[Action]/{direction}/{lastid}/{step}")]
        public async Task<IActionResult> History(bool direction, int lastid, int step)
        {
            if (lastid == 0)
                return Ok(new
                {
                    count = await context.Records.CountAsync(),
                    recordlist = await context.Records
                    .OrderByDescending(r => r.Id)
                    .Take(10)
                    .Select(r => new { id = r.Id, mode = r.Mode.ToString(), time = r.Time.ToString("F") })
                    .ToArrayAsync()
                });
            else
                if (direction) return Ok(new
                {
                    count = await context.Records.CountAsync(),
                    recordlist = await context.Records
                    .OrderByDescending(r => r.Id)
                    .Where(r => r.Id < lastid)
                    .Skip(10 * step)
                    .Take(10)
                    .Select(r => new { id = r.Id, mode = r.Mode.ToString(), time = r.Time.ToString("F") })
                    .ToArrayAsync()
                });
            else return Ok(new
            {
                count = await context.Records.CountAsync(),
                recordlist = await context.Records
                .OrderBy(r => r.Id)
                .Where(r => r.Id > lastid)
                .Skip(10 * step)
                .Take(10)
                .Select(r => new { id = r.Id, mode = r.Mode.ToString(), time = r.Time.ToString("F") })
                .Reverse()
                .ToArrayAsync()
            });
        }
        [HttpGet("/[Controller]/[Action]/{id}")]
        public async Task<IActionResult> History(int id)
        {
            var record = await context.Records.Where(r => r.Id == id).Include(r=>r.Results).FirstAsync();

            //prediction.QueueTask(record.Id);
            return Ok(record);
        }
    }
}
