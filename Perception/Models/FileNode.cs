using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace Perception.Models
{
    public class FileNode
    {
        public int Id { get; set; }
        public string Extension { get; set; }
        [Column(TypeName = "binary(32)")]
        public byte[] SHA { get; set; }
        public bool Ready { get; set; }
        public string SHAString => Convert.ToHexString(SHA).ToLower().PadLeft(64, '0');
        public FileNode() { }
        public FileNode(string ext, byte[] sha, bool ready)
        {
            Extension= ext;
            SHA = sha;
            Ready = ready;
        }
    }
}
