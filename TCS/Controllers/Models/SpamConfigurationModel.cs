namespace TCS.Controllers.Models
{
    public class SpamConfigurationModel
    {
        public int Threads { get; set; }
        public int Delay { get; set; }
        public string[] Messages { get; set; }
    }
}
