using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;

namespace Sohba.Infrastructure
{
    /// <summary>
    /// Concrete implementation of IFileStorageService that saves files to the local
    /// wwwroot/uploads directory. Centralises all OS-level file I/O so that no
    /// Controller or Application Service needs to reference System.IO directly.
    ///
    /// Phase 3: all uploads are now validated by decoding the actual image content
    /// (not just extension/MIME), constrained to a maximum size and dimension, and
    /// re-encoded to WebP before being persisted under a GUID filename. Files already
    /// in WebP format are stored as-is (no unnecessary re-encode/quality loss).
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly Sohba.Domain.Domain_Rules.Interface.IMediaDomainService _mediaDomainService;

        private static readonly HashSet<string> _allowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        private static readonly HashSet<string> _allowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov"
        };

        private static readonly HashSet<string> _allowedDetectedFormats = new(StringComparer.OrdinalIgnoreCase)
        {
            "JPEG", "PNG", "GIF", "WEBP"
        };

        private static readonly HashSet<string> _allowedSubFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "posts", "groups", "pages", "profiles", "stories"
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;       // 5 MB for images
        private const long MaxVideoFileSizeBytes = 50 * 1024 * 1024; // 50 MB for videos
        private const int MaxImageDimension = 4096;
        private const int WebPQuality = 82;

        public LocalFileStorageService(
           IWebHostEnvironment env,
           ILogger<LocalFileStorageService> logger,
           Sohba.Domain.Domain_Rules.Interface.IMediaDomainService mediaDomainService)
        {
            _env = env;
            _logger = logger;
            _mediaDomainService = mediaDomainService;
        }

        /// <inheritdoc />
        public async Task<Result<string>> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                return Result<string>.Success(null);

            if (string.IsNullOrWhiteSpace(subFolder) || !_allowedSubFolders.Contains(subFolder))
            {
                _logger.LogWarning("Rejected upload with disallowed subFolder value: {SubFolder}", subFolder);
                return Result<string>.Failure("Invalid upload destination.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var domainCheck = _mediaDomainService.CanUploadMedia(extension, file.Length, file.ContentType);
            if (!domainCheck.IsSuccess)
                return Result<string>.Failure(domainCheck.Error);

            bool isVideo = _allowedVideoExtensions.Contains(extension);
            bool isImage = _allowedImageExtensions.Contains(extension);

            // Route story uploads to ProtectedUploads outside wwwroot to prevent unrestricted static access
            bool isProtectedStory = subFolder.Equals("stories", StringComparison.OrdinalIgnoreCase);
            var baseDirectory = isProtectedStory
                ? Path.Combine(_env.ContentRootPath, "ProtectedUploads")
                : Path.Combine(_env.WebRootPath, "uploads");

            var targetFolder = Path.Combine(baseDirectory, subFolder);
            Directory.CreateDirectory(targetFolder);

            if (isVideo)
            {
                if (file.Length > MaxVideoFileSizeBytes)
                    return Result<string>.Failure($"Video size ({file.Length / 1024.0 / 1024.0:F1} MB) exceeds the 50 MB limit.");

                // Basic content verification: the file must start with an ISO-BMFF 'ftyp' box.
                // This blocks renamed non-video files; full container validation is out of scope.
                if (!await HasValidVideoSignatureAsync(file, extension))
                {
                    _logger.LogWarning(
                        "Rejected video upload with invalid content signature: {FileName} (extension {Extension}, size {Size})",
                        file.FileName, extension, file.Length);
                    return Result<string>.Failure("The uploaded file is not a valid MP4/MOV video.");
                }

                var uniqueVideoName = $"{Guid.NewGuid()}{extension}";
                var videoFilePath = Path.Combine(targetFolder, uniqueVideoName);

                var resolvedVideoPath = Path.GetFullPath(videoFilePath);
                var resolvedVideoRoot = Path.GetFullPath(baseDirectory) + Path.DirectorySeparatorChar;
                if (!resolvedVideoPath.StartsWith(resolvedVideoRoot, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Rejected video upload escaping root: {Path}", resolvedVideoPath);
                    return Result<string>.Failure("Invalid file path.");
                }

                await using (var outStream = new FileStream(videoFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(outStream);
                }

                return isProtectedStory
                    ? Result<string>.Success($"ProtectedUploads/{subFolder}/{uniqueVideoName}")
                    : Result<string>.Success($"/uploads/{subFolder}/{uniqueVideoName}");
            }


            if (file.Length > MaxFileSizeBytes)
                return Result<string>.Failure($"File size ({file.Length / 1024.0 / 1024.0:F1} MB) exceeds the 5 MB limit.");

            using var memoryStream = new MemoryStream();
            await using (var uploadStream = file.OpenReadStream())
            {
                await uploadStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            IImageFormat detectedFormat;
            Image image;
            try
            {
                detectedFormat = await Image.DetectFormatAsync(memoryStream);
                if (detectedFormat == null || !_allowedDetectedFormats.Contains(detectedFormat.Name))
                    return Result<string>.Failure("The uploaded file is not a valid, supported image.");

                memoryStream.Position = 0;
                image = await Image.LoadAsync(memoryStream);
            }
            catch (UnknownImageFormatException)
            {
                return Result<string>.Failure("The uploaded file is not a valid image.");
            }
            catch (InvalidImageContentException)
            {
                return Result<string>.Failure("The uploaded file is corrupted or not a valid image.");
            }

            using (image)
            {
                if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
                {
                    return Result<string>.Failure(
                        $"Image dimensions ({image.Width}x{image.Height}) exceed the maximum allowed size of {MaxImageDimension}x{MaxImageDimension} pixels.");
                }

                var uniqueFileName = $"{Guid.NewGuid()}.webp";
                var filePath = Path.Combine(targetFolder, uniqueFileName);

                var resolvedPath = Path.GetFullPath(filePath);
                var resolvedRoot = Path.GetFullPath(baseDirectory) + Path.DirectorySeparatorChar;
                if (!resolvedPath.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Rejected upload with path escaping uploads root: {Path}", resolvedPath);
                    return Result<string>.Failure("Invalid file path.");
                }

                var isAlreadyWebP = detectedFormat.Name.Equals("WEBP", StringComparison.OrdinalIgnoreCase);
                if (isAlreadyWebP)
                {
                    memoryStream.Position = 0;
                    await using var outStream = new FileStream(filePath, FileMode.Create);
                    await memoryStream.CopyToAsync(outStream);
                }
                else
                {
                    var encoder = new WebpEncoder
                    {
                        Quality = WebPQuality,
                        FileFormat = WebpFileFormatType.Lossy
                    };
                    await image.SaveAsync(filePath, encoder);
                }

                return isProtectedStory
                    ? Result<string>.Success($"ProtectedUploads/{subFolder}/{uniqueFileName}")
                    : Result<string>.Success($"/uploads/{subFolder}/{uniqueFileName}");
            }
        }

        /// <inheritdoc />
        public Task DeleteFileAsync(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return Task.CompletedTask;

            bool isProtectedStory = relativeUrl.Contains("ProtectedUploads", StringComparison.OrdinalIgnoreCase);
            var baseDirectory = isProtectedStory
                ? Path.Combine(_env.ContentRootPath, "ProtectedUploads")
                : Path.Combine(_env.WebRootPath, "uploads");

            var sanitizedPath = relativeUrl.Replace("ProtectedUploads/", "", StringComparison.OrdinalIgnoreCase)
                                           .Replace("/uploads/", "", StringComparison.OrdinalIgnoreCase)
                                           .TrimStart('/');

            var absolutePath = Path.GetFullPath(
                Path.Combine(baseDirectory, sanitizedPath.Replace('/', Path.DirectorySeparatorChar)));

            var resolvedRoot = Path.GetFullPath(baseDirectory) + Path.DirectorySeparatorChar;
            if (!absolutePath.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Ignored delete request with path escaping uploads root: {Path}", absolutePath);
                return Task.CompletedTask;
            }

            if (File.Exists(absolutePath))
                File.Delete(absolutePath);

            return Task.CompletedTask;
        }


        private static readonly HashSet<string> _allowedMp4MajorBrands = new(StringComparer.Ordinal)
        {
            "isom", "iso2", "iso5", "iso6", "iso9", "isoa", "mp41", "mp42", "mp71",
            "avc1", "dash", "msdh", "msd1", "m4v ", "mpi "
        };

        /// <summary>
        /// Verifies the file starts with an ISO Base Media File Format 'ftyp' box.
        /// MP4 requires a known ISO major brand (and not the QuickTime brand);
        /// MOV requires the QuickTime major brand 'qt  '.
        /// </summary>
        private static async Task<bool> HasValidVideoSignatureAsync(IFormFile file, string extension)
        {
            const int headerLength = 12; // 4 bytes box size + 'ftyp' + 4 bytes major brand
            var buffer = new byte[headerLength];

            await using var stream = file.OpenReadStream();
            var read = 0;
            while (read < headerLength)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, headerLength - read));
                if (n == 0) break;
                read += n;
            }

            if (read < headerLength)
                return false;

            // bytes 4..7 must be 'ftyp'
            if (buffer[4] != (byte)'f' || buffer[5] != (byte)'t' || buffer[6] != (byte)'y' || buffer[7] != (byte)'p')
                return false;

            var majorBrand = System.Text.Encoding.ASCII.GetString(buffer, 8, 4);

            return extension == ".mov"
                ? majorBrand == "qt  "
                : majorBrand != "qt  " && _allowedMp4MajorBrands.Contains(majorBrand);
        }
    }
}