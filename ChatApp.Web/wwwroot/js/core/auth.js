/**
 * Auth Service — replaces Blazor AuthService.cs
 * Handles login, logout, current user loading, and auth state
 */
(function () {
    'use strict';

    ChatApp.auth = {
        /**
         * Load current user from API and populate state
         * Called on app startup (layout.js)
         */
        loadCurrentUser: async function () {
            const result = await ChatApp.api.get('/api/identity/users/me');
            if (result.isSuccess && result.value) {
                ChatApp.state.setCurrentUser(result.value);
                return true;
            }
            return false;
        },

        /**
         * Logout — clear session and redirect
         */
        logout: async function () {
            try {
                await ChatApp.api.post('/api/auth/logout');
            } catch { }
            ChatApp.state.setCurrentUser(null);
            window.location.href = '/auth/login';
        },

        /**
         * Check if user is authenticated (has current user in state)
         */
        isAuthenticated: function () {
            return ChatApp.state.isAuthenticated;
        },

        /**
         * Redirect to login if not authenticated
         */
        requireAuth: function () {
            if (!ChatApp.state.isAuthenticated) {
                window.location.href = '/auth/login';
                return false;
            }
            return true;
        }
    };
})();
