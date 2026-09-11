// sohba-stories.js - Stories functionality

// Story Viewer State
let currentUserId = null;
let currentUserStories = [];
let currentStoryIndex = 0;
let progressInterval = null;

// Open Story Viewer
window.openStoryViewer = async function (userId) {
    currentUserId = userId;
    currentStoryIndex = 0;

    const response = await fetch(`/Stories/GetUserStories?userId=${userId}`);
    const payload = await response.json();

    const stories = payload.data ?? payload.Data ?? (Array.isArray(payload) ? payload : []);

    if (stories && stories.length > 0) {
        currentUserStories = stories;
        showStory(0);
        document.getElementById('storyViewerModal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
        startProgress();
    } else {
        window.SohbaApp.toast('No stories available', 'info');
    }
};

// Show specific story
function showStory(index) {
    if (index < 0 || index >= currentUserStories.length) {
        closeStoryViewer();
        return;
    }

    currentStoryIndex = index;
    const story = currentUserStories[index];

    // Update UI
    document.getElementById('storyUserName').textContent = story.userName;
    document.getElementById('storyUserAvatar').src = story.userProfilePicture ||
        `https://ui-avatars.com/api/?name=${story.userName}&background=345e69&color=fff`;
    document.getElementById('storyTime').textContent = timeAgo(story.createdAt);
    document.getElementById('storyViewersCount').textContent = story.viewersCount || 0;

    // Load media
    const contentDiv = document.getElementById('storyContent');
    contentDiv.replaceChildren();

    if (story.mediaType === 'video') {
        const video = document.createElement('video');
        video.src = story.mediaUrl;
        video.className = 'max-h-full max-w-full';
        video.autoplay = true;
        video.playsInline = true;
        contentDiv.appendChild(video);
    } else {
        const img = document.createElement('img');
        img.src = story.mediaUrl || 'https://via.placeholder.com/600';
        img.className = 'max-h-full max-w-full object-contain';
        contentDiv.appendChild(img);
    }

    const currentUserId = document.querySelector('meta[name="current-user-id"]')?.content;
    const isOwner = currentUserId && story.userId === currentUserId;
    document.getElementById('storyOwnerActions').classList.toggle('hidden', !isOwner);

    const viewersTrigger = document.getElementById('storyViewersTrigger');
    if (viewersTrigger) {
        viewersTrigger.style.cursor = isOwner ? 'pointer' : 'default';
        viewersTrigger.onclick = isOwner ? openStoryViewersPanel : null;
    }

    // reaction state
    document.getElementById('storyLikeCount').textContent = story.reactionsCount || 0;
    document.getElementById('storyLikeIcon').textContent = story.currentUserReacted ? '❤️' : '🤍';

    window.SohbaApp.post('/Stories/MarkAsViewed', { storyId: story.id });
}

// Progress bar
function startProgress() {
    const progressBar = document.getElementById('storyProgress');
    if (progressBar) progressBar.style.width = '0%';

    const currentStory = currentUserStories[currentStoryIndex];
    const videoEl = document.querySelector('#storyContent video');

    if (currentStory && currentStory.mediaType === 'video' && videoEl) {
        videoEl.ontimeupdate = function () {
            if (videoEl.duration) {
                const pct = (videoEl.currentTime / videoEl.duration) * 100;
                if (progressBar) progressBar.style.width = pct + '%';
            }
        };
        videoEl.onended = function () {
            navigateStory('next');
        };
        return;
    }

    let progress = 0;
    progressInterval = setInterval(() => {
        progress += 1;
        if (progressBar) progressBar.style.width = progress + '%';

        if (progress >= 100) {
            clearInterval(progressInterval);
            navigateStory('next');
        }
    }, 50); // 5 seconds total for images
}

// Helper to unconditionally stop and unload all playing videos in the Story Viewer
function stopAllStoryVideos() {
    const contentDiv = document.getElementById('storyContent');
    if (contentDiv) {
        const videos = contentDiv.querySelectorAll('video');
        videos.forEach(video => {
            video.ontimeupdate = null;
            video.onended = null;
            video.pause();
            video.currentTime = 0;
            video.removeAttribute('src');
            try {
                video.load(); // Forces browser to abort media decoding and stop audio
            } catch (e) {
                // Ignore load abort exceptions
            }
        });
        contentDiv.replaceChildren();
    }
}

// Navigation
window.navigateStory = function (direction) {
    clearInterval(progressInterval);
    stopAllStoryVideos();

    if (direction === 'next') {
        if (currentStoryIndex < currentUserStories.length - 1) {
            showStory(currentStoryIndex + 1);
            startProgress();
        } else {
            // Advance to next user's stories in rail if available
            const currentCard = document.querySelector(`.story-user-card[data-user-id="${currentUserId}"]`);
            const nextCard = currentCard ? currentCard.nextElementSibling : null;
            const nextUserId = nextCard ? nextCard.getAttribute('data-user-id') : null;

            if (nextUserId) {
                openStoryViewer(nextUserId);
            } else {
                closeStoryViewer();
            }
        }
    } else if (direction === 'prev') {
        if (currentStoryIndex > 0) {
            showStory(currentStoryIndex - 1);
            startProgress();
        } else {
            const progressBar = document.getElementById('storyProgress');
            if (progressBar) progressBar.style.width = '0%';
            startProgress();
        }
    }
};





// Delete
window.deleteCurrentStory = function () {
    const story = currentUserStories[currentStoryIndex];
    if (!story) return;

    window.showConfirmModal({
        title: 'Delete Story',
        message: 'Are you sure you want to delete this story? This cannot be undone.',
        type: 'delete',
        confirmText: 'Delete',
        onConfirm: async () => {
            const result = await SohbaApp.post('/Stories/Delete', { id: story.id });
            if (result.success) {
                SohbaApp.toast('Story deleted', 'success');
                currentUserStories.splice(currentStoryIndex, 1);
                if (currentUserStories.length === 0) {
                    closeStoryViewer();
                } else {
                    showStory(Math.min(currentStoryIndex, currentUserStories.length - 1));
                }
            } else {
                SohbaApp.toast(result.error || 'Failed to delete story', 'error');
            }
        }
    });
};

// Like/unlike toggle
window.toggleCurrentStoryLike = async function () {
    const story = currentUserStories[currentStoryIndex];
    if (!story) return;

    const result = await SohbaApp.post('/Stories/React', { storyId: story.id, reactionType: 'Like' });
    if (!result.success) {
        SohbaApp.toast(result.error || 'Failed to react', 'error');
        return;
    }

    story.currentUserReacted = result.action === 'added';
    story.reactionsCount = result.newCount;
    document.getElementById('storyLikeCount').textContent = result.newCount;
    document.getElementById('storyLikeIcon').textContent = story.currentUserReacted ? '❤️' : '🤍';
};

// Owner-only viewers list
window.openStoryViewersPanel = async function () {
    const story = currentUserStories[currentStoryIndex];
    if (!story) return;

    const listEl = document.getElementById('storyViewersList');
    listEl.innerHTML = '<p class="text-white/60 text-sm">Loading...</p>';
    document.getElementById('storyViewersPanel').classList.remove('hidden');

    const response = await fetch(`/Stories/GetStoryViewers?storyId=${story.id}`);
    const result = await response.json();

    if (!result.success) {
        listEl.innerHTML = `<p class="text-white/60 text-sm">${result.error || 'Unable to load viewers.'}</p>`;
        return;
    }

    const viewers = result.data || [];
    listEl.innerHTML = viewers.length === 0
        ? '<p class="text-white/60 text-sm">No views yet.</p>'
        : viewers.map(v => `
            <div class="flex items-center gap-3">
                <img src="${v.profilePictureUrl || `https://ui-avatars.com/api/?name=${v.userName}&background=345e69&color=fff`}" class="w-9 h-9 rounded-full object-cover">
                <span class="text-white text-sm">${v.userName}</span>
            </div>`).join('');
};

window.closeStoryViewersPanel = function () {
    document.getElementById('storyViewersPanel').classList.add('hidden');
};









// Close viewer: Immediately terminates video playback and audio, then hides modal
window.closeStoryViewer = function () {
    clearInterval(progressInterval);
    stopAllStoryVideos();

    const modal = document.getElementById('storyViewerModal');
    if (modal) {
        modal.classList.add('hidden');
    }
    document.body.style.overflow = '';
    currentUserId = null;
    currentUserStories = [];
};

// Keyboard navigation
document.addEventListener('keydown', function (e) {
    const modal = document.getElementById('storyViewerModal');
    if (modal && !modal.classList.contains('hidden')) {
        if (e.key === 'ArrowLeft') {
            navigateStory('prev');
        } else if (e.key === 'ArrowRight') {
            navigateStory('next');
        } else if (e.key === 'Escape') {
            closeStoryViewer();
        }
    }
});

// Time ago function
function timeAgo(date) {
    const seconds = Math.floor((new Date() - new Date(date)) / 1000);

    if (seconds < 60) return 'just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return minutes + 'm ago';
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours + 'h ago';
    return Math.floor(hours / 24) + 'd ago';
}

// Rail and trigger event delegation consolidated from stories.js
document.addEventListener('DOMContentLoaded', function () {
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
});