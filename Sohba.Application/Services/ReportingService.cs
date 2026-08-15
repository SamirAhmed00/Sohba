using AutoMapper;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
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

        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        private readonly ILogger<ReportingService> _logger;
        public ReportingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IReportingDomainService reportingDomainService,
            INotificationService notificationService,
            IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _reportingDomainService = reportingDomainService;
            _notificationService = notificationService;
            _userService = userService;
        }

        //public async Task<Result> ReportPostAsync(PostReportRequestDto reportDto, Guid reporterId)
        //{
        //    var post = await _unitOfWork.Posts.GetByIdAsync(reportDto.PostId);
        //    if (post == null)
        //    {
        //        _logger.LogWarning("Report failed: post {PostId} not found", reportDto.PostId);
        //        return Result.Failure("Post not found.");
        //    }

        //    bool alreadyReported = await _unitOfWork.Reports
        //        .HasUserReportedEntityAsync(reporterId, reportDto.PostId);

        //    var validation = _reportingDomainService.CanReportEntity(reporterId, reportDto.PostId, alreadyReported);
        //    if (!validation.IsSuccess)
        //    {
        //        _logger.LogWarning("Report rejected for user {ReporterId} on post {PostId}: {Reason}", reporterId, reportDto.PostId, validation.Error);
        //        return Result.Failure(validation.Error);
        //    }

        //    var report = _mapper.Map<PostReport>(reportDto);
        //    report.UserId = reporterId;
        //    report.ReportedAt = DateTime.UtcNow;

        //    _unitOfWork.Reports.Add(report);

        //    int currentReportCount = await _unitOfWork.Reports.GetReportCountForEntityAsync(reportDto.PostId);
        //    int threshold = 5; 

        //    if (_reportingDomainService.ShouldAutoHideContent(currentReportCount + 1, threshold))
        //    {
        //        post.IsDeleted = true; 
        //        _unitOfWork.Posts.Update(post);
        //    }

        //    await _unitOfWork.CompleteAsync();
        //    _logger.LogInformation("Post {PostId} reported by user {ReporterId}, reason: {Reason}", reportDto.PostId, reporterId, reportDto.Reason);

        //    //  Send notification to post owner
        //    if (post.UserId != reporterId)
        //    {
        //        var reporter = await _userService.GetProfileAsync(reporterId);
        //        var reporterName = reporter.Value?.Name ?? "Someone";

        //        await _notificationService.CreateNotificationAsync(
        //            receiverId: post.UserId,
        //            message: $"{reporterName} reported your post",
        //            type: NotificationType.SystemAlert,
        //            senderId: reporterId,
        //            targetId: post.Id
        //        );
        //    }

        //    //  Send notification to admin
        //    var admins = await _userService.GetUsersByStatusAsync("active");
        //    if (admins.IsSuccess && admins.Value.Any())
        //    {
        //        foreach (var admin in admins.Value.Where(u => u.Email == "admin@sohba.com"))
        //        {
        //            await _notificationService.CreateNotificationAsync(
        //                receiverId: admin.Id,
        //                message: $"New report submitted for post: {post.Title}",
        //                type: NotificationType.SystemAlert,
        //                senderId: reporterId,
        //                targetId: post.Id
        //            );
        //        }
        //    }

        //    return Result.Success();
        //}

        public async Task<Result<PostReportResponseDto>> ReportPostWithDetailsAsync(PostReportRequestDto reportDto, Guid reporterId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(reportDto.PostId);
            if (post == null)
                return Result<PostReportResponseDto>.Failure("Post not found.");

            bool alreadyReported = await _unitOfWork.Reports
                .HasUserReportedEntityAsync(reporterId, reportDto.PostId);

            var validation = _reportingDomainService.CanReportEntity(reporterId, reportDto.PostId, alreadyReported);
            if (!validation.IsSuccess)
                return Result<PostReportResponseDto>.Failure(validation.Error);

            if (!Enum.TryParse<ReportReason>(reportDto.Reason, true, out var reason))
                return Result<PostReportResponseDto>.Failure("Invalid report reason.");

            var report = new PostReport
            {
                PostId = reportDto.PostId,
                UserId = reporterId,
                Reason = reason,
                ReportedAt = DateTime.UtcNow
            };

            _unitOfWork.Reports.Add(report);

            int currentReportCount = await _unitOfWork.Reports.GetReportCountForEntityAsync(reportDto.PostId);
            int threshold = 5;

            if (_reportingDomainService.ShouldAutoHideContent(currentReportCount + 1, threshold))
            {
                post.IsDeleted = true;
                _unitOfWork.Posts.Update(post);
            }

            await _unitOfWork.CompleteAsync();


            if (post.UserId != reporterId)
            {
                var reporter = await _userService.GetProfileAsync(reporterId);
                var reporterName = reporter.Value?.Name ?? "Someone";

                await _notificationService.CreateNotificationAsync(
                    receiverId: post.UserId,
                    message: $"{reporterName} reported your post",
                    type: NotificationType.SystemAlert,
                    senderId: reporterId,
                    targetId: post.Id
                );
            }

            // ✅ Send notification to admin
            var admins = await _userService.GetUsersByStatusAsync("active");
            if (admins.IsSuccess && admins.Value.Any())
            {
                foreach (var admin in admins.Value.Where(u => u.Email == "admin@sohba.com"))
                {
                    await _notificationService.CreateNotificationAsync(
                        receiverId: admin.Id,
                        message: $"New report submitted for post: {post.Title}",
                        type: NotificationType.SystemAlert,
                        senderId: reporterId,
                        targetId: post.Id
                    );
                }
            }

            var createdReport = await _unitOfWork.Reports.GetByIdAsync(report.Id);
            var response = _mapper.Map<PostReportResponseDto>(createdReport);

            return Result<PostReportResponseDto>.Success(response);
        }
        
        

        public async Task<Result<IEnumerable<PostReportResponseDto>>> GetAllReportsAsync()
        {
            var reports = await _unitOfWork.Reports.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<PostReportResponseDto>>(reports);
            return Result<IEnumerable<PostReportResponseDto>>.Success(dtos);
        }

        public async Task<Result> ResolveReportAsync(Guid reportId)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null)
            {
                _logger.LogWarning("Report resolution failed: report {ReportId} not found", reportId);
                return Result.Failure("Report not found");
            }

            report.IsResolved = true;
            _unitOfWork.Reports.Update(report);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Report {ReportId} resolved (post {PostId})", reportId, report.PostId);
            //  Notify the reporter that the report was resolved
            var post = await _unitOfWork.Posts.GetByIdAsync(report.PostId);
            var reporter = await _userService.GetProfileAsync(report.UserId);

            if (post != null && reporter.IsSuccess)
            {
                await _notificationService.CreateNotificationAsync(
                    receiverId: report.UserId,
                    message: $"Your report for post '{post.Title}' has been reviewed and resolved",
                    type: NotificationType.SystemAlert,
                    senderId: null,
                    targetId: report.PostId
                );

                //  Also notify the post owner
                if (post.UserId != report.UserId)
                {
                    await _notificationService.CreateNotificationAsync(
                        receiverId: post.UserId,
                        message: $"A report on your post '{post.Title}' has been resolved",
                        type: NotificationType.SystemAlert,
                        senderId: null,
                        targetId: report.PostId
                    );
                }
            }

            return Result.Success();
        }

        public async Task<Result<int>> GetPendingReportsCountAsync()
        {
            var count = await _unitOfWork.Reports.CountPendingAsync();
            return Result<int>.Success(count);
        }

        public async Task<Result<IEnumerable<PostReportResponseDto>>> GetRecentPendingReportsAsync(int count)
        {
           var reports = await _unitOfWork.Reports.GetRecentPendingAsync(count);
            var dtos = _mapper.Map<IEnumerable<PostReportResponseDto>>(reports);
            return Result<IEnumerable<PostReportResponseDto>>.Success(dtos);
        }
    }
}
