// ------------ Post Modal -------------
window.SohbaApp.openPostModal = async function (postId, focusTab = null) {
    const modal = document.getElementById('postModal');
    if (!modal) return;

    modal.classList.remove('hidden');
    modal.dataset.postId = postId;
    document.body.style.overflow = 'hidden';

    document.getElementById('modalPostImage').src = '';
    document.getElementById('modalAuthorName').innerText = '';
    document.getElementById('modalPostDate').innerText = '';
    document.getElementById('modalPostContent').innerText = '';
    document.getElementById('modalComments').innerHTML = '<p class="text-slate-400 text-sm italic">Loading comments...</p>';
    document.getElementById('modalAuthorAvatar').src = '';

    try {
        const response = await fetch(`/Posts/GetPostDetails?postId=${postId}`);
        if (!response.ok) throw new Error('Failed to load');
        const data = await response.json();

        const modalContainer = document.querySelector('#postModal .flex-col.md\\:flex-row');
        const leftSide = document.getElementById('modalLeft');
        const rightSide = document.getElementById('modalRight');

        if (leftSide) leftSide.style.display = '';
        if (rightSide) {
            rightSide.classList.remove('w-full');
            rightSide.classList.add('w-96');
        }
        modalContainer.style.justifyContent = 'flex-start';

        if (data.post.imageUrl) {
            document.getElementById('modalPostImage').src = data.post.imageUrl;
        } else {
            if (leftSide) leftSide.style.display = 'none';
            if (rightSide) {
                rightSide.classList.remove('w-96');
                rightSide.classList.add('w-full');
            }
            modalContainer.style.justifyContent = 'center';
        }

        const avatarUrl = `https://ui-avatars.com/api/?name=${encodeURIComponent(data.post.authorName)}&background=345e69&color=fff`;
        document.getElementById('modalAuthorAvatar').src = avatarUrl;
        document.getElementById('modalAuthorName').innerText = data.post.authorName;
        document.getElementById('modalPostDate').innerText = new Date(data.post.createdAt).toLocaleString();
        document.getElementById('modalPostContent').innerText = data.post.content;

        // ============================================================
        // BUILD COMMENTS WITH REPLIES
        // ============================================================
        if (data.comments && data.comments.length > 0) {
            const commentsHtml = data.comments.map(c => {
                const commentId = `comment-${c.id}`;
                const fullContent = c.content;
                const maxLength = 100;
                const shouldTruncate = fullContent.length > maxLength;
                const shortContent = shouldTruncate ? fullContent.substring(0, maxLength) + '...' : fullContent;

                // Build replies HTML if any
                let repliesHtml = '';
                if (c.replies && c.replies.length > 0) {
                    repliesHtml = `
                        <div id="replies-${c.id}" class="mt-3 pl-4 border-l-2 border-slate-200 space-y-3">
                            ${c.replies.map(reply => `
                                <div class="flex items-start gap-3">
                                    <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(reply.userName)}&background=random" 
                                         class="w-7 h-7 rounded-full flex-shrink-0">
                                    <div>
                                        <span class="font-semibold text-sm text-gray-900">${reply.userName}</span>
                                        <p class="text-sm text-gray-700">${reply.content}</p>
                                        <span class="text-xs text-gray-400">${new Date(reply.createdAt).toLocaleString()}</span>
                                    </div>
                                </div>
                            `).join('')}
                        </div>
                    `;
                }

                return `
                    <div class="flex items-start gap-3 mb-3">
                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=random" 
                             class="w-8 h-8 rounded-full flex-shrink-0">
                        <div class="flex-1 min-w-0">
                            <span class="font-semibold text-sm text-gray-900">${c.userName}</span>
                            <div id="${commentId}" class="text-sm text-gray-700 break-words">
                                ${shouldTruncate ? shortContent : fullContent}
                            </div>
                            ${shouldTruncate ? `
                                <button class="text-blue-600 hover:underline text-xs mt-1 toggle-comment-btn"
                                        onclick="SohbaApp.toggleComment('${commentId}', '${fullContent.replace(/'/g, "\\'")}', '${shortContent.replace(/'/g, "\\'")}')">
                                    See more
                                </button>
                            ` : ''}
                            <div class="flex items-center gap-3 mt-1">
                                <span class="text-xs text-gray-400">${new Date(c.createdAt).toLocaleString()}</span>
                                
                                <!-- Reply button -->
                                <button onclick="SohbaApp.showReplyForm('${c.id}', '${c.userName}')" 
                                        class="text-xs text-[#345e69] hover:underline font-medium">
                                    Reply
                                </button>
                                
                                <!-- Show replies count -->
                                ${c.replyCount > 0 ? `
                                    <button onclick="SohbaApp.toggleReplies('${c.id}')" 
                                            class="text-xs text-gray-500 hover:text-[#345e69]">
                                        View ${c.replyCount} replies
                                    </button>
                                ` : ''}
                            </div>
                            
                            <!-- Reply form (hidden by default) -->
                            <div id="replyForm-${c.id}" class="mt-2 hidden">
                                <div class="flex items-start gap-3">
                                    <img src="https://ui-avatars.com/api/?name=You&background=345e69&color=fff" 
                                         class="w-7 h-7 rounded-full flex-shrink-0">
                                    <div class="flex-1">
                                        <input type="text" 
                                               id="replyInput-${c.id}" 
                                               placeholder="Write a reply..."
                                               class="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-[#345e69]/20">
                                        <div class="flex gap-2 mt-2">
                                            <button onclick="SohbaApp.submitReply('${c.id}', '${c.postId}')" 
                                                    class="px-4 py-1.5 bg-[#345e69] text-white text-sm font-semibold rounded-lg hover:bg-[#2a4b55]">
                                                Reply
                                            </button>
                                            <button onclick="SohbaApp.hideReplyForm('${c.id}')" 
                                                    class="px-4 py-1.5 text-sm text-gray-500 hover:text-gray-700">
                                                Cancel
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Replies container -->
                            ${repliesHtml}
                        </div>
                    </div>
                `;
            }).join('');
            document.getElementById('modalComments').innerHTML = commentsHtml;
        } else {
            document.getElementById('modalComments').innerHTML = '<p class="text-slate-400 text-sm italic">No comments yet.</p>';
        }

        if (focusTab === 'comments') {
            setTimeout(() => document.getElementById('commentInput')?.focus(), 300);
        }
    } catch (error) {
        console.error('Error loading post:', error);
        window.SohbaApp.toast('Failed to load post', 'error');
        window.SohbaApp.closePostModal();
    }
};