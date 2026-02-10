using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IMediaDomainService
    {
        // Rule: Check file constraints
        Result CanUploadMedia(string fileExtension, long fileSizeInBytes, string mediaType);

        // Rule: Privacy of the file itself
        Result CanAccessMedia(Guid userId, Guid ownerId, bool isPrivate, bool isFriend);

        // Rule: Profile Picture specific rules
        Result CanSetProfilePicture(long fileSize, string extension);
    }
}
