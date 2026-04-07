/**
 * Friends Feature module capturing UI interaction and API calls.
 */

// Search functionality for Find Friends
document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('searchInput');
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
function filterUsers(filter) {
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

function switchTab(tab) {
    console.log('Switching to tab:', tab);
    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active', 'border-[#345e69]', 'text-[#345e69]');
        btn.classList.add('border-transparent', 'text-gray-400');
    });

    const target = event.target || event.currentTarget;
    if (target) {
        target.classList.add('active', 'border-[#345e69]', 'text-[#345e69]');
        target.classList.remove('border-transparent', 'text-gray-400');
    }

    // Show/hide tabs
    const pendingTab = document.getElementById('pending-tab');
    if (pendingTab) pendingTab.classList.toggle('hidden', tab !== 'pending');

    const sentTab = document.getElementById('sent-tab');
    if (sentTab) sentTab.classList.toggle('hidden', tab !== 'sent');
}

async function acceptRequest(userId) {
    // Payload key must match: public class AcceptRequestModel { public Guid senderId { get; set; } }
    const result = await SohbaApp.post('/Friends/AcceptRequest', { senderId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request accepted!', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();

        const countElement = document.querySelector('.tab-btn:first-child');
        if (countElement) {
            const match = countElement.textContent.match(/\d+/);
            if (match) countElement.innerHTML = `Received (${parseInt(match[0]) - 1})`;
        }
    } else {
        SohbaApp.toast(result.error || 'Failed to accept request', 'error');
    }
}

async function rejectRequest(userId) {
    // Payload key must match: public class RejectRequestModel { public Guid requesterId { get; set; } }
    const result = await SohbaApp.post('/Friends/RejectRequest', { requesterId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request declined', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();

        const countElement = document.querySelector('.tab-btn:first-child');
        if (countElement) {
            const match = countElement.textContent.match(/\d+/);
            if (match) countElement.innerHTML = `Received (${parseInt(match[0]) - 1})`;
        }
    } else {
        SohbaApp.toast(result.error || 'Failed to decline request', 'error');
    }
}

async function cancelRequest(userId) {
    // Payload key must match: public class CancelRequestModel { public Guid receiverId { get; set; } }
    if (!confirm('Cancel this friend request?')) return;

    const result = await SohbaApp.post('/Friends/CancelRequest', { receiverId: userId });

    if (result.success) {
        SohbaApp.toast('Request cancelled', 'success');
        const elem = document.querySelector(`[data-request-id="${userId}"]`);
        if (elem) elem.remove();

        const countElements = document.querySelectorAll('.tab-btn');
        if (countElements.length > 1) {
            const match = countElements[1].textContent.match(/\d+/);
            if (match) countElements[1].innerHTML = `Sent (${parseInt(match[0]) - 1})`;
        }
    } else {
        SohbaApp.toast(result.error || 'Failed to cancel request', 'error');
    }
}
