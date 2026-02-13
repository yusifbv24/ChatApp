/**
 * Forwarding — replaces Messages.Forwarding.cs
 * Forward message dialog with conversation/channel search
 */
(function () {
    'use strict';

    const forwardModal = document.getElementById('forwardModal');
    const forwardSearch = document.getElementById('forwardSearch');
    const forwardList = document.getElementById('forwardList');

    if (!forwardModal) return;

    let _bsModal = null;
    let _messagesToForward = [];
    let _targets = [];
    let _channelSearchTargets = [];

    function showForwardDialog(msgOrMsgs) {
        _messagesToForward = Array.isArray(msgOrMsgs) ? msgOrMsgs : [msgOrMsgs];
        if (!_bsModal) _bsModal = new bootstrap.Modal(forwardModal);
        _bsModal.show();
        forwardSearch.value = '';
        loadTargets('');
    }

    async function loadTargets(query) {
        forwardList.innerHTML = '<div class="text-center py-3"><div class="spinner-border spinner-border-sm text-secondary"></div></div>';

        var targets = [];

        // Load conversations
        var endpoint = '/api/unified-conversations?pageNumber=1&pageSize=20';
        if (query) endpoint += '&search=' + encodeURIComponent(query);
        var result = await ChatApp.api.get(endpoint);
        if (result.isSuccess && result.value) {
            targets = result.value.items || result.value || [];
        }

        // Also search users if query provided (for forwarding to new users)
        if (query && query.length >= 2) {
            var userResult = await ChatApp.api.get('/api/identity/users/search?query=' + encodeURIComponent(query));
            if (userResult.isSuccess && userResult.value) {
                var existingUserIds = targets.map(function(t) { return t.otherUserId; }).filter(Boolean);
                var newUsers = (userResult.value || []).filter(function(u) {
                    return existingUserIds.indexOf(u.id) === -1 && u.id !== (ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null);
                }).map(function(u) {
                    return {
                        id: null,
                        type: 'dm',
                        otherUserId: u.id,
                        otherUserFullName: u.fullName || ((u.firstName || '') + ' ' + (u.lastName || '')).trim(),
                        otherUserAvatarUrl: u.avatarUrl,
                        _isNewUser: true
                    };
                });
                targets = targets.concat(newUsers);
            }
        }

        // Sort Notes (self-conversation) to the top
        var currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
        targets.sort(function (a, b) {
            var aIsNotes = a.otherUserId === currentUserId && a.type !== 'channel';
            var bIsNotes = b.otherUserId === currentUserId && b.type !== 'channel';
            if (aIsNotes && !bIsNotes) return -1;
            if (!aIsNotes && bIsNotes) return 1;
            return 0;
        });

        // Also search channels if query provided (Gap #51)
        var channelTargets = [];
        if (query && query.length >= 2) {
            var channelResult = await ChatApp.api.get('/api/channels/search?query=' + encodeURIComponent(query));
            if (channelResult.isSuccess && channelResult.value) {
                var existingChannelIds = targets.filter(function(t) { return t.type === 'channel' || !!t.channelId; }).map(function(t) { return t.id || t.channelId; });
                channelTargets = (channelResult.value || []).filter(function(ch) {
                    return existingChannelIds.indexOf(ch.id) === -1;
                }).map(function(ch) {
                    return {
                        id: ch.id,
                        type: 'channel',
                        name: ch.name,
                        channelName: ch.name,
                        channelType: ch.channelType,
                        _isSearchedChannel: true
                    };
                });
            }
        }

        _targets = targets;
        _channelSearchTargets = channelTargets;
        renderTargets();
    }

    function renderForwardItem(conv) {
        const isChannel = conv.type === 'channel' || !!conv.channelId;
        const name = isChannel ? (conv.name || conv.channelName || '') : (conv.otherUserFullName || conv.displayName || '');
        const avatarId = isChannel ? conv.id : (conv.otherUserId || conv.id);

        const el = document.createElement('div');
        el.className = 'forward-item';

        let avatarHtml;
        if (isChannel) {
            avatarHtml = '<div class="conv-channel-icon" style="background-color:' + ChatApp.utils.getAvatarColor(avatarId) +
                ';width:36px;height:36px;border-radius:50%;display:flex;align-items:center;justify-content:center;">' +
                '<span class="material-icons" style="font-size:16px;color:#fff;">' +
                (conv.channelType === 1 ? 'lock' : 'tag') + '</span></div>';
        } else {
            avatarHtml = ChatApp.utils.renderAvatar(
                { id: avatarId, avatarUrl: conv.otherUserAvatarUrl, fullName: name }, 'forward-avatar-img');
        }

        el.innerHTML = '<div class="forward-item-avatar">' + avatarHtml + '</div>' +
            '<div class="forward-item-name">' + ChatApp.utils.escapeHtml(name) + '</div>';

        if (conv._isNewUser) {
            el.innerHTML += '<span class="forward-new-badge">New</span>';
        }

        el.addEventListener('click', function () {
            forwardMessage(conv);
        });

        return el;
    }

    function renderTargets() {
        forwardList.innerHTML = '';
        if (_targets.length === 0 && _channelSearchTargets.length === 0) {
            forwardList.innerHTML = '<div class="text-center py-3 text-muted">No conversations found</div>';
            return;
        }

        _targets.forEach(function (conv) {
            forwardList.appendChild(renderForwardItem(conv));
        });

        // Render searched channels in a separate section
        if (_channelSearchTargets.length > 0) {
            var header = document.createElement('div');
            header.className = 'forward-section-header';
            header.textContent = 'Channels';
            forwardList.appendChild(header);

            _channelSearchTargets.forEach(function (ch) {
                forwardList.appendChild(renderForwardItem(ch));
            });
        }
    }

    async function forwardMessage(targetConv) {
        if (_messagesToForward.length === 0) return;

        var convId = targetConv.id;
        var isChannel = targetConv.type === 'channel' || !!targetConv.channelId;

        // Create DM if forwarding to a new user
        if (targetConv._isNewUser && targetConv.otherUserId) {
            var createResult = await ChatApp.api.post('/api/conversations', { otherUserId: targetConv.otherUserId });
            if (createResult.isSuccess && createResult.value) {
                convId = createResult.value.id || createResult.value;
            } else {
                return;
            }
        }

        var endpoint = isChannel
            ? '/api/channels/' + convId + '/messages'
            : '/api/conversations/' + convId + '/messages';

        for (var i = 0; i < _messagesToForward.length; i++) {
            var msg = _messagesToForward[i];
            await ChatApp.api.post(endpoint, {
                content: msg.content || '',
                forwardedFromMessageId: msg.id
            });
        }

        if (_bsModal) _bsModal.hide();
        _messagesToForward = [];
    }

    // Search targets
    if (forwardSearch) {
        forwardSearch.addEventListener('input', ChatApp.utils.debounce(function () {
            loadTargets(forwardSearch.value.trim());
        }, 300));
    }

    // --- Public API ---
    ChatApp.forwarding = {
        showForwardDialog: showForwardDialog
    };

})();
