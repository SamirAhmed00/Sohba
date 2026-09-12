// ============================================================
// NOTIFICATION FUNCTIONS
// ============================================================

function getNotificationIcon(type) {
    const icons = {
        'PostLike': '❤️',
        'PostComment': '💬',
        'FriendRequest': '👥',
        'GroupInvitation': '👪',
        'SystemAlert': '🔔',
        'StoryLike': '⭐',
        'PageFollow': '📄'
    };
    return icons[type] || '📢';
}

function getNotificationUrl(notif) {
    const type = notif.notificationType;
    const targetId = notif.targetId || '';

    if ((type === 'PostLike' || type === 'PostComment') && targetId) return `/Posts/Details/${encodeURIComponent(targetId)}`;
    if (type === 'GroupInvitation' && targetId) return `/Groups/Details/${encodeURIComponent(targetId)}`;
    if (type === 'FriendRequest') return '/Friends/Requests';
    if (type === 'PageFollow' && targetId) return `/Pages/Details/${encodeURIComponent(targetId)}`;
    if (type === 'PageFollowRequest' && targetId) return `/Pages/PageRequests?pageId=${encodeURIComponent(targetId)}`;
    if (type === 'PageRequestAccepted' && targetId) return `/Pages/Details/${encodeURIComponent(targetId)}`;
    if (type === 'PageRequestRejected' && targetId) return `/Pages/Details/${encodeURIComponent(targetId)}`;
    if (type === 'StoryLike') return '/Stories';
    return '/Notifications/Index';
}

async function updateNotificationCount() {
    try {
        const response = await fetch('/Notifications/GetUnreadCount');
        const data = await response.json();

        let badge = document.querySelector('.notif-badge');
        const notifBtn = document.getElementById('notifBtn');
        const dropdownBadge = document.getElementById('notifCountBadge');
        const displayCount = data.count > 99 ? '99+' : data.count;

        if (dropdownBadge) {
            dropdownBadge.textContent = data.count > 0 ? displayCount : '0';
        }

        if (data.count > 0) {
            if (!badge) {
                const newBadge = document.createElement('span');
                newBadge.className = 'notif-badge absolute -top-1 -right-1 bg-red-500 text-white text-xs w-5 h-5 rounded-full flex items-center justify-center font-bold';
                newBadge.textContent = displayCount;
                notifBtn?.appendChild(newBadge);
            } else {
                badge.textContent = displayCount;
                badge.classList.remove('hidden');
            }
        } else {
            if (badge) badge.classList.add('hidden');
        }
    } catch (error) {
        console.error('Error updating notification count:', error);
    }
}


async function loadNotifications() {
    const list = document.getElementById('notifList');
    const badge = document.getElementById('notifCountBadge');

    if (!list) return;

    try {
        const response = await fetch('/Notifications/GetUnreadNotifications');
        const result = await response.json();

        list.innerHTML = '';

        if (result.success && result.data && result.data.length > 0) {
            if (badge) badge.textContent = result.data.length;

            result.data.forEach(notif => {
                const targetUrl = notif.targetUrl || getNotificationUrl(notif);
                const itemWrapper = document.createElement('div');
                itemWrapper.className = `flex items-start justify-between gap-3 px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-50 ${notif.isRead ? 'opacity-60' : 'bg-blue-50/30'}`;
                itemWrapper.setAttribute('data-notification-id', notif.id || '');

                const link = document.createElement('a');
                link.href = targetUrl;
                link.className = 'flex items-start gap-3 flex-1 min-w-0';
                link.onclick = function (e) {
                    handleNotificationNavigation(e, notif.id, targetUrl, notif.isRead);
                };

                const iconDiv = document.createElement('div');
                iconDiv.className = 'w-10 h-10 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0';
                const iconSpan = document.createElement('span');
                iconSpan.className = 'text-[#345e69]';
                iconSpan.textContent = getNotificationIcon(notif.notificationType);
                iconDiv.appendChild(iconSpan);

                const textDiv = document.createElement('div');
                textDiv.className = 'flex-1 min-w-0';
                const msgP = document.createElement('p');
                msgP.className = 'text-sm text-gray-800';
                msgP.textContent = notif.message || '';
                const timeP = document.createElement('p');
                timeP.className = 'text-xs text-gray-400 mt-0.5';
                timeP.textContent = notif.timeAgo || '';
                textDiv.appendChild(msgP);
                textDiv.appendChild(timeP);

                link.appendChild(iconDiv);
                link.appendChild(textDiv);
                itemWrapper.appendChild(link);

                if (!notif.isRead) {
                    const markBtn = document.createElement('button');
                    markBtn.type = 'button';
                    markBtn.className = 'text-xs text-[#345e69] hover:underline self-start mt-1 shrink-0';
                    markBtn.textContent = 'Mark read';
                    markBtn.setAttribute('aria-label', 'Mark notification as read');
                    markBtn.onclick = function (e) {
                        e.stopPropagation();
                        markNotificationAsRead(notif.id);
                    };
                    itemWrapper.appendChild(markBtn);
                }

                list.appendChild(itemWrapper);
            });
        } else {
            const emptyDiv = document.createElement('div');
            emptyDiv.className = 'text-center py-8 text-gray-500 text-sm';
            emptyDiv.textContent = 'No new notifications';
            list.appendChild(emptyDiv);
            if (badge) badge.textContent = '0';
        }
    } catch (error) {
        console.error('Error loading notifications:', error);
        list.innerHTML = '';
        const errorDiv = document.createElement('div');
        errorDiv.className = 'text-center py-8 text-red-500 text-sm';
        errorDiv.textContent = 'Failed to load notifications';
        list.appendChild(errorDiv);
    }
}


async function markNotificationAsRead(notificationId) {
    if (!notificationId) return;

    try {
        const token = window.SohbaApp?.getAntiForgeryToken();
        if (!token) {
            console.error('Antiforgery token missing; aborting markNotificationAsRead.');
            return;
        }

        const headers = {
            'X-CSRF-TOKEN': token,
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json'
        };

        const response = await fetch(`/Notifications/MarkAsRead?id=${encodeURIComponent(notificationId)}`, {
            method: 'POST',
            headers: headers
        });

        if (!response.ok) {
            console.error(`MarkAsRead failed with HTTP status ${response.status}`);
            return;
        }

        const result = await response.json();

        if (result.success || result.Success) {
            await loadNotifications();
            await updateNotificationCount();
            if (typeof SohbaApp !== 'undefined' && SohbaApp.toast) {
                SohbaApp.toast('Notification marked as read', 'success');
            }
        }
    } catch (error) {
        console.error('Error marking notification as read:', error);
    }
}

async function markAllNotificationsAsRead() {
    try {
        const token = window.SohbaApp?.getAntiForgeryToken();
        if (!token) {
            console.error('Antiforgery token missing; aborting markAllNotificationsAsRead.');
            return;
        }

        const headers = {
            'X-CSRF-TOKEN': token,
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json'
        };

        const response = await fetch('/Notifications/MarkAllAsRead', {
            method: 'POST',
            headers: headers
        });

        if (!response.ok) {
            console.error(`MarkAllAsRead failed with HTTP status ${response.status}`);
            return;
        }

        const result = await response.json();

        if (result.success || result.Success) {
            await loadNotifications();
            await updateNotificationCount();
            if (typeof SohbaApp !== 'undefined' && SohbaApp.toast) {
                SohbaApp.toast('All notifications marked as read', 'success');
            }
        }
    } catch (error) {
        console.error('Error marking all as read:', error);
    }
}


async function deleteNotification(notificationId) {
    if (!notificationId) return;
    if (typeof window.showConfirmModal !== 'function') return;

    window.showConfirmModal({
        title: 'Delete notification',
        message: 'Delete this notification?',
        type: 'delete',
        confirmText: 'Delete',
        onConfirm: async function () {
            try {
                const token = window.SohbaApp?.getAntiForgeryToken();
                if (!token) {
                    console.error('Antiforgery token missing; aborting deleteNotification.');
                    return;
                }

                const headers = {
                    'X-CSRF-TOKEN': token,
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json'
                };

                const response = await fetch(`/Notifications/Delete?id=${encodeURIComponent(notificationId)}`, {
                    method: 'POST',
                    headers: headers
                });

                if (!response.ok) {
                    console.error(`Delete failed with HTTP status ${response.status}`);
                    if (typeof SohbaApp !== 'undefined' && SohbaApp.toast) {
                        SohbaApp.toast('Failed to delete notification', 'error');
                    }
                    return;
                }

                const result = await response.json();

                if (result.success || result.Success) {
                    const item = document.querySelector(`[data-notification-id="${notificationId}"]`);
                    if (item) {
                        item.style.transition = 'opacity 0.3s ease';
                        item.style.opacity = '0';
                        setTimeout(() => item.remove(), 300);
                    }
                    await updateNotificationCount();
                    if (typeof SohbaApp !== 'undefined' && SohbaApp.toast) {
                        SohbaApp.toast('Notification deleted', 'success');
                    }
                }
            } catch (error) {
                console.error('Error deleting notification:', error);
                if (typeof SohbaApp !== 'undefined' && SohbaApp.toast) {
                    SohbaApp.toast('Failed to delete notification', 'error');
                }
            }
        }
    });
}

async function handleNotificationNavigation(event, notificationId, targetUrl, isRead) {
    if (event) {
        event.preventDefault();
    }

    if (!isRead && notificationId) {
        try {
            const token = window.SohbaApp?.getAntiForgeryToken();
            if (token) {
                const headers = {
                    'X-CSRF-TOKEN': token,
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json'
                };
                await fetch(`/Notifications/MarkAsRead?id=${encodeURIComponent(notificationId)}`, {
                    method: 'POST',
                    headers: headers
                });
            }
        } catch (err) {
            console.error('Error marking notification as read on navigation:', err);
        }
    }

    if (targetUrl) {
        window.location.href = targetUrl;
    }
}
//  {
//     if (!notificationId) return;
//     if (typeof window.showConfirmModal !== 'function') return;

//     window.showConfirmModal({
//         title: 'Delete notification',
//         message: 'Delete this notification?',
//         type: 'delete',
//         confirmText: 'Delete',
//         onConfirm: async function () {

//             try {
//                 const token = window.SohbaApp?.getAntiForgeryToken() || '';
//                 const response = await fetch(`/Notifications/Delete?id=${encodeURIComponent(notificationId)}`, {
//                     method: 'POST',
//                     headers: {
//                         'Content-Type': 'application/json',
//                         'X-CSRF-TOKEN': token
//                     }
//                 });
//                 const result = await response.json();

//                 if (result.success) {
//                     const item = document.querySelector(`[data-notification-id="${notificationId}"]`);
//                     if (item) {
//                         item.style.transition = 'opacity 0.3s ease';
//                         item.style.opacity = '0';
//                         setTimeout(() => item.remove(), 300);
//                     }
//                     await updateNotificationCount();
//                     if (typeof SohbaApp !== 'undefined' && SohbaApp.toast) {
//                         SohbaApp.toast('Notification deleted', 'success');
//                     }
//                 }
//             } catch (error) {
//                 console.error('Error deleting notification:', error);
//                 if (typeof SohbaApp !== 'undefined' && SohbaApp.toast) {
//                     SohbaApp.toast('Failed to delete notification', 'error');
//                 }
//             }
//         }
//     });
// }

// ============================================================
// SIGNALR NOTIFICATION CONNECTION
// ============================================================

let notificationConnection = null;
let isSignalRConnected = false;

function initializeSignalR() {
    if (isSignalRConnected) return;

    if (!document.getElementById('notifBtn')) {
        console.log('⚠️ Notification elements not found, retrying...');
        setTimeout(initializeSignalR, 500);
        return;
    }

    try {
        const tokenMeta = document.querySelector('meta[name="jwt-token"]');
        const token = tokenMeta?.getAttribute('content');

        if (!token) {
            console.warn('⚠️ No JWT token found, SignalR will not connect');
            return;
        }

        notificationConnection = new signalR.HubConnectionBuilder()
            .withUrl('/notificationHub', {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        notificationConnection.on('ReceiveNotification', function (notification) {
            console.log('📨 New notification received:', notification);
            handleNotificationReceived(notification);
        });

        notificationConnection.start()
            .then(() => {
                isSignalRConnected = true;
                console.log('✅ SignalR connected for notifications');
            })
            .catch(function (err) {
                console.error('❌ SignalR connection failed:', err);
                isSignalRConnected = false;
                setTimeout(initializeSignalR, 5000);
            });

        notificationConnection.onclose(function () {
            console.log('⚠️ SignalR connection closed');
            isSignalRConnected = false;
            setTimeout(initializeSignalR, 5000);
        });

    } catch (error) {
        console.error('❌ SignalR initialization error:', error);
        setTimeout(initializeSignalR, 5000);
    }
}

function handleNotificationReceived(notification) {
    const badge = document.getElementById('notifCountBadge');
    if (badge) {
        const currentCount = parseInt(badge.textContent) || 0;
        badge.textContent = currentCount + 1;
    }

    if (window.SohbaApp && SohbaApp.toast) {
        const icon = getNotificationIcon(notification.notificationType);
        SohbaApp.toast(`${icon} ${notification.message}`, 'success');
    }
    // Bridge SignalR notification to active page components in real time
    document.dispatchEvent(new CustomEvent('sohba:notificationReceived', { detail: notification }));

    const list = document.getElementById('notifList');
    const dropdown = document.getElementById('notifDropdown');
    if (list && dropdown && !dropdown.classList.contains('hidden')) {
        const targetUrl = notification.targetUrl || getNotificationUrl(notification);
        const itemWrapper = document.createElement('div');
        itemWrapper.className = 'flex items-start justify-between gap-3 px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-50 bg-blue-50/50';
        itemWrapper.setAttribute('data-notification-id', notification.id || '');

        const link = document.createElement('a');
        link.href = targetUrl;
        link.className = 'flex items-start gap-3 flex-1 min-w-0';
        link.onclick = function (e) {
            handleNotificationNavigation(e, notification.id, targetUrl, false);
        };

        const iconContainer = document.createElement('div');
        iconContainer.className = 'w-10 h-10 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0';
        const iconSpan = document.createElement('span');
        iconSpan.className = 'text-[#345e69]';
        iconSpan.textContent = getNotificationIcon(notification.notificationType);
        iconContainer.appendChild(iconSpan);

        const textContainer = document.createElement('div');
        textContainer.className = 'flex-1 min-w-0';
        const msgP = document.createElement('p');
        msgP.className = 'text-sm text-gray-800';
        msgP.textContent = notification.message || '';
        const timeP = document.createElement('p');
        timeP.className = 'text-xs text-gray-400 mt-0.5';
        timeP.textContent = 'Just now';
        textContainer.appendChild(msgP);
        textContainer.appendChild(timeP);

        link.appendChild(iconContainer);
        link.appendChild(textContainer);
        itemWrapper.appendChild(link);

        const markBtn = document.createElement('button');
        markBtn.type = 'button';
        markBtn.className = 'text-xs text-[#345e69] hover:underline self-start mt-1 shrink-0';
        markBtn.textContent = 'Mark read';
        markBtn.setAttribute('aria-label', 'Mark notification as read');
        markBtn.onclick = function (e) {
            e.stopPropagation();
            markNotificationAsRead(notification.id);
        };
        itemWrapper.appendChild(markBtn);

        list.prepend(itemWrapper);
    }

    updateNotificationCount();
}



// ============================================================
// EXPOSE FUNCTIONS TO GLOBAL SCOPE
// ============================================================

window.markNotificationAsRead = markNotificationAsRead;
window.markAllNotificationsAsRead = markAllNotificationsAsRead;
window.deleteNotification = deleteNotification;
window.updateNotificationCount = updateNotificationCount;
window.loadNotifications = loadNotifications;
window.initializeSignalR = initializeSignalR;
window.handleNotificationNavigation = handleNotificationNavigation;

// ============================================================
// INIT NOTIFICATION SYSTEM
// ============================================================

function initNotificationSystem() {
    const notifBtn = document.getElementById('notifBtn');
    const notifDropdown = document.getElementById('notifDropdown');

    if (!notifBtn || !notifDropdown) {
        console.warn('⚠️ Notification elements not found, retrying...');
        setTimeout(initNotificationSystem, 500);
        return;
    }

    console.log('✅ Notification system initialized');

    updateNotificationCount();
    setInterval(updateNotificationCount, 30000);

    initializeSignalR();

    notifBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        e.preventDefault();

        const isHidden = notifDropdown.classList.contains('hidden');

        if (isHidden) {
            const profileDropdown = document.getElementById('profileDropdown');
            if (profileDropdown) profileDropdown.classList.add('hidden');

            notifDropdown.classList.remove('hidden');
            notifBtn.setAttribute('aria-expanded', 'true');
            loadNotifications();
            if (!notificationConnection) {
                initializeSignalR();
            }
        } else {
            notifDropdown.classList.add('hidden');
            notifBtn.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('click', function (e) {
        if (!notifDropdown.contains(e.target) && !notifBtn.contains(e.target)) {
            notifDropdown.classList.add('hidden');
            notifBtn.setAttribute('aria-expanded', 'false');
        }
    });

    // Keyboard Escape Key Dismissal
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !notifDropdown.classList.contains('hidden')) {
            notifDropdown.classList.add('hidden');
            notifBtn.setAttribute('aria-expanded', 'false');
            notifBtn.focus();
        }
    });

    notifDropdown.addEventListener('click', function (e) {
        e.stopPropagation();
    });

    notifDropdown.addEventListener('click', function (e) {
        e.stopPropagation();
        const markReadBtn = e.target.closest('button');
        if (markReadBtn) {
            e.preventDefault(); // Do not follow the parent <a> when clicking "Mark read".
        }
    });
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initNotificationSystem);
} else {
    initNotificationSystem();
}

// ============================================================
// MOBILE SEARCH & PROFILE DROPDOWN
// ============================================================

document.addEventListener('DOMContentLoaded', function () {

    // Profile Dropdown
    const profileBtn = document.getElementById('profileBtn');
    const profileDropdown = document.getElementById('profileDropdown');
    if (profileBtn && profileDropdown) {
        profileBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            profileDropdown.classList.toggle('hidden');
            const notifDropdown = document.getElementById('notifDropdown');
            if (notifDropdown) notifDropdown.classList.add('hidden');
        });
        document.addEventListener('click', function () {
            if (profileDropdown) profileDropdown.classList.add('hidden');
        });
    }

    // Mobile Menu
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            if (typeof toggleSidebar === 'function') toggleSidebar();
        });
    }

});


// ============================================================
// NOTIFICATIONS FULL PAGE LISTENER (Index.cshtml)
// ============================================================
document.addEventListener('DOMContentLoaded', function () {
    document.addEventListener('sohba:notificationReceived', function (e) {
        const notif = e.detail;
        if (!notif) return;

        const container = document.getElementById('notificationsContainer');
        if (!container) return;

        const emptyState = document.getElementById('notificationsEmptyState');
        if (emptyState) {
            emptyState.classList.add('hidden');
        }

        const targetUrl = notif.targetUrl || (typeof getNotificationUrl === 'function' ? getNotificationUrl(notif) : '/Notifications/Index');
        const icon = typeof getNotificationIcon === 'function' ? getNotificationIcon(notif.notificationType) : '📢';

        const cardWrapper = document.createElement('div');
        cardWrapper.className = 'flex items-center gap-2 bg-white rounded-2xl shadow-sm border border-slate-100 hover:shadow-md transition-shadow border-l-4 border-l-[#345e69]';
        cardWrapper.setAttribute('data-notification-id', notif.id || '');

        const cardLink = document.createElement('a');
        cardLink.href = targetUrl;
        cardLink.className = 'flex-1 flex items-start gap-4 p-4 min-w-0';
        cardLink.onclick = function (event) {
            handleNotificationNavigation(event, notif.id, targetUrl, false);
        };

        const iconDiv = document.createElement('div');
        iconDiv.className = 'w-12 h-12 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0';
        const iconSpan = document.createElement('span');
        iconSpan.className = 'text-xl';
        iconSpan.textContent = icon;
        iconDiv.appendChild(iconSpan);

        const textDiv = document.createElement('div');
        textDiv.className = 'flex-1 min-w-0';
        const msgP = document.createElement('p');
        msgP.className = 'text-gray-800 text-sm';
        msgP.textContent = notif.message || '';
        const timeP = document.createElement('p');
        timeP.className = 'text-xs text-gray-400 mt-1';
        timeP.textContent = 'Just now';
        textDiv.appendChild(msgP);
        textDiv.appendChild(timeP);

        const unreadDot = document.createElement('span');
        unreadDot.className = 'w-2.5 h-2.5 rounded-full bg-[#345e69] flex-shrink-0 mt-2';
        unreadDot.setAttribute('aria-hidden', 'true');

        cardLink.appendChild(iconDiv);
        cardLink.appendChild(textDiv);
        cardLink.appendChild(unreadDot);

        const deleteBtn = document.createElement('button');
        deleteBtn.type = 'button';
        deleteBtn.className = 'p-3 mr-2 text-gray-400 hover:text-red-600 rounded-xl hover:bg-red-50 transition-colors';
        deleteBtn.title = 'Delete notification';
        deleteBtn.setAttribute('aria-label', 'Delete notification');
        deleteBtn.innerHTML = '<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>';
        deleteBtn.onclick = function () {
            deleteNotification(notif.id);
        };

        cardWrapper.appendChild(cardLink);
        cardWrapper.appendChild(deleteBtn);

        container.prepend(cardWrapper);
    });
});