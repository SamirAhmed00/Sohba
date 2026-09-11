// wwwroot/js/features/search.js
// Handles tab switching and search refinement on the Search/Results page.
// Extracted from Results.cshtml inline <script> per RULES.md §2 (Zero Inline JS).

/**
 * Switches the active results tab and updates the URL without a page reload.
 * @param {string} tab - One of: 'all', 'posts', 'people', 'groups', 'pages'
 */
function Search_SwitchTab(tab) {
    // Sync the URL so sharing / back-navigation lands on the same tab.
    const url = new URL(window.location);
    url.searchParams.set('tab', tab);
    window.history.pushState({}, '', url);

    // Deactivate all tab buttons.
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('text-[#345e69]', 'border-b-2', 'border-[#345e69]');
        btn.classList.add('text-gray-400');
    });

    // Activate the target tab button in the header tab bar.
    const targetTabBtn = document.querySelector(`.tab-btn[data-tab="${tab}"]`);
    if (targetTabBtn) {
        targetTabBtn.classList.add('text-[#345e69]', 'border-b-2', 'border-[#345e69]');
        targetTabBtn.classList.remove('text-gray-400');
    }

    // Hide all tab content panels, then show the selected one.
    document.querySelectorAll('.tab-content').forEach(content => {
        content.classList.add('hidden');
    });

    const panel = document.getElementById(`tab-${tab}`);
    if (panel) panel.classList.remove('hidden');
}


/**
 * Navigates to the search results page with the refined query.
 * Triggered when the user presses Enter in the refine-search input.
 */
function refineSearch() {
    const input = document.getElementById('refineSearchInput');
    if (!input) return;

    const query = input.value.trim();
    if (query.length >= 2) {
        const url = new URL(window.location);
        const currentTab = url.searchParams.get('tab') || 'all';
        window.location.href = `/Search/Index?q=${encodeURIComponent(query)}&tab=${encodeURIComponent(currentTab)}`;
    } else if (window.SohbaApp && SohbaApp.toast) {
        window.SohbaApp.toast('Type at least 2 characters', 'info');
    }
}






// ============================================================
// HEADER GLOBAL SEARCH (quick results + submit on Enter/Button)
// ============================================================
function initializeGlobalSearch() {
    const searchInput = document.getElementById('globalSearchInput');
    const quickResults = document.getElementById('quickSearchResults');
    const searchForm = document.getElementById('searchForm');
    const searchQueryHidden = document.getElementById('searchQueryHidden');
    const searchBtn = document.getElementById('globalSearchBtn');
    const mobileSearchInput = document.getElementById('mobileSearchInput');

    if (!searchInput) return;

    let searchTimeout;
    let quickSearchAbortController = null;

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    async function runQuickSearch(query, resultsEl) {
        try {
            if (quickSearchAbortController) {
                quickSearchAbortController.abort();
            }
            quickSearchAbortController = new AbortController();

            const response = await fetch(`/Search/QuickSearch?q=${encodeURIComponent(query)}`, {
                signal: quickSearchAbortController.signal
            });
            const data = await response.json();

            if (!resultsEl) return;

            if (data.success === false || data.data === null) {
                resultsEl.innerHTML = '<div class="p-4 text-center text-gray-500">No results found</div>';
                resultsEl.classList.remove('hidden');
                return;
            }

            const payload = data.data;
            if (!payload || payload.totalCount === 0) {
                resultsEl.innerHTML = '<div class="p-4 text-center text-gray-500">No results found</div>';
                resultsEl.classList.remove('hidden');
                return;
            }

            let html = '';

            const users = payload.users || [];
            if (users.length > 0) {
                html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">PEOPLE</div>';
                html += users.map(user => `
                    <a href="${escapeHtml(user.url)}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                        <img src="${user.profilePictureUrl ? escapeHtml(user.profilePictureUrl) : `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name || '')}&background=345e69&color=fff`}" class="w-8 h-8 rounded-full object-cover">
                        <div>
                            <div class="font-semibold text-gray-900">${escapeHtml(user.name)}</div>
                            <div class="text-xs text-gray-500">${escapeHtml(user.bio || 'User')}</div>
                        </div>
                    </a>`).join('');
            }

            const posts = payload.posts || [];
            if (posts.length > 0) {
                html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">POSTS</div>';
                html += posts.map(post => `
                    <a href="${escapeHtml(post.url)}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                        ${post.imageUrl
                        ? `<img src="${escapeHtml(post.imageUrl)}" class="w-8 h-8 rounded object-cover">`
                        : '<div class="w-8 h-8 bg-gray-200 rounded flex items-center justify-center text-gray-500">📝</div>'}
                        <div>
                            <div class="font-semibold text-gray-900">${escapeHtml(post.title)}</div>
                            <div class="text-xs text-gray-500">${escapeHtml(post.authorName)}</div>
                        </div>
                    </a>`).join('');
            }

            const groups = payload.groups || [];
            if (groups.length > 0) {
                html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">GROUPS</div>';
                html += groups.map(group => `
                    <a href="${escapeHtml(group.url)}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                        <div class="w-8 h-8 bg-gray-200 rounded-lg flex items-center justify-center text-gray-500 font-bold">${escapeHtml(group.name ? group.name[0] : '')}</div>
                        <div>
                            <div class="font-semibold text-gray-900">${escapeHtml(group.name)}</div>
                            <div class="text-xs text-gray-500">${escapeHtml(group.membersCount)} members</div>
                        </div>
                    </a>`).join('');
            }

            const pages = payload.pages || [];
            if (pages.length > 0) {
                html += '<div class="px-4 py-2 bg-gray-50 text-xs font-bold text-gray-500">PAGES</div>';
                html += pages.map(page => `
                    <a href="${escapeHtml(page.url)}" class="flex items-center gap-3 px-4 py-2 hover:bg-gray-50 transition-colors">
                        <div class="w-8 h-8 bg-gray-200 rounded-lg flex items-center justify-center text-gray-500 font-bold">${escapeHtml(page.name ? page.name[0] : '')}</div>
                        <div class="font-semibold text-gray-900">${escapeHtml(page.name)}</div>
                    </a>`).join('');
            }

            if (payload.totalCount > 3) {
                html += `
                    <div class="p-3 border-t border-gray-100 text-center">
                        <a href="/Search/Index?q=${encodeURIComponent(query)}"
                           class="text-sm text-[#345e69] font-semibold hover:underline">
                            See all ${payload.totalCount} results →
                        </a>
                    </div>`;
            }

            resultsEl.innerHTML = html;
            resultsEl.classList.remove('hidden');
        } catch (error) {
            if (error.name === 'AbortError') return;
            console.error('Search error:', error);
            if (window.SohbaApp && SohbaApp.toast) {
                SohbaApp.toast('Search failed', 'error');
            }
        }
    }

    function onSearchInput(e, resultsEl) {
        const query = e.target.value.trim();
        clearTimeout(searchTimeout);

        if (query.length < 2) {
            if (resultsEl) resultsEl.classList.add('hidden');
            return;
        }

        searchTimeout = setTimeout(() => runQuickSearch(query, resultsEl), 300);
    }

    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            onSearchInput(e, quickResults);
        });
    }

    if (mobileSearchInput) {
        mobileSearchInput.addEventListener('input', function (e) {
            onSearchInput(e, quickResults);
        });
    }

    function submitSearch(sourceInput) {
        const inputEl = sourceInput || searchInput || mobileSearchInput;
        const query = inputEl?.value.trim() || '';
        if (query.length >= 2 && searchForm) {
            if (searchQueryHidden) searchQueryHidden.value = query;
            searchForm.submit();
        } else if (query.length < 2) {
            if (window.SohbaApp && SohbaApp.toast) {
                SohbaApp.toast('Type at least 2 characters', 'info');
            }
        }
    }

    const mobileSearchSubmitBtn = document.getElementById('mobileSearchSubmitBtn');

    if (searchBtn) {
        searchBtn.addEventListener('click', function (e) {
            e.preventDefault();
            submitSearch(searchInput);
        });
    }

    if (mobileSearchSubmitBtn) {
        mobileSearchSubmitBtn.addEventListener('click', function (e) {
            e.preventDefault();
            submitSearch(mobileSearchInput);
        });
    }

    if (searchInput) {
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                submitSearch(searchInput);
            }
        });
    }

    if (mobileSearchInput) {
        mobileSearchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                submitSearch(mobileSearchInput);
            }
        });
    }

    document.addEventListener('click', function (e) {
        const inDesktop = searchInput && searchInput.contains(e.target);
        const inMobile = mobileSearchInput && mobileSearchInput.contains(e.target);
        if (quickResults && !inDesktop && !inMobile && !quickResults.contains(e.target)) {
            quickResults.classList.add('hidden');
        }
    });
}

// ============================================================
// MOBILE SEARCH TOGGLE
// ============================================================
function initializeMobileSearch() {
    const searchBtn = document.getElementById('mobileSearchBtn');
    const searchContainer = document.getElementById('mobileSearchContainer');
    if (searchBtn && searchContainer) {
        searchBtn.addEventListener('click', function () {
            const isClosed = searchContainer.classList.contains('max-h-0');
            const isOpen = isClosed;
            searchContainer.classList.toggle('max-h-0', !isOpen);
            searchContainer.classList.toggle('opacity-0', !isOpen);
            searchContainer.classList.toggle('border-transparent', !isOpen);
            searchContainer.classList.toggle('max-h-40', isOpen);
            searchContainer.classList.toggle('opacity-100', isOpen);
            searchContainer.classList.toggle('border-slate-100', isOpen);
            if (isOpen) {
                setTimeout(() => document.getElementById('mobileSearchInput')?.focus(), 100);
            }
        });
    }
}

// ============================================================
// FRIENDS SEARCH BUTTON (Find Friends page)
// ============================================================
function initializeFriendsSearch() {
    const friendsSearchBtn = document.getElementById('friendsSearchBtn');
    const searchInput = document.getElementById('friendsSearchInput');
    if (friendsSearchBtn && searchInput) {
        friendsSearchBtn.addEventListener('click', function () {
            const term = searchInput.value.trim().toLowerCase();
            const userCards = document.querySelectorAll('.user-card');
            let visibleCount = 0;
            userCards.forEach(card => {
                const name = (card.dataset.name || '').toLowerCase();
                if (name.includes(term)) {
                    card.style.display = 'block';
                    visibleCount++;
                } else {
                    card.style.display = 'none';
                }
            });
            const noResults = document.getElementById('noResultsMessage');
            if (noResults) {
                noResults.classList.toggle('hidden', visibleCount > 0);
            }
        });
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        initializeGlobalSearch();
        initializeMobileSearch();
        initializeFriendsSearch();
    });
} else {
    initializeGlobalSearch();
    initializeMobileSearch();
    initializeFriendsSearch();
}