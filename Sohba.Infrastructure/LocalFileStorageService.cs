using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Sohba.Application.Interfaces;

namespace Sohba.Infrastructure
{
    /// <summary>
    /// Concrete implementation of IFileStorageService that saves files to the local
    /// wwwroot/uploads directory. Centralises all OS-level file I/O so that no
    /// Controller or Application Service needs to reference System.IO directly.
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        // Allowed image extensions — enforced centrally so all callers benefit.
        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        // Maximum file size: 5 MB
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <inheritdoc />
        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            // Return null for empty/missing files — callers may keep existing URL.
            if (file == null || file.Length == 0)
                return null;

            // Validate extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    $"File type '{extension}' is not allowed. Accepted types: {string.Join(", ", _allowedExtensions)}");

            // Validate size
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException(
                    $"File size ({file.Length / 1024 / 1024:F1} MB) exceeds the 5 MB limit.");

            // Ensure destination folder exists under wwwroot/uploads/{subFolder}
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(uploadsFolder);

            // Generate a collision-safe file name
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Return a root-relative URL usable in <img src="...">
            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        /// <inheritdoc />
        public Task DeleteFileAsync(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return Task.CompletedTask;

            // Convert "/uploads/groups/abc.jpg" → absolute OS path
            var absolutePath = Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(absolutePath))
                File.Delete(absolutePath);

            return Task.CompletedTask;
        }
    }
}
