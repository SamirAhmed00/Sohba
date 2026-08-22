(function () {

    window.kickMember = function (groupId, targetUserId) {
        window.showConfirmModal({
            title: 'Remove Member',
            message: 'Are you sure you want to remove this member from the group?',
            type: 'warning',
            confirmText: 'Remove',
            onConfirm: async function () {
                const result = await SohbaApp.post('/Groups/KickMember', { groupId, targetUserId });
                if (result.success) { SohbaApp.toast('Member removed', 'success'); location.reload(); }
                else SohbaApp.toast(result.error || 'Failed to remove member', 'error');
            }
        });
    };

    window.promoteMember = function (groupId, targetUserId) {
        window.showConfirmModal({
            title: 'Promote Member',
            message: 'Promote this member to Group Admin?',
            type: 'info',
            confirmText: 'Promote',
            onConfirm: async function () {
                const result = await SohbaApp.post('/Groups/PromoteMember', { groupId, targetUserId });
                if (result.success) { SohbaApp.toast('Member promoted to Admin', 'success'); location.reload(); }
                else SohbaApp.toast(result.error || 'Failed to promote member', 'error');
            }
        });
    };

    function filterMembers(searchTerm) {
        const cards = document.querySelectorAll('.member-card');
        let visibleCount = 0;
        const searchLower = searchTerm.toLowerCase().trim();

        cards.forEach(card => {
            const name = card.dataset.name || '';
            if (name.includes(searchLower)) {
                card.style.display = 'block';
                visibleCount++;
            } else {
                card.style.display = 'none';
            }
        });

        const noMessage = document.getElementById('noMembersMessage');
        if (noMessage) {
            noMessage.classList.toggle('hidden', visibleCount > 0);
        }
    }

    function initMemberSearch() {
        const searchInput = document.getElementById('memberSearchInput');
        if (searchInput) {
            searchInput.addEventListener('keyup', function () {
                filterMembers(this.value);
            });
        }
    }

    window.clearMemberSearch = function () {
        const searchInput = document.getElementById('memberSearchInput');
        if (searchInput) {
            searchInput.value = '';
            filterMembers('');
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initMemberSearch);
    } else {
        initMemberSearch();
    }
})();