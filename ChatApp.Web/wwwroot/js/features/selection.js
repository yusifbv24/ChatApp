/**
 * Selection — replaces Messages.Selection.cs + SelectionToolbar.razor
 * Multi-message selection for bulk delete/forward
 */
(function () {
    'use strict';

    const selectionToolbar = document.getElementById('selectionToolbar');
    const selectedCount = document.getElementById('selectedCount');
    const selForwardBtn = document.getElementById('selForwardBtn');
    const selDeleteBtn = document.getElementById('selDeleteBtn');
    const selCancelBtn = document.getElementById('selCancelBtn');

    if (!selectionToolbar) return;

    let _selectedIds = new Set();
    let _isActive = false;

    function toggleSelect(messageId) {
        if (_selectedIds.has(messageId)) {
            _selectedIds.delete(messageId);
        } else {
            _selectedIds.add(messageId);
        }

        if (_selectedIds.size > 0 && !_isActive) {
            _isActive = true;
            selectionToolbar.style.display = '';
        }

        if (_selectedIds.size === 0) {
            cancelSelection();
            return;
        }

        selectedCount.textContent = _selectedIds.size;
        updateDeleteButton();
        updateCheckboxes();
    }

    function cancelSelection() {
        _isActive = false;
        _selectedIds.clear();
        selectionToolbar.style.display = 'none';
        updateCheckboxes();
    }

    function updateCheckboxes() {
        document.querySelectorAll('.message-bubble-wrapper').forEach(function (el) {
            const msgId = el.dataset.messageId;
            var checkboxWrap = el.querySelector('.message-selection-checkbox');
            if (_isActive) {
                // Create checkbox if it doesn't exist
                if (!checkboxWrap) {
                    checkboxWrap = document.createElement('div');
                    checkboxWrap.className = 'message-selection-checkbox';
                    checkboxWrap.innerHTML = '<input type="checkbox" class="msg-checkbox" />';
                    el.insertBefore(checkboxWrap, el.firstChild);
                    var cb = checkboxWrap.querySelector('.msg-checkbox');
                    cb.addEventListener('change', function () {
                        if (cb.checked) {
                            _selectedIds.add(msgId);
                        } else {
                            _selectedIds.delete(msgId);
                        }
                        updateCount();
                        updateDeleteButton();
                    });
                }
                checkboxWrap.style.display = '';
                var cb = checkboxWrap.querySelector('.msg-checkbox');
                if (cb) cb.checked = _selectedIds.has(msgId);
            } else {
                if (checkboxWrap) checkboxWrap.style.display = 'none';
            }
        });
    }

    // --- CanDelete permission check ---
    function updateDeleteButton() {
        if (!selDeleteBtn) return;
        var currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
        if (!currentUserId || _selectedIds.size === 0) {
            selDeleteBtn.disabled = true;
            selDeleteBtn.title = '';
            return;
        }

        var messages = ChatApp.chatArea ? ChatApp.chatArea.getMessages() : [];
        var allOwned = Array.from(_selectedIds).every(function (id) {
            var msg = messages.find(function (m) { return m.id === id; });
            return msg && (msg.senderUserId === currentUserId || msg.senderId === currentUserId);
        });

        selDeleteBtn.disabled = !allOwned;
        selDeleteBtn.title = allOwned ? '' : 'You can only delete your own messages';
    }

    // Toolbar actions
    if (selCancelBtn) {
        selCancelBtn.addEventListener('click', cancelSelection);
    }

    if (selDeleteBtn) {
        selDeleteBtn.addEventListener('click', async function () {
            if (_selectedIds.size === 0) return;
            const ids = Array.from(_selectedIds);
            await ChatApp.messages.batchDelete(ids);
            cancelSelection();
        });
    }

    if (selForwardBtn) {
        selForwardBtn.addEventListener('click', function () {
            if (_selectedIds.size === 0) return;
            var messages = ChatApp.chatArea.getMessages();
            var selectedMsgs = messages.filter(function (m) { return _selectedIds.has(m.id); });
            if (selectedMsgs.length > 0 && ChatApp.forwarding) {
                ChatApp.forwarding.showForwardDialog(selectedMsgs);
            }
            cancelSelection();
        });
    }

    // ESC key to cancel selection
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && _isActive) {
            cancelSelection();
        }
    });

    // --- Public API ---
    ChatApp.selection = {
        toggleSelect: toggleSelect,
        cancelSelection: cancelSelection,
        isActive: function () { return _isActive; },
        getSelectedIds: function () { return Array.from(_selectedIds); }
    };

})();
