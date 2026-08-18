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
    if (story.mediaType === 'video') {
        contentDiv.innerHTML = `<video src="${story.mediaUrl}" class="max-h-full max-w-full" autoplay></video>`;
    } else {
        contentDiv.innerHTML = `<img src="${story.mediaUrl || 'https://via.placeholder.com/600'}" class="max-h-full max-w-full object-contain">`;
    }


    const currentUserId = document.querySelector('meta[name="current-user-id"]')?.content;
    const isOwner = currentUserId && story.userId === currentUserId;
    document.getElementById('storyOwnerActions').classList.toggle('hidden', !isOwner);
    document.getElementById('storyViewersTrigger').style.cursor = isOwner ? 'pointer' : 'default';
    document.getElementById('storyViewersTrigger').onclick = isOwner ? openStoryViewersPanel : null;

    // reaction state
    document.getElementById('storyLikeCount').textContent = story.reactionsCount || 0;
    document.getElementById('storyLikeIcon').textContent = story.currentUserReacted ? '❤️' : '🤍';

    fetch('/Stories/MarkAsViewed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ storyId: story.id })
    });
}

// Progress bar
function startProgress() {
    let progress = 0;
    progressInterval = setInterval(() => {
        progress += 1;
        document.getElementById('storyProgress').style.width = progress + '%';

        if (progress >= 100) {
            clearInterval(progressInterval);
            navigateStory('next');
        }
    }, 50); // 5 seconds total
}

// Navigation
window.navigateStory = function (direction) {
    clearInterval(progressInterval);

    if (direction === 'next') {
        if (currentStoryIndex < currentUserStories.length - 1) {
            showStory(currentStoryIndex + 1);
            startProgress();
        } else {
            closeStoryViewer();
        }
    } else if (direction === 'prev') {
        if (currentStoryIndex > 0) {
            showStory(currentStoryIndex - 1);
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









// Close viewer
window.closeStoryViewer = function () {
    document.getElementById('storyViewerModal').classList.add('hidden');
    document.body.style.overflow = '';
    clearInterval(progressInterval);
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