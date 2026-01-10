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

    // Scroll container to bottom by ID (used for Jump to Latest)
    scrollToBottomById: (elementId) => {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },

    // Scroll to element by ID (used for unread separator)
    scrollToElement: (elementId) => {
        const element = document.getElementById(elementId);
        if (element) {
            // 'center' - separator mərkəzdə göstərilir
            // 'instant' - scroll animation disable (pozulma qarşısını alır)
            element.scrollIntoView({ behavior: 'instant', block: 'center' });
        }
    },

    // Auto-resize textarea based on content
    autoResizeTextarea: (element) => {
        if (!element) return;
        element.style.height = 'auto';
        const scrollHeight = element.scrollHeight;
        const maxHeight = 200;
        const newHeight = Math.min(scrollHeight, maxHeight);
        element.style.height = newHeight + 'px';

        // Add scroll class only when content exceeds max height
        if (scrollHeight > maxHeight) {
            element.classList.add('has-scroll');
        } else {
            element.classList.remove('has-scroll');
        }
    },

    // Reset textarea height to default
    resetTextareaHeight: (element) => {
        if (!element) return;
        element.style.height = '24px';
        element.classList.remove('has-scroll');
    },

    // Get element position for smart menu positioning
    getElementPosition: (element) => {
        if (!element) return null;
        const rect = element.getBoundingClientRect();

        // Get ChatHeader and MessageInput positions
        const chatHeader = document.querySelector('.chat-header');
        const messageInput = document.querySelector('.message-input-container');

        const chatHeaderRect = chatHeader ? chatHeader.getBoundingClientRect() : null;
        const messageInputRect = messageInput ? messageInput.getBoundingClientRect() : null;

        const chatHeaderHeight = chatHeaderRect ? chatHeaderRect.height : 60;
        const messageInputHeight = messageInputRect ? messageInputRect.height : 80;

        // Calculate actual space below (from element bottom to MessageInput top)
        const messageInputTop = messageInputRect ? messageInputRect.top : (window.innerHeight - 80);
        const actualSpaceBelow = messageInputTop - rect.bottom;

        // Calculate actual space above (from ChatHeader bottom to element top)
        const chatHeaderBottom = chatHeaderRect ? chatHeaderRect.bottom : 60;
        const actualSpaceAbove = rect.top - chatHeaderBottom;

        return {
            top: rect.top,
            bottom: rect.bottom,
            left: rect.left,
            right: rect.right,
            viewportHeight: window.innerHeight,
            viewportWidth: window.innerWidth,
            chatHeaderHeight: chatHeaderHeight,
            messageInputHeight: messageInputHeight,
            messageInputTop: messageInputTop,
            chatHeaderBottom: chatHeaderBottom,
            actualSpaceBelow: actualSpaceBelow,
            actualSpaceAbove: actualSpaceAbove
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

    // Restore scroll position after loading more messages (INSTANT)
    restoreScrollPosition: (element, previousState) => {
        if (!element || !previousState) return;

        // Double rAF pattern - Blazor + browser render cycle tamamlanır
        // Frame 1: Blazor component render
        requestAnimationFrame(() => {
            // Frame 2: DOM fully painted, layout stable
            requestAnimationFrame(() => {
                // Height difference metodu - WhatsApp/Telegram eyni metodu işlədir
                const currentHeight = element.scrollHeight;
                const previousHeight = previousState.scrollHeight;

                if (previousHeight && previousState.scrollTop !== undefined) {
                    const heightDifference = currentHeight - previousHeight;

                    // INSTANT scroll - browser animation disable
                    const oldBehavior = element.style.scrollBehavior;
                    element.style.scrollBehavior = 'auto';

                    element.scrollTop = previousState.scrollTop + heightDifference;

                    // Restore smooth scroll (növbəti frame-də)
                    requestAnimationFrame(() => {
                        element.style.scrollBehavior = oldBehavior;
                    });
                }
            });
        });
    },

    // Scroll to a message wrapper and highlight it
    scrollToMessageAndHighlight: (messageId) => {
        const messageElement = document.getElementById(messageId);
        if (!messageElement) return;

        const container = document.getElementById('chat-messages');
        if (container) {
            // Use getBoundingClientRect for accurate positioning
            const containerRect = container.getBoundingClientRect();
            const messageRect = messageElement.getBoundingClientRect();

            // Calculate message position relative to container's current scroll
            const messageTopRelativeToContainer = messageRect.top - containerRect.top + container.scrollTop;
            const messageHeight = messageRect.height;
            const containerHeight = containerRect.height;

            // Calculate target scroll position (center the message)
            let targetScrollTop = messageTopRelativeToContainer - (containerHeight / 2) + (messageHeight / 2);

            // Clamp to valid scroll range
            const maxScroll = container.scrollHeight - containerHeight;
            targetScrollTop = Math.max(0, Math.min(targetScrollTop, maxScroll));

            // Use instant scroll to prevent any bounce effect
            container.scrollTo({ top: targetScrollTop, behavior: 'instant' });
        } else {
            // Fallback if container not found
            messageElement.scrollIntoView({ behavior: 'instant', block: 'center' });
        }

        // Add highlight class to the message wrapper
        messageElement.classList.add('highlighted');

        // Remove highlight after animation completes (2 seconds to match animation duration)
        setTimeout(() => {
            messageElement.classList.remove('highlighted');
        }, 2000);
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
    },

    // Get scroll position (for infinite scroll detection)
    getScrollTop: (element) => {
        if (!element) return 0;
        return element.scrollTop;
    },

    // Get scroll height
    getScrollHeight: (element) => {
        if (!element) return 0;
        return element.scrollHeight;
    },

    // Get client height (viewport height)
    getClientHeight: (element) => {
        if (!element) return 0;
        return element.clientHeight;
    },

    // Check if element is near bottom (within threshold pixels)
    isNearBottom: (element, threshold = 100) => {
        if (!element) return true;
        const scrollTop = element.scrollTop;
        const scrollHeight = element.scrollHeight;
        const clientHeight = element.clientHeight;
        return (scrollHeight - scrollTop - clientHeight) <= threshold;
    },

    // Get scroll percentage (0% = top, 100% = bottom)
    getScrollPercentage: (element) => {
        if (!element) return 100;
        const scrollTop = element.scrollTop;
        const scrollHeight = element.scrollHeight;
        const clientHeight = element.clientHeight;
        const scrollableHeight = scrollHeight - clientHeight;
        if (scrollableHeight === 0) return 100;
        return (scrollTop / scrollableHeight) * 100;
    },

    // Trigger file input click (for file selection)
    clickFileInput: (element) => {
        if (element) {
            element.click();
        }
    },

    // Calculate optimal page size based on viewport height
    // Bitrix-style: yalnız görünən mesajlar yüklənsin
    calculateOptimalPageSize: (containerElement) => {
        if (!containerElement) return 30; // Default

        const containerHeight = containerElement.clientHeight;
        const estimatedMessageHeight = 80; // Average message height (px)
        const bufferMultiplier = 1.5; // 50% buffer for smooth scrolling

        // Viewport-a sığan mesaj sayı + buffer
        const visibleMessages = Math.ceil(containerHeight / estimatedMessageHeight);
        const optimalSize = Math.ceil(visibleMessages * bufferMultiplier);

        // Min 15, max 50
        return Math.min(Math.max(optimalSize, 15), 50);
    },

    // Get viewport-based page size
    getViewportBasedPageSize: () => {
        const container = document.getElementById('chat-messages');
        return window.chatAppUtils.calculateOptimalPageSize(container);
    }
};