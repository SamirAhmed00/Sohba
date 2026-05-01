// wwwroot/js/features/posts.js
// Handles client-side actions for Posts: Edit, Delete
// Extracted to maintain Zero Inline JS guidelines under RULES.md §2.

async function deletePost(postId) {
    if (!postId) return;

    window.showConfirmModal({
        title: 'Delete Post',
        message: 'Are you sure you want to delete this post? This action cannot be undone.',
        type: 'delete',
        confirmText: 'Delete',
        onConfirm: async () => {
            try {
                const result = await SohbaApp.post('/Posts/Delete', { id: postId });

                if (result.success) {
                    SohbaApp.toast('Post deleted successfully!', 'success');
                    
                    const postElement = document.getElementById(`post-${postId}`);
                    if (postElement) {
                        postElement.style.transition = 'opacity 0.3s ease';
                        postElement.style.opacity = 0;
                        setTimeout(() => postElement.remove(), 300);
                    }
                } else {
                    SohbaApp.toast(result.error || 'Failed to delete post.', 'error');
                }
            } catch (err) {
                console.error("Post deletion failed dynamically:", err);
                SohbaApp.toast('An unexpected error occurred deleting the post.', 'error');
            }
        }
    });
}

async function editPost(formElement) {
    try {
        const formData = new FormData(formElement);
        
        const result = await SohbaApp.postForm('/Posts/Edit', formData);

        if (result.success) {
            SohbaApp.toast('Post updated successfully!', 'success');
            // Check for modal presence and close it
            if (typeof bootstrap !== 'undefined') {
                const modalElement = formElement.closest('.modal');
                if (modalElement) {
                    const modal = bootstrap.Modal.getInstance(modalElement);
                    if (modal) modal.hide();
                }
            }
            
            // In a real SPA, we'd update specific DOM components. 
            // We use simple reload as fallback, but if we updated the cards, we'd swap text.
            location.reload(); 
        } else {
            SohbaApp.toast(result.error || 'Failed to update post.', 'error');
        }
    } catch (err) {
        console.error("Post edit failed dynamically:", err);
        SohbaApp.toast('An unexpected error occurred updating the post.', 'error');
    }
}
