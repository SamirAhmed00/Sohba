// sohba-core.js - Shared core utility functions
window.SohbaApp = window.SohbaApp || {};

// Toast Notification
window.SohbaApp.toast = function (message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `fixed bottom-5 right-5 px-4 py-2 rounded-lg text-white shadow-lg z-[10000] transition-opacity duration-300 ${type === 'success' ? 'bg-green-500' : type === 'error' ? 'bg-red-500' : 'bg-blue-500'}`;
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
};

// HTTP POST Request
window.SohbaApp.post = async function (url, data) {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            throw new Error('Request failed');
        }

        return await response.json();
    } catch (error) {
        console.error('POST Error:', error);
        throw error;
    }
};

// Toggle Menu
window.SohbaApp.toggleMenu = function (menuId) {
    const menu = document.getElementById(menuId);
    if (!menu) return;
    document.querySelectorAll('[id^="menu-"]').forEach(m => m.classList.add('hidden'));
    menu.classList.toggle('hidden');
};

// Initialize
window.SohbaApp.init = function () {
    document.querySelectorAll('[data-like]').forEach(button => {
        button.addEventListener('click', function () {
            window.SohbaApp.animateLike(this);
        });
    });
};