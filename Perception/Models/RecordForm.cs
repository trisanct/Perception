using static Perception.Data.Record;

namespace Perception.Models
{
    public class RecordForm
    {
        public Guid GUID { get; set; }
        public int DatasetId { get; set; }
        public RecordMode Mode { get; set; }
        public int Fps { get; set; }
        public int TestInterval { get; set; }
        public bool Cuda { get; set; }
        public float Confidence { get; set; }
    }
}
