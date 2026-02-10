using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IReportingDomainService
    {
        Result CanReportEntity(Guid userId, Guid targetEntityId, bool alreadyReported);
        Result CanReviewReport(Guid adminId, bool isAdmin);

        // Auto-Moderation Rule
        bool ShouldAutoHideContent(int reportCount, int threshold);

        Result CanAppealReport(Guid userId, bool isReportResolved);
    }
}
