window.resolveReport = async function (reportId, isResolved = true) {
    const actionText = isResolved ? 'resolve' : 'dismiss';
    const endpoint = isResolved ? '/Dashboard/ResolveReport' : '/Dashboard/DismissReport';

    window.showConfirmModal({
        title: isResolved ? 'Resolve Report' : 'Dismiss Report',
        message: `Are you sure you want to ${actionText} this report?`,
        type: isResolved ? 'info' : 'warning',
        confirmText: isResolved ? 'Resolve' : 'Dismiss',
        onConfirm: async () => {
            try {
                const result = await SohbaApp.post(endpoint, { reportId: reportId });
                if (result.success) {
                    SohbaApp.toast(`Report ${actionText}d successfully.`, 'success');
                    const statusEl = document.getElementById(`report-status-${reportId}`);
                    if (statusEl) {
                        statusEl.textContent = isResolved ? 'Resolved' : 'Dismissed';
                        statusEl.className = isResolved ? 'text-green-600' : 'text-gray-500';
                    }
                    const reportRow = document.querySelector(`tr[data-report-id="${reportId}"]`);
                    if (reportRow) {
                        const actionsCell = reportRow.querySelector('td:last-child');
                        if (actionsCell) actionsCell.innerHTML = '<span class="text-xs text-gray-400">No actions</span>';
                    }
                } else {
                    SohbaApp.toast(result.error || `Failed to ${actionText} report.`, 'error');
                }
            } catch (err) {
                SohbaApp.toast('An unexpected error occurred.', 'error');
            }
        }
    });
};

window.dismissReport = function (reportId) {
    window.resolveReport(reportId, false);
};

window.updateTableRowState = function (selector, updates) {
    const row = document.querySelector(selector);
    if (!row) return;

    if (updates.remove) {
        row.style.transition = 'opacity 0.3s ease';
        row.style.opacity = '0';
        setTimeout(() => row.remove(), 300);
        return;
    }

    if (updates.badge) {
        const badge = row.querySelector('.status-badge');
        if (badge) {
            badge.textContent = updates.badge.text;
            badge.className = updates.badge.className;
        }
    }

    if (updates.buttonHtml && updates.buttonContainerSelector) {
        const container = row.querySelector(updates.buttonContainerSelector);
        if (container) container.innerHTML = updates.buttonHtml;
    }
};
