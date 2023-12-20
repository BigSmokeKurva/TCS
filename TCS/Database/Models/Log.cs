namespace TCS.Database.Models
{
    public class Log
    {
        public int LogId { get; set; }
        public int Id { get; set; }

        public string Message { get; set; }

        public DateTime Time { get; set; }

        //public virtual User User { get; set; }
    }
}
