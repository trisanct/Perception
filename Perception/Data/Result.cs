namespace Perception.Data
{
    public class Result
    {
        public int Id { get; set; }
        public string Class { get; set; } = string.Empty;
        public float Score { get; set; }
        public int FileId { get; set; }
    }
}