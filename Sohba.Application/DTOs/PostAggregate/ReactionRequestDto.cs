using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class ReactionRequestDto
    {
        public Guid PostId { get; set; }
        public string ReactionType { get; set; } // Map from Enum
    }
}
