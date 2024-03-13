using System.Text.Json;

namespace TCS.Server.Controllers.Models
{
    public enum ChangeType
    {
        Username = 0,
        Password = 1,
<<<<<<< HEAD:TCS/Controllers/Models/EditUserModel.cs
        Email = 2,
        Admin = 3,
        Tokens = 4,
        Paused = 5,
        FollowbotPermission = 6,
        SpamPermission = 7,
        TokenEditPermission = 8,
=======
        Admin = 2,
        Tokens = 3,
        Paused = 4
>>>>>>> master:TCS/TCS.Server/Controllers/Models/EditUserModel.cs
    }

    public class EditUserModel
    {
        public int Id { get; set; }
        public ChangeType Property { get; set; }
        public JsonElement Value { get; set; }
    }
}
