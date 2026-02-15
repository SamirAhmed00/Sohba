using Sohba.Domain.Common;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IPageDomainService
    {
        Result CanCreatePage(string pageName);
        Result CanFollowPage(Guid userId, Page page, bool alreadyFollowing);
        Result CanUnfollowPage(bool alreadyFollowing);
    }
}
