/**
 * Application State — replaces Blazor AppState.cs + UserState.cs
 * EventEmitter-based global state with typed events
 */
(function () {
    'use strict';

    const _listeners = {};

    const state = {
        // Current user (UserDetailDto equivalent)
        currentUser: null,

        // Auth flags
        isAuthenticated: false,
        isAdmin: false,
        isSuperAdmin: false,

        // Dark mode
        isDarkMode: false,

        // Online users set
        _onlineUsers: new Set(),

        // Unread counts
        unreadMessageCount: 0,
        unreadNotificationCount: 0,

        // Pending chat (one-time consume)
        _pendingChatUserId: null,

        // ------- Event Emitter -------

        on: function (event, fn) {
            if (!_listeners[event]) _listeners[event] = [];
            _listeners[event].push(fn);
        },

        off: function (event, fn) {
            if (!_listeners[event]) return;
            _listeners[event] = _listeners[event].filter(function (f) { return f !== fn; });
        },

        emit: function (event, data) {
            if (!_listeners[event]) return;
            _listeners[event].forEach(function (fn) {
                try { fn(data); } catch (e) { console.error('[State] Event handler error:', event, e); }
            });
        },

        // ------- User State -------

        setCurrentUser: function (user) {
            this.currentUser = user;
            this.isAuthenticated = !!user;
            if (user) {
                this.isAdmin = user.role === 1 || user.role === 'Administrator' ||
                    user.systemRole === 1 || user.systemRole === 'Administrator';
                this.isSuperAdmin = !!user.isSuperAdmin;

                // Derive convenience fields
                user.fullName = ((user.firstName || '') + ' ' + (user.lastName || '')).trim();
            } else {
                this.isAdmin = false;
                this.isSuperAdmin = false;
            }
            this.emit('userLoaded', user);
            this.emit('stateChanged');
        },

        hasPermission: function (permission) {
            if (this.isSuperAdmin) return true;
            if (!this.currentUser || !this.currentUser.permissions) return false;
            return this.currentUser.permissions.indexOf(permission) !== -1;
        },

        // ------- Online Users -------

        addOnlineUser: function (userId) {
            this._onlineUsers.add(userId);
            this.emit('onlineUsersChanged', { userId: userId, online: true });
        },

        removeOnlineUser: function (userId) {
            this._onlineUsers.delete(userId);
            this.emit('onlineUsersChanged', { userId: userId, online: false });
        },

        setOnlineUsers: function (userIds) {
            this._onlineUsers = new Set(userIds || []);
            this.emit('onlineUsersChanged');
        },

        isUserOnline: function (userId) {
            return this._onlineUsers.has(userId);
        },

        // ------- Unread Counts -------

        setUnreadMessageCount: function (count) {
            this.unreadMessageCount = Math.max(0, count);
            this.emit('unreadMessagesChanged', this.unreadMessageCount);
        },

        incrementUnreadMessages: function () {
            this.unreadMessageCount++;
            this.emit('unreadMessagesChanged', this.unreadMessageCount);
        },

        decrementUnreadMessages: function (count) {
            this.unreadMessageCount = Math.max(0, this.unreadMessageCount - (count || 1));
            this.emit('unreadMessagesChanged', this.unreadMessageCount);
        },

        setUnreadNotificationCount: function (count) {
            this.unreadNotificationCount = Math.max(0, count);
            this.emit('unreadNotificationsChanged', this.unreadNotificationCount);
        },

        // ------- Pending Chat -------

        setPendingChatUserId: function (userId) {
            this._pendingChatUserId = userId;
        },

        consumePendingChatUserId: function () {
            const id = this._pendingChatUserId;
            this._pendingChatUserId = null;
            return id;
        },

        // ------- Dark Mode -------

        toggleDarkMode: function () {
            this.isDarkMode = !this.isDarkMode;
            document.body.classList.toggle('dark-theme', this.isDarkMode);
            try { localStorage.setItem('darkMode', this.isDarkMode ? '1' : '0'); } catch { }
            this.emit('darkModeChanged', this.isDarkMode);
        },

        loadDarkMode: function () {
            try {
                this.isDarkMode = localStorage.getItem('darkMode') === '1';
                document.body.classList.toggle('dark-theme', this.isDarkMode);
            } catch { }
        }
    };

    ChatApp.state = state;
})();