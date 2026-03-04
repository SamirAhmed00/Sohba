// sohba-posts.js - Posts and interactions functions

// ------------ Reaction Functions -------------
window.SohbaApp.toggleReactionPicker = function (postId) {
    const button = document.querySelector(`[data-like-button="${postId}"]`);
    if (button.dataset.currentReaction) {
        window.SohbaApp.reactToPost(postId, button.dataset.currentReaction);
        return;
    }
    const picker = document.getElementById(`reaction-picker-${postId}`);
    if (!picker) return;
    document.querySelectorAll('[id^="reaction-picker-"]').forEach(p => p.classList.add('hidden'));
    picker.classList.toggle('hidden');
};

window.SohbaApp.reactToPost = async function (postId, reactionType) {
    document.getElementById(`reaction-picker-${postId}`)?.classList.add('hidden');

    try {
        const result = await window.SohbaApp.post('/Posts/React', { postId, reactionType });

        if (!result.success) {
            window.SohbaApp.toast(result.error || 'Failed', 'error');
            return;
        }

        const button = document.querySelector(`[data-like-button="${postId}"]`);
        const icon = button.querySelector('.like-icon');
        const text = button.querySelector('.like-text');

        const map = {
            Like: { icon: '👍', classes: 'text-blue-700 bg-blue-100 hover:bg-blue-200' },
            Love: { icon: '❤️', classes: 'text-rose-700 bg-rose-100 hover:bg-rose-200' },
            Haha: { icon: '😂', classes: 'text-amber-600 bg-amber-100 hover:bg-amber-200' },
            Wow: { icon: '😮', classes: 'text-orange-700 bg-orange-100 hover:bg-orange-200' },
            Sad: { icon: '😢', classes: 'text-indigo-700 bg-indigo-100 hover:bg-indigo-200' },
            Angry: { icon: '😠', classes: 'text-red-700 bg-red-100 hover:bg-red-200' }
        };

        if (result.action === 'added') {
            window.SohbaApp.toast('Reaction added!', 'success');
            button.dataset.currentReaction = result.reactionType;
            const r = map[result.reactionType];
            icon.innerText = r.icon;
            text.innerText = result.reactionType;
            button.className = `w-full flex items-center justify-center gap-2 py-2.5 rounded-xl transition-all duration-200 font-bold ${r.classes}`;
        } else if (result.action === 'removed') {
            window.SohbaApp.toast('Reaction removed!', 'success');
            button.dataset.currentReaction = '';
            icon.innerText = '👍';
            text.innerText = 'Like';
            button.className = `w-full flex items-center justify-center gap-2 py-2.5 rounded-xl transition-all duration-200 font-bold text-slate-600 hover:bg-slate-50`;
        }

        const countSpan = document.querySelector(`.reaction-count-${postId}`);
        if (countSpan && result.newCount !== undefined) {
            countSpan.innerText = result.newCount + ' reactions';
        }
    } catch (e) {
        console.error(e);
        window.SohbaApp.toast('Network error', 'error');
    }
};

// ------------ Save & Favorite Functions -------------
window.SohbaApp.savePost = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleSavePost', {
            postId: postId,
            isFavorite: false
        });

        if (result.success) {
            const button = document.querySelector(`[data-save-button="${postId}"]`);
            const icon = button.querySelector('svg');
            const text = button.querySelector('.btn-text');

            if (result.saved) {
                button.classList.add('text-amber-600', 'bg-amber-50');
                icon.setAttribute('fill', 'currentColor');
                text.textContent = 'Saved';
            } else {
                button.classList.remove('text-amber-600', 'bg-amber-50');
                icon.setAttribute('fill', 'none');
                text.textContent = 'Save Post';
            }

            window.SohbaApp.toast(result.message, 'success');
        } else {
            window.SohbaApp.toast(result.error || 'Failed to save post', 'error');
        }
    } catch (error) {
        console.error('Save error:', error);
        window.SohbaApp.toast('Network error', 'error');
    }
};

window.SohbaApp.addToFavorites = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleSavePost', {
            postId: postId,
            isFavorite: true
        });

        if (result.success) {
            const button = document.querySelector(`[data-fav-button="${postId}"]`);
            const icon = button.querySelector('svg');
            const text = button.querySelector('.btn-text');

            if (result.saved) {
                button.classList.add('text-pink-600', 'bg-pink-50');
                icon.setAttribute('fill', 'currentColor');
                text.textContent = 'Favorited';
            } else {
                button.classList.remove('text-pink-600', 'bg-pink-50');
                icon.setAttribute('fill', 'none');
                text.textContent = 'Add to Favorites';
            }

            window.SohbaApp.toast(result.message, 'success');
        } else {
            window.SohbaApp.toast(result.error || 'Failed to add to favorites', 'error');
        }
    } catch (error) {
        console.error('Favorite error:', error);
        window.SohbaApp.toast('Network error', 'error');
    }
};

// ------------ Comment Functions -------------
window.SohbaApp.submitComment = async function () {
    const modal = document.getElementById('postModal');
    const postId = modal.dataset.postId;
    const input = document.getElementById('commentInput');
    const content = input.value.trim();

    if (!content) return;

    try {
        const response = await fetch('/Posts/Comment', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ postId, content })
        });
        const result = await response.json();

        if (!result.success) {
            window.SohbaApp.toast(result.error || 'Failed', 'error');
            return;
        }

        const commentHtml = `
            <div class="flex items-start gap-3">
                <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(result.comment.userName)}&background=random" class="w-8 h-8 rounded-full">
                <div class="flex-1">
                    <span class="font-semibold text-sm">${result.comment.userName}</span>
                    <p class="text-sm text-gray-700">${result.comment.content}</p>
                    <span class="text-xs text-gray-400">${new Date(result.comment.createdAt).toLocaleString()}</span>
                </div>
            </div>
        `;
        document.getElementById('modalComments').insertAdjacentHTML('afterbegin', commentHtml);
        input.value = '';

        const countSpan = document.getElementById(`comments-count-${postId}`);
        if (countSpan) {
            const newCount = parseInt(countSpan.innerText) + 1;
            countSpan.innerText = newCount;
            const labelSpan = document.getElementById(`comments-label-${postId}`);
            if (labelSpan) labelSpan.innerText = newCount === 1 ? 'comment' : 'comments';
        }
        window.SohbaApp.toast('Comment posted!', 'success');
    } catch (e) {
        console.error(e);
        window.SohbaApp.toast('Network error', 'error');
    }
};

window.SohbaApp.toggleComment = function (commentId, fullText, shortText) {
    const commentDiv = document.getElementById(commentId);
    const button = commentDiv?.nextElementSibling;

    if (!commentDiv || !button || button.tagName !== 'BUTTON') return;

    const isExpanded = commentDiv.innerText === fullText;

    if (isExpanded) {
        commentDiv.innerText = shortText;
        button.innerText = 'See more';
    } else {
        commentDiv.innerText = fullText;
        button.innerText = 'See less';
    }
};