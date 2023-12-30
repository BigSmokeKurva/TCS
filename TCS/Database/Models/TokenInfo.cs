namespace TCS.Database.Models
{
    public class TokenInfo
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public List<string> Followed { get; set; } = new();
    }
}
