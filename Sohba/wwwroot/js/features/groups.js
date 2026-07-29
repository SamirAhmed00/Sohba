(function () {
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