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
    }
}
