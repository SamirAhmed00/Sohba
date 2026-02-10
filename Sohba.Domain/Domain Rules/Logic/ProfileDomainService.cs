using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class ProfileDomainService : IProfileDomainService
    {
        public Result CanUpdateProfile(Guid userId, Guid profileOwnerId)
        {
            if (userId != profileOwnerId)
                return Result.Failure("You can only update your own profile.");

            return Result.Success();
        }

        public Result CanViewProfile(Guid viewerId, Guid ownerId, bool isPrivate, bool isFriend, bool isBlocked)
        {
            if (isBlocked)
                return Result.Failure("You cannot view this profile due to blocking.");

            if (viewerId == ownerId) return Result.Success();

            if (isPrivate && !isFriend)
                return Result.Failure("This profile is private.");

            return Result.Success();
        }

        public Result CanViewFriendsList(Guid viewerId, Guid ownerId, string privacySetting, bool isFriend)
        {
            if (viewerId == ownerId) return Result.Success();

            // Privacy settings: "Public", "FriendsOnly", "Private"
            if (privacySetting == "Private")
                return Result.Failure("Friends list is private.");

            if (privacySetting == "FriendsOnly" && !isFriend)
                return Result.Failure("Only friends can view the friends list.");

            return Result.Success();
        }

        public Result CanViewContactInfo(Guid viewerId, Guid ownerId, bool isFriend)
        {
            if (viewerId == ownerId) return Result.Success();

            // Usually strict privacy on contact info
            if (!isFriend)
                return Result.Failure("Only friends can view contact information.");

            return Result.Success();
        }

        public Result CanChangeUsername(DateTime? lastChangedDate, int daysLimit)
        {
            // If never changed, allow it
            if (lastChangedDate == null) return Result.Success();

            // Check if enough time has passed
            var nextAllowedDate = lastChangedDate.Value.AddDays(daysLimit);
            if (DateTime.UtcNow < nextAllowedDate)
            {
                var remainingDays = (nextAllowedDate - DateTime.UtcNow).Days;
                return Result.Failure($"You can change your username again in {remainingDays} days.");
            }

            return Result.Success();
        }
    }
}
