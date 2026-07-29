// sohba-core.js - Shared core utility functions
window.SohbaApp = window.SohbaApp || {};

// Toast Notification
function createToastContainer() {
    const div = document.createElement('div');
    div.id = 'toast-container';
    div.setAttribute('aria-live', 'polite');
    div.setAttribute('aria-atomic', 'true');
    div.className = 'fixed bottom-4 right-4 z-[9999] flex flex-col gap-2';
    document.body.appendChild(div);
    return div;
}

window.SohbaApp.toast = function (message, type = 'info') {
    const container = document.getElementById('toast-container') || createToastContainer();
    const toast = document.createElement('div');
    toast.setAttribute('role', 'status');
    toast.setAttribute('aria-live', 'polite');
    toast.setAttribute('aria-atomic', 'true');
    toast.className = `fixed bottom-5 right-5 px-4 py-2 rounded-lg text-white shadow-lg z-[10000] transition-opacity duration-300 ${type === 'success' ? 'bg-green-500' : type === 'error' ? 'bg-red-500' : 'bg-blue-500'}`;
    toast.textContent = message;
    container.appendChild(toast);
    // document.body.appendChild(toast);
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

window.SohbaApp.postForm = async function (url, formData) {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
                
            },
            body: formData
        });

        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            const statusLabel = response.status === 401 || response.status === 302
                ? 'Session expired. Please refresh and log in again.'
                : `Server error (HTTP ${response.status}). Please try again.`;
            return { success: false, error: statusLabel };
        }

        const json = await response.json();
        if (json.Success !== undefined && json.success === undefined) json.success = json.Success;
        if (json.Error !== undefined && json.error === undefined) json.error = json.Error;
        return json;
    } catch (error) {
        console.error('[SohbaApp.postForm] Network error:', error);
        return { success: false, error: 'Network error. Check your connection and try again.' };
    }
};


// Toggle Menu
window.SohbaApp.toggleMenu = function (menuId) {
    const menu = document.getElementById(menuId);
    if (!menu) return;
    const wasOpen = !menu.classList.contains('hidden');
    document.querySelectorAll('[id^="menu-"]').forEach(m => m.classList.add('hidden'));
    if (!wasOpen) {
        menu.classList.remove('hidden');
    }
};

document.addEventListener('click', function (e) {
    if (!e.target.closest('[onclick*="toggleMenu"]') && !e.target.closest('[id^="menu-"]')) {
            document.querySelectorAll('[id^="menu-"]').forEach(m => m.classList.add('hidden'));
    }
});

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

window.SohbaApp.setButtonLoading = function (button, loadingText = 'Loading...') {
    button.dataset.originalText = button.innerHTML;
    button.disabled = true;
    button.innerHTML = `<span class="inline-flex items-center gap-2">
        <svg class="animate-spin h-4 w-4 text-current" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        ${loadingText}
    </span>`;
};

window.SohbaApp.resetButton = function (button) {
    button.disabled = false;
    button.innerHTML = button.dataset.originalText || button.innerHTML;
};

document.addEventListener('DOMContentLoaded', function () {

    document.querySelectorAll('form[method="post"]').forEach(form => {
        if (form.dataset.skipAutoLoading) return;

        form.addEventListener('submit', function () {
            if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) {
                return;
            }
            const btn = form.querySelector('button[type="submit"]');
            if (btn) {
                window.SohbaApp.setButtonLoading(btn, 'Please wait...');
            }

        });
    });
});