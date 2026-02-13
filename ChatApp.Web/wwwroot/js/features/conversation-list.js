/**
 * Conversation List — replaces ConversationList.razor
 * Manages unified conversation/channel list, search, filters, infinite scroll
 */
(function () {
    'use strict';

    const listEl = document.getElementById('conversationList');
    const loadingEl = document.getElementById('convListLoading');
    const searchContainer = document.getElementById('convSearchContainer');
    const searchInput = document.getElementById('convSearchInput');
    const searchToggleBtn = document.getElementById('searchToggleBtn');
    const searchCloseBtn = document.getElementById('convSearchClose');
    const filterBtn = document.getElementById('filterBtn');
    const filterMenu = document.getElementById('filterMenu');
    const newChannelBtn = document.getElementById('newChannelBtn');

    if (!listEl) return;

    let _conversations = [];
    let _currentPage = 1;
    let _pageSize = 30;
    let _hasMore = true;
    let _isLoading = false;
    let _activeFilter = 'all';
    let _isSearchMode = false;
    let _isSearchLoading = false;
    let _searchResults = [];
    let _selectedConvId = null;
    let _selectedConvType = null; // 'dm' | 'channel'
    let _typingInConv = {}; // { convId: { userId: fullName, ... } }

    // --- Load conversations ---
    async function loadConversations(page, append) {
        if (_isLoading) return;
        _isLoading = true;
        if (!append) {
            showLoading();
        } else {
            showScrollSpinner();
        }

        let endpoint = '/api/unified-conversations?pageNumber=' + page + '&pageSize=' + _pageSize;
        if (_activeFilter !== 'all') endpoint += '&filter=' + _activeFilter;

        const result = await ChatApp.api.get(endpoint);
        _isLoading = false;
        hideLoading();
        hideScrollSpinner();

        if (result.isSuccess && result.value) {
            const items = result.value.items || result.value;
            const list = Array.isArray(items) ? items : [];

            if (append) {
                _conversations = _conversations.concat(list);
            } else {
                _conversations = list;
            }

            _hasMore = list.length >= _pageSize;
            _currentPage = page;
            renderConversations();

            // Set total unread count
            if (result.value.totalUnreadCount !== undefined) {
                ChatApp.state.setUnreadMessageCount(result.value.totalUnreadCount);
            }
        }
    }

    // --- Render conversations ---
    function renderConversations() {
        // Clear all dynamic items (items, headers, empties, spinners)
        var toRemove = listEl.querySelectorAll('.conversation-item, .conversation-list-empty, .search-section-header, .search-hint, .search-loading-spinner');
        toRemove.forEach(function (el) { el.remove(); });

        if (_isSearchMode) {
            renderSearchResults();
            return;
        }

        const items = _conversations;
        const fragment = document.createDocumentFragment();

        items.forEach(function (conv) {
            fragment.appendChild(createConvItem(conv));
        });

        listEl.appendChild(fragment);

        if (items.length === 0) {
            var empty = document.createElement('div');
            empty.className = 'conversation-list-empty';
            empty.innerHTML = '<span class="material-icons-outlined" style="font-size:48px;color:rgba(0,0,0,0.1);">chat</span>' +
                '<p>No conversations yet</p>' +
                '<button class="btn btn-primary btn-sm conv-start-chat-btn">Start a new chat</button>';
            empty.querySelector('.conv-start-chat-btn').addEventListener('click', function () {
                if (searchToggleBtn) searchToggleBtn.click();
            });
            listEl.appendChild(empty);
        }
    }

    function renderSearchResults() {
        var query = searchInput ? searchInput.value.trim() : '';
        var fragment = document.createDocumentFragment();

        // Show loading spinner while searching
        if (_isSearchLoading) {
            var spinner = document.createElement('div');
            spinner.className = 'search-loading-spinner';
            spinner.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"></div><span>Searching...</span>';
            fragment.appendChild(spinner);
            listEl.appendChild(fragment);
            return;
        }

        // Hint when query is too short
        if (query.length > 0 && query.length < 3) {
            var hint = document.createElement('div');
            hint.className = 'search-hint';
            hint.innerHTML = '<span class="material-icons-outlined" style="font-size:32px;color:rgba(0,0,0,0.15);">search</span>' +
                '<p>Enter at least 3 characters to search</p>';
            fragment.appendChild(hint);
            listEl.appendChild(fragment);
            return;
        }

        if (_searchResults.length === 0 && query.length >= 3) {
            var empty = document.createElement('div');
            empty.className = 'conversation-list-empty';
            empty.innerHTML = '<span class="material-icons-outlined" style="font-size:48px;color:rgba(0,0,0,0.1);">search_off</span><p>No results found</p>';
            fragment.appendChild(empty);
            listEl.appendChild(fragment);
            return;
        }

        // Separate into Channels and People
        var channels = _searchResults.filter(function (r) { return r.type === 'channel'; });
        var people = _searchResults.filter(function (r) { return r.type !== 'channel'; });

        if (channels.length > 0) {
            var chHeader = document.createElement('div');
            chHeader.className = 'search-section-header';
            chHeader.innerHTML = '<span>Channels</span><span class="search-section-count">' + channels.length + '</span>';
            fragment.appendChild(chHeader);
            channels.forEach(function (conv) {
                fragment.appendChild(createConvItem(conv));
            });
        }

        if (people.length > 0) {
            var pHeader = document.createElement('div');
            pHeader.className = 'search-section-header';
            pHeader.innerHTML = '<span>People</span><span class="search-section-count">' + people.length + '</span>';
            fragment.appendChild(pHeader);
            people.forEach(function (conv) {
                fragment.appendChild(createConvItem(conv));
            });
        }

        listEl.appendChild(fragment);
    }

    // --- Event delegation for conversation list ---
    // Single click/contextmenu/more-btn handlers on the list container
    // instead of per-item listeners (avoids O(N) listener attachment)
    listEl.addEventListener('click', function (e) {
        var moreBtn = e.target.closest('.conv-more-btn');
        if (moreBtn) {
            e.stopPropagation();
            var item = moreBtn.closest('.conversation-item');
            if (item) {
                var conv = _findConvById(item.dataset.id);
                if (conv) showConvContextMenu(e, conv);
            }
            return;
        }
        var item = e.target.closest('.conversation-item');
        if (!item) return;
        var conv = _findConvById(item.dataset.id);
        if (conv) selectConversation(conv);
    });

    listEl.addEventListener('contextmenu', function (e) {
        var item = e.target.closest('.conversation-item');
        if (!item) return;
        e.preventDefault();
        var conv = _findConvById(item.dataset.id);
        if (conv) showConvContextMenu(e, conv);
    });

    function _findConvById(id) {
        if (_isSearchMode) {
            return _searchResults.find(function (c) { return c.id === id; });
        }
        return _conversations.find(function (c) { return c.id === id; });
    }

    function createConvItem(conv) {
        const el = document.createElement('div');
        el.className = 'conversation-item' + (_selectedConvId === conv.id ? ' active' : '');
        el.dataset.id = conv.id;
        el.dataset.type = conv.type || (conv.channelId ? 'channel' : 'dm');

        const isChannel = el.dataset.type === 'channel';
        const currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
        const isNotes = !isChannel && conv.otherUserId && conv.otherUserId === currentUserId;
        const name = isNotes ? 'Notes' : (isChannel ? (conv.name || conv.channelName || '') : (conv.otherUserFullName || conv.displayName || ''));
        const avatarId = isChannel ? conv.id : (conv.otherUserId || conv.id);
        const avatarUrl = isChannel ? null : conv.otherUserAvatarUrl;
        const lastMsg = conv.lastMessageContent || conv.lastMessage || '';
        const lastTime = conv.lastMessageDate || conv.lastActivityDate || '';
        const unread = conv.unreadCount || 0;
        const isPinned = conv.isPinned || false;
        const isMuted = conv.isMuted || false;

        if (isNotes) {
            el.classList.add('notes-conversation');
        }

        let html = '<div class="conv-item-avatar">';
        if (isNotes) {
            html += '<div class="conv-channel-icon notes-icon" style="background-color:#6c63ff;">' +
                '<span class="material-icons" style="font-size:20px;">bookmark</span></div>';
        } else if (isChannel) {
            html += '<div class="conv-channel-icon" style="background-color:' + ChatApp.utils.getAvatarColor(avatarId) + ';">' +
                '<span class="material-icons" style="font-size:20px;">' + (conv.channelType === 1 ? 'lock' : 'tag') + '</span></div>';
        } else if (avatarUrl) {
            html += '<img src="' + ChatApp.utils.escapeHtml(avatarUrl) + '" alt="" class="conv-avatar-img" loading="lazy" />';
        } else {
            html += '<div class="conv-avatar-placeholder" style="background-color:' +
                ChatApp.utils.getAvatarColor(avatarId) + ';">' +
                ChatApp.utils.getInitials(name) + '</div>';
        }

        // Online dot (DM only, not for Notes)
        if (!isChannel && !isNotes && conv.otherUserId && ChatApp.state.isUserOnline(conv.otherUserId)) {
            html += '<span class="conv-online-dot"></span>';
        }
        html += '</div>';

        html += '<div class="conv-item-content">';
        html += '<div class="conv-item-top">';
        html += '<span class="conv-item-name">' + ChatApp.utils.escapeHtml(name) + '</span>';
        if (conv.lastReadLaterMessageId || conv.isReadLater) html += '<span class="material-icons conv-readlater-icon" style="font-size:14px;color:#fb8c00;">bookmark</span>';
        if (isPinned) html += '<span class="material-icons conv-pin-icon" style="font-size:14px;">push_pin</span>';
        if (isMuted) html += '<span class="material-icons conv-mute-icon" style="font-size:14px;">volume_off</span>';
        html += '<span class="conv-item-time">' + ChatApp.utils.formatRelativeTime(lastTime) + '</span>';
        html += '</div>';
        html += '<div class="conv-item-bottom">';

        // Typing indicator or draft or last message
        var typingNames = _typingInConv[conv.id] ? Object.values(_typingInConv[conv.id]) : [];
        if (typingNames.length > 0) {
            var typingText;
            if (typingNames.length === 1) {
                typingText = ChatApp.utils.escapeHtml(typingNames[0]) + ' is typing...';
            } else if (typingNames.length === 2) {
                typingText = ChatApp.utils.escapeHtml(typingNames[0]) + ' and ' +
                    ChatApp.utils.escapeHtml(typingNames[1]) + ' are typing...';
            } else {
                typingText = typingNames.length + ' people are typing...';
            }
            html += '<span class="conv-item-preview typing-preview"><em>' + typingText + '</em></span>';
        } else {
            var draft = sessionStorage.getItem('chatDraft_' + conv.id);
            if (draft) {
                html += '<span class="conv-item-preview draft-preview"><span class="material-icons" style="font-size:12px;color:#f44336;vertical-align:middle;">edit</span> <span style="color:#f44336;">Draft:</span> ' +
                    ChatApp.utils.escapeHtml(ChatApp.utils.truncateText(draft, 30)) + '</span>';
            } else {
                var previewHtml = '';
                // Message status icons for current user's sent messages
                if (currentUserId && conv.lastMessageSenderId === currentUserId) {
                    var msgStatus = conv.lastMessageStatus;
                    if (msgStatus === 'Read' || msgStatus === 2) {
                        previewHtml += '<span class="material-icons conv-msg-status" style="font-size:14px;color:#4fc3f7;vertical-align:middle;">done_all</span> ';
                    } else if (msgStatus === 'Delivered' || msgStatus === 1) {
                        previewHtml += '<span class="material-icons conv-msg-status" style="font-size:14px;color:rgba(0,0,0,0.4);vertical-align:middle;">done_all</span> ';
                    } else {
                        previewHtml += '<span class="material-icons conv-msg-status" style="font-size:14px;color:rgba(0,0,0,0.4);vertical-align:middle;">done</span> ';
                    }
                }
                // Sender avatar in channel preview
                if (isChannel && conv.lastMessageSenderAvatarUrl) {
                    previewHtml += '<img src="' + ChatApp.utils.escapeHtml(conv.lastMessageSenderAvatarUrl) + '" alt="" class="conv-preview-sender-avatar" style="width:16px;height:16px;border-radius:50%;vertical-align:middle;margin-right:4px;" loading="lazy" /> ';
                } else if (isChannel && conv.lastMessageSenderName) {
                    previewHtml += '<span class="conv-preview-sender-initial" style="display:inline-block;width:16px;height:16px;border-radius:50%;background:' + ChatApp.utils.getAvatarColor(conv.lastMessageSenderId || conv.id) + ';color:#fff;font-size:9px;line-height:16px;text-align:center;vertical-align:middle;margin-right:4px;">' + ChatApp.utils.getInitials(conv.lastMessageSenderName) + '</span> ';
                }
                previewHtml += ChatApp.utils.escapeHtml(ChatApp.utils.truncateText(lastMsg, 50));
                html += '<span class="conv-item-preview">' + previewHtml + '</span>';
            }
        }

        if (conv.hasUnreadMentions) {
            html += '<span class="conv-mention-badge">@</span>';
        }
        if (unread > 0) {
            html += '<span class="conv-unread-badge' + (isMuted ? ' muted' : '') + '">' + (unread > 99 ? '99+' : unread) + '</span>';
        }
        html += '</div></div>';

        // More button included in HTML (no per-item listener needed — delegation handles it)
        html += '<button class="conv-more-btn"><span class="material-icons" style="font-size:16px;">more_vert</span></button>';

        el.innerHTML = html;

        return el;
    }

    // --- Select conversation ---
    function selectConversation(conv) {
        const type = conv.type || (conv.channelId ? 'channel' : 'dm');
        _selectedConvId = conv.id;
        _selectedConvType = type;

        // Highlight active
        listEl.querySelectorAll('.conversation-item').forEach(function (el) {
            el.classList.toggle('active', el.dataset.id === conv.id);
        });

        // Notify chat area
        ChatApp.state.emit('conversationSelected', {
            id: conv.id,
            type: type,
            conv: conv
        });
    }

    // --- Context menu ---
    let _contextMenu = null;
    function showConvContextMenu(e, conv) {
        hideConvContextMenu();
        const menu = document.createElement('div');
        menu.className = 'conv-context-menu';
        menu.style.position = 'fixed';
        menu.style.left = e.clientX + 'px';
        menu.style.top = e.clientY + 'px';
        menu.style.zIndex = '1000';

        const isChannel = conv.type === 'channel' || !!conv.channelId;

        var menuHtml = '<button data-action="pin"><span class="material-icons" style="font-size:16px;">push_pin</span>' +
            (conv.isPinned ? 'Unpin' : 'Pin') + '</button>' +
            '<button data-action="mute"><span class="material-icons" style="font-size:16px;">volume_off</span>' +
            (conv.isMuted ? 'Unmute' : 'Mute') + '</button>' +
            '<button data-action="markRead"><span class="material-icons" style="font-size:16px;">done_all</span>Mark as read</button>' +
            '<button data-action="readLater"><span class="material-icons" style="font-size:16px;">bookmark_border</span>Mark to read later</button>';

        if (!isChannel && conv.otherUserId) {
            menuHtml += '<button data-action="viewProfile"><span class="material-icons" style="font-size:16px;">person</span>View profile</button>';
            menuHtml += '<button data-action="findChats"><span class="material-icons" style="font-size:16px;">forum</span>Find chats with this user</button>';
        }

        menuHtml += '<button data-action="hide"><span class="material-icons" style="font-size:16px;">visibility_off</span>Hide</button>';
        if (isChannel) {
            menuHtml += '<button data-action="leave"><span class="material-icons" style="font-size:16px;">exit_to_app</span>Leave channel</button>';
        }
        menuHtml += '<button data-action="close"><span class="material-icons" style="font-size:16px;">close</span>Close conversation</button>';

        menu.innerHTML = menuHtml;

        menu.addEventListener('click', function (ev) {
            const action = ev.target.closest('button')?.dataset.action;
            if (!action) return;
            handleConvAction(action, conv);
            hideConvContextMenu();
        });

        document.body.appendChild(menu);
        _contextMenu = menu;

        setTimeout(function () {
            document.addEventListener('click', hideConvContextMenu, { once: true });
        }, 0);
    }

    function hideConvContextMenu() {
        if (_contextMenu) {
            _contextMenu.remove();
            _contextMenu = null;
        }
    }

    async function handleConvAction(action, conv) {
        const isChannel = conv.type === 'channel' || !!conv.channelId;
        // DM endpoints are under /api/conversations/{id}/messages/...
        // Channel endpoints are under /api/channels/{id}/...
        const dmBase = '/api/conversations/' + conv.id + '/messages/';
        const chBase = '/api/channels/' + conv.id + '/';

        if (action === 'pin') {
            await ChatApp.api.post((isChannel ? chBase : dmBase) + 'toggle-pin');
            conv.isPinned = !conv.isPinned;
            renderConversations();
        } else if (action === 'mute') {
            await ChatApp.api.post((isChannel ? chBase : dmBase) + 'toggle-mute');
            conv.isMuted = !conv.isMuted;
            renderConversations();
        } else if (action === 'markRead') {
            await ChatApp.api.post((isChannel ? chBase : dmBase) + 'mark-as-read');
            conv.unreadCount = 0;
            renderConversations();
        } else if (action === 'leave') {
            if (confirm('Are you sure you want to leave this channel?')) {
                await ChatApp.api.post('/api/channels/' + conv.id + '/leave');
                loadConversations(1, false);
            }
        } else if (action === 'readLater') {
            await ChatApp.api.post((isChannel ? chBase : dmBase) + 'toggle-read-later');
            conv.isReadLater = !conv.isReadLater;
            renderConversations();
        } else if (action === 'viewProfile') {
            if (!isChannel && conv.otherUserId && ChatApp.profilePanel) {
                ChatApp.profilePanel.open(conv.otherUserId);
            }
        } else if (action === 'findChats') {
            if (!isChannel && conv.otherUserId) {
                ChatApp.state.emit('findChatsWithUser', { userId: conv.otherUserId, fullName: conv.otherUserFullName || conv.displayName || '' });
            }
        } else if (action === 'hide') {
            await ChatApp.api.post((isChannel ? chBase : dmBase) + 'hide');
            _conversations = _conversations.filter(function(c) { return c.id !== conv.id; });
            renderConversations();
        }
    }

    // --- Search mode ---
    if (searchToggleBtn) {
        searchToggleBtn.addEventListener('click', function () {
            _isSearchMode = true;
            searchContainer.style.display = '';
            searchInput.focus();
        });
    }

    if (searchCloseBtn) {
        searchCloseBtn.addEventListener('click', function () {
            _isSearchMode = false;
            searchContainer.style.display = 'none';
            searchInput.value = '';
            _searchResults = [];
            renderConversations();
        });
    }

    if (searchInput) {
        searchInput.addEventListener('input', ChatApp.utils.debounce(async function () {
            const query = searchInput.value.trim();
            if (query.length < 3) {
                _searchResults = [];
                _isSearchLoading = false;
                renderConversations();
                return;
            }
            _isSearchLoading = true;
            renderConversations();

            const result = await ChatApp.api.get('/api/identity/users/search?query=' + encodeURIComponent(query));
            if (result.isSuccess) {
                _searchResults = (result.value || []).map(function (u) {
                    return {
                        id: u.conversationId || u.id,
                        type: 'dm',
                        otherUserId: u.id,
                        otherUserFullName: u.fullName || ((u.firstName || '') + ' ' + (u.lastName || '')).trim(),
                        otherUserAvatarUrl: u.avatarUrl,
                        lastMessageContent: u.email || '',
                        unreadCount: 0,
                        _isSearchResult: true,
                        _user: u
                    };
                });
            }
            // Also search channels
            const chResult = await ChatApp.api.get('/api/channels/search?query=' + encodeURIComponent(query));
            if (chResult.isSuccess && chResult.value) {
                const channels = (chResult.value || []).map(function (ch) {
                    return {
                        id: ch.id,
                        type: 'channel',
                        name: ch.name,
                        channelType: ch.channelType,
                        lastMessageContent: ch.description || '',
                        unreadCount: 0,
                        _isSearchResult: true
                    };
                });
                _searchResults = _searchResults.concat(channels);
            }
            _isSearchLoading = false;
            renderConversations();
        }, 300));
    }

    // --- Filter ---
    if (filterBtn) {
        filterBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            filterMenu.style.display = filterMenu.style.display === 'none' ? '' : 'none';
        });
    }

    document.addEventListener('click', function () {
        if (filterMenu) filterMenu.style.display = 'none';
    });

    if (filterMenu) {
        // Append "Mark all as read" button to filter menu
        var markAllReadBtn = document.createElement('button');
        markAllReadBtn.className = 'filter-item mark-all-read-btn';
        markAllReadBtn.innerHTML = '<span class="material-icons" style="font-size:16px;vertical-align:middle;">done_all</span> Mark all as read';
        markAllReadBtn.addEventListener('click', async function (e) {
            e.stopPropagation();
            filterMenu.style.display = 'none';
            // Mark all as read for each conversation/channel individually
            var promises = _conversations.filter(function (c) { return c.unreadCount > 0; }).map(function (c) {
                if (c.type === 'channel') {
                    return ChatApp.api.post('/api/channels/' + c.id + '/messages/mark-all-read');
                } else if (c.type !== 'departmentUser') {
                    return ChatApp.api.post('/api/conversations/' + c.id + '/messages/mark-all-read');
                }
                return Promise.resolve();
            });
            await Promise.allSettled(promises);
            _conversations.forEach(function (c) { c.unreadCount = 0; c.hasUnreadMentions = false; });
            ChatApp.state.setUnreadMessageCount(0);
            renderConversations();
        });
        filterMenu.appendChild(markAllReadBtn);

        filterMenu.addEventListener('click', function (e) {
            e.stopPropagation();
            const btn = e.target.closest('.filter-item:not(.mark-all-read-btn)');
            if (!btn) return;
            _activeFilter = btn.dataset.filter;
            filterMenu.querySelectorAll('.filter-item:not(.mark-all-read-btn)').forEach(function (f) { f.classList.remove('active'); });
            btn.classList.add('active');
            filterMenu.style.display = 'none';
            _currentPage = 1;
            loadConversations(1, false);
        });
    }

    // --- Infinite scroll ---
    if (listEl) {
        listEl.addEventListener('scroll', ChatApp.utils.throttle(function () {
            if (!_hasMore || _isLoading || _isSearchMode) return;
            const scrollBottom = listEl.scrollHeight - listEl.scrollTop - listEl.clientHeight;
            if (scrollBottom < 100) {
                loadConversations(_currentPage + 1, true);
            }
        }, 200));
    }

    // --- New channel (permission gated) ---
    if (newChannelBtn) {
        // Check if current user has channel creation permission
        ChatApp.api.get('/api/channels/can-create').then(function (result) {
            if (result.isSuccess && result.value === false) {
                newChannelBtn.style.display = 'none';
            }
        });
        newChannelBtn.addEventListener('click', function () {
            if (ChatApp.createGroup) {
                ChatApp.createGroup.open();
            }
        });
    }

    // --- SignalR events ---
    ChatApp.signalR.on('newDirectMessage', function (msg) {
        updateConvLastMessage(msg.conversationId || msg.directConversationId, msg, 'dm');
    });

    ChatApp.signalR.on('newChannelMessage', function (msg) {
        updateConvLastMessage(msg.channelId, msg, 'channel');
    });

    function updateConvLastMessage(convId, msg, type) {
        const conv = _conversations.find(function (c) { return c.id === convId; });
        if (conv) {
            conv.lastMessageContent = msg.content || '';
            conv.lastMessageDate = msg.createdAt || msg.sentAt || new Date().toISOString();
            // Increment unread if not the active conversation
            if (_selectedConvId !== convId) {
                conv.unreadCount = (conv.unreadCount || 0) + 1;
            }
            // Move to top
            const idx = _conversations.indexOf(conv);
            if (idx > 0) {
                _conversations.splice(idx, 1);
                _conversations.unshift(conv);
            }
            renderConversations();
        } else {
            // New conversation — reload
            loadConversations(1, false);
        }
    }

    ChatApp.signalR.on('addedToChannel', function () {
        loadConversations(1, false);
    });

    // Typing indicators in conversation list
    ChatApp.signalR.on('userTypingInChannel', function (data) {
        handleConvTyping(data.channelId, data.userId, data.fullName, data.isTyping);
    });
    ChatApp.signalR.on('userTypingInConversation', function (data) {
        handleConvTyping(data.conversationId, data.userId, null, data.isTyping);
    });

    function handleConvTyping(convId, userId, fullName, isTyping) {
        if (userId === (ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null)) return;
        if (!_typingInConv[convId]) _typingInConv[convId] = {};
        if (isTyping) {
            _typingInConv[convId][userId] = fullName || 'Someone';
            // Auto-clear after 4 seconds
            setTimeout(function () {
                if (_typingInConv[convId]) {
                    delete _typingInConv[convId][userId];
                    if (Object.keys(_typingInConv[convId]).length === 0) delete _typingInConv[convId];
                    renderConversations();
                }
            }, 4000);
        } else {
            delete _typingInConv[convId][userId];
            if (Object.keys(_typingInConv[convId]).length === 0) delete _typingInConv[convId];
        }
        renderConversations();
    }

    // Online status changes — debounced to avoid excessive re-renders
    // when multiple users come online/offline simultaneously
    ChatApp.state.on('onlineUsersChanged', ChatApp.utils.debounce(function () {
        renderConversations();
    }, 500));

    // --- Utility ---
    function showLoading() { if (loadingEl) loadingEl.style.display = ''; }
    function hideLoading() { if (loadingEl) loadingEl.style.display = 'none'; }

    function showScrollSpinner() {
        hideScrollSpinner();
        var spinner = document.createElement('div');
        spinner.className = 'conv-scroll-spinner';
        spinner.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"></div>';
        listEl.appendChild(spinner);
    }
    function hideScrollSpinner() {
        var existing = listEl.querySelector('.conv-scroll-spinner');
        if (existing) existing.remove();
    }

    // --- Public API ---
    ChatApp.conversationList = {
        reload: function () { return loadConversations(1, false); },
        getSelectedId: function () { return _selectedConvId; },
        getSelectedType: function () { return _selectedConvType; },
        selectById: function (id) {
            const conv = _conversations.find(function (c) { return c.id === id; });
            if (conv) selectConversation(conv);
        }
    };

    // --- Initial load ---
    loadConversations(1, false);

    // Check for pending chat user
    const pendingUserId = ChatApp.state.consumePendingChatUserId();
    if (pendingUserId) {
        // Create or open DM with this user
        ChatApp.api.post('/api/conversations', { otherUserId: pendingUserId })
            .then(function (result) {
                if (result.isSuccess && result.value) {
                    loadConversations(1, false).then(function () {
                        ChatApp.conversationList.selectById(result.value.id || result.value);
                    });
                }
            });
    }

})();
