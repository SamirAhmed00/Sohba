using AutoMapper;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class ReportingService : IReportingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IReportingDomainService _reportingDomainService;

        public ReportingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IReportingDomainService reportingDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _reportingDomainService = reportingDomainService;
        }

        public async Task<Result> ReportPostAsync(PostReportRequestDto reportDto, Guid reporterId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(reportDto.PostId);
            if (post == null) return Result.Failure("Post not found.");
            
            bool alreadyReported = await _unitOfWork.Reports
                .HasUserReportedEntityAsync(reporterId, reportDto.PostId);

            var validation = _reportingDomainService.CanReportEntity(reporterId, reportDto.PostId, alreadyReported);
            if (!validation.IsSuccess)
                return Result.Failure(validation.Error);

            var report = _mapper.Map<PostReport>(reportDto);
            report.UserId = reporterId;
            report.ReportedAt = DateTime.UtcNow;

            _unitOfWork.Reports.Add(report);

            int currentReportCount = await _unitOfWork.Reports.GetReportCountForEntityAsync(reportDto.PostId);
            int threshold = 5; 

            if (_reportingDomainService.ShouldAutoHideContent(currentReportCount + 1, threshold))
            {
                post.IsDeleted = true; 
                _unitOfWork.Posts.Update(post);
            }

            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }
    }
}
