// Sohba/wwwroot/js/features/modal.js
(function () {
    let confirmCallback = null;

    function initModal() {
        const overlay = document.getElementById('confirmModalOverlay');
        const cancelBtn = document.getElementById('confirmModalCancel');
        const actionBtn = document.getElementById('confirmModalAction');

        if (overlay) overlay.addEventListener('click', closeConfirmModal);
        if (cancelBtn) cancelBtn.addEventListener('click', closeConfirmModal);
        if (actionBtn) actionBtn.addEventListener('click', onConfirmClicked);
    }

    window.showConfirmModal = function (options) {
        const modal = document.getElementById('confirmModal');
        const modalContent = document.getElementById('confirmModalContent');
        const icon = document.getElementById('confirmModalIcon');
        const title = document.getElementById('confirmModalTitle');
        const message = document.getElementById('confirmModalMessage');
        const actionBtn = document.getElementById('confirmModalAction');
        const inputContainer = document.getElementById('confirmModalInputContainer');
        const reasonInput = document.getElementById('confirmModalReasonInput');

        title.textContent = options.title || 'Confirm Action';
        message.textContent = options.message || 'Are you sure?';
        actionBtn.textContent = options.confirmText || 'Confirm';

        if (inputContainer && reasonInput) {
            if (options.showReasonInput) {
                inputContainer.classList.remove('hidden');
                reasonInput.value = '';
            } else {
                inputContainer.classList.add('hidden');
                reasonInput.value = '';
            }
        }

        // Icon styling based on type
        if (options.type === 'delete') {
            icon.innerHTML = '<svg class="w-6 h-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>';
            actionBtn.className = 'flex-1 py-2.5 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-xl shadow-lg shadow-red-600/30 transition-colors';
        } else if (options.type === 'warning') {
            icon.innerHTML = '<svg class="w-6 h-6 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>';
            actionBtn.className = 'flex-1 py-2.5 bg-amber-600 hover:bg-amber-700 text-white font-semibold rounded-xl shadow-lg shadow-amber-600/30 transition-colors';
        } else {
            icon.innerHTML = '<svg class="w-6 h-6 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>';
            actionBtn.className = 'flex-1 py-2.5 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-xl shadow-lg shadow-blue-600/30 transition-colors';
        }

        confirmCallback = options.onConfirm;

        modal.classList.remove('hidden');
        requestAnimationFrame(() => {
            modalContent.classList.remove('scale-95', 'opacity-0');
            modalContent.classList.add('scale-100', 'opacity-100');
        });
        document.body.style.overflow = 'hidden';
    };

    function onConfirmClicked() {
        const reasonInput = document.getElementById('confirmModalReasonInput');
        const reason = reasonInput ? reasonInput.value.trim() : null;
        if (typeof confirmCallback === 'function') {
            confirmCallback(reason);
        }
        closeConfirmModal();
    }


    window.closeConfirmModal = function () {
        const modal = document.getElementById('confirmModal');
        const modalContent = document.getElementById('confirmModalContent');
        if (!modal || !modalContent) return;

        modalContent.classList.add('scale-95', 'opacity-0');
        modalContent.classList.remove('scale-100', 'opacity-100');
        setTimeout(() => {
            modal.classList.add('hidden');
            document.body.style.overflow = '';
        }, 200);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initModal);
    } else {
        initModal();
    }
})();