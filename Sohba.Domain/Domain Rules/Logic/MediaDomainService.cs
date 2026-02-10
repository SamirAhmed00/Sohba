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
            // 1. Validate Extension
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var allowedVideoExtensions = new[] { ".mp4", ".mov" };

            var ext = fileExtension.ToLower();
            bool isImage = allowedImageExtensions.Contains(ext);
            bool isVideo = allowedVideoExtensions.Contains(ext);

            if (!isImage && !isVideo)
                return Result.Failure("Unsupported file format.");

            // 2. Validate Size (Example: 5MB for images, 50MB for videos)
            long maxImageSize = 5 * 1024 * 1024;
            long maxVideoSize = 50 * 1024 * 1024;

            if (isImage && fileSizeInBytes > maxImageSize)
                return Result.Failure("Image size exceeds the 5MB limit.");

            if (isVideo && fileSizeInBytes > maxVideoSize)
                return Result.Failure("Video size exceeds the 50MB limit.");

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
