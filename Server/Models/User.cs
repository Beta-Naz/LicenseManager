namespace Server.Models
{
    public class User
    {
        public int Id { get; set; }
        public bool IsBan { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
