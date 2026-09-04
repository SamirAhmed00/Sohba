using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class PageDomainService : IPageDomainService
    {
        // Numeric ordering of the enum doubles as the privilege level:
        //   Member(1) < CoAdmin(2) < Admin(3) < PageOwner(4)
        // Comparisons use `>=` / `<` against these values.

        public Result CanCreatePage(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
                return Result.Failure("Page name cannot be empty.");

            if (pageName.Length < 3)
                return Result.Failure("Page name is too short.");

            if (pageName.Length > 100)
                return Result.Failure("Page name is too long (max 100 characters).");

            return Result.Success();
        }

        public Result CanFollowPage(Guid userId, Page page, bool alreadyFollowing)
        {
            if (page == null)
                return Result.Failure("Target page does not exist.");

            if (page.AdminId == userId)
                return Result.Failure("As the page owner, you already follow this page.");

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

        public Result CanEditPage(PageRole? actorRole)
        {
            // Admins and the Page Owner can edit page settings.
            if (actorRole == null || actorRole < PageRole.Admin)
                return Result.Failure("Only Admins or the Page Owner can edit this page.");

            return Result.Success();
        }

        public Result CanPostAsPage(PageRole? actorRole)
        {
            // CoAdmins, Admins, and the Page Owner can post as the page.
            if (actorRole == null || actorRole < PageRole.CoAdmin)
                return Result.Failure("You do not have permission to post as this page.");

            return Result.Success();
        }

        public Result CanDeletePage(Guid userId, PageRole? actorRole)
        {
            // Only the Page Owner can hard-delete the page.
            if (actorRole == null || actorRole < PageRole.PageOwner)
                return Result.Failure("Only the Page Owner can delete this page.");

            return Result.Success();
        }

        public Result CanKickPageMember(Guid actorUserId, PageRole? actorRole, Guid targetUserId, PageRole? targetRole)
        {
            if (actorUserId == targetUserId)
                return Result.Failure("You cannot remove yourself. Use Leave Page instead.");

            if (actorRole == null || actorRole < PageRole.Admin)
                return Result.Failure("You do not have permission to remove members.");

            if (targetRole == null)
                return Result.Failure("Target member was not found.");

            // Users cannot remove members with equal or higher privilege.
            // PageOwner (4) can remove Admin (3), CoAdmin (2), and Member (1).
            // Admin (3) can only remove CoAdmin (2) and Member (1).
            if (targetRole >= actorRole)
                return Result.Failure("You cannot remove a member with the same or higher role.");


            return Result.Success();
        }

        public Result CanPromotePageMember(Guid actorUserId, Guid targetUserId, PageRole? actorRole, PageRole? targetRole, PageRole newRole)
        {
            if (actorUserId == targetUserId)
                return Result.Failure("You cannot change your own role.");

            if (actorRole == null || actorRole < PageRole.Admin)
                return Result.Failure("You do not have permission to promote members.");

            if (targetRole != null && targetRole >= newRole)
                return Result.Failure("The target already holds this role or a higher one.");

            // Admins (non-owners) can only assign up to CoAdmin.
            if (actorRole < PageRole.PageOwner && newRole > PageRole.CoAdmin)
                return Result.Failure("Only the Page Owner can promote a member to Admin.");

            return Result.Success();
        }

        public Result CanDemotePageMember(Guid actorUserId, Guid targetUserId, PageRole? actorRole, PageRole? targetRole, PageRole newRole)
        {
            if (actorUserId == targetUserId)
                return Result.Failure("You cannot change your own role.");

            if (actorRole == null || actorRole < PageRole.Admin)
                return Result.Failure("You do not have permission to demote members.");

            if (targetRole == null || targetRole <= newRole)
                return Result.Failure("The target already holds this role or a lower one.");

            // The Page Owner can never be demoted.
            if (targetRole == PageRole.PageOwner)
                return Result.Failure("The Page Owner cannot be demoted.");

            // Admins (non-owners) cannot demote other Admins.
            if (actorRole < PageRole.PageOwner && targetRole >= PageRole.Admin)
                return Result.Failure("You cannot demote a member with the same or higher role.");

            return Result.Success();
        }

        public Result CanTransferOwnership(Guid actorUserId, PageRole? actorRole, PageRole? targetRole)
        {
            if (actorRole != PageRole.PageOwner)
                return Result.Failure("Only the Page Owner can transfer ownership.");

            if (targetRole != PageRole.Admin)
                return Result.Failure("Ownership can only be transferred to an Admin.");

            return Result.Success();
        }
    }
}
