// wwwroot/js/features/sidebar.js
// Handles right-sidebar dynamic content: friend suggestions loading and quick friend requests.
// Extracted from _RightSidebar.cshtml per RULES.md §2 (Zero Inline JS).

document.addEventListener('DOMContentLoaded', async function () {
    await loadFriendSuggestions();
});

/**
 * Fetches and renders friend suggestions into #friendSuggestionsContainer.
 * Fixes the "Sidebar Loading..." indefinite state by catching all fetch errors.
 */
async function loadFriendSuggestions() {
    const container = document.getElementById('friendSuggestionsContainer');
    if (!container) return;

    try {
        const response = await fetch('/Friends/GetFriendSuggestions?count=5');
        const payload = await response.json();

        if (!payload.success && !payload.Success) {
            container.innerHTML = '<div class="text-xs text-center text-slate-400 py-2">Could not load suggestions</div>';
            return;
        }

        const users = payload.data ?? payload.Data ?? [];

        if (users.length > 0) {
            container.innerHTML = users.map(user => `
                <div class="flex items-center justify-between group">
                    <div class="flex items-center gap-3">
                        <img src="${user.profilePictureUrl || user.ProfilePictureUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name || user.Name)}&background=345e69&color=fff`}"
                             class="w-10 h-10 rounded-xl object-cover" alt="${user.name || user.Name}">
                        <div>
                            <h5 class="text-sm font-bold text-gray-800 group-hover:text-[#345e69] transition-colors">
                                ${user.name || user.Name}
                            </h5>
                            <p class="text-xs text-gray-400">Suggested for you</p>
                        </div>
                    </div>
                    <button onclick="sendSidebarFriendRequest('${user.id || user.Id}')"
                            class="text-[#345e69] bg-[#345e69]/10 hover:bg-[#345e69] hover:text-white p-2 rounded-lg transition-all duration-300"
                            aria-label="Add friend">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                  d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
                        </svg>
                    </button>
                </div>
            `).join('');
        } else {
            container.innerHTML = '<div class="text-xs text-center text-slate-400 py-2">No suggestions right now</div>';
        }
    } catch (error) {
        // Prevent the "Loading..." state from staying indefinitely.
        console.warn('[sidebar.js] Failed to load friend suggestions:', error);
        container.innerHTML = '<div class="text-xs text-center text-slate-400 py-2">Could not load suggestions</div>';
    }
}

/**
 * Sends a friend request from the sidebar suggestion card.
 * Payload key is `receiverId` — must match SendRequestModel in FriendsController.
 * @param {string} userId - The target user's GUID string.
 */
async function sendSidebarFriendRequest(userId) {
    if (!window.SohbaApp) return;

    // Key must be `receiverId` — matches: public class SendRequestModel { public Guid receiverId }
    const result = await SohbaApp.post('/Friends/SendRequest', { receiverId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request sent!', 'success');
        // Remove the suggestion card from the DOM instead of full-page reload.
        const btn = event?.target?.closest('div.flex');
        if (btn) btn.remove();
    } else {
        SohbaApp.toast(result.error || 'Failed to send request', 'error');
    }
}
