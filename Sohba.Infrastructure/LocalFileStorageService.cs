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

        // First-pass, cheap extension whitelist. This is NOT the authoritative check —
        // the actual file bytes are decoded and validated below regardless of what the
        // extension/filename claims (rejects e.g. "image.jpg" that is not really a JPEG,
        // and "image.jpg.exe" is already rejected here since its extension is ".exe").
        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        // Image formats accepted once the content is actually decoded. Kept in sync
        // with _allowedExtensions so behavior doesn't silently change per format.
        private static readonly HashSet<string> _allowedDetectedFormats = new(StringComparer.OrdinalIgnoreCase)
        {
            "JPEG", "PNG", "GIF", "WEBP"
        };

        // The only subfolders any caller is allowed to request. All current controllers
        // already pass hardcoded literals (never user input), but this whitelist is a
        // defense-in-depth guard against any future caller introducing a user-controlled
        // value that could otherwise influence the physical storage path.
        private static readonly HashSet<string> _allowedSubFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "posts", "groups", "pages", "profiles", "stories"
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;   // 5 MB — unchanged from existing behavior
        private const int MaxImageDimension = 4096;               // px, either side — new, reasonable ceiling
        private const int WebPQuality = 82;                       // lossy WebP quality (0-100)

        public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
        {
            _env = env;
            _logger = logger;
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
            if (!_allowedExtensions.Contains(extension))
                return Result<string>.Failure($"File type '{extension}' is not allowed. Accepted types: {string.Join(", ", _allowedExtensions)}");

            if (file.Length > MaxFileSizeBytes)
                return Result<string>.Failure($"File size ({file.Length / 1024.0 / 1024.0:F1} MB) exceeds the 5 MB limit.");

            // Buffer the upload once into memory so it can be format-detected and then
            // decoded without re-reading the underlying multipart stream.
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

                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
                var targetFolder = Path.Combine(uploadsRoot, subFolder);
                Directory.CreateDirectory(targetFolder);

                // GUID filename; original user-provided filename is never used for storage.
                // Files are normalized to .webp on disk (see conversion note below).
                var uniqueFileName = $"{Guid.NewGuid()}.webp";
                var filePath = Path.Combine(targetFolder, uniqueFileName);

                // Defense-in-depth: confirm the resolved path actually stays inside the
                // uploads root before writing anything to disk.
                var resolvedPath = Path.GetFullPath(filePath);
                var resolvedRoot = Path.GetFullPath(uploadsRoot) + Path.DirectorySeparatorChar;
                if (!resolvedPath.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Rejected upload with path escaping uploads root: {Path}", resolvedPath);
                    return Result<string>.Failure("Invalid file path.");
                }

                var isAlreadyWebP = detectedFormat.Name.Equals("WEBP", StringComparison.OrdinalIgnoreCase);
                if (isAlreadyWebP)
                {
                    // Already in the correct format — persist the original bytes verbatim.
                    // Avoids unnecessary re-encoding and the associated quality loss.
                    memoryStream.Position = 0;
                    await using var outStream = new FileStream(filePath, FileMode.Create);
                    await memoryStream.CopyToAsync(outStream);
                }
                else
                {
                    // Server-side conversion to WebP. Animated GIFs retain their frames
                    // (ImageSharp's WebpEncoder encodes all loaded frames), preserving
                    // animation instead of collapsing to a single static frame.
                    var encoder = new WebpEncoder
                    {
                        Quality = WebPQuality,
                        FileFormat = WebpFileFormatType.Lossy
                    };
                    await image.SaveAsync(filePath, encoder);
                }

                return Result<string>.Success($"/uploads/{subFolder}/{uniqueFileName}");
            }
        }

        /// <inheritdoc />
        public Task DeleteFileAsync(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return Task.CompletedTask;

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
            var absolutePath = Path.GetFullPath(
                Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

            // Defense-in-depth: never delete a path outside the uploads root, even if a
            // malformed/malicious relativeUrl were ever passed in.
            var resolvedRoot = Path.GetFullPath(uploadsRoot) + Path.DirectorySeparatorChar;
            if (!absolutePath.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Ignored delete request with path escaping uploads root: {Path}", absolutePath);
                return Task.CompletedTask;
            }

            if (File.Exists(absolutePath))
                File.Delete(absolutePath);

            return Task.CompletedTask;
        }
    }
}