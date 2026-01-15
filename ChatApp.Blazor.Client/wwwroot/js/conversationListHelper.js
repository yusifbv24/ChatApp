// Menu Panel Helper - manages global scroll and click listeners for all more menu panels
// Supports both ConversationList and MessageBubble components

window.menuPanelHelper = {
    registeredComponents: [],
    scrollHandler: null,
    clickHandler: null,
    initialized: false,

    // Register a component (ConversationList or MessageBubble)
    register: function (dotNetReference) {
        // Check if already registered
        const exists = this.registeredComponents.some(ref => ref === dotNetReference);
        if (exists) {
            return;
        }

        this.registeredComponents.push(dotNetReference);

        // Initialize global listeners only once
        if (!this.initialized) {
            this.initializeGlobalListeners();
            this.initialized = true;
        }
    },

    // Unregister a component
    unregister: function (dotNetReference) {
        this.registeredComponents = this.registeredComponents.filter(ref => ref !== dotNetReference);

        // If no more components registered, remove global listeners
        if (this.registeredComponents.length === 0 && this.initialized) {
            this.disposeGlobalListeners();
            this.initialized = false;
        }
    },

    // Initialize global event listeners
    initializeGlobalListeners: function () {
        // Create scroll handler
        this.scrollHandler = () => {
            this.notifyAllComponents();
        };

        // Create click handler
        this.clickHandler = () => {
            this.notifyAllComponents();
        };

        // Add scroll listeners to window (capture phase to catch all scroll events)
        window.addEventListener('scroll', this.scrollHandler, true);

        // Add click listener to document
        document.addEventListener('click', this.clickHandler, true);

        // Also add scroll listeners to common scroll containers
        const scrollContainers = [
            '.messages-main',
            '.chat-messages',
            '.conversation-items',
            '.messages-sidebar'
        ];

        scrollContainers.forEach(selector => {
            const elements = document.querySelectorAll(selector);
            elements.forEach(el => {
                el.addEventListener('scroll', this.scrollHandler);
            });
        });
    },

    // Notify all registered components about interaction
    notifyAllComponents: function () {
        this.registeredComponents.forEach(dotNetRef => {
            if (dotNetRef) {
                try {
                    dotNetRef.invokeMethodAsync('OnGlobalInteraction')
                        .catch(err => {
                            // Silently ignore errors from disposed components
                            // This is expected when components are disposed during navigation
                        });
                } catch (err) {
                    // Catch synchronous errors
                }
            }
        });
    },

    // Dispose global listeners
    disposeGlobalListeners: function () {
        if (this.scrollHandler) {
            // Remove from window
            window.removeEventListener('scroll', this.scrollHandler, true);

            // Remove from scroll containers
            const scrollContainers = [
                '.messages-main',
                '.chat-messages',
                '.conversation-items',
                '.messages-sidebar'
            ];

            scrollContainers.forEach(selector => {
                const elements = document.querySelectorAll(selector);
                elements.forEach(el => {
                    el.removeEventListener('scroll', this.scrollHandler);
                });
            });

            this.scrollHandler = null;
        }

        if (this.clickHandler) {
            document.removeEventListener('click', this.clickHandler, true);
            this.clickHandler = null;
        }
    }
};

// Keep backward compatibility with old name
window.conversationListHelper = {
    initialize: function (dotNetReference) {
        window.menuPanelHelper.register(dotNetReference);
    },
    dispose: function () {
        // Note: We can't unregister without the reference, but this will be handled by component disposal
    }
};