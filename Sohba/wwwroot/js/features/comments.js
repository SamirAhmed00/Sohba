// wwwroot/js/features/comments.js
// Handles client-side actions for comments: Create, Reply, Delete, Toggle

window.SohbaApp = window.SohbaApp || {};

/**
 * Displays the reply input form for a comment.
 */
window.SohbaApp.showReplyForm = function (commentId, userName) {
    const form = document.getElementById(`replyForm-${commentId}`);
    if (form) {
        form.classList.remove('hidden');
        const input = document.getElementById(`replyInput-${commentId}`);
        if (input) input.focus();
    }
};

/**
 * Hides the reply input form for a comment.
 */
window.SohbaApp.hideReplyForm = function (commentId) {
    const form = document.getElementById(`replyForm-${commentId}`);
    if (form) form.classList.add('hidden');
};

/**
 * Toggles visibility of nested replies container.
 */
window.SohbaApp.toggleReplies = function (commentId) {
    const replies = document.getElementById(`replies-${commentId}`);
    if (replies) {
        replies.classList.toggle('hidden');
    }
};

/**
 * Expands or truncates long comment text.
 */
window.SohbaApp.toggleComment = function (commentId, fullContent, shortContent) {
    const container = document.getElementById(commentId);
    if (!container) return;

    const btn = container.parentElement.querySelector('.toggle-comment-btn');
    if (container.innerText.length > shortContent.length) {
        container.innerText = shortContent;
        if (btn) btn.innerText = 'See more';
    } else {
        container.innerText = fullContent;
        if (btn) btn.innerText = 'See less';
    }
};

/**
 * Deletes a comment and updates client DOM.
 */
async function deleteComment(commentId, postId = null) {
    if (!commentId) return;

    window.showConfirmModal({
        title: 'Delete Comment',
        message: 'Are you sure you want to delete this comment? This action cannot be undone.',
        type: 'delete',
        confirmText: 'Delete',
        onConfirm: async () => {
            try {
                const result = await window.SohbaApp.post('/Comments/Delete', { id: commentId });

                if (result.success) {
                    window.SohbaApp.toast('Comment deleted successfully!', 'success');

                    const commentElement = document.querySelector(`[data-comment-id="${commentId}"]`);
                    if (commentElement) {
                        commentElement.style.transition = 'opacity 0.3s ease';
                        commentElement.style.opacity = '0';
                        setTimeout(() => commentElement.remove(), 300);
                    }

                    if (postId) {
                        const countEl = document.getElementById(`comments-count-${postId}`);
                        if (countEl) {
                            let currentCount = parseInt(countEl.innerText) || 0;
                            if (currentCount > 0) {
                                countEl.innerText = currentCount - 1;
                            }
                        }
                    }
                } else {
                    window.SohbaApp.toast(result.error || 'Failed to delete comment.', 'error');
                }
            } catch (err) {
                console.error("Comment deletion failed:", err);
                window.SohbaApp.toast('An unexpected error occurred.', 'error');
            }
        }
    });
}

window.SohbaApp.deleteComment = deleteComment;