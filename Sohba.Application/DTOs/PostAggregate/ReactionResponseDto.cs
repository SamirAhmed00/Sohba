using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class ReactionResponseDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
