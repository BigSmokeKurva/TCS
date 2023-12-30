using System.Text.Json;

namespace TCS.Controllers.Models
{
    public enum ChangeType
    {
        Username = 0,
        Password = 1,
        Email = 2,
        Admin = 3,
        Tokens = 4,
        Paused = 5
    }

    public class EditUserModel
    {
        public int Id { get; set; }
        public ChangeType Property { get; set; }
        public JsonElement Value { get; set; }
    }
}
