using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class GroupCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid AdminId { get; set; }
    }
}
