using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.SearchAggregate
{
    public class UserSearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public string Url => $"/Profile/Index/{Id}";
    }

}
