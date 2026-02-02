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

    // Handle mention clicks (event delegation)
    const mention = e.target.closest('.message-mention');
    if (mention) {
        const username = mention.getAttribute('data-username');
        if (username) {
            // Dispatch custom event with username
            window.dispatchEvent(new CustomEvent('mentionClicked', { detail: { username } }));
        }
    }
});

// Utilities
window.chatAppUtils = {
    // Subscribe to outside click events
    subscribeToOutsideClick: (dotNetHelper) => {
        const handler = () => {
            dotNetHelper.invokeMethodAsync('CloseUserMenuFromJS')
                .catch(() => {
                    // Ignore errors from disposed components
                });
        };
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

    // Scroll to bottom instantly without flash effect
    // Uses double rAF pattern to ensure DOM is fully rendered before scroll
    scrollToBottomInstant: (elementId) => {
        const element = document.getElementById(elementId);
        if (!element) return;

        // Double rAF pattern - ensures Blazor + browser render cycles complete
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                element.scrollTop = element.scrollHeight;
            });
        });
    },

    // Hide element before render (called before StateHasChanged)
    hideElement: (elementId) => {
        const element = document.getElementById(elementId);
        if (element) {
            element.style.visibility = 'hidden';
        }
    },

    // Scroll to bottom and show element (called after render)
    // Uses scroll correction pattern - scrolls multiple times to handle dynamic content
    // Container remains HIDDEN until final scroll is complete
    scrollToBottomAndShow: (elementId) => {
        const element = document.getElementById(elementId);
        if (!element) return;

        // Double rAF - wait for DOM to be fully rendered
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                // First scroll (container still hidden)
                element.scrollTop = element.scrollHeight;

                // Scroll correction at 50ms (container still hidden)
                setTimeout(() => {
                    element.scrollTop = element.scrollHeight;
                }, 50);

                // Final correction + SHOW (only after all scrolls complete)
                setTimeout(() => {
                    element.scrollTop = element.scrollHeight;
                    element.style.visibility = 'visible';
                }, 150);
            });
        });
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
        // Reset height to default
        element.style.height = '24px';
        element.classList.remove('has-scroll');

        // Force clear any lingering content (including newlines)
        if (element.value === '' || element.value === '\n') {
            element.value = '';
        }
    },

    // Prevent Enter key default behavior (newline) when sending message
    setupTextareaKeydownHandler: (element) => {
        if (!element) return;

        // Remove existing listener if any
        if (element._keydownHandler) {
            element.removeEventListener('keydown', element._keydownHandler);
        }

        // Create new handler
        const handler = (e) => {
            // Enter without Shift - prevent default (don't add newline)
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
            }
        };

        // Store handler reference for cleanup
        element._keydownHandler = handler;
        element.addEventListener('keydown', handler);
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
            dotNetHelper.invokeMethodAsync('OnVisibilityChanged', isVisible)
                .catch(() => {
                    // Ignore errors from disposed components
                });
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
    },

    // Subscribe to mention click events
    subscribeToMentionClick: (dotNetHelper) => {
        const handler = (e) => {
            if (e.detail && e.detail.username) {
                dotNetHelper.invokeMethodAsync('HandleMentionClick', e.detail.username);
            }
        };
        window.addEventListener('mentionClicked', handler);
        return {
            dispose: () => window.removeEventListener('mentionClicked', handler)
        };
    },

    getScrollInfo: (selector) => {
        const el = document.querySelector(selector);
        if (!el) return null;
        return {
            scrollTop: el.scrollTop,
            clientHeight: el.clientHeight,
            scrollHeight: el.scrollHeight
        };
    }
};

// Mention click handlers - global delegation
window.initializeMentionClickHandlers = function(dotNetHelper) {
    // Remove existing listener if any
    if (window._mentionClickHandler) {
        document.removeEventListener('click', window._mentionClickHandler);
    }

    // Event delegation - listen to clicks on the document
    window._mentionClickHandler = async (e) => {
        // Check if the clicked element or its parent is a mention span
        let target = e.target;

        // Traverse up to find .message-mention
        while (target && target !== document.body) {
            if (target.classList && target.classList.contains('message-mention')) {
                const userId = target.getAttribute('data-userid');
                const username = target.getAttribute('data-username');

                if (userId) {
                    try {
                        await dotNetHelper.invokeMethodAsync('HandleMentionClickFromJS', userId);
                    } catch (err) {
                        console.error('[ERROR] Failed to invoke HandleMentionClickFromJS:', err);
                    }
                }
                break;
            }
            target = target.parentElement;
        }
    };

    document.addEventListener('click', window._mentionClickHandler);
};

window.disposeMentionClickHandlers = function() {
    if (window._mentionClickHandler) {
        document.removeEventListener('click', window._mentionClickHandler);
        window._mentionClickHandler = null;
    }
};

// Mention panel outside click handler
window.setupMentionOutsideClickHandler = function(dotNetHelper) {
    // Remove existing listener if any
    if (window._mentionOutsideClickHandler) {
        document.removeEventListener('click', window._mentionOutsideClickHandler);
    }

    window._mentionOutsideClickHandler = (e) => {
        // Check if mention panel is visible
        const mentionPanelWrapper = document.querySelector('.mention-panel-wrapper');
        if (!mentionPanelWrapper) return;

        // Check if click is outside mention panel and outside textarea
        const messageInput = document.querySelector('.message-input-container');
        if (!mentionPanelWrapper.contains(e.target) && !messageInput?.contains(e.target)) {
            try {
                dotNetHelper.invokeMethodAsync('OnMentionPanelOutsideClick')
                    .catch(() => {
                        // Ignore errors from disposed components
                    });
            } catch (err) {
                console.error('[ERROR] Failed to invoke OnMentionPanelOutsideClick:', err);
            }
        }
    };

    document.addEventListener('click', window._mentionOutsideClickHandler);
};

window.disposeMentionOutsideClickHandler = function() {
    if (window._mentionOutsideClickHandler) {
        document.removeEventListener('click', window._mentionOutsideClickHandler);
        window._mentionOutsideClickHandler = null;
    }
};

// Global menu handlers - store all menu handlers (message menus and conversation menus)
// Key format: "message-{messageId}" or "conversation-{conversationId}"
window._allMenuHandlers = window._allMenuHandlers || {};

window.setupMessageMenuOutsideClickHandler = function(messageId, dotNetHelper) {
    const menuKey = `message-${messageId}`;

    // FIRST: Close all other open menus (both message and conversation menus)
    Object.keys(window._allMenuHandlers).forEach(otherKey => {
        if (otherKey !== menuKey) {
            const otherData = window._allMenuHandlers[otherKey];
            if (otherData && otherData.dotNetHelper) {
                try {
                    otherData.dotNetHelper.invokeMethodAsync(otherData.closeMethod)
                        .catch(() => {
                            // Ignore errors from disposed components
                        });
                } catch (err) {
                    // Ignore errors
                }
            }
            // Remove the handler
            if (otherData && otherData.handler) {
                document.removeEventListener('click', otherData.handler);
            }
            delete window._allMenuHandlers[otherKey];
        }
    });

    // Remove existing handler for this menu if any
    if (window._allMenuHandlers[menuKey]) {
        const oldData = window._allMenuHandlers[menuKey];
        if (oldData.handler) {
            document.removeEventListener('click', oldData.handler);
        }
        delete window._allMenuHandlers[menuKey];
    }

    const handler = (e) => {
        // Check if more menu is visible for this message
        const messageElement = document.getElementById(`message-${messageId}`);
        if (!messageElement) return;

        const moreMenu = messageElement.querySelector('.chevron-more-menu');
        const chevronWrapper = messageElement.querySelector('.chevron-wrapper');

        // Menu is not visible, remove handler
        if (!moreMenu || !moreMenu.offsetParent) {
            document.removeEventListener('click', handler);
            delete window._allMenuHandlers[menuKey];
            return;
        }

        // Ignore clicks on chevron (let Blazor handle toggle)
        if (chevronWrapper && chevronWrapper.contains(e.target)) {
            return;
        }

        // Check if click is outside menu
        if (!moreMenu.contains(e.target)) {
            try {
                dotNetHelper.invokeMethodAsync('OnMessageMenuOutsideClick')
                    .catch(() => {
                        // Ignore errors from disposed components
                    });
            } catch (err) {
                console.error('[ERROR] Failed to invoke OnMessageMenuOutsideClick:', err);
            }
        }
    };

    // Add handler on next tick to avoid catching the opening click event
    setTimeout(() => {
        window._allMenuHandlers[menuKey] = {
            handler: handler,
            dotNetHelper: dotNetHelper,
            closeMethod: 'OnMessageMenuOutsideClick'
        };
        document.addEventListener('click', handler);
    }, 0);
};

window.disposeMessageMenuOutsideClickHandler = function(messageId) {
    const menuKey = messageId ? `message-${messageId}` : null;

    // If messageId provided, dispose specific handler
    if (menuKey && window._allMenuHandlers[menuKey]) {
        const data = window._allMenuHandlers[menuKey];
        if (data.handler) {
            document.removeEventListener('click', data.handler);
        }
        delete window._allMenuHandlers[menuKey];
    } else if (!messageId) {
        // Otherwise dispose all handlers
        if (window._allMenuHandlers) {
            Object.keys(window._allMenuHandlers).forEach(key => {
                const data = window._allMenuHandlers[key];
                if (data && data.handler) {
                    document.removeEventListener('click', data.handler);
                }
            });
            window._allMenuHandlers = {};
        }
    }
};

// Conversation menu outside click handler
window.setupConversationMenuOutsideClickHandler = function(conversationId, dotNetHelper) {
    const menuKey = `conversation-${conversationId}`;

    // FIRST: Close all other open menus (both message and conversation menus)
    Object.keys(window._allMenuHandlers).forEach(otherKey => {
        if (otherKey !== menuKey) {
            const otherData = window._allMenuHandlers[otherKey];
            if (otherData && otherData.dotNetHelper) {
                try {
                    otherData.dotNetHelper.invokeMethodAsync(otherData.closeMethod)
                        .catch(() => {
                            // Ignore errors from disposed components
                        });
                } catch (err) {
                    // Ignore errors
                }
            }
            // Remove the handler
            if (otherData && otherData.handler) {
                document.removeEventListener('click', otherData.handler);
            }
            delete window._allMenuHandlers[otherKey];
        }
    });

    // Remove existing handler for this menu if any
    if (window._allMenuHandlers[menuKey]) {
        const oldData = window._allMenuHandlers[menuKey];
        if (oldData.handler) {
            document.removeEventListener('click', oldData.handler);
        }
        delete window._allMenuHandlers[menuKey];
    }

    const handler = (e) => {
        // Check if menu is still visible
        const allMenus = document.querySelectorAll('.conversation-more-menu');
        let menuStillVisible = false;

        allMenus.forEach(menu => {
            if (menu.offsetParent) {
                menuStillVisible = true;
            }
        });

        // Menu is not visible, remove handler
        if (!menuStillVisible) {
            document.removeEventListener('click', handler);
            delete window._allMenuHandlers[menuKey];
            return;
        }

        // Check if click is inside any conversation more menu or more button
        const clickedOnMenu = e.target.closest('.conversation-more-menu');
        const clickedOnButton = e.target.closest('.conversation-more-btn');

        // If click is outside both menu and button, close the menu
        if (!clickedOnMenu && !clickedOnButton) {
            try {
                dotNetHelper.invokeMethodAsync('OnConversationMenuOutsideClick')
                    .catch(() => {
                        // Ignore errors from disposed components
                    });
            } catch (err) {
                console.error('[ERROR] Failed to invoke OnConversationMenuOutsideClick:', err);
            }
        }
    };

    // Add handler on next tick to avoid catching the opening click event
    setTimeout(() => {
        window._allMenuHandlers[menuKey] = {
            handler: handler,
            dotNetHelper: dotNetHelper,
            closeMethod: 'OnConversationMenuOutsideClick'
        };
        document.addEventListener('click', handler);
    }, 0);
};

window.disposeConversationMenuOutsideClickHandler = function(conversationId) {
    const menuKey = conversationId ? `conversation-${conversationId}` : null;

    // If conversationId provided, dispose specific handler
    if (menuKey && window._allMenuHandlers[menuKey]) {
        const data = window._allMenuHandlers[menuKey];
        if (data.handler) {
            document.removeEventListener('click', data.handler);
        }
        delete window._allMenuHandlers[menuKey];
    } else if (!conversationId) {
        // Otherwise dispose all handlers
        if (window._allMenuHandlers) {
            Object.keys(window._allMenuHandlers).forEach(key => {
                const data = window._allMenuHandlers[key];
                if (data && data.handler) {
                    document.removeEventListener('click', data.handler);
                }
            });
            window._allMenuHandlers = {};
        }
    }
};