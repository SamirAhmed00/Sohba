using Sohba.Application.DTOs.PostAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IReportingService
    {
        Task<Result> ReportPostAsync(PostReportRequestDto reportDto, Guid reporterId);
    }
}
