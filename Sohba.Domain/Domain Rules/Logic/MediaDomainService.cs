using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class MediaDomainService : IMediaDomainService
    {
        public Result CanUploadMedia(string fileExtension, long fileSizeInBytes, string mediaType)
        {
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedVideoExtensions = new[] { ".mp4", ".mov" };

            var ext = (fileExtension ?? string.Empty).ToLowerInvariant();
            bool isImage = allowedImageExtensions.Contains(ext);
            bool isVideo = allowedVideoExtensions.Contains(ext);

            if (!isImage && !isVideo)
                return Result.Failure($"File type '{fileExtension}' is not allowed.");

            long maxImageSize = 5 * 1024 * 1024;
            long maxVideoSize = 50 * 1024 * 1024;

            if (isImage && fileSizeInBytes > maxImageSize)
                return Result.Failure($"Image size ({fileSizeInBytes / 1024.0 / 1024.0:F1} MB) exceeds the 5 MB limit.");

            if (isVideo && fileSizeInBytes > maxVideoSize)
                return Result.Failure($"Video size ({fileSizeInBytes / 1024.0 / 1024.0:F1} MB) exceeds the 50 MB limit.");

            return Result.Success();
        }

        public Result CanAccessMedia(Guid userId, Guid ownerId, bool isPrivate, bool isFriend)
        {
            if (userId == ownerId) return Result.Success();

            if (isPrivate && !isFriend)
                return Result.Failure("Access denied. Private media.");

            return Result.Success();
        }

        public Result CanSetProfilePicture(long fileSize, string extension)
        {
            // Profile pictures usually have stricter limits
            long maxProfilePicSize = 2 * 1024 * 1024; // 2MB
            var allowed = new[] { ".jpg", ".jpeg", ".png" };

            if (fileSize > maxProfilePicSize)
                return Result.Failure("Profile picture is too large (Max 2MB).");

            if (!allowed.Contains(extension.ToLower()))
                return Result.Failure("Invalid file format for profile picture.");

            return Result.Success();
        }
    }
}
