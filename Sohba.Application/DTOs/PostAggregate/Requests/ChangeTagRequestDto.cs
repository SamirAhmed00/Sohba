using System;

namespace Sohba.Application.DTOs.PostAggregate.Requests
{
    public class ChangeTagRequestDto
    {
        public Guid PostId { get; set; }
        public string Tag { get; set; }
    }
}