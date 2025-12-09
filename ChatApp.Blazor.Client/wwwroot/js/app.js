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
    },

    // Get element position for smart menu positioning
    getElementPosition: (element) => {
        if (!element) return null;
        const rect = element.getBoundingClientRect();
        return {
            top: rect.top,
            bottom: rect.bottom,
            left: rect.left,
            right: rect.right,
            viewportHeight: window.innerHeight,
            viewportWidth: window.innerWidth
        };
    },

    // Save scroll position before loading more messages
    saveScrollPosition: (element) => {
        if (!element) return null;
        return {
            scrollHeight: element.scrollHeight,
            scrollTop: element.scrollTop
        };
    },

    // Restore scroll position after loading more messages
    restoreScrollPosition: (element, previousState) => {
        if (!element || !previousState) return;
        const heightDifference = element.scrollHeight - previousState.scrollHeight;
        element.scrollTop = previousState.scrollTop + heightDifference;
    },

    // Scroll to a specific message and highlight it
    scrollToMessage: (messageElement) => {
        if (!messageElement) return;

        // Scroll the message into view
        messageElement.scrollIntoView({ behavior: 'smooth', block: 'center' });

        // Add highlight class
        messageElement.classList.add('highlighted');

        // Remove highlight after animation completes
        setTimeout(() => {
            messageElement.classList.remove('highlighted');
        }, 2000);
    },

    // Scroll to a message by ID and highlight it
    scrollToMessageById: (messageId) => {
        const messageElement = document.getElementById(messageId);
        if (messageElement) {
            // Scroll the message into view
            messageElement.scrollIntoView({ behavior: 'smooth', block: 'center' });

            // Add highlight class to the bubble inside
            const bubble = messageElement.querySelector('.message-bubble');
            if (bubble) {
                bubble.classList.add('highlighted');

                // Remove highlight after animation completes
                setTimeout(() => {
                    bubble.classList.remove('highlighted');
                }, 2000);
            }
        }
    },

    // Page Visibility API - Check if page is currently visible
    isPageVisible: () => {
        return !document.hidden;
    },

    // Subscribe to page visibility changes
    subscribeToVisibilityChange: (dotNetHelper) => {
        const handler = () => {
            const isVisible = !document.hidden;
            dotNetHelper.invokeMethodAsync('OnVisibilityChanged', isVisible);
        };
        document.addEventListener('visibilitychange', handler);
        return {
            dispose: () => document.removeEventListener('visibilitychange', handler)
        };
    }
};