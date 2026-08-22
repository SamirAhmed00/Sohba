using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class PageDomainService : IPageDomainService
    {
        public Result CanCreatePage(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
                return Result.Failure("Page name cannot be empty.");

            if (pageName.Length < 3)
                return Result.Failure("Page name is too short.");

            return Result.Success();
        }

        public Result CanFollowPage(Guid userId, Page page, bool alreadyFollowing)
        {
            if (page == null)
                return Result.Failure("Target page does not exist.");

            if (page.AdminId == userId)
                return Result.Failure("As an admin, you already own and follow this page.");

            if (alreadyFollowing)
                return Result.Failure("You are already following this page.");

            return Result.Success();
        }

        public Result CanUnfollowPage(bool alreadyFollowing)
        {
            if (!alreadyFollowing)
                return Result.Failure("You cannot unfollow a page you don't follow.");

            return Result.Success();
        }

        public Result CanKickPageMember(Guid actionUserId, string actionUserRole, Guid targetUserId, string targetUserRole)
        {
            if (actionUserId == targetUserId)
                return Result.Failure("You cannot remove yourself. Use Leave Page instead.");

            if (actionUserRole != "Admin")
                return Result.Failure("You do not have permission to remove members.");

            if (targetUserRole == "Admin")
                return Result.Failure("You cannot remove another Admin.");

            return Result.Success();
        }

        public Result CanPromotePageMember(Guid actionUserId, string actionUserRole, string targetUserRole)
        {
            if (actionUserRole != "Admin")
                return Result.Failure("You do not have permission to promote members.");

            if (targetUserRole == "Admin")
                return Result.Failure("User is already an Admin.");

            return Result.Success();
        }

        public Result CanDeletePage(Guid userId, string userRole)
        {
            if (userRole != "Admin")
                return Result.Failure("Only a Page Admin can delete this page.");

            return Result.Success();
        }

    }
}
