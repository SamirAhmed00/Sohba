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
        Result CanKickPageMember(Guid actionUserId, string actionUserRole, Guid targetUserId, string targetUserRole);
        Result CanPromotePageMember(Guid actionUserId, string actionUserRole, string targetUserRole);
        Result CanDeletePage(Guid userId, string userRole);

    }
}
