using System;

namespace Sohba.Application.DTOs.PostAggregate.Requests
{
    public class ToggleSaveRequestDto
    {
        public Guid PostId { get; set; }
        public bool IsFavorite { get; set; }
    }
}