// ChatApp JavaScript utilities

// Dismiss error UI
window.addEventListener('DOMContentLoaded', () => {
    const errorUI = document.getElementById('blazor-error-ui');
    if (errorUI) {
        const dismissButton = errorUI.querySelector('.dismiss-link');
        if (dismissButton) {
            dismissButton.addEventListener('click', () => {
                errorUI.style.display = 'none';
            });
        }
    }
});

// Smooth scroll behavior
document.documentElement.style.scrollBehavior = 'smooth';

// Close dropdowns when clicking outside
document.addEventListener('click', (e) => {
    // Close user menu dropdown if click is outside
    const userMenuWrapper = e.target.closest('.user-menu-wrapper');
    if (!userMenuWrapper) {
        const dropdown = document.querySelector('.user-menu-dropdown');
        if (dropdown) {
            // Trigger a custom event that Blazor can listen to
            window.dispatchEvent(new CustomEvent('closeUserMenu'));
        }
    }
});

// Utilities
window.chatAppUtils = {
    focusElement: (element) => {
        if (element) {
            element.focus();
        }
    },

    scrollToTop: () => {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    copyToClipboard: async (text) => {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (err) {
            console.error('Failed to copy:', err);
            return false;
        }
    },

    // Subscribe to outside click events
    subscribeToOutsideClick: (dotNetHelper) => {
        const handler = () => dotNetHelper.invokeMethodAsync('CloseUserMenuFromJS');
        window.addEventListener('closeUserMenu', handler);
        return {
            dispose: () => window.removeEventListener('closeUserMenu', handler)
        };
    },

    // Scroll element to bottom (used for chat messages)
    scrollToBottom: (element) => {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },

    // Scroll element into view
    scrollIntoView: (element) => {
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'end' });
        }
    }
};