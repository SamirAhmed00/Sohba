// wwwroot/js/features/comments.js
// Handles client-side actions for comments: Delete
// Extracted to maintain Zero Inline JS guidelines under RULES.md §2.

/**
 * Sends an AJAX request to delete a comment and removes it from the DOM.
 * @param {string} commentId - The GUID string of the comment to delete.
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
                const result = await SohbaApp.post('/Comments/Delete', { id: commentId });

                if (result.success) {
                    SohbaApp.toast('Comment deleted successfully!', 'success');
                    
                    // Remove from the DOM smoothly
                    const commentElement = document.getElementById(`comment-${commentId}`) || 
                                           document.querySelector(`[data-comment-id="${commentId}"]`);
                    
                    if (commentElement) {
                        // Traverse up to find the closest wrapping container if the id was on text instead of the wrapper
                        const wrapperElement = commentElement.closest('.flex.items-start') || commentElement;
                        wrapperElement.style.transition = 'opacity 0.3s ease';
                        wrapperElement.style.opacity = 0;
                        setTimeout(() => wrapperElement.remove(), 300);
                    }

                    // Dynamically decrement comment count if postId is provided or found
                    if (postId) {
                        const countEl = document.getElementById(`comment-count-${postId}`);
                        if (countEl) {
                            let currentCount = parseInt(countEl.innerText) || 0;
                            if (currentCount > 0) {
                                countEl.innerText = currentCount - 1;
                            }
                        }
                    }
                } else {
                    SohbaApp.toast(result.error || 'Failed to delete comment.', 'error');
                }
            } catch (err) {
                console.error("Comment deletion failed dynamically:", err);
                SohbaApp.toast('An unexpected error occurred deleting the comment.', 'error');
            }
        }
    });
}
