using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IHashtagRepository : IGenericRepository<Hashtag>
    {
        Task<IEnumerable<Hashtag>> GetTrendingHashtagsAsync(int count = 10);
        Task<Hashtag?> GetHashtagByTagAsync(string tag);
        Task IncrementHashtagCountAsync(string tag);
    }

}
