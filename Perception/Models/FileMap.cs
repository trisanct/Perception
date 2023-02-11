using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Perception.Models
{
    public class FileMap
    {
        public int Id { get; set; }
        public Guid GUID { get; set; }
        public string Name { get; set; }
        public bool IsSubmitted { get; set; }
        public int NodeId { get; set; }
        public FileNode Node { get; set; }
        public FileMap() { }
        public FileMap(Guid guid, string name, bool issubmitted, int nodeid)
        {
            GUID = guid;
            Name = name;
            IsSubmitted = issubmitted;
            NodeId = nodeid;
        }
    }
    public class FileVessel
    {
        public string? Filename { get; set; }
        public string? Sha { get; set; }
    }
}
