using System;
using System.Collections.Generic;
using System.Text;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Entities;

namespace Sohba.Domain.Entities.UserAggregate
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }
        public bool IsDeleted { get; set; }
        public string PasswordHash { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Story> Stories { get; set; } = new List<Story>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        // Many-to-Many Relationships
        public virtual ICollection<Friend> Friends { get; set; } = new List<Friend>();
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public virtual ICollection<PageFollower> FollowedPages { get; set; } = new List<PageFollower>();
        public virtual ICollection<PostReport> SentReports { get; set; } = new List<PostReport>();
        public virtual ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
        // Admin Roles
        public virtual ICollection<Group> AdministeredGroups { get; set; } = new List<Group>(); 
        public virtual ICollection<Page> AdministeredPages { get; set; } = new List<Page>();


    }
}
