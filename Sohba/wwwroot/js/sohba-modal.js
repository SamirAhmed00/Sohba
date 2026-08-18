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

    // Post context icon (Group / Page) + Privacy indicator
    const sourceEl = document.getElementById('modalSourceContext');
    const privacyEl = document.getElementById('modalPrivacyIndicator');
    
    if (data.post.sourceType === 'Group' || data.post.sourceType === 'Page') {
            const icon = data.post.sourceType === 'Group' ? '👪' : '📄';
            sourceEl.innerHTML = `<span>•</span><span>${icon} ${data.post.sourceName || data.post.sourceType}</span>`;
            sourceEl.classList.remove('hidden'); sourceEl.classList.add('flex');
    } else {
        sourceEl.classList.add('hidden'); sourceEl.classList.remove('flex'); sourceEl.innerHTML = '';
    }
    
    const privacyLabels = { 0: '🌐 Public', 1: '👥 Friends Only', 2: '🔒 Only Me' };
    privacyEl.innerHTML = `<span>•</span><span>${privacyLabels[data.post.privacy] ?? privacyLabels[0]}</span>`;
    privacyEl.classList.remove('hidden'); privacyEl.classList.add('flex');
    
    // Multiple images: thumbnail strip; falls back to the single legacy image.
    const images = (data.post.imageUrls && data.post.imageUrls.length > 0)
                ? data.post.imageUrls
                : (data.post.imageUrl ? [data.post.imageUrl] : []);
    const thumbStrip = document.getElementById('modalImageThumbnails');

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

        if (images.length > 0) {
            document.getElementById('modalPostImage').src = images[0];
            if (images.length > 1) {
                    thumbStrip.innerHTML = images.map((url, idx) => `
                        <button type="button" class="w-12 h-12 rounded-lg overflow-hidden border-2 ${idx === 0 ? 'border-white' : 'border-transparent'} flex-shrink-0"
                                    onclick="document.getElementById('modalPostImage').src='${url}'; document.querySelectorAll('#modalImageThumbnails button').forEach(b=>b.classList.remove('border-white')); this.classList.add('border-white');">
                                <img src="${url}" class="w-full h-full object-cover">
                            </button>`).join('');
                    thumbStrip.classList.remove('hidden'); thumbStrip.classList.add('flex');
            } else {
                    thumbStrip.classList.add('hidden'); thumbStrip.innerHTML = '';
            }
        } else {
            if (leftSide) leftSide.style.display = 'none';
            if (rightSide) {
                rightSide.classList.remove('w-96');
                rightSide.classList.add('w-full');
            }
            modalContainer.style.justifyContent = 'center';
            thumbStrip.classList.add('hidden');
        }

        const avatarUrl = `https://ui-avatars.com/api/?name=${encodeURIComponent(data.post.authorName)}&background=345e69&color=fff`;
        document.getElementById('modalAuthorAvatar').src = avatarUrl;
        document.getElementById('modalAuthorName').innerText = data.post.authorName;
        document.getElementById('modalPostDate').innerText = new Date(data.post.createdAt).toLocaleString();
        document.getElementById('modalPostContent').innerText = data.post.content;

        // ============================================================
        // BUILD COMMENTS WITH NESTED REPLIES (max depth 4)
        // ============================================================
        if (data.comments && data.comments.length > 0) {
            function renderComment(c, depth) {
                const commentId = `comment-${c.id}`;
                const fullContent = c.content;
                const maxLength = 100;
                const shouldTruncate = fullContent.length > maxLength;
                const shortContent = shouldTruncate ? fullContent.substring(0, maxLength) + '...' : fullContent;
                const canReply = depth < 4;
                const indent = Math.min(depth - 1, 3); // max 3 levels of indent

                const replies = (c.replies || [])
                    .map(r => renderComment(r, depth + 1))
                    .join('');

                return `
                    <div class="flex items-start gap-3" data-comment-id="${c.id}">
                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=random" 
                             class="w-${depth === 1 ? 8 : 7} h-${depth === 1 ? 8 : 7} rounded-full flex-shrink-0">
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

                                ${canReply ? `
                                    <button onclick="SohbaApp.showReplyForm('${c.id}', '${c.userName}')" 
                                            class="text-xs text-[#345e69] hover:underline font-medium">
                                        Reply
                                    </button>
                                ` : ''}

                                ${c.replyCount > 0 ? `
                                    <button onclick="SohbaApp.toggleReplies('${c.id}')" 
                                            class="text-xs text-gray-500 hover:text-[#345e69]">
                                        View ${c.replyCount} replies
                                    </button>
                                ` : ''}

                                ${c.isAuthor ? `
                                    <button onclick="SohbaApp.deleteComment('${c.id}', '${c.postId}')"
                                            class="text-xs text-red-500 hover:underline font-medium ml-2">
                                        Delete
                                    </button>
                                ` : ''}
                            </div>

                            ${canReply ? `
                                <div id="replyForm-${c.id}" class="mt-2 hidden">
                                    <div class="flex items-start gap-3">
                                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=345e69&color=fff" 
                                             class="w-7 h-7 rounded-full flex-shrink-0">
                                        <div class="flex-1">
                                            <input type="text" 
                                                   id="replyInput-${c.id}" 
                                                   placeholder="Reply to ${c.userName}..."
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
                            ` : ''}

                            ${replies ? `
                                <div id="replies-${c.id}" class="mt-3 ml-${indent + 2} border-l-2 border-slate-200 space-y-3 pl-3">
                                    ${replies}
                                </div>
                            ` : ''}
                        </div>
                    </div>
                `;
            }

            const commentsHtml = data.comments
                .map(c => renderComment(c, c.depth || 1))
                .join('');

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


window.SohbaApp.closePostModal = function () {
    const modal = document.getElementById('postModal');
    if (modal) modal.classList.add('hidden');
    document.body.style.overflow = '';
};

document.addEventListener('DOMContentLoaded', function () {
    const overlay = document.querySelector('#postModal > .absolute.inset-0.bg-black\\/60');
    if (overlay) {
        overlay.addEventListener('click', () => window.SohbaApp.closePostModal());
    }
});

window.SohbaApp.reportPost = function (postId) {
    const modal = document.getElementById('reportModal');
    if (!modal) return;
    modal.dataset.postId = postId;
    modal.classList.remove('hidden');
};

window.SohbaApp.closeReportModal = function () {
    const modal = document.getElementById('reportModal');
    if (modal) modal.classList.add('hidden');
};

window.SohbaApp.submitReport = async function () {
    const modal = document.getElementById('reportModal');
    const postId = modal.dataset.postId;
    const selectedReason = document.querySelector('input[name="reportReason"]:checked');
    if (!selectedReason) {
        window.SohbaApp.toast('Please select a reason', 'error');
        return;
    }
    const reason = selectedReason.value;
    const otherText = document.getElementById('otherReasonText')?.value || null;

    const result = await window.SohbaApp.post('/Posts/ReportPost', { postId, reason, otherText });
    if (result.success) {
        window.SohbaApp.toast('Post reported. Thank you.', 'success');
        window.SohbaApp.closeReportModal();
        const btn = document.querySelector(`[data-report-button="${postId}"]`);
        if (btn) btn.setAttribute('disabled', 'true');
    } else {
        window.SohbaApp.toast(result.error || 'Failed to report post', 'error');
    }
};

window.SohbaApp.sharePost = function (postId) {
    const modal = document.getElementById('shareModal');
    if (!modal) return;
    const urlInput = document.getElementById('sharePostUrl');
    if (urlInput) urlInput.value = `${window.location.origin}/Posts/Details/${postId}`;
    modal.classList.remove('hidden');
};

window.SohbaApp.closeShareModal = function () {
    const modal = document.getElementById('shareModal');
    if (modal) modal.classList.add('hidden');
};

window.SohbaApp.copyShareLink = function () {
    const urlInput = document.getElementById('sharePostUrl');
    if (!urlInput) return;
    urlInput.select();
    navigator.clipboard.writeText(urlInput.value);
    window.SohbaApp.toast('Link copied!', 'success');
};