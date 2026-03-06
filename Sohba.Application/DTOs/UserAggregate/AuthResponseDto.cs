namespace Sohba.Application.DTOs.UserAggregate
{
    public class AuthResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}