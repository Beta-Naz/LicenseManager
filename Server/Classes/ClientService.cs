namespace Server.Classes
{
    public class ClientService
    {
        public string Token { get; set; }
        public DateTime LastDateConnect { get; set; }
        public ClientService()
        {
            Random random = new Random();
            string Chars = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm0123456789";
            Token = new string(Enumerable.Repeat(Chars,15).Select(x => x[random.Next(Chars.Length)]).ToArray());
            LastDateConnect = DateTime.Now;
        }
    }
}
