using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class SaveToCollectionDto
    {
        public Guid PostId { get; set; }
        public Guid CollectionId { get; set; }
    }
}
