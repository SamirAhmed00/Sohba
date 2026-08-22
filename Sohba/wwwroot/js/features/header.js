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
        'StoryLike': '⭐'
    };
    return icons[type] || '📢';
}

function getNotificationUrl(notif) {
    const type = notif.notificationType;
    const targetId = notif.targetId || '';

    if (type === 'PostLike' || type === 'PostComment') return `/Posts/Details/${targetId}`;
    if (type === 'GroupInvitation') return `/Groups/Details/${targetId}`;
    if (type === 'FriendRequest') return '/Friends/Requests';
    if (type === 'SystemAlert' && targetId) return `/Groups/Details/${targetId}`;
    return '/Notifications/Index'; 
}

async function updateNotificationCount() {
    try {
        const response = await fetch('/Notifications/GetUnreadCount');
        const data = await response.json();

        let badge = document.querySelector('.notif-badge');
        const notifBtn = document.getElementById('notifBtn');

        if (data.count > 0) {
            if (!badge) {
                const newBadge = document.createElement('span');
                newBadge.className = 'notif-badge absolute -top-1 -right-1 bg-red-500 text-white text-xs w-5 h-5 rounded-full flex items-center justify-center font-bold';
                newBadge.textContent = data.count > 99 ? '99+' : data.count;
                notifBtn?.appendChild(newBadge);
            } else {
                badge.textContent = data.count > 99 ? '99+' : data.count;
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

        if (result.success && result.data && result.data.length > 0) {
            if (badge) badge.textContent = result.data.length;

            list.innerHTML = result.data.map(notif => `
                <a href="${getNotificationUrl(notif)}"
                   class="flex items-start gap-3 px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-50 ${notif.isRead ? 'opacity-60' : 'bg-blue-50/30'}">
                    <div class="w-10 h-10 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0">
                        <span class="text-[#345e69]">${getNotificationIcon(notif.notificationType)}</span>
                    </div>
                    <div class="flex-1 min-w-0">
                        <p class="text-sm text-gray-800">${notif.message}</p>
                        <p class="text-xs text-gray-400 mt-0.5">${notif.timeAgo}</p>
                    </div>
                    ${!notif.isRead ? `
                        <button onclick="event.preventDefault(); event.stopPropagation(); markNotificationAsRead('${notif.id}')"
                                class="text-xs text-[#345e69] hover:underline self-start mt-1">
                            Mark read
                        </button>
                    ` : ''}
                </a>
            `).join('');
        } else {
            list.innerHTML = '<div class="text-center py-8 text-gray-500 text-sm">No new notifications</div>';
            if (badge) badge.textContent = '0';
        }
    } catch (error) {
        console.error('Error loading notifications:', error);
        list.innerHTML = '<div class="text-center py-8 text-red-500 text-sm">Failed to load notifications</div>';
    }
}

async function markNotificationAsRead(notificationId) {
    if (!notificationId) return;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const response = await fetch(`/Notifications/MarkAsRead?id=${notificationId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            }
        });
        const result = await response.json();

        if (result.success) {
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
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const response = await fetch('/Notifications/MarkAllAsRead', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            }
        });
        const result = await response.json();

        if (result.success) {
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
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const response = await fetch(`/Notifications/Delete?id=${notificationId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    }
                });
                const result = await response.json();

                if (result.success) {
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

    const list = document.getElementById('notifList');
    const dropdown = document.getElementById('notifDropdown');
    if (list && dropdown && !dropdown.classList.contains('hidden')) {
        const notifHtml = `
            <div class="flex items-start gap-3 px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-50 bg-blue-50/50" data-notification-id="${notification.id}">
                <div class="w-10 h-10 rounded-full bg-[#345e69]/10 flex items-center justify-center flex-shrink-0">
                    <span class="text-[#345e69]">${getNotificationIcon(notification.notificationType)}</span>
                </div>
                <div class="flex-1 min-w-0">
                    <p class="text-sm text-gray-800">${notification.message}</p>
                    <p class="text-xs text-gray-400 mt-0.5">Just now</p>
                </div>
                <button onclick="markNotificationAsRead('${notification.id}')"
                        class="text-xs text-[#345e69] hover:underline self-start mt-1">
                    Mark read
                </button>
            </div>
        `;
        list.insertAdjacentHTML('afterbegin', notifHtml);
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
            notifDropdown.classList.remove('hidden');
            loadNotifications();
            if (!notificationConnection) {
                initializeSignalR();
            }
        } else {
            notifDropdown.classList.add('hidden');
        }
    });

    document.addEventListener('click', function (e) {
        if (!notifDropdown.contains(e.target) && !notifBtn.contains(e.target)) {
            notifDropdown.classList.add('hidden');
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