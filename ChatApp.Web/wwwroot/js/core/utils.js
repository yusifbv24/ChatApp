/**
 * ChatApp Global Namespace & Utility Functions
 * Replaces: AvatarHelper.cs, StringHelper.cs, and general Blazor utilities
 */
window.ChatApp = window.ChatApp || {};

ChatApp.apiBase = '';  // Set during init from config

ChatApp.utils = {
    /**
     * Avatar color palette — deterministic color from GUID
     * Matches AvatarHelper.GetAvatarBackgroundColor exactly
     */
    _avatarColors: [
        '#C62828', '#00695C', '#1565C0', '#D84315',
        '#2E7D32', '#F9A825', '#6A1B9A', '#0277BD',
        '#E65100', '#00838F'
    ],

    getAvatarColor(id) {
        if (!id) return this._avatarColors[0];
        let hash = 0;
        const str = id.toString().replace(/-/g, '');
        for (let i = 0; i < str.length; i++) {
            hash = ((hash << 5) - hash + str.charCodeAt(i)) | 0;
        }
        return this._avatarColors[Math.abs(hash) % this._avatarColors.length];
    },

    /**
     * Get initials from full name — "John Doe" → "JD"
     * Matches StringHelper.GetInitials exactly
     */
    getInitials(name) {
        if (!name) return '?';
        const parts = name.trim().split(/\s+/);
        if (parts.length >= 2) {
            return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
        }
        return parts[0].substring(0, 2).toUpperCase();
    },

    /**
     * Truncate text with ellipsis
     */
    truncateText(text, maxLength) {
        if (!text || text.length <= maxLength) return text || '';
        return text.substring(0, maxLength) + '...';
    },

    /**
     * Escape HTML to prevent XSS — critical for DOM innerHTML
     * Optimized: uses string replacement instead of DOM creation
     */
    escapeHtml(text) {
        if (!text) return '';
        return text.toString()
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    },

    /**
     * File icon cache — avoids repeated string comparisons
     */
    _fileIconCache: {},
    getFileIcon(fileName) {
        if (!fileName) return { icon: 'description', color: '' };
        var ext = fileName.split('.').pop().toLowerCase();
        if (this._fileIconCache[ext]) return this._fileIconCache[ext];
        var result;
        if (ext === 'pdf') result = { icon: 'picture_as_pdf', color: 'color:#e53935;' };
        else if (ext === 'doc' || ext === 'docx') result = { icon: 'article', color: 'color:#1565c0;' };
        else if (ext === 'xls' || ext === 'xlsx') result = { icon: 'table_chart', color: 'color:#2e7d32;' };
        else if (ext === 'ppt' || ext === 'pptx') result = { icon: 'slideshow', color: 'color:#ef6c00;' };
        else if (ext === 'zip' || ext === 'rar' || ext === '7z') result = { icon: 'folder_zip', color: 'color:#757575;' };
        else result = { icon: 'description', color: '' };
        this._fileIconCache[ext] = result;
        return result;
    },

    /**
     * Avatar URL memoization — caches renderAvatar output by user id + url
     */
    _avatarCache: {},
    _avatarCacheSize: 0,
    renderAvatarCached(user, cssClass) {
        if (!user || !user.id) return this.renderAvatar(user, cssClass);
        var key = user.id + '|' + (user.avatarUrl || '') + '|' + (cssClass || '');
        if (this._avatarCache[key]) return this._avatarCache[key];
        var html = this.renderAvatar(user, cssClass);
        // Limit cache size to prevent unbounded growth
        if (this._avatarCacheSize > 500) {
            this._avatarCache = {};
            this._avatarCacheSize = 0;
        }
        this._avatarCache[key] = html;
        this._avatarCacheSize++;
        return html;
    },

    /**
     * Render avatar HTML (img or placeholder)
     */
    renderAvatar(user, cssClass) {
        cssClass = cssClass || '';
        if (!user) return '<div class="' + cssClass + ' avatar-empty"></div>';
        if (user.avatarUrl) {
            return '<img src="' + this.escapeHtml(user.avatarUrl) +
                '" alt="' + this.escapeHtml(user.fullName || '') +
                '" class="' + cssClass + '" />';
        }
        return '<div class="' + cssClass + ' avatar-placeholder" style="background-color:' +
            this.getAvatarColor(user.id) + ';">' +
            this.getInitials(user.fullName || user.firstName || '') + '</div>';
    },

    /**
     * Month names for date formatting
     */
    _monthNames: [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ],

    /**
     * Format time as HH:mm
     */
    _formatHHmm(date) {
        return date.getHours().toString().padStart(2, '0') + ':' +
            date.getMinutes().toString().padStart(2, '0');
    },

    /**
     * Format relative time — detailed formats:
     * "just now" (< 1 min), "X minutes ago" (< 60 min),
     * "today at HH:mm" (same day), "yesterday at HH:mm" (yesterday),
     * "MMMM dd at HH:mm" (older, same year), "MMMM dd, yyyy at HH:mm" (different year)
     */
    formatRelativeTime(dateStr) {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        const now = new Date();
        const diffMs = now - date;
        const diffMin = Math.floor(diffMs / 60000);

        if (diffMin < 1) return 'just now';
        if (diffMin < 60) return diffMin + ' minutes ago';

        const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        const yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);
        const dateDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());

        const time = this._formatHHmm(date);

        if (dateDay.getTime() === today.getTime()) return 'today at ' + time;
        if (dateDay.getTime() === yesterday.getTime()) return 'yesterday at ' + time;

        const day = date.getDate().toString().padStart(2, '0');
        const month = this._monthNames[date.getMonth()];

        if (date.getFullYear() === now.getFullYear()) {
            return month + ' ' + day + ' at ' + time;
        }
        return month + ' ' + day + ', ' + date.getFullYear() + ' at ' + time;
    },

    /**
     * Format date as "MMMM dd, yyyy HH:mm" for last visit display
     */
    formatLastVisit(dateStr) {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        const month = this._monthNames[date.getMonth()];
        const day = date.getDate().toString().padStart(2, '0');
        const year = date.getFullYear();
        const time = this._formatHHmm(date);
        return month + ' ' + day + ', ' + year + ' ' + time;
    },

    /**
     * Format date for HTML date input — "yyyy-MM-dd"
     */
    formatDateForInput(dateStr) {
        if (!dateStr) return '';
        const d = new Date(dateStr);
        if (isNaN(d.getTime())) return '';
        return d.getFullYear() + '-' +
            (d.getMonth() + 1).toString().padStart(2, '0') + '-' +
            d.getDate().toString().padStart(2, '0');
    },

    /**
     * Format time for message bubbles — "14:30"
     */
    formatMessageTime(dateStr) {
        if (!dateStr) return '';
        const d = new Date(dateStr);
        return d.getHours().toString().padStart(2, '0') + ':' +
            d.getMinutes().toString().padStart(2, '0');
    },

    /**
     * Format date for message date separators — "Today", "Yesterday", "12 Feb 2026"
     */
    formatMessageDate(dateStr) {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        const today = new Date();
        const yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);

        if (date.toDateString() === today.toDateString()) return 'Today';
        if (date.toDateString() === yesterday.toDateString()) return 'Yesterday';

        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
            'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return date.getDate() + ' ' + months[date.getMonth()] + ' ' + date.getFullYear();
    },

    /**
     * Format file size — "1.2 MB", "340 KB"
     */
    formatFileSize(bytes) {
        if (!bytes || bytes === 0) return '0 B';
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return (bytes / Math.pow(1024, i)).toFixed(i > 0 ? 1 : 0) + ' ' + sizes[i];
    },

    /**
     * Debounce utility
     */
    debounce(fn, delay) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => fn.apply(this, args), delay);
        };
    },

    /**
     * Throttle utility
     */
    throttle(fn, limit) {
        let inThrottle = false;
        return function (...args) {
            if (!inThrottle) {
                fn.apply(this, args);
                inThrottle = true;
                setTimeout(() => (inThrottle = false), limit);
            }
        };
    },

    /**
     * Generate UUID v4
     */
    generateId() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            const r = (Math.random() * 16) | 0;
            return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
        });
    },

    /**
     * Check if current path matches navigation item
     */
    isActivePath(path, exact) {
        const currentPath = window.location.pathname;
        if (exact) return currentPath === path;
        return currentPath === path || currentPath.startsWith(path + '/');
    },

    /**
     * Toast notification system
     * Replaces Blazor's MudSnackbar / toast notifications
     * @param {string} message - text to show
     * @param {string} [type='info'] - 'success' | 'error' | 'warning' | 'info'
     * @param {number} [duration=4000] - auto-dismiss ms (0 = manual only)
     */
    showToast(message, type, duration) {
        type = type || 'info';
        duration = duration !== undefined ? duration : 4000;

        // Ensure container exists
        var container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.style.cssText = 'position:fixed;top:16px;right:16px;z-index:10000;display:flex;flex-direction:column;gap:8px;max-width:380px;';
            document.body.appendChild(container);
        }

        var icons = { success: 'check_circle', error: 'error', warning: 'warning', info: 'info' };
        var colors = { success: '#2e7d32', error: '#c62828', warning: '#f57c00', info: '#1565c0' };

        var toast = document.createElement('div');
        toast.className = 'toast-notification toast-' + type;
        toast.style.cssText = 'display:flex;align-items:center;gap:10px;padding:12px 16px;background:#fff;border-radius:8px;box-shadow:0 4px 12px rgba(0,0,0,0.15);border-left:4px solid ' + (colors[type] || colors.info) + ';transform:translateX(120%);transition:transform 0.3s ease;font-size:0.9rem;';
        toast.innerHTML = '<span class="material-icons" style="color:' + (colors[type] || colors.info) + ';font-size:20px;">' + (icons[type] || icons.info) + '</span>' +
            '<span style="flex:1;">' + this.escapeHtml(message) + '</span>' +
            '<span class="material-icons toast-close" style="cursor:pointer;font-size:18px;color:#999;">close</span>';

        container.appendChild(toast);

        // Slide in
        requestAnimationFrame(function () {
            toast.style.transform = 'translateX(0)';
        });

        // Close on click
        var closeBtn = toast.querySelector('.toast-close');
        closeBtn.addEventListener('click', function () { dismiss(); });

        // Auto-dismiss
        var timer = null;
        if (duration > 0) {
            timer = setTimeout(dismiss, duration);
        }

        function dismiss() {
            if (timer) clearTimeout(timer);
            toast.style.transform = 'translateX(120%)';
            setTimeout(function () { toast.remove(); }, 350);
        }
    }
};
