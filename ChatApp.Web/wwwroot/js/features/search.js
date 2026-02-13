/**
 * Search Panel — replaces SearchPanel.razor
 * Full-text message search within conversation/channel
 */
(function () {
    'use strict';

    const rightPanel = document.getElementById('rightPanel');
    const searchPanel = document.getElementById('searchPanel');
    const searchPanelInput = document.getElementById('searchPanelInput');
    const searchPanelResults = document.getElementById('searchPanelResults');
    const searchPanelCloseBtn = document.getElementById('searchPanelCloseBtn');

    if (!searchPanel) return;

    let _isOpen = false;
    let _convId = null;
    let _convType = null;
    let _abortController = null;

    function toggle(convId, convType) {
        if (_isOpen && _convId === convId) {
            close();
        } else {
            open(convId, convType);
        }
    }

    function open(convId, convType) {
        _convId = convId;
        _convType = convType;
        _isOpen = true;
        rightPanel.style.display = '';
        searchPanel.style.display = '';
        // Hide sidebar if open
        const sidebarPanel = document.getElementById('sidebarPanel');
        if (sidebarPanel) sidebarPanel.style.display = 'none';

        searchPanelInput.value = '';
        searchPanelResults.innerHTML = '<div class="search-empty"><span class="material-icons-outlined" style="font-size:48px;color:rgba(0,0,0,0.1);">search</span><p>Type to search messages</p></div>';
        searchPanelInput.focus();
    }

    function close() {
        _isOpen = false;
        searchPanel.style.display = 'none';
        rightPanel.style.display = 'none';
        _convId = null;
    }

    if (searchPanelCloseBtn) {
        searchPanelCloseBtn.addEventListener('click', close);
    }

    // Search with debounce
    if (searchPanelInput) {
        searchPanelInput.addEventListener('input', ChatApp.utils.debounce(async function () {
            const query = searchPanelInput.value.trim();

            // Update clear button visibility
            updateClearButton();

            if (query.length < 3) {
                // Cancel any in-flight request
                if (_abortController) { _abortController.abort(); _abortController = null; }
                searchPanelResults.innerHTML = '<div class="search-empty"><span class="material-icons-outlined" style="font-size:48px;color:rgba(0,0,0,0.1);">search</span><p>Type at least 3 characters to search</p></div>';
                return;
            }

            // Cancel previous in-flight request
            if (_abortController) { _abortController.abort(); }
            _abortController = new AbortController();
            var signal = _abortController.signal;

            searchPanelResults.innerHTML = '<div class="search-loading"><div class="spinner-border spinner-border-sm text-secondary"></div></div>';

            const isChannel = _convType === 'channel';
            const endpoint = '/api/search/messages?query=' + encodeURIComponent(query) +
                (isChannel ? '&channelId=' + _convId : '&conversationId=' + _convId);

            const result = await ChatApp.api.get(endpoint, { signal: signal });

            // If aborted, don't update UI
            if (signal.aborted) return;

            if (result.isSuccess && result.value) {
                const items = result.value.items || result.value || [];
                renderResults(items);
            } else if (!signal.aborted) {
                searchPanelResults.innerHTML = '<div class="search-empty"><p>Search failed</p></div>';
            }
        }, 300));
    }

    // --- Clear button ---
    function updateClearButton() {
        var clearBtn = document.getElementById('searchPanelClearBtn');
        if (clearBtn) {
            clearBtn.style.display = searchPanelInput.value.length > 0 ? '' : 'none';
        }
    }

    function initClearButton() {
        // Create clear button if it doesn't exist
        if (!document.getElementById('searchPanelClearBtn') && searchPanelInput && searchPanelInput.parentElement) {
            var clearBtn = document.createElement('button');
            clearBtn.id = 'searchPanelClearBtn';
            clearBtn.className = 'search-clear-btn';
            clearBtn.type = 'button';
            clearBtn.style.display = 'none';
            clearBtn.innerHTML = '<span class="material-icons" style="font-size:18px;">close</span>';
            clearBtn.addEventListener('click', function () {
                searchPanelInput.value = '';
                searchPanelResults.innerHTML = '<div class="search-empty"><span class="material-icons-outlined" style="font-size:48px;color:rgba(0,0,0,0.1);">search</span><p>Type at least 3 characters to search</p></div>';
                if (_abortController) { _abortController.abort(); _abortController = null; }
                updateClearButton();
                searchPanelInput.focus();
            });
            searchPanelInput.parentElement.style.position = 'relative';
            searchPanelInput.parentElement.appendChild(clearBtn);
        }
    }

    initClearButton();

    // Event delegation for search result clicks (instead of per-item listeners)
    if (searchPanelResults) {
        searchPanelResults.addEventListener('click', function (e) {
            var item = e.target.closest('.search-result-item');
            if (!item) return;
            var msgId = item.dataset.messageId;
            if (msgId && ChatApp.chatArea) {
                ChatApp.chatArea.scrollToMessage(msgId);
                close();
            }
        });
    }

    function renderResults(items) {
        if (items.length === 0) {
            searchPanelResults.innerHTML = '<div class="search-empty"><span class="material-icons-outlined" style="font-size:48px;color:rgba(0,0,0,0.1);">search_off</span><p>No results found</p></div>';
            return;
        }

        // Use DocumentFragment for batch DOM insertion
        var fragment = document.createDocumentFragment();
        let lastDate = null;

        items.forEach(function (item) {
            const date = new Date(item.sentAt || item.createdAt);
            const dateStr = date.toDateString();

            // Date group header
            if (dateStr !== lastDate) {
                lastDate = dateStr;
                const header = document.createElement('div');
                header.className = 'search-date-header';
                header.textContent = ChatApp.utils.formatMessageDate(item.sentAt || item.createdAt);
                fragment.appendChild(header);
            }

            const el = document.createElement('div');
            el.className = 'search-result-item';
            el.dataset.messageId = item.messageId || item.id;
            el.innerHTML = '<div class="search-result-avatar">' +
                ChatApp.utils.renderAvatarCached({
                    id: item.senderUserId || item.senderId,
                    avatarUrl: item.senderAvatarUrl,
                    fullName: item.senderFullName || item.senderName
                }, 'search-avatar-img') + '</div>' +
                '<div class="search-result-content">' +
                '<div class="search-result-header">' +
                '<span class="search-result-name">' + ChatApp.utils.escapeHtml(item.senderFullName || item.senderName || '') + '</span>' +
                '<span class="search-result-time">' + ChatApp.utils.formatMessageTime(item.sentAt || item.createdAt) + '</span>' +
                '</div>' +
                '<div class="search-result-text">' + (item.highlightedContent || ChatApp.utils.escapeHtml(item.content || '')) + '</div>' +
                '</div>';

            fragment.appendChild(el);
        });

        searchPanelResults.innerHTML = '';
        searchPanelResults.appendChild(fragment);
    }

    // --- Public API ---
    ChatApp.search = {
        toggle: toggle,
        open: open,
        close: close,
        isOpen: function () { return _isOpen; }
    };

})();
