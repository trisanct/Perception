namespace Perception.Data
{
    public class Dataset
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Augmentation { get; set; }
        public bool Ready { get; set; }
        public int Epoch { get; set; }
        
    }
}
