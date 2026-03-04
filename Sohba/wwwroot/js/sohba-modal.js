// sohba-modal.js - Modal handling functions

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

        if (data.comments && data.comments.length > 0) {
            const commentsHtml = data.comments.map(c => {
                const commentId = `comment-${c.id}`;
                const fullContent = c.content;
                const maxLength = 100;
                const shouldTruncate = fullContent.length > maxLength;
                const shortContent = shouldTruncate ? fullContent.substring(0, maxLength) + '...' : fullContent;

                return `
                    <div class="flex items-start gap-3 mb-3">
                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(c.userName)}&background=random" class="w-8 h-8 rounded-full flex-shrink-0">
                        <div class="flex-1 min-w-0">
                            <span class="font-semibold text-sm text-gray-900">${c.userName}</span>
                            <div id="${commentId}" class="text-sm text-gray-700 break-words">
                                ${shortContent}
                            </div>
                            ${shouldTruncate ? `
                                <button class="text-blue-600 hover:underline text-xs mt-1 toggle-comment-btn"
                                        onclick="SohbaApp.toggleComment('${commentId}', '${fullContent.replace(/'/g, "\\'")}', '${shortContent.replace(/'/g, "\\'")}')">
                                    See more
                                </button>
                            ` : ''}
                            <span class="text-xs text-gray-400 block mt-1">${new Date(c.createdAt).toLocaleString()}</span>
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

window.SohbaApp.closePostModal = function () {
    document.getElementById('postModal')?.classList.add('hidden');
    document.body.style.overflow = '';
};

// ------------ Report Modal -------------
window.SohbaApp.openReportModal = function (postId) {
    const modal = document.getElementById('reportModal');
    modal.dataset.postId = postId;

    document.querySelectorAll('input[name="reportReason"]').forEach(radio => {
        radio.checked = false;
    });

    document.getElementById('otherReasonContainer').classList.add('hidden');
    document.getElementById('otherReasonText').value = '';

    document.getElementById('reportModal').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
};

window.SohbaApp.closeReportModal = function () {
    const modal = document.getElementById('reportModal');
    modal.classList.add('hidden');
    document.body.style.overflow = '';
    delete modal.dataset.postId;
};

window.SohbaApp.submitReport = async function () {
    const selectedRadio = document.querySelector('input[name="reportReason"]:checked');
    if (!selectedRadio) {
        window.SohbaApp.toast('Please select a reason for reporting', 'error');
        return;
    }

    let reason = selectedRadio.value;
    let additionalInfo = '';

    if (reason === 'Other') {
        additionalInfo = document.getElementById('otherReasonText').value.trim();
        if (!additionalInfo) {
            window.SohbaApp.toast('Please describe the issue', 'error');
            return;
        }
    }

    try {
        const modal = document.getElementById('reportModal');
        const postId = modal.dataset.postId;

        const result = await window.SohbaApp.post('/Posts/ReportPost', {
            postId: postId,
            reason: reason,
            additionalInfo: additionalInfo
        });

        if (result.success) {
            window.SohbaApp.closeReportModal();

            const reportButton = document.querySelector(`[data-report-button="${postId}"]`);
            if (reportButton) {
                reportButton.disabled = true;
                reportButton.classList.add('opacity-50', 'cursor-not-allowed');
                reportButton.innerHTML = `
                    <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <span>Reported</span>
                `;
            }

            window.SohbaApp.toast('Post reported successfully. Thank you!', 'success');
        } else {
            window.SohbaApp.toast(result.error || 'Failed to report post', 'error');
        }
    } catch (e) {
        console.error(e);
        window.SohbaApp.toast('Network error', 'error');
    }
};

window.SohbaApp.reportPost = function (postId) {
    window.SohbaApp.openReportModal(postId);
};

// ------------ Share Modal -------------
window.SohbaApp.openShareModal = function (postId) {
    const modal = document.getElementById('shareModal');
    if (!modal) return;

    modal.dataset.postId = postId;
    const baseUrl = window.location.origin;
    const postUrl = `${baseUrl}/Posts/Details/${postId}`;
    document.getElementById('sharePostUrl').value = postUrl;

    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';
};

window.SohbaApp.closeShareModal = function () {
    const modal = document.getElementById('shareModal');
    if (!modal) return;
    modal.classList.add('hidden');
    document.body.style.overflow = '';
    delete modal.dataset.postId;
};

window.SohbaApp.copyShareLink = function () {
    const urlInput = document.getElementById('sharePostUrl');
    urlInput.select();
    urlInput.setSelectionRange(0, 99999);
    navigator.clipboard.writeText(urlInput.value).then(() => {
        window.SohbaApp.toast('Link copied to clipboard!', 'success');
    }).catch(() => {
        window.SohbaApp.toast('Failed to copy', 'error');
    });
};

window.SohbaApp.sharePost = function (postId) {
    window.SohbaApp.openShareModal(postId);
};