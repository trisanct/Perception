namespace Perception.Models
{
    public class DatasetModel
    {
        public string Name { get; set; }
        public int Epoch { get; set; }
        public Guid Guid { get; set; }
        public bool Augmentation { get; set; }
    }
}
