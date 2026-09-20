using Sohba.Domain.Domain_Rules.Logic;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for reporting rules: duplicate-report prevention, admin-only
    /// review, crowdsourced auto-hiding thresholds and appeals.
    /// </summary>
    public class ReportingDomainServiceTests
    {
        private readonly ReportingDomainService _sut = new();

        private static readonly Guid User = Guid.NewGuid();
        private static readonly Guid Target = Guid.NewGuid();
        private static readonly Guid Admin = Guid.NewGuid();

        // ---- CanReportEntity ----

        /// <summary>Verifies that content can be reported the first time.</summary>
        [Fact]
        public void CanReportEntity_WhenNotAlreadyReported_ReturnsSuccess()
        {
            Assert.True(_sut.CanReportEntity(User, Target, alreadyReported: false).IsSuccess);
        }

        /// <summary>Verifies that reporting the same content twice is rejected.</summary>
        [Fact]
        public void CanReportEntity_WhenAlreadyReported_ReturnsFailure()
        {
            var result = _sut.CanReportEntity(User, Target, alreadyReported: true);

            Assert.True(result.IsFailure);
            Assert.Contains("already reported", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanReviewReport ----

        /// <summary>Verifies that an admin can review reports.</summary>
        [Fact]
        public void CanReviewReport_WhenCallerIsAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanReviewReport(Admin, isAdmin: true).IsSuccess);
        }

        /// <summary>Verifies that a non-admin cannot review reports.</summary>
        [Fact]
        public void CanReviewReport_WhenCallerIsNotAdmin_ReturnsFailure()
        {
            var result = _sut.CanReviewReport(User, isAdmin: false);

            Assert.True(result.IsFailure);
            Assert.Contains("admins", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- ShouldAutoHideContent ----

        /// <summary>Verifies the auto-hide boundary: reports one below the threshold do not hide content.</summary>
        [Fact]
        public void ShouldAutoHideContent_WhenOneBelowThreshold_ReturnsFalse()
        {
            Assert.False(_sut.ShouldAutoHideContent(reportCount: 4, threshold: 5));
        }

        /// <summary>Verifies the auto-hide boundary: reports exactly at the threshold hide content.</summary>
        [Fact]
        public void ShouldAutoHideContent_WhenAtThreshold_ReturnsTrue()
        {
            Assert.True(_sut.ShouldAutoHideContent(reportCount: 5, threshold: 5));
        }

        /// <summary>Verifies that reports above the threshold hide content.</summary>
        [Fact]
        public void ShouldAutoHideContent_WhenAboveThreshold_ReturnsTrue()
        {
            Assert.True(_sut.ShouldAutoHideContent(reportCount: 7, threshold: 5));
        }

        /// <summary>Verifies that zero reports never hide content.</summary>
        [Fact]
        public void ShouldAutoHideContent_WhenNoReports_ReturnsFalse()
        {
            Assert.False(_sut.ShouldAutoHideContent(reportCount: 0, threshold: 5));
        }

        // ---- CanAppealReport ----

        /// <summary>Verifies that an unresolved report can be appealed.</summary>
        [Fact]
        public void CanAppealReport_WhenReportIsNotResolved_ReturnsSuccess()
        {
            Assert.True(_sut.CanAppealReport(User, isReportResolved: false).IsSuccess);
        }

        /// <summary>Verifies that an already-resolved report cannot be appealed.</summary>
        [Fact]
        public void CanAppealReport_WhenReportIsResolved_ReturnsFailure()
        {
            var result = _sut.CanAppealReport(User, isReportResolved: true);

            Assert.True(result.IsFailure);
            Assert.Contains("resolved", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}
