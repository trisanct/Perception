using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Perception.Data
{
    public class FileMap
    {
        public int Id { get; set; }
        public Guid GUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? RecordId { get; set; }
        public int NodeId { get; set; }
        public FileNode Node { get; set; }
        public List<Result> Results { get; set; } = new List<Result>();
        public FileMap() { }
        public FileMap(Guid guid, string name, int nodeid)
        {
            GUID = guid;
            Name = name;
            NodeId = nodeid;
        }
    }
    public class FileVessel
    {
        public string? Filename { get; set; }
        public string? Sha { get; set; }
    }
}
