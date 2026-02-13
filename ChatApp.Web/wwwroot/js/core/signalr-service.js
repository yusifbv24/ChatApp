/**
 * SignalR Service — replaces ChatHubConnection.cs + SignalRService.cs + ExponentialBackoffRetryPolicy.cs
 * Production-grade: circuit breaker, exponential backoff, network/visibility detection, token refresh
 */
(function () {
    'use strict';

    // --- Circuit Breaker ---
    const CB_THRESHOLD = 5;
    const CB_OPEN_DURATION_MS = 300000;  // 5 minutes

    // --- Retry ---
    const RETRY_BASE_MS = 1000;
    const RETRY_MAX_MS = 60000;
    const RETRY_JITTER = 0.25;

    // --- Token refresh interval ---
    const TOKEN_REFRESH_MS = 720000;  // 12 minutes (JWT expires in 15)

    // --- Health check interval ---
    const HEALTH_CHECK_MS = 120000;  // 2 minutes

    let _connection = null;
    let _isInitialized = false;
    let _isConnecting = false;
    let _isManuallyDisconnecting = false;
    let _lastActiveTime = Date.now();

    // Circuit breaker state
    let _cbState = 'closed';  // closed | open | halfOpen
    let _cbFailureCount = 0;
    let _cbOpenedAt = 0;

    // Timers
    let _tokenRefreshTimer = null;
    let _healthCheckTimer = null;

    // Event handlers map (for typed events)
    const _handlers = {};

    // --- Exponential Backoff with Jitter ---
    function getRetryDelay(attempt) {
        const baseDelay = Math.min(RETRY_BASE_MS * Math.pow(2, attempt), RETRY_MAX_MS);
        const jitter = baseDelay * RETRY_JITTER * (Math.random() * 2 - 1);
        return Math.max(100, Math.round(baseDelay + jitter));
    }

    // --- Circuit Breaker ---
    function cbRecordSuccess() {
        _cbFailureCount = 0;
        _cbState = 'closed';
    }

    function cbRecordFailure() {
        _cbFailureCount++;
        if (_cbFailureCount >= CB_THRESHOLD) {
            _cbState = 'open';
            _cbOpenedAt = Date.now();
            console.warn('[SignalR] Circuit breaker OPEN after', _cbFailureCount, 'failures');
        }
    }

    function cbCanAttempt() {
        if (_cbState === 'closed') return true;
        if (_cbState === 'open') {
            if (Date.now() - _cbOpenedAt >= CB_OPEN_DURATION_MS) {
                _cbState = 'halfOpen';
                console.log('[SignalR] Circuit breaker half-open, allowing attempt');
                return true;
            }
            return false;
        }
        // halfOpen — allow one attempt
        return true;
    }

    function cbReset() {
        _cbFailureCount = 0;
        _cbState = 'closed';
        _cbOpenedAt = 0;
    }

    // --- SignalR Token ---
    async function getAccessToken() {
        try {
            const resp = await fetch(ChatApp.apiBase + '/api/auth/signalr-token', {
                method: 'GET',
                credentials: 'include'
            });
            if (resp.ok) {
                const data = await resp.json();
                return data.token || data.accessToken || '';
            }
        } catch (e) {
            console.error('[SignalR] Token fetch error:', e);
        }
        return '';
    }

    // --- Build Connection ---
    function buildConnection() {
        if (_connection) {
            try { _connection.stop(); } catch { }
        }

        _connection = new signalR.HubConnectionBuilder()
            .withUrl(ChatApp.apiBase + '/hubs/chat', {
                accessTokenFactory: getAccessToken,
                withCredentials: true
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: function (ctx) {
                    return getRetryDelay(ctx.previousRetryCount);
                }
            })
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        _connection.serverTimeoutInMilliseconds = 45000;
        _connection.keepAliveIntervalInMilliseconds = 15000;

        // --- Connection lifecycle events ---
        _connection.onreconnecting(function (error) {
            console.log('[SignalR] Reconnecting...', error ? error.message : '');
            emit('reconnecting');
            // Proactively refresh token for the SDK's next attempt
            getAccessToken().catch(function () { });
        });

        _connection.onreconnected(function (connectionId) {
            console.log('[SignalR] Reconnected:', connectionId);
            cbRecordSuccess();
            emit('reconnected');
            emit('connected');
            rejoinGroups();
        });

        _connection.onclose(function (error) {
            console.warn('[SignalR] Connection closed', error ? error.message : '');
            if (_isManuallyDisconnecting) return;
            cbRecordFailure();
            emit('disconnected');
            scheduleReconnect();
        });
    }

    // --- Group management (channels/conversations joined) ---
    let _joinedChannels = new Set();
    let _joinedConversations = new Set();

    async function rejoinGroups() {
        for (const channelId of _joinedChannels) {
            try { await _connection.invoke('JoinChannel', channelId); } catch { }
        }
        for (const convId of _joinedConversations) {
            try { await _connection.invoke('JoinConversation', convId); } catch { }
        }
    }

    // --- Manual reconnect with circuit breaker ---
    let _reconnectTimer = null;
    let _reconnectAttempt = 0;

    function scheduleReconnect() {
        if (_reconnectTimer) return;
        if (_isManuallyDisconnecting) return;
        if (!navigator.onLine) {
            console.log('[SignalR] Offline — skipping reconnect attempt');
            return;
        }
        if (!cbCanAttempt()) {
            const waitMs = CB_OPEN_DURATION_MS - (Date.now() - _cbOpenedAt);
            console.log('[SignalR] Circuit open, waiting', Math.round(waitMs / 1000), 's');
            _reconnectTimer = setTimeout(function () {
                _reconnectTimer = null;
                scheduleReconnect();
            }, Math.max(waitMs, 1000));
            return;
        }

        const delay = getRetryDelay(_reconnectAttempt);
        _reconnectAttempt++;
        console.log('[SignalR] Reconnect attempt', _reconnectAttempt, 'in', delay, 'ms');

        _reconnectTimer = setTimeout(async function () {
            _reconnectTimer = null;
            try {
                await startConnection();
                _reconnectAttempt = 0;
            } catch {
                scheduleReconnect();
            }
        }, delay);
    }

    // --- Start connection ---
    async function startConnection() {
        if (_isConnecting) return;
        if (_connection && _connection.state === signalR.HubConnectionState.Connected) return;

        _isConnecting = true;
        try {
            if (!_connection || _connection.state === signalR.HubConnectionState.Disconnected) {
                buildConnection();
                registerServerEvents();
            }
            await _connection.start();
            console.log('[SignalR] Connected');
            _lastActiveTime = Date.now();
            cbRecordSuccess();
            _reconnectAttempt = 0;
            emit('connected');
            startTokenRefresh();
            startHealthCheck();
        } catch (err) {
            console.error('[SignalR] Start failed:', err.message);
            cbRecordFailure();
            throw err;
        } finally {
            _isConnecting = false;
        }
    }

    // --- Token refresh timer ---
    function startTokenRefresh() {
        stopTokenRefresh();
        _tokenRefreshTimer = setInterval(async function () {
            if (_connection && _connection.state === signalR.HubConnectionState.Connected) {
                try {
                    await getAccessToken();  // Refreshes the token server-side
                } catch (e) {
                    console.warn('[SignalR] Token refresh failed:', e);
                }
            }
        }, TOKEN_REFRESH_MS);
    }

    function stopTokenRefresh() {
        if (_tokenRefreshTimer) {
            clearInterval(_tokenRefreshTimer);
            _tokenRefreshTimer = null;
        }
    }

    // --- Health check timer ---
    function startHealthCheck() {
        stopHealthCheck();
        _healthCheckTimer = setInterval(function () {
            if (_connection && _connection.state !== signalR.HubConnectionState.Connected) {
                console.log('[SignalR] Health check — not connected, attempting reconnect');
                scheduleReconnect();
            }
        }, HEALTH_CHECK_MS);
    }

    function stopHealthCheck() {
        if (_healthCheckTimer) {
            clearInterval(_healthCheckTimer);
            _healthCheckTimer = null;
        }
    }

    // --- Network Status API ---
    window.addEventListener('online', function () {
        console.log('[SignalR] Network online — setting circuit breaker to half-open');
        _cbState = 'halfOpen';
        _cbFailureCount = 0;
        if (_connection && _connection.state !== signalR.HubConnectionState.Connected) {
            scheduleReconnect();
        }
    });

    window.addEventListener('offline', function () {
        console.log('[SignalR] Network offline');
        emit('disconnected');
    });

    // --- Page Visibility API ---
    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            // Track when page became hidden
            return;
        }
        // Page became visible
        var elapsed = Date.now() - _lastActiveTime;
        _lastActiveTime = Date.now();

        if (_connection && _connection.state !== signalR.HubConnectionState.Connected) {
            // If hidden for >30s, treat as wake from sleep — refresh token first
            if (elapsed > 30000) {
                console.log('[SignalR] Page visible after', Math.round(elapsed / 1000), 's — refreshing token before reconnect');
                getAccessToken().then(function () {
                    _cbState = 'halfOpen';
                    _cbFailureCount = 0;
                    scheduleReconnect();
                }).catch(function () {
                    _cbState = 'halfOpen';
                    _cbFailureCount = 0;
                    scheduleReconnect();
                });
            } else {
                _cbState = 'halfOpen';
                _cbFailureCount = 0;
                scheduleReconnect();
            }
        }
    });

    // --- Typed event emitter ---
    function on(event, fn) {
        if (!_handlers[event]) _handlers[event] = [];
        _handlers[event].push(fn);
    }

    function off(event, fn) {
        if (!_handlers[event]) return;
        _handlers[event] = _handlers[event].filter(function (f) { return f !== fn; });
    }

    function emit(event, data) {
        if (!_handlers[event]) return;
        _handlers[event].forEach(function (fn) {
            try { fn(data); } catch (e) { console.error('[SignalR] Handler error:', event, e); }
        });
    }

    // --- Register all server → client events ---
    function registerServerEvents() {
        if (!_connection) return;

        // Presence
        _connection.on('UserOnline', function (userId) {
            ChatApp.state.addOnlineUser(userId);
            emit('userOnline', userId);
        });
        _connection.on('UserOffline', function (userId) {
            ChatApp.state.removeOnlineUser(userId);
            emit('userOffline', userId);
        });

        // Direct Messages
        _connection.on('NewDirectMessage', function (msg) { emit('newDirectMessage', msg); });
        _connection.on('DirectMessageEdited', function (msg) { emit('directMessageEdited', msg); });
        _connection.on('DirectMessageDeleted', function (msg) { emit('directMessageDeleted', msg); });
        _connection.on('MessageRead', function (convId, msgId, readBy) {
            emit('messageRead', { conversationId: convId, messageId: msgId, readByUserId: readBy });
        });
        _connection.on('DirectMessagePinned', function (msg) { emit('directMessagePinned', msg); });
        _connection.on('DirectMessageUnpinned', function (msg) { emit('directMessageUnpinned', msg); });
        _connection.on('DirectMessageReactionToggled', function (convId, msgId, reactions) {
            emit('directMessageReactionToggled', { conversationId: convId, messageId: msgId, reactions: reactions });
        });

        // Channel Messages
        _connection.on('NewChannelMessage', function (msg) { emit('newChannelMessage', msg); });
        _connection.on('ChannelMessageEdited', function (msg) { emit('channelMessageEdited', msg); });
        _connection.on('ChannelMessageDeleted', function (msg) { emit('channelMessageDeleted', msg); });
        _connection.on('ChannelMessagesRead', function (chId, userId, readCounts) {
            emit('channelMessagesRead', { channelId: chId, userId: userId, messageReadCounts: readCounts });
        });
        _connection.on('ChannelMessagePinned', function (msg) { emit('channelMessagePinned', msg); });
        _connection.on('ChannelMessageUnpinned', function (msg) { emit('channelMessageUnpinned', msg); });

        // Reactions
        _connection.on('ChannelReactionAdded', function (chId, msgId, userId, reaction) {
            emit('channelReactionAdded', { channelId: chId, messageId: msgId, userId: userId, reaction: reaction });
        });
        _connection.on('ChannelReactionRemoved', function (chId, msgId, userId, reaction) {
            emit('channelReactionRemoved', { channelId: chId, messageId: msgId, userId: userId, reaction: reaction });
        });
        _connection.on('ChannelMessageReactionsUpdated', function (msgId, reactions) {
            emit('channelMessageReactionsUpdated', { messageId: msgId, reactions: reactions });
        });

        // Typing
        _connection.on('UserTypingInChannel', function (chId, userId, fullName, isTyping) {
            emit('userTypingInChannel', { channelId: chId, userId: userId, fullName: fullName, isTyping: isTyping });
        });
        _connection.on('UserTypingInConversation', function (convId, userId, isTyping) {
            emit('userTypingInConversation', { conversationId: convId, userId: userId, isTyping: isTyping });
        });

        // Channel membership
        _connection.on('AddedToChannel', function (channel) {
            emit('addedToChannel', channel);
            // Auto-reload conversation list when added to a channel
            if (ChatApp.conversationList && ChatApp.conversationList.reload) {
                ChatApp.conversationList.reload();
            }
        });
        _connection.on('MemberLeftChannel', function (chId, leftUserId, leftUserFullName) {
            emit('memberLeftChannel', { channelId: chId, leftUserId: leftUserId, leftUserFullName: leftUserFullName });
        });
    }

    // --- Public API ---
    ChatApp.signalR = {
        initialize: async function () {
            if (_isInitialized) return;
            _isInitialized = true;
            try {
                await startConnection();
            } catch (err) {
                console.warn('[SignalR] Initial connection failed, will retry:', err.message);
                scheduleReconnect();
            }
        },

        disconnect: async function () {
            _isManuallyDisconnecting = true;
            _isInitialized = false;
            stopTokenRefresh();
            stopHealthCheck();
            if (_reconnectTimer) {
                clearTimeout(_reconnectTimer);
                _reconnectTimer = null;
            }
            if (_connection) {
                try { await _connection.stop(); } catch { }
            }
        },

        isConnected: function () {
            return _connection && _connection.state === signalR.HubConnectionState.Connected;
        },

        // Event subscriptions
        on: on,
        off: off,

        // Channel group management
        joinChannel: async function (channelId) {
            _joinedChannels.add(channelId);
            if (this.isConnected()) {
                try { await _connection.invoke('JoinChannel', channelId); } catch { }
            }
        },

        leaveChannel: async function (channelId) {
            _joinedChannels.delete(channelId);
            if (this.isConnected()) {
                try { await _connection.invoke('LeaveChannel', channelId); } catch { }
            }
        },

        joinConversation: async function (conversationId) {
            _joinedConversations.add(conversationId);
            if (this.isConnected()) {
                try { await _connection.invoke('JoinConversation', conversationId); } catch { }
            }
        },

        leaveConversation: async function (conversationId) {
            _joinedConversations.delete(conversationId);
            if (this.isConnected()) {
                try { await _connection.invoke('LeaveConversation', conversationId); } catch { }
            }
        },

        // Typing indicators
        sendTypingInChannel: function (channelId, isTyping) {
            if (this.isConnected()) {
                _connection.invoke('TypingInChannel', channelId, isTyping).catch(function () { });
            }
        },

        sendTypingInConversation: function (conversationId, recipientUserId, isTyping) {
            if (this.isConnected()) {
                _connection.invoke('TypingInConversation', conversationId, recipientUserId, isTyping).catch(function () { });
            }
        },

        // Online status query
        getOnlineStatus: function (userIds) {
            if (!this.isConnected() || !userIds || userIds.length === 0) return Promise.resolve({});
            return _connection.invoke('GetOnlineStatus', userIds).catch(function () { return {}; });
        },

        // Generic invoke
        invoke: function (method) {
            if (!this.isConnected()) return Promise.resolve(null);
            var args = Array.prototype.slice.call(arguments, 1);
            return _connection.invoke.apply(_connection, [method].concat(args));
        }
    };
})();
