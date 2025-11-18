window.chatApp = {
    // Scroll to bottom of element
    scrollToBottom: function(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },
    // Smooth scroll to bottom
    smoothScrollToBottom: function(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTo({
                top: element.scrollHeight,
                behavior: 'smooth'
            });
        }
    },
 
    // Focus element
    focusElement: function(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            setTimeout(() => element.focus(), 100);
        }
    },

    // Copy to clipboard

    copyToClipboard: async function(text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (err) {
            console.error('Failed to copy:', err);
            return false;
        }
    },

    // Download file
    downloadFile: function(filename, content, contentType) {
        const blob = new Blob([content], { type: contentType });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    },

    // Get element height
    getElementHeight: function(elementId) {
        const element = document.getElementById(elementId);
        return element ? element.offsetHeight : 0;
    },

    // Set page title
    setPageTitle: function(title) {
        document.title = title;
    },

    // Show/hide loading screen
    hideLoadingScreen: function() {
        const loadingScreen = document.querySelector('.loading-screen');
        if (loadingScreen) {
            loadingScreen.style.opacity = '0';
            setTimeout(() => {
                loadingScreen.style.display = 'none';
            }, 300);
        }
    },

    // Detect dark mode preference
    isDarkMode: function() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    // Listen for dark mode changes
    onDarkModeChange: function(dotNetHelper) {
        if (window.matchMedia) {
            const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');
            darkModeQuery.addEventListener('change', (e) => {
                dotNetHelper.invokeMethodAsync('OnDarkModeChanged', e.matches);
            });
        }
    },

    // Save to local storage
    saveToLocalStorage: function(key, value) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
            return true;
        } catch (err) {
            console.error('Failed to save to localStorage:', err);
            return false;
        }
    },

    // Load from local storage
    loadFromLocalStorage: function(key) {
        try {
            const value = localStorage.getItem(key);
            return value ? JSON.parse(value) : null;
        } catch (err) {
            console.error('Failed to load from localStorage:', err);
            return null;
        }
    },
 
    // Remove from local storage
    removeFromLocalStorage: function(key) {
        try {
            localStorage.removeItem(key);
            return true;
        } catch (err) {
            console.error('Failed to remove from localStorage:', err);
            return false;
        }
    },

    // Play notification sound
    playNotificationSound: function() {
        const audio = new Audio('/sounds/notification.mp3');
        audio.play().catch(err => console.error('Failed to play sound:', err));
    },


    // Request notification permission
    requestNotificationPermission: async function() {
        if ('Notification' in window) {
            const permission = await Notification.requestPermission();
            return permission === 'granted';
        }
        return false;
    },


    // Show browser notification
    showBrowserNotification: function(title, body, icon) {
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, {
                body: body,
                icon: icon || '/favicon.png',
                badge: '/favicon.png'
            });
        }
    },


    // Detect online/offline status
    onConnectionChange: function(dotNetHelper) {
        window.addEventListener('online', () => {
            dotNetHelper.invokeMethodAsync('OnConnectionChanged', true);
        });
        window.addEventListener('offline', () => {
            dotNetHelper.invokeMethodAsync('OnConnectionChanged', false);
        });
    },


    // Get connection status
    isOnline: function() {
        return navigator.onLine;
    },
 

    // Vibrate device (mobile)
    vibrate: function(duration) {
        if ('vibrate' in navigator) {
            navigator.vibrate(duration || 200);
        }
    },

    // Prevent default scroll behavior
    preventScroll: function() {
        document.body.style.overflow = 'hidden';
    },
 
    // Allow scroll
    allowScroll: function() {
        document.body.style.overflow = '';
    },

    // Add CSS class to body
    addBodyClass: function(className) {
        document.body.classList.add(className);

    },

    // Remove CSS class from body
    removeBodyClass: function(className) {
        document.body.classList.remove(className);
    },

    // Initialize tooltips

    initTooltips: function() {

        const tooltips = document.querySelectorAll('[data-tooltip]');

        tooltips.forEach(element => {

            element.addEventListener('mouseenter', function() {
                const tooltip = document.createElement('div');
                tooltip.className = 'custom-tooltip';
                tooltip.textContent = this.getAttribute('data-tooltip');
                document.body.appendChild(tooltip);

                const rect = this.getBoundingClientRect();
                tooltip.style.top = `${rect.top - tooltip.offsetHeight - 5}px`;
                tooltip.style.left = `${rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2)}px`;
 
                this._tooltip = tooltip;
            });

 

            element.addEventListener('mouseleave', function() {
                if (this._tooltip) {
                    document.body.removeChild(this._tooltip);
                    delete this._tooltip;
                }
            });
        });
    }
};
 
// Initialize on load
window.addEventListener('load', () => {
    // Check if the function exists before calling it
    if (window.chatApp && window.chatApp.hideLoadingScreen) {
        window.chatApp.hideLoadingScreen();
    }
});