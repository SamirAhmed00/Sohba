using Sohba.Domain.Common;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
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
        Result CanEditPage(PageRole? actorRole);
        Result CanPostAsPage(PageRole? actorRole);
        Result CanDeletePage(Guid userId, PageRole? actorRole);
        Result CanKickPageMember(Guid actorUserId, PageRole? actorRole, Guid targetUserId, PageRole? targetRole);
        Result CanPromotePageMember(Guid actorUserId, Guid targetUserId, PageRole? actorRole, PageRole? targetRole, PageRole newRole);
        Result CanDemotePageMember(Guid actorUserId, Guid targetUserId, PageRole? actorRole, PageRole? targetRole, PageRole newRole);
        Result CanTransferOwnership(Guid actorUserId, PageRole? actorRole, PageRole? targetRole);
    }
}
