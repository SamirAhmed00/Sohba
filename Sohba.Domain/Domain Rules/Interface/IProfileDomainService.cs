using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IProfileDomainService
    {
        Result CanUpdateProfile(Guid userId, Guid profileOwnerId);
        Result CanViewProfile(Guid viewerId, Guid ownerId, bool isPrivate, bool isFriend, bool isBlocked);

        // Privacy Rules for specific sections
        Result CanViewFriendsList(Guid viewerId, Guid ownerId, string privacySetting, bool isFriend);
        Result CanViewContactInfo(Guid viewerId, Guid ownerId, bool isFriend);

        Result CanChangeUsername(DateTime? lastChangedDate, int daysLimit);
    }
}
