using Microsoft.AspNetCore.Http;

namespace Sohba.Application.Interfaces
{
    /// <summary>
    /// Abstracts all file system I/O for uploaded media.
    /// Implementations live in the Infrastructure layer (LocalFileStorageService).
    /// Controllers call this service; Application services receive the resulting URL string.
    /// This keeps file I/O out of both the MVC and Application layers.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves an uploaded file to a sub-folder under wwwroot/uploads and returns
        /// the relative URL (e.g. "/uploads/groups/abc123.jpg").
        /// Returns null if the file is null or empty.
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string subFolder);

        /// <summary>
        /// Deletes a previously saved file given its relative URL.
        /// Silently succeeds if the file does not exist.
        /// </summary>
        Task DeleteFileAsync(string relativeUrl);
    }
}
