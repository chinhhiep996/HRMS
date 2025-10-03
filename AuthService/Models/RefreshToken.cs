namespace AuthService.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public bool IsRevoked { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}
