using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class ReportingDomainService : IReportingDomainService
    {
        public Result CanReportEntity(Guid userId, Guid targetEntityId, bool alreadyReported)
        {
            if (alreadyReported)
                return Result.Failure("You have already reported this content.");

            return Result.Success();
        }

        public Result CanReviewReport(Guid adminId, bool isAdmin)
        {
            if (!isAdmin)
                return Result.Failure("Only admins can review reports.");

            return Result.Success();
        }

        public bool ShouldAutoHideContent(int reportCount, int threshold)
        {
            // Rule: Automatically hide content if reports exceed threshold (Crowdsourced moderation)
            return reportCount >= threshold;
        }

        public Result CanAppealReport(Guid userId, bool isReportResolved)
        {
            if (isReportResolved)
                return Result.Failure("This report is already resolved and closed.");

            return Result.Success();
        }
    }
}
