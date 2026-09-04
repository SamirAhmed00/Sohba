(function () {
    window.kickMember = function (groupId, targetUserId) {
        window.showConfirmModal({
            title: 'Remove Member',
            message: 'Are you sure you want to remove this member from the group?',
            type: 'warning',
            confirmText: 'Remove',

            onConfirm: async function () {
                try {
                    const result = await SohbaApp.post('/Groups/KickMember', {
                        groupId,
                        targetUserId
                    });

                    if (result.success) {
                        SohbaApp.toast(
                            'Member removed successfully',
                            'success'
                        );

                        if (typeof executeMemberSearch === 'function') {
                            executeMemberSearch(groupId);
                        } else if (typeof Group_SwitchTab === 'function') {
                            Group_SwitchTab('members');
                        }
                    } else {
                        SohbaApp.toast(
                            result.error || 'Failed to remove member',
                            'error'
                        );
                    }
                } catch (error) {
                    console.error('Kick member error:', error);
                    SohbaApp.toast('Network error', 'error');
                }
            }
        });
    };

    window.promoteMember = function (
        groupId,
        targetUserId,
        newRoleLabel
    ) {
        const role = newRoleLabel || 'next leadership rank';

        window.showConfirmModal({
            title: 'Promote Member',
            message: `Promote this member to ${ role }?`,
            type: 'info',
            confirmText: 'Promote',

            onConfirm: async function () {
                try {
                    const result = await SohbaApp.post(
                        '/Groups/PromoteMember',
                        {
                            groupId,
                            targetUserId
                        }
                    );

                    if (result.success) {
                        SohbaApp.toast(
                            `Member promoted to ${ role } `,
                            'success'
                        );

                        if (typeof executeMemberSearch === 'function') {
                            executeMemberSearch(groupId);
                        } else if (typeof Group_SwitchTab === 'function') {
                            Group_SwitchTab('members');
                        }
                    } else {
                        SohbaApp.toast(
                            result.error || 'Failed to promote member',
                            'error'
                        );
                    }
                } catch (error) {
                    console.error('Promote member error:', error);
                    SohbaApp.toast('Network error', 'error');
                }
            }
        });
    };

    window.demoteMember = function (
        groupId,
        targetUserId,
        newRoleLabel
    ) {
        const role = newRoleLabel || 'lower rank';

        window.showConfirmModal({
            title: 'Demote Leader',
            message: `Demote this user to ${ role }?`,
            type: 'warning',
            confirmText: 'Demote',

            onConfirm: async function () {
                try {
                    const result = await SohbaApp.post(
                        '/Groups/DemoteMember',
                        {
                            groupId,
                            targetUserId
                        }
                    );

                    if (result.success) {
                        SohbaApp.toast(
                            `User demoted to ${ role } `,
                            'success'
                        );

                        if (typeof executeMemberSearch === 'function') {
                            executeMemberSearch(groupId);
                        } else if (typeof Group_SwitchTab === 'function') {
                            Group_SwitchTab('members');
                        }
                    } else {
                        SohbaApp.toast(
                            result.error || 'Failed to demote user',
                            'error'
                        );
                    }
                } catch (error) {
                    console.error('Demote member error:', error);
                    SohbaApp.toast('Network error', 'error');
                }
            }
        });
    };

    window.reviewJoinRequest = async function (
        groupId,
        requestId,
        approve
    ) {
        const actionLabel = approve ? 'Accept' : 'Reject';

        try {
            const result = await SohbaApp.post(
                '/Groups/ReviewJoinRequest',
                {
                    requestId,
                    approve
                }
            );

            if (result.success) {
                SohbaApp.toast(
                    `Request ${ approve ? 'approved' : 'rejected' } successfully`,
                    'success'
                );

                const card = document.getElementById(
                    `request-card-${ requestId }`
                );

                if (card) {
                    card.style.transition = 'all 0.3s ease';
                    card.style.opacity = '0';
                    card.style.transform = 'scale(0.95)';

                    setTimeout(() => {
                        card.remove();
                    }, 300);
                }

                const badge = document.getElementById(
                    'pendingRequestsBadge'
                );

                if (badge) {
                    let count =
                        parseInt(badge.textContent.trim(), 10) || 0;

                    count = Math.max(0, count - 1);

                    badge.textContent = count;

                    if (count === 0) {
                        badge.classList.add('hidden');
                    }
                }
            } else {
                SohbaApp.toast(
                    result.error ||
                    `Failed to ${ actionLabel.toLowerCase() } request`,
                    'error'
                );
            }
        } catch (error) {
            console.error(
                'Review join request error:',
                error
            );

            SohbaApp.toast(
                'An unexpected error occurred.',
                'error'
            );
        }
    };
})();
