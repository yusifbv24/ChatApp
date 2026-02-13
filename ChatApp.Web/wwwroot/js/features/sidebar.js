/**
 * Sidebar — replaces Sidebar.razor
 * Right panel showing conversation/channel info, favorites, files, members
 */
(function () {
    'use strict';

    const rightPanel = document.getElementById('rightPanel');
    const sidebarPanel = document.getElementById('sidebarPanel');
    const sidebarContent = document.getElementById('sidebarContent');
    const sidebarTitle = document.getElementById('sidebarTitle');
    const sidebarCloseBtn = document.getElementById('sidebarCloseBtn');

    if (!sidebarPanel) return;

    let _isOpen = false;
    let _convId = null;
    let _convType = null;
    let _conv = null;
    let _activeTab = 'info';
    let _favCount = 0;
    let _fileCount = 0;
    let _linkCount = 0;
    let _channelData = null;

    function toggle(convId, convType, conv) {
        if (_isOpen && _convId === convId) {
            close();
        } else {
            open(convId, convType, conv);
        }
    }

    function open(convId, convType, conv) {
        _convId = convId;
        _convType = convType;
        _conv = conv;
        _isOpen = true;
        _activeTab = 'info';

        rightPanel.style.display = '';
        sidebarPanel.style.display = '';
        // Hide search panel if open
        const searchPanel = document.getElementById('searchPanel');
        if (searchPanel) searchPanel.style.display = 'none';

        sidebarTitle.textContent = convType === 'channel' ? 'Channel Info' : 'Conversation Info';
        renderSidebarHeaderMenu();
        loadContent();
    }

    function close() {
        _isOpen = false;
        sidebarPanel.style.display = 'none';
        rightPanel.style.display = 'none';
    }

    if (sidebarCloseBtn) {
        sidebarCloseBtn.addEventListener('click', close);
    }

    async function loadContent() {
        sidebarContent.innerHTML = '<div class="sidebar-loading"><div class="spinner-border spinner-border-sm text-secondary"></div></div>';

        const isChannel = _convType === 'channel';
        let html = '';

        // Tabs
        html += '<div class="sidebar-tabs">' +
            '<button class="sidebar-tab active" data-tab="info">Info</button>' +
            '<button class="sidebar-tab" data-tab="favorites">Favorites<span class="sidebar-tab-badge" id="badgeFavorites" style="display:none;"></span></button>' +
            '<button class="sidebar-tab" data-tab="files">Files<span class="sidebar-tab-badge" id="badgeFiles" style="display:none;"></span></button>' +
            '<button class="sidebar-tab" data-tab="links">Links<span class="sidebar-tab-badge" id="badgeLinks" style="display:none;"></span></button>';
        if (isChannel) html += '<button class="sidebar-tab" data-tab="members">Members<span class="sidebar-tab-badge" id="badgeMembers" style="display:none;"></span></button>';
        html += '</div>';

        // Info tab
        html += '<div class="sidebar-tab-content" id="sidebarTabInfo">';
        if (isChannel) {
            const result = await ChatApp.api.get('/api/channels/' + _convId);
            if (result.isSuccess && result.value) {
                const ch = result.value;
                _channelData = ch;
                html += '<div class="sidebar-info-section">';
                html += '<div class="sidebar-info-avatar">' +
                    '<div class="conv-channel-icon" style="background-color:' + ChatApp.utils.getAvatarColor(ch.id) +
                    ';width:64px;height:64px;border-radius:50%;display:flex;align-items:center;justify-content:center;">' +
                    '<span class="material-icons" style="font-size:28px;color:#fff;">' +
                    (ch.channelType === 1 ? 'lock' : 'tag') + '</span></div></div>';
                html += '<div class="sidebar-info-name">' + ChatApp.utils.escapeHtml(ch.name || '') + '</div>';
                if (ch.description) html += '<div class="sidebar-info-desc">' + ChatApp.utils.escapeHtml(ch.description) + '</div>';
                html += '<div class="sidebar-info-meta">' +
                    '<span class="material-icons" style="font-size:16px;">people</span> ' +
                    (ch.memberCount || 0) + ' members</div>';
                var chMuted = ch.isMuted || (_conv && _conv.isMuted) || false;
                html += '<label class="sidebar-mute-toggle">' +
                    '<input type="checkbox" id="sidebarMuteToggle"' + (chMuted ? ' checked' : '') + ' />' +
                    '<span class="sidebar-mute-slider"></span>' +
                    '<span class="sidebar-mute-label">Mute notifications</span></label>';
                html += '</div>';
            }
        } else if (_conv) {
            const name = _conv.otherUserFullName || _conv.displayName || '';
            const userId = _conv.otherUserId || _conv.id;
            html += '<div class="sidebar-info-section">';
            html += '<div class="sidebar-info-avatar" style="cursor:pointer;" onclick="ChatApp.profilePanel.open(\'' + userId + '\')">' +
                ChatApp.utils.renderAvatar({
                    id: userId, avatarUrl: _conv.otherUserAvatarUrl, fullName: name
                }, 'sidebar-avatar-img') + '</div>';
            html += '<div class="sidebar-info-name">' + ChatApp.utils.escapeHtml(name) + '</div>';
            const isOnline = ChatApp.state.isUserOnline(userId);
            html += '<div class="sidebar-info-meta">' +
                '<span class="sidebar-status-dot ' + (isOnline ? 'online' : '') + '"></span> ' +
                (isOnline ? 'Online' : 'Offline') + '</div>';
            var dmMuted = _conv.isMuted || false;
            html += '<label class="sidebar-mute-toggle">' +
                '<input type="checkbox" id="sidebarMuteToggle"' + (dmMuted ? ' checked' : '') + ' />' +
                '<span class="sidebar-mute-slider"></span>' +
                '<span class="sidebar-mute-label">Mute notifications</span></label>';
            html += '</div>';
        }
        html += '</div>';

        // Favorites tab
        html += '<div class="sidebar-tab-content" id="sidebarTabFavorites" style="display:none;">' +
            '<div class="sidebar-loading-tab" id="favoritesLoading"><div class="spinner-border spinner-border-sm text-secondary"></div></div></div>';

        // Files tab
        html += '<div class="sidebar-tab-content" id="sidebarTabFiles" style="display:none;">' +
            '<div class="sidebar-loading-tab" id="filesLoading"><div class="spinner-border spinner-border-sm text-secondary"></div></div></div>';

        // Links tab
        html += '<div class="sidebar-tab-content" id="sidebarTabLinks" style="display:none;">' +
            '<div class="sidebar-loading-tab" id="linksLoading"><div class="spinner-border spinner-border-sm text-secondary"></div></div></div>';

        // Members tab (channel only)
        if (isChannel) {
            html += '<div class="sidebar-tab-content" id="sidebarTabMembers" style="display:none;">' +
                '<div class="sidebar-loading-tab" id="membersLoading"><div class="spinner-border spinner-border-sm text-secondary"></div></div></div>';
        }

        // Chats With User panel (hidden by default)
        html += '<div class="sidebar-tab-content" id="sidebarChatsWithUser" style="display:none;">' +
            '<div class="sidebar-loading-tab" id="chatsWithUserLoading"><div class="spinner-border spinner-border-sm text-secondary"></div></div></div>';

        sidebarContent.innerHTML = html;

        // Tab switching
        sidebarContent.querySelectorAll('.sidebar-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                _activeTab = tab.dataset.tab;
                sidebarContent.querySelectorAll('.sidebar-tab').forEach(function (t) { t.classList.remove('active'); });
                tab.classList.add('active');
                sidebarContent.querySelectorAll('.sidebar-tab-content').forEach(function (c) { c.style.display = 'none'; });
                var tabId = 'sidebarTab' + _activeTab.charAt(0).toUpperCase() + _activeTab.slice(1);
                var content = document.getElementById(tabId);
                if (content) {
                    content.style.display = '';
                    loadTabContent(_activeTab);
                }
            });
        });

        // Mute toggle event
        var muteToggle = document.getElementById('sidebarMuteToggle');
        if (muteToggle) {
            muteToggle.addEventListener('change', async function () {
                var base = isChannel ? '/api/channels/' : '/api/conversations/';
                await ChatApp.api.post(base + _convId + '/toggle-mute');
                if (_conv) _conv.isMuted = muteToggle.checked;
            });
        }

        // Fetch badge counts asynchronously
        fetchBadgeCounts();
    }

    async function loadTabContent(tab) {
        const isChannel = _convType === 'channel';

        if (tab === 'favorites') {
            const container = document.getElementById('sidebarTabFavorites');
            const endpoint = isChannel
                ? '/api/channels/' + _convId + '/messages/favorites'
                : '/api/conversations/' + _convId + '/messages/favorites';
            const result = await ChatApp.api.get(endpoint);
            if (result.isSuccess) {
                const items = result.value || [];
                _favCount = items.length;
                updateBadge('badgeFavorites', _favCount);
                if (items.length === 0) {
                    container.innerHTML = '<div class="sidebar-empty"><span class="material-icons-outlined" style="font-size:36px;color:rgba(0,0,0,0.1);">star_outline</span><p>No favorites yet</p></div>';
                } else {
                    container.innerHTML = '';
                    // Use DocumentFragment for batch insertion
                    var fragment = document.createDocumentFragment();
                    var grouped = groupByDate(items);
                    Object.keys(grouped).forEach(function (dateLabel) {
                        var header = document.createElement('div');
                        header.className = 'sidebar-date-group-header';
                        header.textContent = dateLabel;
                        fragment.appendChild(header);
                        grouped[dateLabel].forEach(function (msg) {
                            fragment.appendChild(createFavoriteMessageItem(msg));
                        });
                    });
                    container.appendChild(fragment);
                }
            }
        } else if (tab === 'links') {
            const container = document.getElementById('sidebarTabLinks');
            const endpoint = isChannel
                ? '/api/channels/' + _convId + '/messages?page=1&pageSize=100'
                : '/api/conversations/' + _convId + '/messages?page=1&pageSize=100';
            const result = await ChatApp.api.get(endpoint);
            if (result.isSuccess) {
                var allMsgs = result.value?.items || result.value || [];
                var linkRegex = /https?:\/\/[^\s<>"']+/gi;
                var links = [];
                allMsgs.forEach(function (msg) {
                    if (!msg.content) return;
                    var matches = msg.content.match(linkRegex);
                    if (matches) {
                        matches.forEach(function (url) {
                            var domain = '';
                            try { domain = new URL(url).hostname; } catch (e) { domain = url; }
                            links.push({ url: url, domain: domain, title: url, sentAt: msg.sentAt || msg.createdAt });
                        });
                    }
                });
                _linkCount = links.length;
                updateBadge('badgeLinks', _linkCount);
                if (links.length === 0) {
                    container.innerHTML = '<div class="sidebar-empty"><span class="material-icons-outlined" style="font-size:36px;color:rgba(0,0,0,0.1);">link_off</span><p>No links shared</p></div>';
                } else {
                    container.innerHTML = '';
                    // Use DocumentFragment for batch insertion
                    var fragment = document.createDocumentFragment();
                    links.forEach(function (link) {
                        fragment.appendChild(createLinkItem(link));
                    });
                    container.appendChild(fragment);
                }
            }
        } else if (tab === 'files') {
            const container = document.getElementById('sidebarTabFiles');
            const endpoint = isChannel
                ? '/api/channels/' + _convId + '/messages?hasFiles=true'
                : '/api/conversations/' + _convId + '/messages?hasFiles=true';
            const result = await ChatApp.api.get(endpoint);
            if (result.isSuccess) {
                const items = (result.value?.items || result.value || []).filter(function (m) {
                    return m.files && m.files.length > 0;
                });
                var totalFiles = 0;
                items.forEach(function (m) { totalFiles += m.files.length; });
                _fileCount = totalFiles;
                updateBadge('badgeFiles', _fileCount);
                if (items.length === 0) {
                    container.innerHTML = '<div class="sidebar-empty"><span class="material-icons-outlined" style="font-size:36px;color:rgba(0,0,0,0.1);">folder_open</span><p>No files shared</p></div>';
                } else {
                    container.innerHTML = '';
                    // Use DocumentFragment for batch insertion
                    var fragment = document.createDocumentFragment();
                    items.forEach(function (msg) {
                        msg.files.forEach(function (file) {
                            fragment.appendChild(createFileItem(file, msg));
                        });
                    });
                    container.appendChild(fragment);
                }
            }
        } else if (tab === 'members' && isChannel) {
            const container = document.getElementById('sidebarTabMembers');
            const result = await ChatApp.api.get('/api/channels/' + _convId + '/members');
            if (result.isSuccess) {
                const members = result.value || [];
                updateBadge('badgeMembers', members.length);
                container.innerHTML = '';
                // Use DocumentFragment + event delegation for member list
                var fragment = document.createDocumentFragment();
                members.forEach(function (member) {
                    const el = document.createElement('div');
                    el.className = 'sidebar-member-item';
                    el.dataset.userId = member.userId || member.id;
                    const fullName = ((member.firstName || '') + ' ' + (member.lastName || '')).trim() || member.fullName || '';
                    el.innerHTML = '<div class="sidebar-member-avatar">' +
                        ChatApp.utils.renderAvatarCached({ id: member.userId || member.id, avatarUrl: member.avatarUrl, fullName: fullName }, 'sidebar-member-avatar-img') +
                        '</div>' +
                        '<div class="sidebar-member-info">' +
                        '<span class="sidebar-member-name">' + ChatApp.utils.escapeHtml(fullName) + '</span>' +
                        (member.role === 'Owner' || member.memberRole === 2
                            ? '<span class="badge bg-warning text-dark ms-1" style="font-size:10px;">Owner</span>'
                            : (member.role === 'Admin' || member.memberRole === 1
                                ? '<span class="badge bg-primary ms-1" style="font-size:10px;">Admin</span>'
                                : '')) +
                        '</div>';
                    el.style.cursor = 'pointer';
                    fragment.appendChild(el);
                });
                container.appendChild(fragment);
                // Event delegation for member clicks
                container.addEventListener('click', function (e) {
                    var item = e.target.closest('.sidebar-member-item');
                    if (item && item.dataset.userId && ChatApp.profilePanel) {
                        ChatApp.profilePanel.open(item.dataset.userId);
                    }
                });
            }
        }
    }

    function createSidebarMessageItem(msg) {
        const el = document.createElement('div');
        el.className = 'sidebar-message-item';
        el.innerHTML = '<div class="sidebar-msg-header">' +
            '<span class="sidebar-msg-sender">' + ChatApp.utils.escapeHtml(msg.senderFullName || msg.senderName || '') + '</span>' +
            '<span class="sidebar-msg-time">' + ChatApp.utils.formatMessageTime(msg.sentAt || msg.createdAt) + '</span></div>' +
            '<div class="sidebar-msg-text">' + ChatApp.utils.escapeHtml(ChatApp.utils.truncateText(msg.content || '', 80)) + '</div>';
        el.style.cursor = 'pointer';
        el.addEventListener('click', function () {
            if (ChatApp.chatArea) ChatApp.chatArea.scrollToMessage(msg.id || msg.messageId);
            close();
        });
        return el;
    }

    function createFavoriteMessageItem(msg) {
        var el = document.createElement('div');
        el.className = 'sidebar-message-item sidebar-fav-item';

        // Build preview text with file/image indicator prefix
        var previewText = msg.content || '';
        if (msg.files && msg.files.length > 0) {
            var hasImage = msg.files.some(function (f) {
                return /\.(jpg|jpeg|png|gif|webp|svg)$/i.test(f.fileName || '');
            });
            var prefix = hasImage ? '[Image] ' : '[File] ';
            previewText = prefix + previewText;
        }

        el.innerHTML = '<div class="sidebar-msg-header">' +
            '<span class="sidebar-msg-sender">' + ChatApp.utils.escapeHtml(msg.senderFullName || msg.senderName || '') + '</span>' +
            '<span class="sidebar-msg-time">' + ChatApp.utils.formatMessageTime(msg.sentAt || msg.createdAt) + '</span>' +
            '<div class="sidebar-fav-actions">' +
            '<button class="sidebar-fav-action-btn" data-action="context" title="View Context"><span class="material-icons" style="font-size:16px;">visibility</span></button>' +
            '<button class="sidebar-fav-action-btn" data-action="unfavorite" title="Remove from Favorites"><span class="material-icons" style="font-size:16px;">star</span></button>' +
            '</div></div>' +
            '<div class="sidebar-msg-text">' + ChatApp.utils.escapeHtml(ChatApp.utils.truncateText(previewText, 80)) + '</div>';
        el.style.cursor = 'pointer';
        el.addEventListener('click', function (e) {
            var actionBtn = e.target.closest('.sidebar-fav-action-btn');
            if (actionBtn) {
                e.stopPropagation();
                var action = actionBtn.dataset.action;
                if (action === 'context') {
                    if (ChatApp.chatArea) ChatApp.chatArea.scrollToMessage(msg.id || msg.messageId);
                    close();
                } else if (action === 'unfavorite') {
                    handleUnfavorite(msg, el);
                }
                return;
            }
            if (ChatApp.chatArea) ChatApp.chatArea.scrollToMessage(msg.id || msg.messageId);
            close();
        });
        return el;
    }

    async function handleUnfavorite(msg, el) {
        var isChannel = _convType === 'channel';
        var endpoint = isChannel
            ? '/api/channels/' + _convId + '/messages/' + (msg.id || msg.messageId) + '/toggle-favorite'
            : '/api/conversations/' + _convId + '/messages/' + (msg.id || msg.messageId) + '/toggle-favorite';
        var result = await ChatApp.api.post(endpoint);
        if (result.isSuccess) {
            el.remove();
            _favCount = Math.max(0, _favCount - 1);
            updateBadge('badgeFavorites', _favCount);
            var container = document.getElementById('sidebarTabFavorites');
            if (container && container.querySelectorAll('.sidebar-fav-item').length === 0) {
                container.innerHTML = '<div class="sidebar-empty"><span class="material-icons-outlined" style="font-size:36px;color:rgba(0,0,0,0.1);">star_outline</span><p>No favorites yet</p></div>';
            }
        }
    }

    function groupByDate(items) {
        var groups = {};
        var today = new Date();
        var yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);
        var weekAgo = new Date(today);
        weekAgo.setDate(weekAgo.getDate() - 7);

        items.forEach(function (item) {
            var date = new Date(item.sentAt || item.createdAt);
            var label;
            if (date.toDateString() === today.toDateString()) {
                label = 'Today';
            } else if (date.toDateString() === yesterday.toDateString()) {
                label = 'Yesterday';
            } else if (date > weekAgo) {
                label = 'This Week';
            } else {
                label = 'Older';
            }
            if (!groups[label]) groups[label] = [];
            groups[label].push(item);
        });
        return groups;
    }

    function createLinkItem(link) {
        var el = document.createElement('div');
        el.className = 'sidebar-link-item';
        el.innerHTML = '<div class="sidebar-link-icon"><span class="material-icons" style="font-size:20px;">link</span></div>' +
            '<div class="sidebar-link-info">' +
            '<a href="' + ChatApp.utils.escapeHtml(link.url) + '" target="_blank" rel="noopener noreferrer" class="sidebar-link-title">' +
            ChatApp.utils.escapeHtml(ChatApp.utils.truncateText(link.title, 60)) + '</a>' +
            '<span class="sidebar-link-domain">' + ChatApp.utils.escapeHtml(link.domain) + '</span>' +
            '</div>';
        return el;
    }

    function updateBadge(badgeId, count) {
        var badge = document.getElementById(badgeId);
        if (!badge) return;
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.style.display = '';
        } else {
            badge.style.display = 'none';
        }
    }

    async function fetchBadgeCounts() {
        var isChannel = _convType === 'channel';
        // Fetch favorites count
        var favEndpoint = isChannel
            ? '/api/channels/' + _convId + '/messages/favorites'
            : '/api/conversations/' + _convId + '/messages/favorites';
        ChatApp.api.get(favEndpoint).then(function (r) {
            if (r.isSuccess) {
                _favCount = (r.value || []).length;
                updateBadge('badgeFavorites', _favCount);
            }
        });
        // Fetch files count
        var fileEndpoint = isChannel
            ? '/api/channels/' + _convId + '/messages?hasFiles=true'
            : '/api/conversations/' + _convId + '/messages?hasFiles=true';
        ChatApp.api.get(fileEndpoint).then(function (r) {
            if (r.isSuccess) {
                var msgs = (r.value?.items || r.value || []).filter(function (m) { return m.files && m.files.length > 0; });
                _fileCount = 0;
                msgs.forEach(function (m) { _fileCount += m.files.length; });
                updateBadge('badgeFiles', _fileCount);
            }
        });
        // Fetch links count
        var linkEndpoint = isChannel
            ? '/api/channels/' + _convId + '/messages?page=1&pageSize=100'
            : '/api/conversations/' + _convId + '/messages?page=1&pageSize=100';
        ChatApp.api.get(linkEndpoint).then(function (r) {
            if (r.isSuccess) {
                var allMsgs = r.value?.items || r.value || [];
                var linkRegex = /https?:\/\/[^\s<>"']+/gi;
                _linkCount = 0;
                allMsgs.forEach(function (msg) {
                    if (!msg.content) return;
                    var matches = msg.content.match(linkRegex);
                    if (matches) _linkCount += matches.length;
                });
                updateBadge('badgeLinks', _linkCount);
            }
        });
        // Fetch members count (channels only)
        if (isChannel) {
            ChatApp.api.get('/api/channels/' + _convId + '/members').then(function (r) {
                if (r.isSuccess) {
                    updateBadge('badgeMembers', (r.value || []).length);
                }
            });
        }
    }

    function createFileItem(file, msg) {
        const el = document.createElement('div');
        el.className = 'sidebar-file-item';
        const isImage = /\.(jpg|jpeg|png|gif|webp|svg)$/i.test(file.fileName || '');
        // Use cached file icon from utils
        var fi = isImage ? { icon: 'image', color: '' } : ChatApp.utils.getFileIcon(file.fileName);
        el.innerHTML = '<div class="sidebar-file-icon">' +
            '<span class="material-icons" style="' + fi.color + '">' + fi.icon + '</span></div>' +
            '<div class="sidebar-file-info">' +
            '<span class="sidebar-file-name">' + ChatApp.utils.escapeHtml(file.fileName || 'file') + '</span>' +
            '<span class="sidebar-file-meta">' + ChatApp.utils.formatFileSize(file.fileSizeInBytes) + ' \u2022 ' +
            ChatApp.utils.formatRelativeTime(msg.sentAt || msg.createdAt) + '</span></div>' +
            '<button class="sidebar-file-download" data-url="' + ChatApp.utils.escapeHtml(file.downloadUrl || '') +
            '" data-filename="' + ChatApp.utils.escapeHtml(file.fileName || 'file') +
            '" title="Download"><span class="material-icons" style="font-size:18px;">download</span></button>';
        el.querySelector('.sidebar-file-download').addEventListener('click', function (e) {
            e.stopPropagation();
            ChatApp.api.download(file.downloadUrl, file.fileName);
        });
        return el;
    }

    // --- Sidebar header three-dot menu ---
    var _sidebarContextMenu = null;

    function renderSidebarHeaderMenu() {
        // Remove any existing menu button
        var existing = sidebarPanel.querySelector('.sidebar-header-menu-btn');
        if (existing) existing.remove();

        var menuBtn = document.createElement('button');
        menuBtn.className = 'sidebar-header-menu-btn';
        menuBtn.innerHTML = '<span class="material-icons" style="font-size:20px;">more_vert</span>';
        menuBtn.title = 'More options';
        // Insert before the close button in the sidebar header
        var header = sidebarPanel.querySelector('.right-panel-header') || sidebarCloseBtn?.parentElement;
        if (header) {
            if (sidebarCloseBtn) {
                header.insertBefore(menuBtn, sidebarCloseBtn);
            } else {
                header.appendChild(menuBtn);
            }
        }
        menuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            showSidebarContextMenu(e);
        });
    }

    function showSidebarContextMenu(e) {
        hideSidebarContextMenu();
        var isChannel = _convType === 'channel';
        var menu = document.createElement('div');
        menu.className = 'conv-context-menu sidebar-context-menu';
        menu.style.position = 'fixed';
        menu.style.right = '12px';
        menu.style.top = (e.clientY || 48) + 'px';
        menu.style.zIndex = '1100';

        var menuHtml = '';
        // Pin/Unpin
        var isPinned = _conv && _conv.isPinned;
        menuHtml += '<button data-action="pin"><span class="material-icons" style="font-size:16px;">push_pin</span>' +
            (isPinned ? 'Unpin conversation' : 'Pin conversation') + '</button>';

        if (!isChannel && _conv) {
            // View Profile (DM)
            menuHtml += '<button data-action="viewProfile"><span class="material-icons" style="font-size:16px;">person</span>View Profile</button>';
        }

        // Hide conversation
        menuHtml += '<button data-action="hide"><span class="material-icons" style="font-size:16px;">visibility_off</span>Hide conversation</button>';

        if (isChannel) {
            // Leave channel
            menuHtml += '<button data-action="leaveChannel"><span class="material-icons" style="font-size:16px;">exit_to_app</span>Leave channel</button>';
            // Edit channel (for admins/owners)
            var currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
            var isOwnerOrAdmin = _channelData && (
                _channelData.createdByUserId === currentUserId ||
                _channelData.ownerId === currentUserId ||
                _channelData.memberRole === 1 ||
                _channelData.role === 'Admin' ||
                ChatApp.state.isAdmin
            );
            if (isOwnerOrAdmin) {
                menuHtml += '<button data-action="editChannel"><span class="material-icons" style="font-size:16px;">edit</span>Edit channel</button>';
            }
            // Delete channel (for owners)
            var isOwner = _channelData && (
                _channelData.createdByUserId === currentUserId ||
                _channelData.ownerId === currentUserId ||
                ChatApp.state.isSuperAdmin
            );
            if (isOwner) {
                menuHtml += '<button data-action="deleteChannel" style="color:#e53935;"><span class="material-icons" style="font-size:16px;">delete</span>Delete channel</button>';
            }
        }

        if (!isChannel && _conv) {
            // Find Chats With User (DMs)
            menuHtml += '<button data-action="chatsWithUser"><span class="material-icons" style="font-size:16px;">forum</span>Find Chats With User</button>';
        }

        menu.innerHTML = menuHtml;

        menu.addEventListener('click', function (ev) {
            var btn = ev.target.closest('button');
            if (!btn) return;
            var action = btn.dataset.action;
            hideSidebarContextMenu();
            handleSidebarAction(action);
        });

        document.body.appendChild(menu);
        _sidebarContextMenu = menu;

        setTimeout(function () {
            document.addEventListener('click', hideSidebarContextMenu, { once: true });
        }, 0);
    }

    function hideSidebarContextMenu() {
        if (_sidebarContextMenu) {
            _sidebarContextMenu.remove();
            _sidebarContextMenu = null;
        }
    }

    async function handleSidebarAction(action) {
        var isChannel = _convType === 'channel';
        var base = isChannel ? '/api/channels/' : '/api/conversations/';

        if (action === 'pin') {
            await ChatApp.api.post(base + _convId + '/toggle-pin');
            if (_conv) _conv.isPinned = !_conv.isPinned;
            if (ChatApp.conversationList) ChatApp.conversationList.reload();
        } else if (action === 'viewProfile') {
            var userId = _conv.otherUserId || _conv.id;
            if (ChatApp.profilePanel) ChatApp.profilePanel.open(userId);
        } else if (action === 'hide') {
            await ChatApp.api.post(base + _convId + '/close');
            close();
            if (ChatApp.conversationList) ChatApp.conversationList.reload();
        } else if (action === 'leaveChannel') {
            if (confirm('Are you sure you want to leave this channel?')) {
                await ChatApp.api.post('/api/channels/' + _convId + '/leave');
                close();
                if (ChatApp.conversationList) ChatApp.conversationList.reload();
            }
        } else if (action === 'editChannel') {
            showEditChannelDialog();
        } else if (action === 'deleteChannel') {
            showDeleteChannelConfirm();
        } else if (action === 'chatsWithUser') {
            showChatsWithUser();
        }
    }

    // --- Edit Channel Dialog ---
    function showEditChannelDialog() {
        // Remove any existing dialog
        var existingDialog = document.getElementById('editChannelDialog');
        if (existingDialog) existingDialog.remove();

        var ch = _channelData || {};
        var overlay = document.createElement('div');
        overlay.id = 'editChannelDialog';
        overlay.className = 'sidebar-dialog-overlay';
        overlay.innerHTML =
            '<div class="sidebar-dialog">' +
            '<div class="sidebar-dialog-header">' +
            '<h3>Edit Channel</h3>' +
            '<button class="sidebar-dialog-close" id="editChannelClose"><span class="material-icons">close</span></button></div>' +
            '<div class="sidebar-dialog-body">' +
            '<div class="sidebar-dialog-field"><label>Channel Name</label>' +
            '<input type="text" id="editChannelName" value="' + ChatApp.utils.escapeHtml(ch.name || '') + '" maxlength="100" /></div>' +
            '<div class="sidebar-dialog-field"><label>Description</label>' +
            '<textarea id="editChannelDesc" rows="3" maxlength="500">' + ChatApp.utils.escapeHtml(ch.description || '') + '</textarea></div>' +
            '<div class="sidebar-dialog-field"><label class="sidebar-checkbox-label">' +
            '<input type="checkbox" id="editChannelPrivate"' + (ch.channelType === 1 ? ' checked' : '') + ' /> Private channel</label></div>' +
            '</div>' +
            '<div class="sidebar-dialog-footer">' +
            '<button class="sidebar-dialog-btn cancel" id="editChannelCancel">Cancel</button>' +
            '<button class="sidebar-dialog-btn primary" id="editChannelSave">Save</button></div></div>';

        document.body.appendChild(overlay);

        document.getElementById('editChannelClose').addEventListener('click', function () { overlay.remove(); });
        document.getElementById('editChannelCancel').addEventListener('click', function () { overlay.remove(); });
        overlay.addEventListener('click', function (e) { if (e.target === overlay) overlay.remove(); });

        document.getElementById('editChannelSave').addEventListener('click', async function () {
            var name = document.getElementById('editChannelName').value.trim();
            var description = document.getElementById('editChannelDesc').value.trim();
            var isPrivate = document.getElementById('editChannelPrivate').checked;
            if (!name) { alert('Channel name is required.'); return; }

            var result = await ChatApp.api.put('/api/channels/' + _convId, {
                name: name,
                description: description,
                isPrivate: isPrivate
            });
            if (result.isSuccess) {
                overlay.remove();
                // Refresh sidebar
                if (_channelData) {
                    _channelData.name = name;
                    _channelData.description = description;
                    _channelData.channelType = isPrivate ? 1 : 0;
                }
                loadContent();
                if (ChatApp.conversationList) ChatApp.conversationList.reload();
            } else {
                alert(result.error || 'Failed to update channel.');
            }
        });
    }

    // --- Delete Channel Confirmation ---
    function showDeleteChannelConfirm() {
        var existingDialog = document.getElementById('deleteChannelDialog');
        if (existingDialog) existingDialog.remove();

        var ch = _channelData || {};
        var overlay = document.createElement('div');
        overlay.id = 'deleteChannelDialog';
        overlay.className = 'sidebar-dialog-overlay';
        overlay.innerHTML =
            '<div class="sidebar-dialog">' +
            '<div class="sidebar-dialog-header">' +
            '<h3 style="color:#e53935;">Delete Channel</h3>' +
            '<button class="sidebar-dialog-close" id="deleteChannelClose"><span class="material-icons">close</span></button></div>' +
            '<div class="sidebar-dialog-body">' +
            '<p>Are you sure you want to delete <strong>' + ChatApp.utils.escapeHtml(ch.name || 'this channel') + '</strong>? ' +
            'This action cannot be undone. All messages and files in this channel will be permanently deleted.</p></div>' +
            '<div class="sidebar-dialog-footer">' +
            '<button class="sidebar-dialog-btn cancel" id="deleteChannelCancel">Cancel</button>' +
            '<button class="sidebar-dialog-btn danger" id="deleteChannelConfirm">Delete</button></div></div>';

        document.body.appendChild(overlay);

        document.getElementById('deleteChannelClose').addEventListener('click', function () { overlay.remove(); });
        document.getElementById('deleteChannelCancel').addEventListener('click', function () { overlay.remove(); });
        overlay.addEventListener('click', function (e) { if (e.target === overlay) overlay.remove(); });

        document.getElementById('deleteChannelConfirm').addEventListener('click', async function () {
            var result = await ChatApp.api.del('/api/channels/' + _convId);
            if (result.isSuccess) {
                overlay.remove();
                close();
                if (ChatApp.conversationList) ChatApp.conversationList.reload();
            } else {
                alert(result.error || 'Failed to delete channel.');
            }
        });
    }

    // --- Chats With User ---
    async function showChatsWithUser() {
        if (!_conv) return;
        var otherUserId = _conv.otherUserId || _conv.id;

        // Show the panel and hide tabs
        sidebarContent.querySelectorAll('.sidebar-tab-content').forEach(function (c) { c.style.display = 'none'; });
        sidebarContent.querySelectorAll('.sidebar-tab').forEach(function (t) { t.classList.remove('active'); });
        var chatsPanel = document.getElementById('sidebarChatsWithUser');
        if (chatsPanel) {
            chatsPanel.style.display = '';
            chatsPanel.innerHTML = '<div class="sidebar-chats-with-header">' +
                '<button class="sidebar-back-btn" id="chatsWithUserBack"><span class="material-icons" style="font-size:18px;">arrow_back</span></button>' +
                '<span>Shared Channels</span></div>' +
                '<div class="sidebar-loading-tab"><div class="spinner-border spinner-border-sm text-secondary"></div></div>';
        }

        var result = await ChatApp.api.get('/api/channels/shared-with-user/' + otherUserId);
        if (!chatsPanel) return;

        if (result.isSuccess) {
            var channels = result.value || [];
            var bodyHtml = '<div class="sidebar-chats-with-header">' +
                '<button class="sidebar-back-btn" id="chatsWithUserBack"><span class="material-icons" style="font-size:18px;">arrow_back</span></button>' +
                '<span>Shared Channels</span></div>';
            if (channels.length === 0) {
                bodyHtml += '<div class="sidebar-empty"><span class="material-icons-outlined" style="font-size:36px;color:rgba(0,0,0,0.1);">forum</span><p>No shared channels</p></div>';
            }
            chatsPanel.innerHTML = bodyHtml;

            channels.forEach(function (ch) {
                var item = document.createElement('div');
                item.className = 'sidebar-member-item sidebar-shared-channel';
                item.style.cursor = 'pointer';
                item.innerHTML = '<div class="sidebar-member-avatar">' +
                    '<div class="conv-channel-icon" style="background-color:' + ChatApp.utils.getAvatarColor(ch.id) +
                    ';width:36px;height:36px;border-radius:50%;display:flex;align-items:center;justify-content:center;">' +
                    '<span class="material-icons" style="font-size:16px;color:#fff;">' +
                    (ch.channelType === 1 ? 'lock' : 'tag') + '</span></div></div>' +
                    '<div class="sidebar-member-info"><span class="sidebar-member-name">' +
                    ChatApp.utils.escapeHtml(ch.name || '') + '</span></div>';
                item.addEventListener('click', function () {
                    if (ChatApp.conversationList) ChatApp.conversationList.selectById(ch.id);
                    close();
                });
                chatsPanel.appendChild(item);
            });

            // Back button
            var backBtn = document.getElementById('chatsWithUserBack');
            if (backBtn) {
                backBtn.addEventListener('click', function () {
                    chatsPanel.style.display = 'none';
                    // Re-show info tab
                    var infoTab = document.getElementById('sidebarTabInfo');
                    if (infoTab) infoTab.style.display = '';
                    var tabs = sidebarContent.querySelectorAll('.sidebar-tab');
                    if (tabs.length > 0) tabs[0].classList.add('active');
                    _activeTab = 'info';
                });
            }
        } else {
            chatsPanel.innerHTML = '<div class="sidebar-empty"><p>Failed to load shared channels.</p></div>';
        }
    }

    // --- Public API ---
    ChatApp.sidebar = {
        toggle: toggle,
        open: open,
        close: close,
        isOpen: function () { return _isOpen; }
    };

})();
