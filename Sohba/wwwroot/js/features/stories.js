(function () {
    function initStories() {
        document.querySelectorAll('[data-action="scroll-stories"]').forEach(btn => {
            btn.addEventListener('click', function () {
                const direction = this.dataset.direction;
                const container = document.getElementById('storiesContainer');
                if (!container) return;
                const scrollAmount = 200;
                container.scrollBy({ left: direction === 'left' ? -scrollAmount : scrollAmount, behavior: 'smooth' });
            });
        });

        const createStoryCard = document.querySelector('[data-action="open-create-story"]');
        if (createStoryCard) {
            createStoryCard.addEventListener('click', function () {
                if (typeof openStoryModal === 'function') openStoryModal();
                else console.warn('openStoryModal is not defined');
            });
        }

        document.querySelectorAll('[data-action="open-story-viewer"]').forEach(card => {
            card.addEventListener('click', function () {
                const userId = this.dataset.userId;
                if (userId && typeof openStoryViewer === 'function') {
                    openStoryViewer(userId);
                }
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initStories);
    } else {
        initStories();
    }
})();