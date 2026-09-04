/**
 * Friends Feature module capturing UI interaction and API calls.
 */

// Search functionality for Find Friends
document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('friendsSearchInput');
    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            const searchTerm = e.target.value.toLowerCase().trim();
            const userCards = document.querySelectorAll('.user-card');
            let visibleCount = 0;

            userCards.forEach(card => {
                const name = card.dataset.name;
                if (name && name.includes(searchTerm)) {
                    card.style.display = 'block';
                    visibleCount++;
                } else {
                    card.style.display = 'none';
                }
            });

            // Show/hide no results message
            const noResults = document.getElementById('noResultsMessage');
            if (noResults) {
                if (visibleCount === 0) {
                    noResults.classList.remove('hidden');
                } else {
                    noResults.classList.add('hidden');
                }
            }
        });
    }
});

// Filter functionality
function filterUsers(filter, event) {
    // Update active button
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.classList.remove('active', 'bg-[#345e69]', 'text-white');
        btn.classList.add('bg-slate-100', 'text-gray-700');
    });
    const target = event.target;
    target.classList.add('active', 'bg-[#345e69]', 'text-white');
    target.classList.remove('bg-slate-100', 'text-gray-700');

    // TODO: Implement actual filtering with AJAX
    if (window.SohbaApp) {
        SohbaApp.toast('Filter by ' + filter + ' coming soon!', 'info');
    }
}

// Redirect to view profile
function viewProfile(userId) {
    window.location.href = `/Profile/Index/${userId}`;
}

// API CALLS
// Note: SohbaApp.post never throws — it always returns { success, error }.
async function sendFriendRequest(userId) {
    // Payload key must match: public class SendRequestModel { public Guid receiverId { get; set; } }
    if (!window.SohbaApp) return;

    const result = await SohbaApp.post('/Friends/SendRequest', { receiverId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request sent!', 'success');
        const userElement = document.querySelector(`[data-user-id="${userId}"]`);
        if (userElement) userElement.remove();
    } else {
        SohbaApp.toast(result.error || 'Failed to send request', 'error');
    }
}

function Friends_SwitchTab(tab, btnEl) {
    
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active', 'border-[#345e69]', 'text-[#345e69]');
        btn.classList.add('border-transparent', 'text-gray-400');
    });

    if (btnEl) {
        btnEl.classList.add('active', 'border-[#345e69]', 'text-[#345e69]');
        btnEl.classList.remove('border-transparent', 'text-gray-400');
    }

    // Show/hide tabs
    const pendingTab = document.getElementById('pending-tab');
    if (pendingTab) pendingTab.classList.toggle('hidden', tab !== 'pending');

    const sentTab = document.getElementById('sent-tab');
    if (sentTab) sentTab.classList.toggle('hidden', tab !== 'sent');
}

async function acceptRequest(userId, btn) {
    if (btn) { btn.disabled = true; }

    const result = await SohbaApp.post('/Friends/AcceptRequest', { senderId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request accepted!', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
        updatePendingRequestCount(-1);

    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to accept request', 'error');
    }
}

async function rejectRequest(userId, btn) {
    if (btn) { btn.disabled = true; }

    const result = await SohbaApp.post('/Friends/RejectRequest', { requesterId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request declined', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();
        updatePendingRequestCount(-1);

    } else {
        if (btn) { btn.disabled = false; }
        SohbaApp.toast(result.error || 'Failed to decline request', 'error');
    }
}

function updatePendingRequestCount(delta) {
    const tabBtn = document.querySelector('.tab-btn.active');
    const countMatch = tabBtn && tabBtn.textContent.match(/\(\s*(\d+)\s*\)/);
    if (!tabBtn || !countMatch) return;

    const newCount = Math.max(0, parseInt(countMatch[1], 10) + delta);
    tabBtn.textContent = tabBtn.textContent.replace(/\(\s*\d+\s*\)/, `(${newCount})`);
}

async function cancelRequest(userId, btn) {
    window.showConfirmModal({
        title: 'Cancel Friend Request',
        message: 'Are you sure you want to cancel this friend request?',
        type: 'warning',
        confirmText: 'Cancel Request',
        onConfirm: async () => {
            if (btn) { btn.disabled = true; btn.innerHTML = 'Cancelling...'; }

            try {
                const result = await SohbaApp.post('/Friends/CancelRequest', { receiverId: userId });

                if (result.success) {
                    SohbaApp.toast('Request cancelled', 'success');
                    const elem = document.querySelector(`[data-request-id="${userId}"]`);
                    if (elem) {
                        elem.style.transition = 'opacity 0.3s ease';
                        elem.style.opacity = '0';
                        setTimeout(() => elem.remove(), 300);
                    }
                    // Update counter
                    const countElements = document.querySelectorAll('.tab-btn');
                    if (countElements.length > 1) {
                        const match = countElements[1].textContent.match(/\d+/);
                        if (match) countElements[1].innerHTML = `Sent (${parseInt(match[0]) - 1})`;
                    }
                } else {
                    SohbaApp.toast(result.error || 'Failed to cancel request', 'error');
                }
            } finally {
                if (btn) { btn.disabled = false; btn.innerHTML = 'Cancel'; }
            }
        }
    });
}


window.blockUser = async function (userId) {
    window.showConfirmModal({
        title: 'Block User',
        message: 'Are you sure you want to block this user? They will no longer be able to interact with you.',
        type: 'warning',
        confirmText: 'Block',
        onConfirm: async () => {
            try {
                const result = await SohbaApp.post('/Friends/BlockUser', { userId: userId });
                if (result.success) {
                    SohbaApp.toast('User blocked successfully.', 'success');
                    setTimeout(() => window.location.reload(), 500); // Refresh to update UI state
                } else {
                    SohbaApp.toast(result.error || 'Failed to block user.', 'error');
                }
            } catch (err) {
                SohbaApp.toast('An unexpected error occurred.', 'error');
            }
        }
    });
};


window.sendFriendRequestFromProfile = async function (userId) {
    const btn = document.getElementById('addFriendBtn');
    if (btn) { btn.disabled = true; btn.innerHTML = 'Sending...'; }

    const result = await SohbaApp.post('/Friends/SendRequest', { receiverId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request sent!', 'success');
        if (btn) {
            btn.innerHTML = `
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                <span>Request Sent</span>
            `;
            btn.classList.remove('bg-[#345e69]', 'hover:bg-[#2a4b55]');
            btn.classList.add('bg-green-600', 'hover:bg-green-700', 'cursor-not-allowed');
        }
    } else {
        SohbaApp.toast(result.error || 'Failed to send request', 'error');
        if (btn) { btn.disabled = false; btn.innerHTML = '<span>Add Friend</span>'; }
    }
};

window.checkFriendshipStatus = async function (targetUserId) {
    try {
        const response = await fetch(`/Friends/CheckStatus?userId=${targetUserId}`);
        const result = await response.json();
        const data = result.data ?? result.Data;

        const btn = document.getElementById('addFriendBtn');
        if (!btn) return;

        if (data === 'pending' || data === 'pending_sent') {
            btn.innerHTML = `
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Pending</span>
            `;
            btn.disabled = true;
            btn.classList.remove('bg-[#345e69]', 'hover:bg-[#2a4b55]');
            btn.classList.add('bg-yellow-600', 'hover:bg-yellow-700', 'cursor-not-allowed');
        } else if (data === 'pending_received') {
            btn.innerHTML = `
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                </svg>
                <span>Respond to Request</span>
            `;
            btn.disabled = false;
            btn.onclick = function () { window.location.href = '/Friends/Requests'; };
            btn.classList.remove('bg-yellow-600', 'hover:bg-yellow-700', 'cursor-not-allowed');
            btn.classList.add('bg-[#345e69]', 'hover:bg-[#2a4b55]');
        } else if (data === 'accepted') {
            btn.innerHTML = `
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                <span>Friends</span>
            `;
            btn.disabled = true;
            btn.classList.remove('bg-[#345e69]', 'hover:bg-[#2a4b55]');
            btn.classList.add('bg-green-600', 'hover:bg-green-700', 'cursor-not-allowed');
        }
    } catch (error) {
        console.error('Error checking friendship status:', error);
    }
};



function resolveTargetUserId(userId) {
    if (typeof userId === 'string' && userId.trim().length > 0) {
        return userId.trim();
    }
    // Fallback: Check for user ID in URL /Profile/Index/{id}
    const pathParts = window.location.pathname.split('/').filter(p => p.length > 0);
    if (pathParts.length >= 2) {
        const lastPart = pathParts[pathParts.length - 1];
        // GUID verification regex
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        if (guidRegex.test(lastPart)) {
            return lastPart;
        }
    }
    return null;
}

async function blockUserFromProfile(userId) {
    const targetId = resolveTargetUserId(userId);
    if (!targetId) {
        if (window.SohbaApp && SohbaApp.toast) {
            SohbaApp.toast('Could not resolve user ID to block.', 'error');
        }
        return;
    }

    window.showConfirmModal({
        title: 'Block User',
        message: 'Are you sure you want to block this user? They will no longer be able to interact with you.',
        type: 'warning',
        confirmText: 'Block',
        onConfirm: async () => {
            try {
                const result = await SohbaApp.post('/Friends/BlockUser', { userId: targetId });
                if (result.success) {
                    SohbaApp.toast('User blocked', 'success');
                    setTimeout(() => window.location.reload(), 800);
                } else {
                    SohbaApp.toast(result.error || 'Failed to block user', 'error');
                }
            } catch (error) {
                console.error('Block error:', error);
                SohbaApp.toast('Network error', 'error');
            }
        }
    });
}

async function unblockUserFromProfile(userId) {
    const targetId = resolveTargetUserId(userId);
    if (!targetId) {
        if (window.SohbaApp && SohbaApp.toast) {
            SohbaApp.toast('Could not resolve user ID to unblock.', 'error');
        }
        return;
    }

    try {
        const result = await SohbaApp.post('/Friends/UnblockUser', { userId: targetId });
        if (result.success) {
            SohbaApp.toast('User unblocked', 'success');
            setTimeout(() => window.location.reload(), 800);
        } else {
            SohbaApp.toast(result.error || 'Failed to unblock user', 'error');
        }
    } catch (error) {
        console.error('Unblock error:', error);
        SohbaApp.toast('Network error', 'error');
    }
}


window.blockUserFromProfile = blockUserFromProfile;
window.unblockUserFromProfile = unblockUserFromProfile;