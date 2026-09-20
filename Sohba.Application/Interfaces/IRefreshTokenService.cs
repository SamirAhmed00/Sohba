using Sohba.Domain.Common;
using System;

namespace Sohba.Application.Interfaces
{
    /// <summary>Result of a successful rotation.</summary>
    public sealed record RefreshResult(Guid UserId, string NewRawToken);

    public interface IRefreshTokenService
    {
        Task<Result<string>> IssueAsync(Guid userId, string? createdByIp);

        Task<Result<RefreshResult>> RotateAsync(string? rawToken, string? requestIp);

        Task<Result> RevokeAsync(string? rawToken, string? revokedByIp);

        Task<Result> RevokeAllForUserAsync(Guid userId, string? revokedByIp);
    }
}