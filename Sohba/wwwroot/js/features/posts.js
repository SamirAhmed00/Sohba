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

// async function editPost(formElement) {
//     try {
//         const formData = new FormData(formElement);

//         const result = await SohbaApp.postForm('/Posts/Edit', formData);

//         if (result.success) {
//             SohbaApp.toast('Post updated successfully!', 'success');
//             Check for modal presence and close it
//             if (typeof bootstrap !== 'undefined') {
//                 const modalElement = formElement.closest('.modal');
//                 if (modalElement) {
//                     const modal = bootstrap.Modal.getInstance(modalElement);
//                     if (modal) modal.hide();
//                 }
//             }

//             In a real SPA, we'd update specific DOM components. 
//             We use simple reload as fallback, but if we updated the cards, we'd swap text.
//             location.reload(); 
//         } else {
//             SohbaApp.toast(result.error || 'Failed to update post.', 'error');
//         }
//     } catch (err) {
//         console.error("Post edit failed dynamically:", err);
//         SohbaApp.toast('An unexpected error occurred updating the post.', 'error');
//     }
// }

async function editPost(formElement) {
    const submitBtn = formElement.querySelector('button[type="submit"]');
    if (submitBtn) { submitBtn.disabled = true; submitBtn.innerHTML = 'Saving...'; }

    try {
        const formData = new FormData(formElement);
        const result = await SohbaApp.postForm('/Posts/Edit', formData);

        if (result.success) {
            SohbaApp.toast('Post updated successfully!', 'success');

            // DOM Manipulation Instead Of reload
            const postId = formData.get('Id');
            const postCard = document.querySelector(`[data-post-id="${postId}"]`);
            if (postCard && result.data) {
                // Update title
                const titleEl = postCard.querySelector('.post-title');
                if (titleEl && result.data.title) titleEl.textContent = result.data.title;

                // Update content
                const contentEl = postCard.querySelector('.post-content');
                if (contentEl && result.data.content) contentEl.textContent = result.data.content;

                // Update privacy badge
                // const privacyEl = postCard.querySelector('.post-privacy-badge');
                // if (privacyEl && result.data.privacy) {
                //     privacyEl.textContent = result.data.privacy;
                //     privacyEl.className = `post-privacy-badge px-2 py-1 rounded-full text-xs font-bold ${getPrivacyClass(result.data.privacy)}`;
                // }

                // Update image if changed
                if (result.data.imageUrl) {
                    const imgEl = postCard.querySelector('.post-image');
                    if (imgEl) imgEl.src = result.data.imageUrl;
                }
            }

            // Close modal if open
            const modal = formElement.closest('.modal');
            if (modal) {
                modal.classList.add('hidden');
                document.body.style.overflow = '';
            }
        } else {
            SohbaApp.toast(result.error || 'Failed to update post.', 'error');
        }
    } catch (err) {
        console.error("Post edit failed:", err);
        SohbaApp.toast('An unexpected error occurred updating the post.', 'error');
    } finally {
        if (submitBtn) { submitBtn.disabled = false; submitBtn.innerHTML = 'Save Changes'; }
    }
}

function getPrivacyClass(privacy) {
    switch (privacy) {
        case 'Public': return 'bg-green-100 text-green-700';
        case 'Friends': return 'bg-blue-100 text-blue-700';
        case 'Private': return 'bg-gray-100 text-gray-700';
        default: return 'bg-slate-100 text-slate-700';
    }
}