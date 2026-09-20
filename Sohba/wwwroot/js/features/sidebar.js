// wwwroot/js/features/sidebar.js
// Handles right-sidebar dynamic content: friend suggestions loading and quick friend requests.
// Extracted from _RightSidebar.cshtml per RULES.md §2 (Zero Inline JS).

document.addEventListener('DOMContentLoaded', async function () {
    await loadFriendSuggestions();
    const showMoreHashtagsBtn = document.getElementById('showMoreHashtagsBtn');
    if (showMoreHashtagsBtn) {
        showMoreHashtagsBtn.addEventListener('click', loadMoreTrendingHashtags);
    }
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
            container.innerHTML = users.map(user => {
                // Stored XSS protection: escape every user-controlled value with the
                // shared helper from sohba-modal.js (loaded before this runs).
                const safeId = escapeModalHtml(user.id || user.Id);
                const safeName = escapeModalHtml(user.name || user.Name);
                const safeAvatar = escapeModalHtml(user.profilePictureUrl || user.ProfilePictureUrl
                    || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name || user.Name)}&background=345e69&color=fff`);

                return `
                <div class="flex items-center justify-between group">
                    <a href="/Profile/Index/${safeId}" class="flex items-center gap-3 min-w-0">
                        <img src="${safeAvatar}"
                             class="w-10 h-10 rounded-xl object-cover" alt="${safeName}">
                        <div>
                            <h5 class="text-sm font-bold text-gray-800 group-hover:text-[#345e69] transition-colors">
                                ${safeName}
                            </h5>
                            <p class="text-xs text-gray-400">Suggested for you</p>
                        </div>
                    </a>
                    <button onclick="sendSidebarFriendRequest('${safeId}', event)"
                            class="text-[#345e69] bg-[#345e69]/10 hover:bg-[#345e69] hover:text-white p-2 rounded-lg transition-all duration-300"
                            aria-label="Add friend">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                  d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
                        </svg>
                    </button>
                </div>
            `;
            }).join('');
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
async function sendSidebarFriendRequest(userId, event) {
    if (!window.SohbaApp) return;

    // Key must be `receiverId` — matches: public class SendRequestModel { public Guid receiverId }
    const result = await SohbaApp.post('/Friends/SendRequest', { receiverId: userId });

    if (result.success) {
        SohbaApp.toast('Friend request sent!', 'success');
        // Remove the suggestion card from the DOM instead of full-page reload.
        const card = event?.target?.closest('div.flex.items-center.justify-between');
        if (card) card.remove();
    } else {
        SohbaApp.toast(result.error || 'Failed to send request', 'error');
    }
}
function renderTrendingHashtagItem(tag, count) {
    const safeTag = encodeURIComponent(tag);
    const safeTagDisplay = escapeModalHtml(tag);
    const countLabel = Number(count || 0).toLocaleString();
    return `
        <div class="hover:bg-slate-50 p-2 rounded-lg cursor-pointer transition-colors -mx-2" data-hashtag-tag="${safeTagDisplay}">
            <div class="flex justify-between items-start">
                <span class="text-xs text-gray-400 font-medium">Trending</span>
            </div>
            <a href="/Posts/Hashtag?tag=${safeTag}" class="block">
                <h4 class="font-bold text-gray-800 text-sm mt-0.5 hover:text-[#345e69]">
                    #${safeTagDisplay}
                </h4>
            </a>
            <p class="text-xs text-gray-400 mt-1">${countLabel} posts</p>
        </div>`;
}

let currentHashtagPage = 1;
const hashtagPageSize = 5;
let isHashtagLoading = false;

async function loadMoreTrendingHashtags() {
    const container = document.getElementById('trendingHashtagsContainer');
    const button = document.getElementById('showMoreHashtagsBtn');

    if (!container || !button || isHashtagLoading) {
        return;
    }

    isHashtagLoading = true;
    button.disabled = true;

    const originalText = button.textContent;
    button.textContent = 'Loading...';

    try {
        const nextPage = currentHashtagPage + 1;

        const response = await fetch(
            `/Home/TrendingHashtags?page=${nextPage}&pageSize=${hashtagPageSize}`,
            {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            }
        );

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const payload = await response.json();

        if (!payload.success && !payload.Success) {
            throw new Error(
                payload.error ||
                payload.Error ||
                'Failed to load hashtags'
            );
        }

        const pagedData =
            payload.data ??
            payload.Data;

        const hashtags =
            pagedData?.items ??
            pagedData?.Items ??
            [];

        const totalPages =
            pagedData?.totalPages ??
            pagedData?.TotalPages ??
            1;

        const currentPage =
            pagedData?.page ??
            pagedData?.Page ??
            nextPage;

        const existing = new Set(
            Array.from(
                container.querySelectorAll('[data-hashtag-tag]')
            ).map(el =>
                (el.getAttribute('data-hashtag-tag') || '')
                    .trim()
                    .toLowerCase()
            )
        );

        const extras = hashtags.filter(hashtag => {
            const tag = hashtag.tag || hashtag.Tag;

            if (!tag) {
                return false;
            }

            return !existing.has(
                String(tag).trim().toLowerCase()
            );
        });

        extras.forEach(hashtag => {
            const tag = hashtag.tag || hashtag.Tag;
            const count = hashtag.count ?? hashtag.Count ?? 0;

            container.insertAdjacentHTML(
                'beforeend',
                renderTrendingHashtagItem(tag, count)
            );
        });

        currentHashtagPage = currentPage;

        if (currentHashtagPage >= totalPages) {
            button.classList.add('hidden');
        }

    } catch (error) {
        console.warn(
            '[sidebar.js] Failed to load more hashtags:',
            error
        );

        if (window.SohbaApp && SohbaApp.toast) {
            SohbaApp.toast(
                'Failed to load hashtags',
                'error'
            );
        }

    } finally {
        isHashtagLoading = false;
        button.disabled = false;
        button.textContent = originalText;
    }
}