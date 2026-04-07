// wwwroot/js/features/search.js
// Handles tab switching and search refinement on the Search/Results page.
// Extracted from Results.cshtml inline <script> per RULES.md §2 (Zero Inline JS).

/**
 * Switches the active results tab and updates the URL without a page reload.
 * @param {string} tab - One of: 'all', 'posts', 'people', 'groups', 'pages'
 */
function switchTab(tab) {
    // Sync the URL so sharing / back-navigation lands on the same tab.
    const url = new URL(window.location);
    url.searchParams.set('tab', tab);
    window.history.pushState({}, '', url);

    // Deactivate all tab buttons.
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('text-[#345e69]', 'border-b-2', 'border-[#345e69]');
        btn.classList.add('text-gray-400');
    });

    // Activate the clicked button (event is available from the inline onclick attribute).
    if (event && event.target) {
        event.target.classList.add('text-[#345e69]', 'border-b-2', 'border-[#345e69]');
        event.target.classList.remove('text-gray-400');
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
        window.location.href = `/Search/Index?q=${encodeURIComponent(query)}`;
    }
}
