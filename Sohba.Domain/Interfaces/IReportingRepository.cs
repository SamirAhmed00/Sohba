using System;
using System.Collections.Generic;
using Sohba.Domain.Entities.PostAggregate;


namespace Sohba.Domain.Interfaces
{
    public interface IReportingRepository : IGenericRepository<PostReport>
    {
        Task<bool> HasUserReportedEntityAsync(Guid userId, Guid entityId);
        Task<int> GetReportCountForEntityAsync(Guid entityId);
    }
}
