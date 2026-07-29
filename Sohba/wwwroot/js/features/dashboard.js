window.resolveReport = async function (reportId, isResolved) {
    const actionText = isResolved ? 'resolve' : 'dismiss';
    window.showConfirmModal({
        title: isResolved ? 'Resolve Report' : 'Dismiss Report',
        message: `Are you sure you want to ${actionText} this report?`,
        type: isResolved ? 'warning' : 'info',
        confirmText: isResolved ? 'Resolve' : 'Dismiss',
        onConfirm: async () => {
            try {
                const result = await SohbaApp.post('/Dashboard/ResolveReport', { reportId: reportId, isResolved: isResolved });
                if (result.success) {
                    SohbaApp.toast(`Report ${actionText}d successfully.`, 'success');
                    const statusEl = document.getElementById(`report-status-${reportId}`);
                    if (statusEl) {
                        statusEl.textContent = isResolved ? 'Resolved' : 'Dismissed';
                        statusEl.className = isResolved ? 'text-green-600' : 'text-gray-500';
                    }
                    // Remove action buttons to prevent double-action
                    const btnContainer = event.target.closest('.flex.gap-3');
                    if (btnContainer) btnContainer.remove();
                } else {
                    SohbaApp.toast(result.error || `Failed to ${actionText} report.`, 'error');
                }
            } catch (err) {
                SohbaApp.toast('An unexpected error occurred.', 'error');
            }
        }
    });
};