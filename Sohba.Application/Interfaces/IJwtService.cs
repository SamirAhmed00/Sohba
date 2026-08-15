using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user, IList<string> roles);
    }
}
