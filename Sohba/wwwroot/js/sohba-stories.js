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

    // جلب stories الخاصه بالمستخدم
    const response = await fetch(`/Stories/GetUserStories?userId=${userId}`);
    const stories = await response.json();

    if (stories && stories.length > 0) {
        currentUserStories = stories;
        showStory(0);
        document.getElementById('storyViewerModal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
        startProgress();
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

    // Mark as viewed
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