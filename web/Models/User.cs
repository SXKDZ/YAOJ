namespace YAOJ.Models
{
    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int AcceptanceCount { get; set; }
        public bool IsAdmin { get; set; }
    }
}
