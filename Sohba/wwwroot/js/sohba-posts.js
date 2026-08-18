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
            icon.innerText = 'React';
            text.innerText = '';
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


// -- We Will Delete It --- But Kept Now For Compatibility With Old Code --
window.SohbaApp.savePost = async function (postId) {
    try {
        const result = await window.SohbaApp.post('/Posts/ToggleSavePost', {
            postId: postId,
            isFavorite: false
        });

        if (result.success) {
            updateSaveFavoriteButtons(postId, result.saved, false);

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
        const result = await window.SohbaApp.post('/Posts/ToggleFavorite', { postId });

        if (result.success) {
            const btn = document.querySelector(`[data-fav-button="${postId}"]`);
            const isCurrentlyFav = btn && btn.classList.contains('text-pink-600');
            const newFavState = !isCurrentlyFav;

            // Only update the Favorite button; the Save button state is unchanged.
            updateSaveFavoriteButtons(postId, null, newFavState);

            window.SohbaApp.toast(newFavState ? 'Added to favorites!' : 'Removed from favorites', 'success');
        } else {
            window.SohbaApp.toast(result.error || 'Failed to update favorites', 'error');
        }
    } catch (error) {
        console.error('Favorite error:', error);
        window.SohbaApp.toast('Network error', 'error');
    }
};

function updateSaveFavoriteButtons(postId, isSaved, isFavorite) {
    const saveBtn = document.querySelector(`[data-save-button="${postId}"]`);
    const favBtn = document.querySelector(`[data-fav-button="${postId}"]`);

    if (saveBtn && isSaved !== null && isSaved !== undefined) {
        const icon = saveBtn.querySelector('svg');
        const text = saveBtn.querySelector('.btn-text');
        if (isSaved) {
            saveBtn.classList.add('text-amber-600', 'bg-amber-50');
            icon.setAttribute('fill', 'currentColor');
            text.textContent = 'Saved';
        } else {
            saveBtn.classList.remove('text-amber-600', 'bg-amber-50');
            icon.setAttribute('fill', 'none');
            text.textContent = 'Save Post';
        }
    }
    if (favBtn && isFavorite !== null && isFavorite !== undefined) {
        const icon = favBtn.querySelector('svg');
        const text = favBtn.querySelector('.btn-text');
        if (isFavorite) {
            favBtn.classList.add('text-pink-600', 'bg-pink-50');
            icon.setAttribute('fill', 'currentColor');
            text.textContent = 'Favorited';
        } else {
            favBtn.classList.remove('text-pink-600', 'bg-pink-50');
            icon.setAttribute('fill', 'none');
            text.textContent = 'Add to Favorites';
        }
    }
}

// ------------ Comment Functions -------------
window.SohbaApp.submitComment = async function () {
    const modal = document.getElementById('postModal');
    const postId = modal.dataset.postId;
    const input = document.getElementById('commentInput');
    const content = input.value.trim();

    if (!content) return;

    try {
       
        const result = await window.SohbaApp.post('/Posts/Comment', { postId, content });

        if (!result.success) {
            window.SohbaApp.toast(result.error || 'Failed', 'error');
            return;
        }

        

        const commentId = `comment-${result.comment.id}`;
        const commentHtml = `
                <div class="flex items-start gap-3 mb-3">
                    <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(result.comment.userName)}&background=random"
                         class="w-8 h-8 rounded-full flex-shrink-0">
                    <div class="flex-1 min-w-0">
                        <span class="font-semibold text-sm text-gray-900">${result.comment.userName}</span>
                        <div id="${commentId}" class="text-sm text-gray-700 break-words">
                            ${result.comment.content}
                        </div>
                        <div class="flex items-center gap-3 mt-1">
                            <span class="text-xs text-gray-400">${new Date(result.comment.createdAt).toLocaleString()}</span>
                            <button onclick="SohbaApp.showReplyForm('${result.comment.id}', '${result.comment.userName}')"
                                    class="text-xs text-[#345e69] hover:underline font-medium">
                                Reply
                            </button>
                            <button onclick="SohbaApp.deleteComment('${result.comment.id}', '${result.comment.postId}')"
                                    class="text-xs text-red-500 hover:underline font-medium ml-2">
                                Delete
                            </button>
                        </div>

                        <!-- Reply form (hidden by default) -->
                        <div id="replyForm-${result.comment.id}" class="mt-2 hidden">
                            <div class="flex items-start gap-3">
                                <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(result.comment.userName)}&background=345e69&color=fff"
                                     class="w-7 h-7 rounded-full flex-shrink-0">
                                <div class="flex-1">
                                    <input type="text" id="replyInput-${result.comment.id}"
                                           placeholder="Write a reply..."
                                           class="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#345e69]/20">
                                    <div class="flex gap-2 mt-2">
                                        <button onclick="SohbaApp.submitReply('${result.comment.id}', '${result.comment.postId}')"
                                                class="px-4 py-1.5 bg-[#345e69] text-white text-sm font-semibold rounded-lg hover:bg-[#2a4b55]">
                                            Reply
                                        </button>
                                        <button onclick="SohbaApp.hideReplyForm('${result.comment.id}')"
                                                class="px-4 py-1.5 text-sm text-gray-500 hover:text-gray-700">
                                            Cancel
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Replies container -->
                        <div id="replies-${result.comment.id}" class="mt-3 pl-4 border-l-2 border-slate-200 space-y-3"></div>
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

// Show reply form
window.showReplyForm = function (commentId, userName) {
    // Hide all other reply forms
    document.querySelectorAll('[id^="replyForm-"]').forEach(el => el.classList.add('hidden'));

    const form = document.getElementById(`replyForm-${commentId}`);
    if (form) {
        form.classList.remove('hidden');
        const input = document.getElementById(`replyInput-${commentId}`);
        if (input) {
            input.placeholder = `Reply to ${userName}...`;
            input.focus();
        }
    }
};

// Hide reply form
window.hideReplyForm = function (commentId) {
    const form = document.getElementById(`replyForm-${commentId}`);
    if (form) form.classList.add('hidden');
    const input = document.getElementById(`replyInput-${commentId}`);
    if (input) input.value = '';
};

// Toggle replies visibility
window.toggleReplies = function (commentId) {
    const container = document.getElementById(`replies-${commentId}`);
    if (container) {
        container.classList.toggle('hidden');
        const btn = container.previousElementSibling;
        if (btn && btn.tagName === 'BUTTON') {
            btn.textContent = container.classList.contains('hidden')
                ? `View ${container.querySelectorAll('.flex.items-start.gap-3').length} replies`
                : 'Hide replies';
        }
    }
};

// Submit reply
window.submitReply = async function (commentId, postId) {
    const input = document.getElementById(`replyInput-${commentId}`);
    if (!input) return;

    const content = input.value.trim();
    if (!content) {
        window.SohbaApp.toast('Please enter a reply', 'error');
        return;
    }

    try {
        const result = await window.SohbaApp.post('/Posts/Comment', {
            postId: postId,
            content: content,
            parentCommentId: commentId
        });

        if (result.success) {
            window.SohbaApp.toast('Reply posted!', 'success');
            input.value = '';
            hideReplyForm(commentId);

            // Reload comments to show the new reply
            // You can either reload the post modal or append the reply dynamically
            // For now, we'll reload the modal
            const modal = document.getElementById('postModal');
            if (modal) {
                const postIdFromModal = modal.dataset.postId;
                if (postIdFromModal) {
                    await window.SohbaApp.openPostModal(postIdFromModal);
                }
            }
        } else {
            window.SohbaApp.toast(result.error || 'Failed to post reply', 'error');
        }
    } catch (error) {
        console.error('Error posting reply:', error);
        window.SohbaApp.toast('Network error', 'error');
    }
};


// ------------ Delete / Edit Post -------------
window.SohbaApp.deletePost = function (postId) {
    window.showConfirmModal({
        title: 'Delete Post',
        message: 'Are you sure you want to delete this post? This cannot be undone.',
        type: 'delete',
        confirmText: 'Delete',
        onConfirm: async () => {
            try {
                const result = await window.SohbaApp.post('/Posts/Delete', { id: postId });

                if (result.success) {
                    window.SohbaApp.toast('Post deleted successfully.', 'success');
                    const card = document.querySelector(`[data-post-id="${postId}"]`);
                    if (card) {
                        card.style.transition = 'opacity 0.3s ease';
                        card.style.opacity = '0';
                        setTimeout(() => card.remove(), 300);
                    }
                } else {
                    window.SohbaApp.toast(result.error || 'Failed to delete post.', 'error');
                }
            } catch (err) {
                console.error('Delete post error:', err);
                window.SohbaApp.toast('Network error', 'error');
            }
        }
    });
};

window.SohbaApp.editPostModal = function (postId) {
    
    window.location.href = `/Posts/Edit/${postId}`;
};



// --------------- Save Posts ------------

// Toggle Save behaviour: if already saved -> remove from collections; otherwise -> open the picker.
window.SohbaApp.toggleSavePost = async function (postId, isSaved) {
    if (isSaved) {
        try {
            const result = await window.SohbaApp.post('/Posts/RemoveFromSaved', { postId });

            if (result.success) {
                const favBtn = document.querySelector(`[data-fav-button="${postId}"]`);
                const isFavorite = favBtn && favBtn.classList.contains('text-pink-600');
                updateSaveFavoriteButtons(postId, false, isFavorite);
                window.SohbaApp.toast('Removed from saved', 'success');
            } else {
                window.SohbaApp.toast(result.error || 'Failed to remove from saved', 'error');
            }
        } catch (error) {
            console.error('Remove saved error:', error);
            window.SohbaApp.toast('Network error', 'error');
        }
    } else {
        window.SohbaApp.openSavePostModal(postId);
    }
};

window.SohbaApp.openSavePostModal = async function (postId) {
    const modal = document.getElementById('savePostModal');
    if (!modal) return;

    modal.dataset.postId = postId;
    const listEl = document.getElementById('saveCollectionsList');
    const nameInput = document.getElementById('newCollectionName');
    listEl.innerHTML = '<div class="text-sm text-gray-400 text-center py-4">Loading...</div>';
    nameInput.value = '';

    const result = await window.SohbaApp.get('/Posts/GetUserCollections');
    const collections = result.data ?? [];

    if (collections.length === 0) {
        listEl.innerHTML = '<div class="text-sm text-gray-400 text-center py-4">No collections yet. Create one below.</div>';
    } else {
        listEl.innerHTML = collections.map(c => `
            <button onclick="SohbaApp.saveToCollection('${postId}', '${c.id}')"
                    class="w-full text-left px-4 py-2.5 rounded-xl hover:bg-slate-50 text-sm font-semibold text-gray-700">
                ${c.name}
            </button>
        `).join('');
    }

    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';
};

window.SohbaApp.saveToCollection = async function (postId, collectionId) {
    const result = await window.SohbaApp.post('/Posts/SaveToCollection', { postId, collectionId });
    if (result.success) {
        window.SohbaApp.toast('Post saved to collection!', 'success');
        window.SohbaApp.closeSavePostModal();
        updateSaveFavoriteButtons(postId, true, false);
    } else {
        window.SohbaApp.toast(result.error || 'Failed to save post', 'error');
    }
};

window.SohbaApp.createNewCollection = async function () {
    const name = document.getElementById('newCollectionName')?.value.trim();
    const postId = document.getElementById('savePostModal')?.dataset.postId;
    if (!name) { window.SohbaApp.toast('Please enter a collection name', 'error'); return; }

    const createResult = await window.SohbaApp.post('/Posts/CreateCollection', { name });
    if (!createResult.success) {
        window.SohbaApp.toast(createResult.error || 'Failed to create collection', 'error');
        return;
    }

    const collectionId = createResult.data?.id;
    if (postId && collectionId) {
        await window.SohbaApp.saveToCollection(postId, collectionId);
    } else {
        window.SohbaApp.closeSavePostModal();
        window.SohbaApp.toast('Collection created!', 'success');
    }
};

window.SohbaApp.closeSavePostModal = function () {
    const modal = document.getElementById('savePostModal');
    if (modal) modal.classList.add('hidden');
    document.body.style.overflow = '';
};

window.SohbaApp.get = async function (url) {
    try {
        const response = await fetch(url);
        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            return { success: false, error: `Server error (HTTP ${response.status}).` };
        }
        return await response.json();
    } catch (error) {
        console.error('[SohbaApp.get] Network error:', error);
        return { success: false, error: 'Network error.' };
    }
};



// ---- Namespace aliases: HTML attributes call SohbaApp.* ----
window.SohbaApp.showReplyForm = window.showReplyForm;
window.SohbaApp.hideReplyForm = window.hideReplyForm;
window.SohbaApp.submitReply = window.submitReply;
window.SohbaApp.toggleReplies = window.toggleReplies;
window.SohbaApp.deleteComment = window.deleteComment;

// Close any open reaction picker when clicking outside it or its toggle button —
// mirrors the existing pattern used for notifDropdown/profileDropdown in header.js.
document.addEventListener('click', function (e) {
    if (e.target.closest('[id^="reaction-picker-"]') || e.target.closest('[data-like-button]')) {
        return;
    }
    document.querySelectorAll('[id^="reaction-picker-"]').forEach(p => p.classList.add('hidden'));
});