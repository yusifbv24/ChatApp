/**
 * Mention Panel — replaces MentionPanel.razor + mention.js from Blazor
 * @mention detection, user search, keyboard navigation, insertion
 */
(function () {
    'use strict';

    const mentionPanel = document.getElementById('mentionPanel');
    const mentionList = document.getElementById('mentionList');

    if (!mentionPanel || !mentionList) return;

    let _isOpen = false;
    let _selectedIndex = 0;
    let _users = [];
    let _mentionStart = -1;

    // --- Detect @mention in textarea ---
    const handleInput = ChatApp.utils.debounce(function (textarea) {
        const cursorPos = textarea.selectionStart;
        const text = textarea.value;

        // Find @ before cursor
        let atPos = -1;
        for (let i = cursorPos - 1; i >= 0; i--) {
            if (text[i] === '@') {
                // Valid mention: start of text or after whitespace
                if (i === 0 || /\s/.test(text[i - 1])) {
                    atPos = i;
                    break;
                }
                break;
            }
            if (/\s/.test(text[i])) break;
        }

        if (atPos === -1) {
            close();
            return;
        }

        _mentionStart = atPos;
        const query = text.substring(atPos + 1, cursorPos);

        if (query.length === 0) {
            // Show channel/conversation members
            loadMembers('');
        } else {
            searchUsers(query);
        }
    }, 200);

    async function loadMembers(query) {
        const convId = ChatApp.chatArea.getCurrentConvId();
        const convType = ChatApp.chatArea.getCurrentConvType();
        if (!convId) return;

        let endpoint;
        if (convType === 'channel') {
            endpoint = '/api/channels/' + convId + '/members/mentionable';
            if (query) endpoint += '?search=' + encodeURIComponent(query);
        } else {
            endpoint = '/api/identity/users/search?query=' + encodeURIComponent(query || '');
        }

        const result = await ChatApp.api.get(endpoint);
        if (result.isSuccess) {
            _users = result.value || [];
            render();
        }
    }

    async function searchUsers(query) {
        await loadMembers(query);
    }

    function render() {
        if (_users.length === 0) {
            close();
            return;
        }

        _selectedIndex = 0;
        mentionList.innerHTML = '';

        // Add "All" option for channels
        if (ChatApp.chatArea.getCurrentConvType() === 'channel') {
            const allItem = createItem({ id: 'all', fullName: 'All', isAll: true }, 0);
            mentionList.appendChild(allItem);
            _users = [{ id: 'all', fullName: 'All', isAll: true }].concat(_users.filter(function(u) { return u.id !== 'all'; }));
        }

        _users.forEach(function (user, index) {
            if (user.isAll) return; // Already added
            mentionList.appendChild(createItem(user, user.isAll ? 0 : index));
        });

        mentionPanel.style.display = '';
        _isOpen = true;
        highlightSelected();
    }

    function createItem(user, index) {
        const item = document.createElement('div');
        item.className = 'mention-item';
        item.dataset.index = index;

        const fullName = user.fullName || ((user.firstName || '') + ' ' + (user.lastName || '')).trim();

        if (user.isAll) {
            item.innerHTML = '<div class="mention-avatar-all"><span class="material-icons" style="font-size:16px;">groups</span></div>' +
                '<div class="mention-info"><span class="mention-name">@All</span><span class="mention-desc">Notify everyone</span></div>';
        } else {
            item.innerHTML = '<div class="mention-avatar">' +
                ChatApp.utils.renderAvatar({ id: user.id, avatarUrl: user.avatarUrl, fullName: fullName }, 'mention-avatar-img') +
                '</div>' +
                '<div class="mention-info"><span class="mention-name">' + ChatApp.utils.escapeHtml(fullName) + '</span></div>';
        }

        item.addEventListener('click', function () {
            select(index);
        });

        item.addEventListener('mouseenter', function () {
            _selectedIndex = index;
            highlightSelected();
        });

        return item;
    }

    function highlightSelected() {
        mentionList.querySelectorAll('.mention-item').forEach(function (item, i) {
            item.classList.toggle('active', i === _selectedIndex);
        });
        // Scroll into view
        const active = mentionList.querySelector('.mention-item.active');
        if (active) active.scrollIntoView({ block: 'nearest' });
    }

    function select(index) {
        const user = _users[index];
        if (!user) return;

        const textarea = ChatApp.messageInput.getTextarea();
        if (!textarea) return;

        const fullName = user.isAll ? 'All' : (user.fullName || ((user.firstName || '') + ' ' + (user.lastName || '')).trim());
        const before = textarea.value.substring(0, _mentionStart);
        const after = textarea.value.substring(textarea.selectionStart);
        const mentionText = '@' + fullName + ' ';

        textarea.value = before + mentionText + after;
        textarea.selectionStart = textarea.selectionEnd = _mentionStart + mentionText.length;
        textarea.focus();

        // Track mention
        if (!user.isAll && user.id) {
            ChatApp.messageInput.addMention(user.id);
        }

        close();
    }

    function close() {
        _isOpen = false;
        mentionPanel.style.display = 'none';
        _users = [];
        _mentionStart = -1;
    }

    function handleKeydown(e) {
        if (!_isOpen) return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            _selectedIndex = _selectedIndex >= _users.length - 1 ? 0 : _selectedIndex + 1;
            highlightSelected();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            _selectedIndex = _selectedIndex <= 0 ? _users.length - 1 : _selectedIndex - 1;
            highlightSelected();
        } else if (e.key === 'Enter') {
            e.preventDefault();
            select(_selectedIndex);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            close();
        }
    }

    // --- Outside click to close mention panel ---
    document.addEventListener('click', function (e) {
        if (!_isOpen) return;
        if (mentionPanel.contains(e.target)) return;
        var textarea = ChatApp.messageInput ? ChatApp.messageInput.getTextarea() : null;
        if (textarea && textarea === e.target) return;
        close();
    });

    // --- Public API ---
    ChatApp.mention = {
        handleInput: handleInput,
        handleKeydown: handleKeydown,
        isOpen: function () { return _isOpen; },
        close: close
    };

})();
