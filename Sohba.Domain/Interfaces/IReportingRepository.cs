using System;
using System.Collections.Generic;
using Sohba.Domain.Entities.PostAggregate;


namespace Sohba.Domain.Interfaces
{
    public interface IReportingRepository : IGenericRepository<PostReport>
    {
        Task<int> GetReportCountAsync(Guid entityId);
    }
}
