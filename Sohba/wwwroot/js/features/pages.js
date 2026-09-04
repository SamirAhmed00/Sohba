// ============================================================
// SOHBA - PAGES FEATURE JAVASCRIPT
// Path: Sohba/wwwroot/js/features/pages.js
// ============================================================

(function () {
    // ------------------------------------------------------------
    // 1. TABS & URL HASH NAVIGATION
    // ------------------------------------------------------------
    window.Page_SwitchTab = function (tab) {
        const validTabs = ['posts', 'about', 'followers'];
        if (!validTabs.includes(tab)) {
            tab = 'posts';
        }

        document.querySelectorAll('.tab-btn').forEach(btn => {
            const isActive = btn.dataset.tab === tab;
            btn.classList.toggle('active', isActive);
            btn.classList.toggle('border-emerald-600', isActive);
            btn.classList.toggle('text-emerald-600', isActive);
            btn.classList.toggle('border-transparent', !isActive);
            btn.classList.toggle('text-gray-500', !isActive);
        });

        document.querySelectorAll('.tab-pane').forEach(pane => {
            pane.classList.add('hidden');
        });

        const selectedPane = document.getElementById(`${tab}-tab`);
        if (selectedPane) {
            selectedPane.classList.remove('hidden');
        }

        history.replaceState(null, '', `#${tab}`);

        if (tab === 'followers') {
            loadAllFollowers();
        } else if (tab === 'about') {
            loadPageStats();
        } else if (tab === 'posts') {
            loadPagePosts();
        }
    };

    // ------------------------------------------------------------
    // 2. POSTS LOADING & STATES
    // ------------------------------------------------------------
    window.loadPagePosts = async function () {
        const pageId = window.__pagesCurrentPageId;
        const container = document.getElementById('page-posts');
        if (!container || !pageId) return;

        // Skip loading if unauthorized on private page
        if (window.__pagesIsPrivate && !window.__pagesIsAuthorized) {
            return;
        }

        container.innerHTML = `
            <div class="text-center py-10 text-gray-500 flex items-center justify-center gap-2">
                <span>Loading posts...</span>
            </div>
        `;

        try {
            const response = await fetch(
                `/Pages/GetPagePosts?pageId=${encodeURIComponent(pageId)}`,
                { headers: { 'X-Requested-With': 'XMLHttpRequest' } }
            );

            if (response.status === 403) {
                container.innerHTML = `
                    <div class="bg-white rounded-2xl p-8 border border-slate-200 text-center">
                        <h3 class="text-base font-bold text-gray-900 mb-1">This Page is Private</h3>
                        <p class="text-sm text-gray-500">Only authorized followers can view posts from this page.</p>
                    </div>
                `;
                return;
            }

            if (!response.ok) {
                throw new Error(`Failed to load posts. HTTP ${response.status}`);
            }

            const html = (await response.text()).trim();
            const postsCountEl = document.getElementById('postsCount');

            if (!html) {
                container.innerHTML = `
                    <div class="bg-white rounded-2xl p-10 border border-slate-200 text-center">
                        <h3 class="text-base font-bold text-gray-900 mb-1">No posts available yet.</h3>
                        <p class="text-sm text-gray-500">This page has not published any posts yet.</p>
                    </div>
                `;
                if (postsCountEl) postsCountEl.textContent = '0';
                return;
            }

            container.innerHTML = html;
            const postElements = container.querySelectorAll('article');
            const count = postElements.length;

            if (postsCountEl) {
                postsCountEl.textContent = count.toString();
            }

            if (count === 0) {
                container.innerHTML = `
                    <div class="bg-white rounded-2xl p-10 border border-slate-200 text-center">
                        <h3 class="text-base font-bold text-gray-900 mb-1">No posts available yet.</h3>
                        <p class="text-sm text-gray-500">This page has not published any posts yet.</p>
                    </div>
                `;
                if (postsCountEl) postsCountEl.textContent = '0';
            }
        } catch (error) {
            console.error('Error loading page posts:', error);
            container.innerHTML = `
                <div class="bg-white rounded-2xl p-8 border border-slate-200 text-center">
                    <h3 class="text-base font-bold text-gray-900 mb-1">Unable to load posts</h3>
                    <p class="text-sm text-gray-500">An error occurred while loading this page's posts.</p>
                    <button type="button" onclick="loadPagePosts()" class="mt-4 px-4 py-2 bg-slate-100 hover:bg-slate-200 text-gray-700 rounded-xl text-xs font-semibold">
                        Try Again
                    </button>
                </div>
            `;
        }
    };

    // ------------------------------------------------------------
    // 3. CREATE POST MODAL TRIGGER
    // ------------------------------------------------------------
    window.openPageCreatePostModal = function (pageId, pageName) {
        if (window.SohbaApp && typeof window.SohbaApp.openCreatePostModal === 'function') {
            return window.SohbaApp.openCreatePostModal({
                pageId: pageId,
                sourceType: 'Page',
                sourceName: pageName
            });
        }
    };

    // ------------------------------------------------------------
    // 4. FOLLOWERS PREVIEW & DIRECTORY
    // ------------------------------------------------------------
    window.loadFollowersPreview = async function () {
        const pageId = window.__pagesCurrentPageId;
        const container = document.getElementById('followersPreview');
        if (!container || !pageId) return;

        if (window.__pagesIsPrivate && !window.__pagesIsAuthorized) {
            container.innerHTML = '<div class="text-xs text-center text-slate-500 py-4 col-span-5">Members are hidden on private pages.</div>';
            return;
        }

        try {
            const response = await fetch(`/Pages/GetFollowersPreview?pageId=${pageId}&count=10`);
            if (!response.ok) throw new Error('Failed to fetch preview');
            const followers = await response.json();

            if (followers && followers.length > 0) {
                container.innerHTML = followers.map(f => {
                    const safeName = String(f.userName || 'User')
                        .replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
                    const encodedName = encodeURIComponent(f.userName || 'User');
                    const safeAvatar = String(f.profilePictureUrl || '').replace(/"/g, '&quot;');
                    return `
                        <div class="cursor-pointer" title="${safeName}">
                            <img src="${safeAvatar || `https://ui-avatars.com/api/?name=${encodedName}&background=345e69&color=fff`}"
                                 class="w-10 h-10 rounded-xl object-cover border border-slate-200 shadow-sm" alt="${safeName}" />
                        </div>
                    `;
                }).join('');
            } else {
                container.innerHTML = '<div class="text-xs text-center text-slate-400 py-4 col-span-5">No followers yet.</div>';
            }
        } catch (error) {
            console.error('Error loading followers preview:', error);
            container.innerHTML = '<div class="text-xs text-center text-slate-400 py-4 col-span-5">Could not load preview.</div>';
        }
    };

    let currentFollowersPage = 1;
    let hasMoreFollowers = false;

    window.loadAllFollowers = async function () {
        currentFollowersPage = 1;
        const container = document.getElementById('followersList');
        if (container) {
            container.innerHTML = '<div class="text-center text-gray-500 py-8 col-span-full">Loading followers...</div>';
        }
        await fetchFollowersBatch(1, false);
    };

    window.loadMoreFollowers = async function () {
        if (!hasMoreFollowers) return;
        const btn = document.getElementById('loadMoreFollowersBtn');
        if (btn) btn.disabled = true;
        await fetchFollowersBatch(currentFollowersPage + 1, true);
        if (btn) btn.disabled = false;
    };

    async function fetchFollowersBatch(page, append = false) {
        const pageId = window.__pagesCurrentPageId;
        const container = document.getElementById('followersList');
        const loadMoreContainer = document.getElementById('followersLoadMoreContainer');
        if (!container || !pageId) return;

        if (window.__pagesIsPrivate && !window.__pagesIsAuthorized) {
            container.innerHTML = `
                <div class="text-center py-10 col-span-full">
                    <h3 class="text-base font-bold text-gray-900 mb-1">Followers List is Private</h3>
                    <p class="text-sm text-gray-500">Only authorized followers can view members of this page.</p>
                </div>
            `;
            if (loadMoreContainer) loadMoreContainer.classList.add('hidden');
            return;
        }

        try {
            const response = await fetch(`/Pages/GetAllFollowers?pageId=${pageId}&page=${page}&pageSize=20`);
            if (!response.ok) throw new Error('Failed to fetch followers');
            const data = await response.json();

            if (!data.success || !data.followers || data.followers.length === 0) {
                if (!append) {
                    container.innerHTML = `
                        <div class="text-center py-10 col-span-full">
                            <h3 class="text-base font-bold text-gray-900 mb-1">No followers yet</h3>
                            <p class="text-sm text-gray-500">Be the first to follow this page.</p>
                        </div>
                    `;
                }
                if (loadMoreContainer) loadMoreContainer.classList.add('hidden');
                return;
            }

            currentFollowersPage = data.page || page;
            hasMoreFollowers = !!data.hasMore;
            if (loadMoreContainer) {
                loadMoreContainer.classList.toggle('hidden', !hasMoreFollowers);
            }

            const roleValues = { Member: 1, CoAdmin: 2, Admin: 3, PageOwner: 4 };
            const roleNameLookup = Object.fromEntries(Object.entries(roleValues).map(([k, v]) => [v, k]));
            function getRoleValue(role) {
                if (typeof role === 'number') return role;
                return roleValues[role] ?? 1;
            }
            const actorRoleValue = getRoleValue(window.__pagesCurrentUserRole);

            const html = data.followers.map(f => {
                const targetRoleValue = getRoleValue(f.role);
                const isTargetOtherUser = String(f.userId) !== String(window.__pagesCurrentUserId);

                const canKick = isTargetOtherUser && actorRoleValue >= roleValues.Admin && targetRoleValue < actorRoleValue;
                const canPromoteToCoAdmin = isTargetOtherUser && actorRoleValue >= roleValues.Admin && targetRoleValue === roleValues.Member;
                const canPromoteToAdmin = window.__pagesCurrentUserRole === 'PageOwner' && isTargetOtherUser && (targetRoleValue === roleValues.CoAdmin || targetRoleValue === roleValues.Member);
                const canDemote = window.__pagesCurrentUserRole === 'PageOwner' && isTargetOtherUser && (targetRoleValue === roleValues.Admin || targetRoleValue === roleValues.CoAdmin);
                const canTransferOwnership = window.__pagesCurrentUserRole === 'PageOwner' && isTargetOtherUser && targetRoleValue === roleValues.Admin;

                const roleName = typeof f.role === 'string' ? f.role : (roleNameLookup[targetRoleValue] || 'Member');
                const safeUserName = String(f.userName || 'User').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
                const encodedUserName = encodeURIComponent(f.userName || 'User');
                const safeAvatar = String(f.profilePictureUrl || '').replace(/"/g, '&quot;');

                return `
                    <div class="text-center p-3 rounded-xl border border-slate-100 bg-white">
                        <a href="/Profile/Index/${encodeURIComponent(f.userId)}" class="block">
                            <img src="${safeAvatar || `https://ui-avatars.com/api/?name=${encodedUserName}&background=345e69&color=fff`}"
                                 class="w-full aspect-square rounded-xl object-cover border border-slate-200" alt="${safeUserName}" />
                            <p class="text-xs font-semibold text-gray-800 mt-2 truncate hover:text-emerald-600">${safeUserName}</p>
                            <p class="text-[11px] font-bold text-emerald-700 mt-0.5">${roleName}</p>
                            <p class="text-[10px] text-gray-400 mt-0.5">Joined ${new Date(f.followedAt).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })}</p>
                        </a>
                        ${isTargetOtherUser ? `
                            <div class="flex flex-col gap-1.5 mt-3">
                                ${canPromoteToCoAdmin ? `<button onclick="promotePageMember('${encodeURIComponent(f.userId)}', 'CoAdmin')" class="text-[11px] px-2 py-1 bg-slate-100 text-gray-700 rounded-lg hover:bg-slate-200 font-semibold">Promote to Co-Admin</button>` : ''}
                                ${canPromoteToAdmin ? `<button onclick="promotePageMember('${encodeURIComponent(f.userId)}', 'Admin')" class="text-[11px] px-2 py-1 bg-slate-100 text-gray-700 rounded-lg hover:bg-slate-200 font-semibold">Promote to Admin</button>` : ''}
                                ${canDemote ? `<button onclick="demotePageMember('${encodeURIComponent(f.userId)}', '${targetRoleValue === roleValues.Admin ? 'CoAdmin' : 'Member'}')" class="text-[11px] px-2 py-1 bg-amber-50 text-amber-800 rounded-lg hover:bg-amber-100 font-semibold">Demote</button>` : ''}
                                ${canTransferOwnership ? `<button onclick="transferPageOwnership('${encodeURIComponent(f.userId)}')" class="text-[11px] px-2 py-1 bg-indigo-50 text-indigo-700 rounded-lg hover:bg-indigo-100 font-semibold">Transfer Ownership</button>` : ''}
                                ${canKick ? `<button onclick="kickPageMember('${encodeURIComponent(f.userId)}')" class="text-[11px] px-2 py-1 bg-red-50 text-red-600 rounded-lg hover:bg-red-100 font-semibold">Remove</button>` : ''}
                            </div>
                        ` : ''}
                    </div>
                `;
            }).join('');

            if (append) {
                container.insertAdjacentHTML('beforeend', html);
            } else {
                container.innerHTML = html;
            }
        } catch (error) {
            console.error('Error loading followers:', error);
            if (!append) {
                container.innerHTML = `
                    <div class="text-center py-8 col-span-full text-gray-500 text-sm">
                        <p>Failed to load followers.</p>
                        <button type="button" onclick="loadAllFollowers()" class="mt-2 px-3 py-1 bg-slate-100 hover:bg-slate-200 text-gray-700 rounded-lg text-xs font-semibold">Try Again</button>
                    </div>
                `;
            }
        }
    }

    // ------------------------------------------------------------
    // 5. FOLLOW & UNFOLLOW TOGGLE
    // ------------------------------------------------------------
    window.toggleFollow = async function (pageId, buttonElement) {
        const targetPageId = pageId || window.__pagesCurrentPageId;
        if (!targetPageId) return;

        const isIndexCard = !!buttonElement;
        const btn = buttonElement || document.getElementById('followBtn');
        const originalText = btn ? (btn.innerText || btn.textContent) : '';

        if (btn) btn.disabled = true;

        try {
            const result = await SohbaApp.post('/Pages/ToggleFollow', { pageId: targetPageId });

            if (result.success) {
                const isFollowing = !!result.isFollowing;

                if (isIndexCard && btn) {
                    if (isFollowing) {
                        btn.innerText = "Following";
                        btn.className = "w-full mt-4 py-2 bg-slate-200 text-slate-700 font-semibold rounded-xl text-sm";
                        SohbaApp.toast('Page followed.', 'success');
                    } else {
                        btn.innerText = "Follow";
                        btn.className = "w-full mt-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold rounded-xl text-sm";
                        SohbaApp.toast('Unfollowed page.', 'info');
                    }
                } else {
                    window.__pagesIsFollowing = isFollowing;
                    updateFollowButton();
                    SohbaApp.toast(isFollowing ? 'Page followed.' : 'Unfollowed page.', 'success');

                    await loadFollowersPreview();
                    await loadPageStats();

                    const followersTab = document.getElementById('followers-tab');
                    if (followersTab && !followersTab.classList.contains('hidden')) {
                        await loadAllFollowers();
                    }
                }
            } else {
                if (isIndexCard && btn) {
                    btn.innerText = originalText;
                }
                SohbaApp.toast(result.error || 'Failed action.', 'error');
            }
        } catch (e) {
            if (isIndexCard && btn) {
                btn.innerText = originalText;
            }
            console.error('Error toggling follow:', e);
            SohbaApp.toast('Network error.', 'error');
        } finally {
            if (btn) btn.disabled = false;
        }
    };

    window.updateFollowButton = function () {
        const btn = document.getElementById('followBtn');
        const leaveBtn = document.getElementById('leavePageBtn');
        const isFollowing = !!window.__pagesIsFollowing;
        const isOwner = window.__pagesCurrentUserRole === 'PageOwner' || !!window.__pagesIsOwner;

        if (leaveBtn && !isOwner) {
            leaveBtn.classList.toggle('hidden', !isFollowing);
        }

        if (!btn) return;

        const text = btn.querySelector('span') || btn;

        if (isFollowing) {
            btn.className = "px-5 py-2.5 bg-slate-200 hover:bg-slate-300 text-gray-700 font-bold rounded-xl flex items-center gap-2 text-sm";
            text.textContent = 'Following';
        } else {
            btn.className = "px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl flex items-center gap-2 text-sm";
            text.textContent = 'Follow Page';
        }
    };

    // ------------------------------------------------------------
    // 6. PRIVATE PAGE FOLLOW REQUEST MODAL
    // ------------------------------------------------------------
    window.openFollowRequestModal = function () {
        const modal = document.getElementById('pageFollowRequestModal');
        const msg = document.getElementById('pageFollowRequestMessage');
        if (!modal) return;

        if (msg) msg.value = '';
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    };

    window.closeFollowRequestModal = function () {
        const modal = document.getElementById('pageFollowRequestModal');
        if (!modal) return;
        modal.classList.add('hidden');
        document.body.style.overflow = '';
    };

    window.submitFollowRequest = async function () {
        const pageId = window.__pagesCurrentPageId;
        const msgInput = document.getElementById('pageFollowRequestMessage');
        const submitBtn = document.getElementById('submitFollowRequestBtn');
        if (!pageId || !msgInput) return;

        const message = msgInput.value.trim();
        if (!message) {
            SohbaApp.toast('Please write a message explaining your request.', 'error');
            msgInput.focus();
            return;
        }

        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerText = 'Submitting...';
        }

        try {
            const result = await SohbaApp.post('/Pages/SubmitFollowRequest', { pageId, message });
            if (result.success) {
                SohbaApp.toast('Follow request submitted successfully.', 'success');
                closeFollowRequestModal();

                const reqBtn = document.getElementById('followRequestBtn');
                if (reqBtn) {
                    reqBtn.innerText = 'Request Pending';
                    reqBtn.disabled = true;
                    reqBtn.className = 'px-5 py-2.5 bg-slate-100 text-gray-500 font-bold rounded-xl text-sm cursor-not-allowed';
                }
            } else {
                SohbaApp.toast(result.error || 'Failed to submit request.', 'error');
            }
        } catch (err) {
            console.error('Error submitting follow request:', err);
            SohbaApp.toast('Network error while submitting request.', 'error');
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerText = 'Submit Request';
            }
        }
    };

    // ------------------------------------------------------------
    // 7. REQUEST REVIEW ACTIONS (Accept / Reject)
    // ------------------------------------------------------------
    window.acceptPageRequest = async function (requestId) {
        if (!requestId) return;
        const result = await SohbaApp.post('/Pages/AcceptFollowRequest', { requestId });
        if (result.success) {
            SohbaApp.toast('Request approved.', 'success');
            const row = document.getElementById(`request-row-${requestId}`);
            if (row) row.remove();
            checkEmptyRequests();
        } else {
            SohbaApp.toast(result.error || 'Failed to accept request.', 'error');
        }
    };

    window.rejectPageRequest = async function (requestId) {
        if (!requestId) return;
        const result = await SohbaApp.post('/Pages/RejectFollowRequest', { requestId });
        if (result.success) {
            SohbaApp.toast('Request declined.', 'info');
            const row = document.getElementById(`request-row-${requestId}`);
            if (row) row.remove();
            checkEmptyRequests();
        } else {
            SohbaApp.toast(result.error || 'Failed to decline request.', 'error');
        }
    };

    function checkEmptyRequests() {
        const tbody = document.getElementById('requestsTableBody');
        const emptyCard = document.getElementById('emptyRequestsCard');
        if (tbody && tbody.children.length === 0 && emptyCard) {
            tbody.closest('div').classList.add('hidden');
            emptyCard.classList.remove('hidden');
        }
    }

    // ------------------------------------------------------------
    // 8. FULLSCREEN IMAGE LIGHTBOX
    // ------------------------------------------------------------
    window.openPageImageFullscreen = function (imageUrl) {
        if (!imageUrl) return;
        if (window.SohbaApp && typeof window.SohbaApp.openImageLightbox === 'function') {
            window.SohbaApp.openImageLightbox(imageUrl);
        }
    };

    // ------------------------------------------------------------
    // 9. LEAVE & DELETE PAGE
    // ------------------------------------------------------------
    window.leavePage = async function () {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        const isOwner = window.__pagesCurrentUserRole === 'PageOwner' || !!window.__pagesIsOwner;
        const confirmMessage = isOwner
            ? 'As Page Owner, leaving will transfer ownership to the earliest Admin, or delete the page if no other members remain.'
            : 'Are you sure you want to leave this page?';

        if (typeof window.showConfirmModal === 'function') {
            window.showConfirmModal({
                title: 'Leave Page',
                message: confirmMessage,
                type: 'warning',
                confirmText: 'Leave',
                onConfirm: async function () {
                    await executeLeave(pageId);
                }
            });
        }
    };

    async function executeLeave(pageId) {
        const result = await SohbaApp.post('/Pages/Leave', { pageId: pageId });
        if (result.success) {
            if (result.outcome === 'deleted') {
                SohbaApp.toast('Page deleted (no members remaining).', 'success');
            } else if (result.outcome === 'ownership_transferred') {
                SohbaApp.toast('Ownership transferred to the earliest Admin.', 'success');
            } else {
                SohbaApp.toast('You left the page.', 'success');
            }
            setTimeout(() => window.location.href = '/Pages', 1000);
        } else {
            SohbaApp.toast(result.error || 'Failed to leave page.', 'error');
        }
    }

    window.openPageDeleteReasonModal = function () {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        if (typeof window.showConfirmModal === 'function') {
            window.showConfirmModal({
                title: 'Delete Page',
                message: 'This will permanently delete the page and remove all followers. This action cannot be undone.',
                type: 'delete',
                confirmText: 'Delete',
                showReasonInput: true,
                onConfirm: async function (reason) {
                    if (!reason || reason.trim().length === 0) {
                        SohbaApp.toast('A deletion reason is required.', 'error');
                        return;
                    }
                    const result = await SohbaApp.post('/Pages/Delete', { id: pageId, reason: reason.trim() });
                    if (result.success) {
                        SohbaApp.toast(result.message || 'Page deleted successfully.', 'success');
                        setTimeout(() => window.location.href = '/Pages', 1000);
                    } else {
                        SohbaApp.toast(result.error || 'Failed to delete page.', 'error');
                    }
                }
            });
        }
    };

    // ------------------------------------------------------------
    // 10. MEMBER MANAGEMENT (Kick, Promote, Demote, Transfer)
    // ------------------------------------------------------------
    window.kickPageMember = async function (targetUserId) {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        window.showConfirmModal({
            title: 'Remove Member',
            message: 'Remove this member from the page?',
            type: 'warning',
            confirmText: 'Remove',
            onConfirm: async function () {
                const result = await SohbaApp.post('/Pages/KickMember', { pageId: pageId, targetUserId: targetUserId });
                if (result.success) {
                    SohbaApp.toast('Member removed.', 'success');
                    loadAllFollowers();
                } else {
                    SohbaApp.toast(result.error || 'Failed to remove member.', 'error');
                }
            }
        });
    };

    window.promotePageMember = async function (targetUserId, newRole = 'CoAdmin') {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        const roleLabel = newRole === 'Admin' ? 'Admin' : 'Co-Admin';
        window.showConfirmModal({
            title: 'Promote Member',
            message: `Promote this member to ${roleLabel}?`,
            type: 'info',
            confirmText: 'Promote',
            onConfirm: async function () {
                const result = await SohbaApp.post('/Pages/PromoteMember', {
                    pageId: pageId,
                    targetUserId: targetUserId,
                    newRole: newRole
                });
                if (result.success) {
                    SohbaApp.toast('Member promoted.', 'success');
                    loadAllFollowers();
                } else {
                    SohbaApp.toast(result.error || 'Failed to promote member.', 'error');
                }
            }
        });
    };

    window.demotePageMember = async function (targetUserId, newRole = 'Member') {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        const roleLabel = newRole === 'CoAdmin' ? 'Co-Admin' : 'Member';
        window.showConfirmModal({
            title: 'Demote Member',
            message: `Demote this member to ${roleLabel}?`,
            type: 'warning',
            confirmText: 'Demote',
            onConfirm: async function () {
                const result = await SohbaApp.post('/Pages/DemoteMember', {
                    pageId: pageId,
                    targetUserId: targetUserId,
                    newRole: newRole
                });
                if (result.success) {
                    SohbaApp.toast('Member demoted.', 'success');
                    loadAllFollowers();
                } else {
                    SohbaApp.toast(result.error || 'Failed to demote member.', 'error');
                }
            }
        });
    };

    window.transferPageOwnership = async function (targetUserId) {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        window.showConfirmModal({
            title: 'Transfer Page Ownership',
            message: 'Transfer ownership of this page to the selected Admin? You will become a regular Admin.',
            type: 'warning',
            confirmText: 'Transfer',
            onConfirm: async function () {
                const result = await SohbaApp.post('/Pages/TransferOwnership', { pageId: pageId, targetUserId: targetUserId });
                if (result.success) {
                    SohbaApp.toast('Ownership transferred.', 'success');
                    setTimeout(() => location.reload(), 800);
                } else {
                    SohbaApp.toast(result.error || 'Failed to transfer ownership.', 'error');
                }
            }
        });
    };

    // ------------------------------------------------------------
    // 11. PAGE STATS & DETAILS INITIALIZATION
    // ------------------------------------------------------------
    window.loadPageStats = async function () {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        try {
            const response = await fetch(`/Pages/GetPageStats?pageId=${pageId}`);
            if (!response.ok) return;
            const data = await response.json();
            if (data && data.success) {
                const postsCountEl = document.getElementById('postsCount');
                const followersCountEl = document.getElementById('followersCount');
                if (postsCountEl) postsCountEl.textContent = data.postsCount;
                if (followersCountEl) followersCountEl.textContent = data.followersCount;
            }
        } catch (error) {
            console.error('Error loading page stats:', error);
        }
    };

    window.checkPageRole = async function () {
        const pageId = window.__pagesCurrentPageId;
        if (!pageId) return;

        try {
            const response = await fetch(`/Pages/CheckFollowStatus?pageId=${pageId}`);
            if (!response.ok) return;
            const data = await response.json();
            if (data && data.success) {
                window.__pagesIsFollowing = !!data.isFollowing;
                window.__pagesCurrentUserRole = data.role || 'None';
                updateFollowButton();
            }
        } catch (error) {
            console.error('Error checking page role:', error);
        }
    };

    window.initializePageDetails = async function () {
        const hash = window.location.hash.replace('#', '').toLowerCase();
        const validTabs = ['posts', 'about', 'followers'];
        const tabToShow = validTabs.includes(hash) ? hash : 'posts';

        Page_SwitchTab(tabToShow);

        if (!window.__pagesIsPrivate || window.__pagesIsAuthorized) {
            loadPagePosts();
            loadFollowersPreview();
        }
        loadPageStats();
        await checkPageRole();
    };

    window.addEventListener('hashchange', function () {
        const hash = window.location.hash.replace('#', '').toLowerCase();
        if (['posts', 'about', 'followers'].includes(hash)) {
            Page_SwitchTab(hash);
        }
    });

    // ------------------------------------------------------------
    // 12. IMAGE PREVIEW HELPERS (Create & Edit)
    // ------------------------------------------------------------
    window.setupLiveImagePreview = function (inputId, previewImgId, containerId, maxSizeMB = 5) {
        const input = document.getElementById(inputId);
        const img = document.getElementById(previewImgId);
        const container = document.getElementById(containerId);
        if (!input || !img || !container) return;

        input.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (!file) return;

            if (file.size > maxSizeMB * 1024 * 1024) {
                const msg = `Image must be ${maxSizeMB}MB or smaller.`;
                if (window.SohbaApp && SohbaApp.toast) {
                    SohbaApp.toast(msg, 'error');
                } else {
                    alert(msg);
                }
                e.target.value = '';
                return;
            }

            const reader = new FileReader();
            reader.onload = function (ev) {
                img.src = ev.target.result;
                container.classList.remove('hidden');
            };
            reader.readAsDataURL(file);
        });
    };
})();