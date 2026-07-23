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
// Returns a standardised { success, error } object — never throws, never returns HTML.
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

        // Guard: if the server returned HTML (e.g. Developer Exception page on 500,
        // or the auth login-redirect resolved to a 200 HTML page), response.json()
        // would throw a SyntaxError. Check Content-Type first.
        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            const statusLabel = response.status === 401 || response.status === 302
                ? 'Session expired. Please refresh and log in again.'
                : `Server error (HTTP ${response.status}). Please try again.`;
            console.error(`[SohbaApp.post] Non-JSON response from ${url}:`, response.status, contentType);
            return { success: false, Success: false, error: statusLabel, Error: statusLabel };
        }

        const json = await response.json();

        // Normalise casing so callers can use result.success or result.Success interchangeably.
        if (json.Success !== undefined && json.success === undefined) json.success = json.Success;
        if (json.Error   !== undefined && json.error   === undefined) json.error   = json.Error;

        return json;
    } catch (error) {
        // Network-level error (offline, CORS, DNS failure, etc.)
        console.error('[SohbaApp.post] Network error:', error);
        return { success: false, Success: false, error: 'Network error. Check your connection and try again.', Error: 'Network error.' };
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



// Global alias for toggleMenu (for views that call it without SohbaApp.)
window.toggleMenu = function (menuId) {
    window.SohbaApp.toggleMenu(menuId);
};