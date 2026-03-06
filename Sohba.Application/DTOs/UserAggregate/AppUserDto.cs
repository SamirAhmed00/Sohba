using System;

namespace Sohba.Application.DTOs.UserAggregate
{
    public class AppUserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } // Name / Display Name
        public string UserName { get; set; }    // For Identity 
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Bio { get; set; }
        public bool IsDeleted { get; set; }
        public string PasswordHash { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}