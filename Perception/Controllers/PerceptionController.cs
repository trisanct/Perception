using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perception.Models;
using System.Data.SqlTypes;
using System.IO;

namespace Perception.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PerceptionController : ControllerBase
    {
        private readonly PerceptionContext _context;

        public PerceptionController(PerceptionContext context)
        {
            _context = context;
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
            var node = await _context.Nodes.Where(f => f.SHA == sha).FirstOrDefaultAsync();
            if (node == null)
            {
                node = new FileNode(ext, sha, false);
                await _context.Nodes.AddRangeAsync(node);
                await _context.SaveChangesAsync();
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + $@"\wwwroot\input\{node.Id}");
            }
            var f = new FileMap(guid, name, false, node.Id);
            await _context.Files.AddAsync(f);
            await _context.SaveChangesAsync();
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
                        var node = await _context.Nodes.Where(f => f.SHA == sha).FirstOrDefaultAsync();
                        var (name, ext) = Apart(file.FileName);
                        if (node == null)
                        {
                            node = new FileNode(ext, sha, true);
                            await _context.Nodes.AddAsync(node);
                            await _context.SaveChangesAsync();
                            var path = Directory.GetCurrentDirectory() + $@"\wwwroot\input\{node.Id}{node.Extension}";
                            using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream);
                        }
                        var f = new FileMap(guid, name, false, node.Id);
                        await _context.Files.AddAsync(f);
                        await _context.SaveChangesAsync();
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
                        var f = await _context.Files.Where(f => f.GUID == guid).SingleAsync();
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
                var vessel = await _context.Files.Where(f => f.GUID == guid).Include(f => f.Node).SingleAsync();
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
                await _context.SaveChangesAsync();
                Directory.Delete(Directory.GetCurrentDirectory() + $@"\wwwroot\input\{vessel.NodeId}", true);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost("/[Controller]/[Action]")]
        public async Task<IActionResult> Submit([FromBody] RecordView recordv)
        {
            var record = new Record(recordv);
            await _context.Records.AddAsync(record);
            var files = await _context.Files.Where(f => f.GUID == record.GUID).ToListAsync();
            foreach (var f in files) f.IsSubmitted = true;
            await _context.SaveChangesAsync();
            //运行python程序进行预测(Task)
            return Ok(record.Id);
        }
        //for History(s).vue
        [HttpGet("/[Controller]/[Action]/{direction}/{lastid}/{step}")]
        public async Task<IActionResult> History(bool direction, int lastid, int step)
        {
            if (lastid == 0)
                return Ok(new
                {
                    count = await _context.Records.CountAsync(),
                    recordlist = await _context.Records
                    .OrderByDescending(r => r.Id)
                    .Take(10)
                    .Select(r => new { id = r.Id, mode = r.Mode.ToString(), time = r.Time.ToString("F"), result = r.Result })
                    .ToArrayAsync()
                });
            else
                if (direction) return Ok(await _context.Records
                    .OrderByDescending(r => r.Id)
                    .Where(r => r.Id < lastid)
                    .Skip(10 * step)
                    .Take(10)
                    .Select(r => new { id = r.Id, mode = r.Mode.ToString(), time = r.Time.ToString("F"), result = r.Result })
                    .ToArrayAsync());
            else return Ok(await _context.Records
                .OrderBy(r => r.Id)
                .Where(r => r.Id > lastid)
                .Skip(10 * step)
                .Take(10)
                .Select(r => new { id = r.Id, mode = r.Mode.ToString(), time = r.Time.ToString("F"), result = r.Result })
                .Reverse()
                .ToArrayAsync());
        }
        [HttpGet("/[Controller]/[Action]/{id}")]
        public async Task<IActionResult> History(int id)
        {
            var record = await _context.Records.Where(r => r.Id == id).FirstAsync();
            var filelist = await _context.Files.Where(f => f.GUID == record.GUID).ToArrayAsync();
            return Ok();
        }
    }
}
