
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sohba.Domain.Interfaces
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        // ==================== Membership ====================

        Task<bool> IsMemberAsync(
            Guid userId,
            Guid groupId);

        GroupRole? GetUserRoleInGroup(
            Guid userId,
            Guid groupId);

        bool IsUserBannedFromGroup(
            Guid userId,
            Guid groupId);

        void AddMember(
            GroupMember member);

        void RemoveMember(
            GroupMember member);

        Task<GroupMember?> GetMemberByUserAndGroupAsync(
            Guid groupId,
            Guid userId);

        // ==================== Groups ====================

        Task<IEnumerable<Group>> GetGroupsByUserIdAsync(
            Guid userId);

        Task<IEnumerable<Group>> SearchGroupsAsync(
            string query,
            int limit = 10);

        Task<(IReadOnlyList<Group> Items, int TotalCount)>
            GetGroupsPagedAsync(
                string? search,
                int page,
                int pageSize);

        Task<IEnumerable<Group>> GetRecommendedGroupsAsync(
            Guid userId,
            int count = 5);

        Task<IEnumerable<GroupMember>> GetGroupMembersAsync(
            Guid groupId);

        Task<(IReadOnlyList<GroupMember> Items, int TotalCount)>
            GetMembersPagedAsync(
                Guid groupId,
                string? search,
                int page,
                int pageSize);

        Task<Group?> GetTrackedGroupByIdAsync(
            Guid id);

        // ==================== Ownership ====================

        Task<GroupMember?>
            GetEarliestEligibleMemberForOwnershipTransferAsync(
                Guid groupId,
                Guid excludeUserId);

        // ==================== Deleted Groups ====================

        Task<IEnumerable<Group>> GetDeletedGroupsAsync();

        // ==================== Join Requests ====================

        void AddJoinRequest(
            GroupJoinRequest request);

        Task<GroupJoinRequest?> GetJoinRequestByIdAsync(
            Guid requestId);

        Task<GroupJoinRequest?> GetPendingJoinRequestAsync(
            Guid groupId,
            Guid userId);

        Task<(IEnumerable<GroupJoinRequest> Items, int TotalCount)>
            GetPendingJoinRequestsPagedAsync(
                Guid groupId,
                int page,
                int pageSize);

        Task<int> GetPendingJoinRequestsCountAsync(
            Guid groupId);

        Task<IEnumerable<GroupMember>> GetGroupAdminsAsync(
            Guid groupId);
    }
}
