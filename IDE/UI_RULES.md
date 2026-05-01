# Sohba UI & Frontend Development Standards

This document establishes strict standards for all UI and frontend development within the Sohba project to maintain consistency, reliability, and excellent user experience dynamically.

## 1. AJAX State Management
- **Loading State:** Every button triggering an AJAX call MUST be disabled upon click and display a loading spinner or an opacity/loading state.
- **Resolution:** Re-enable the button or remove the loading spinner ONLY after the HTTP response has completely returned, regardless of success or failure.

## 2. Form Validation
- **Client-Side First:** Implement client-side validation logic immediately before attempting an AJAX payload dispatch.
- **Error Display:** Validation errors MUST be displayed as simple, red text placed directly underneath the invalid input field instead of relying solely on generic alerts.

## 3. User Feedback & Notifications
- **Global Toast Notification:** Use the custom **`window.SohbaApp.toast(message, type)`** function (found in `sohba-core.js`) to display simple, non-intrusive notifications across the application.
- **For Success:** Render a robust success message (e.g., `SohbaApp.toast('Post updated successfully!', 'success')`) unless explicitly required otherwise.
- **For Failure:** You MUST display the exact error message explicitly provided by the server payload inside `BaseResponseDto.message` or `BaseResponseDto.Error`. (e.g., `SohbaApp.toast(result.error, 'error')`).

## 4. Confirmation Flow for Sensitive Actions
- **Global Confirm Modal:** Any "Delete", "Remove", or otherwise sensitive destruction action MUST trigger the custom global Confirm Modal via **`window.showConfirmModal(options)`** (defined in `_ConfirmModal.cshtml`).
- **Implementation Required:** Never use native `confirm()`. The user MUST confirm their choice visually through the animated `_ConfirmModal` before proceeding with the designated AJAX dispatch. Example:
```javascript
window.showConfirmModal({
    title: 'Delete Post',
    message: 'Are you sure you want to delete this post? This action cannot be undone.',
    type: 'delete',
    confirmText: 'Delete',
    onConfirm: () => {
        // execute SohbaApp.post...
    }
});
```

## 5. UI Updates (No Reload Policy)
- **DOM Manipulation:** Strictly favor DOM Manipulation over hard page reloads (`location.reload()`).
- **Entity Removal/Updates:** Upon a successful deletion/update response, gracefully remove the targeted element from the DOM (e.g., set opacity to 0 and remove after 300ms) or dynamically update text counters visually without refreshing the browser state.

## 6. Javascript Architecture Consistency
- **Zero Inline JS:** Strict adherence to the rule established in `RULES.md` — There should be virtually zero inline JavaScript inside `.cshtml` files (with the absolute rare exception of library initialization logic if injected centrally).
- **Consolidation:** All primary feature mapping event registrations and logic implementations must be centralized into `/wwwroot/js/features/` separated strictly by their feature (e.g., `posts.js`, `comments.js`, `friends.js`).
