/**
 * Message Input — replaces MessageInput.razor
 * Auto-resize textarea, reply/edit mode, emoji picker, send with Enter
 */
(function () {
    'use strict';

    const textarea = document.getElementById('messageTextarea');
    const sendBtn = document.getElementById('sendBtn');
    const attachBtn = document.getElementById('attachBtn');
    const fileInput = document.getElementById('fileInput');
    const emojiBtn = document.getElementById('emojiBtn');
    const emojiPicker = document.getElementById('emojiPicker');
    const emojiGrid = document.getElementById('emojiGrid');
    const replyPreview = document.getElementById('inputReplyPreview');
    const replyPreviewName = document.getElementById('replyPreviewName');
    const replyPreviewText = document.getElementById('replyPreviewText');
    const replyPreviewIcon = document.getElementById('replyPreviewIcon');
    const replyPreviewClose = document.getElementById('replyPreviewClose');

    const charCounter = document.getElementById('charCounter');
    const charCountText = document.getElementById('charCountText');
    const emojiSearchInput = document.getElementById('emojiSearchInput');
    const emojiTabs = document.getElementById('emojiTabs');

    if (!textarea) return;

    let _mode = 'normal'; // normal | reply | edit
    let _replyToMessage = null;
    let _editMessage = null;
    let _mentionedUserIds = [];
    const _maxChars = 4000;

    // Full emoji categories
    const emojiCategories = {
        smileys: ['😀','😃','😄','😁','😆','😅','🤣','😂','🙂','😊','😇','🥰','😍','🤩','😘','😗','😋','😛','😜','🤪','😝','🤑','🤗','🤭','🤫','🤔','🤐','🤨','😐','😑','😶','😏','😒','🙄','😬','🤥','😌','😔','😪','🤤','😴','😷','🤒','🤕','🤢','🤮','🤧','🥵','🥶','🥴','😵','🤯','🤠','🥳','🥸','😎','🤓','🧐','😕','😟','🙁','☹️','😮','😯','😲','😳','🥺','😦','😧','😨','😰','😥','😢','😭','😱','😖','😣','😞','😓','😩','😫','🥱','😤','😡','😠','🤬'],
        people: ['👋','🤚','🖐','✋','🖖','👌','🤌','🤏','✌️','🤞','🤟','🤘','🤙','👈','👉','👆','👇','☝️','👍','👎','✊','👊','🤛','🤜','👏','🙌','👐','🤲','🤝','🙏','💪','🦾','🖕'],
        nature: ['🐶','🐱','🐭','🐹','🐰','🦊','🐻','🐼','🐨','🐯','🦁','🐮','🐷','🐸','🐵','🙈','🙉','🙊','🐒','🐔','🐧','🐦','🐤','🦆','🦅','🦉','🦇','🐺','🐗','🐴','🦄','🐝','🐛','🦋','🐌','🐞','🐜','🦟','🦗'],
        food: ['🍏','🍎','🍐','🍊','🍋','🍌','🍉','🍇','🍓','🫐','🍈','🍒','🍑','🥭','🍍','🥥','🥝','🍅','🍆','🥑','🥦','🥬','🥒','🌶','🫑','🌽','🥕','🧄','🧅','🥔','🍠','🥐','🥖','🍞','🥨','🧀','🥚','🍳','🧈','🥞','🧇','🥓','🥩','🍗','🍖','🌭','🍔','🍟','🍕'],
        activities: ['⚽️','🏀','🏈','⚾️','🥎','🎾','🏐','🏉','🥏','🎱','🏓','🏸','🏒','🥅','⛳️','🏹','🎣','🤿','🥊','🥋','🎽','🛹','🛼','🛷','⛸','🥌','🎿','⛷','🏂','🪂','🏋️'],
        objects: ['⌚️','📱','💻','⌨️','🖥','🖨','🖱','🖲','🕹','🗜','💽','💾','💿','📀','📷','📸','📹','🎥','📽','🎞','📞','☎️','📟','📠','📺','📻','🎙','🎚','🎛','🧭','⏱','⏲','⏰','🕰','⌛️','⏳','💡','🔦','🕯','🪔']
    };
    let _activeEmojiCategory = 'smileys';

    // File validation config
    const allowedFileTypes = [
        'image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml', 'image/bmp',
        'application/pdf',
        'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'application/vnd.ms-excel', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'application/vnd.ms-powerpoint', 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
        'text/plain',
        'application/zip', 'application/x-rar-compressed', 'application/x-7z-compressed',
        'video/mp4', 'audio/mpeg', 'audio/mp3'
    ];
    const allowedExtensions = /\.(jpg|jpeg|png|gif|webp|svg|bmp|pdf|doc|docx|xls|xlsx|ppt|pptx|txt|zip|rar|7z|mp4|mp3)$/i;
    const maxFileSize = 100 * 1024 * 1024; // 100MB
    const maxFileCount = 10;

    // --- Debounced draft save (300ms) to avoid excessive sessionStorage writes ---
    var _debouncedSaveDraft = ChatApp.utils.debounce(function () {
        saveDraft();
    }, 300);

    // --- Auto-resize textarea ---
    textarea.addEventListener('input', function () {
        autoResize();
        updateCharCounter();
        _debouncedSaveDraft();
        ChatApp.messages.sendTyping();
        // Trigger mention detection
        if (ChatApp.mention) ChatApp.mention.handleInput(textarea);
    });

    function autoResize() {
        // Batch DOM reads then writes to reduce layout thrashing
        textarea.style.height = 'auto';
        var scrollH = textarea.scrollHeight;
        var maxH = 200;
        var newHeight = Math.min(scrollH, maxH);
        var newOverflow = scrollH > maxH ? 'auto' : 'hidden';
        textarea.style.height = newHeight + 'px';
        textarea.style.overflowY = newOverflow;
    }

    // --- Character counter ---
    function updateCharCounter() {
        if (!charCounter || !charCountText) return;
        var len = textarea.value.length;
        if (len === 0) {
            charCounter.style.display = 'none';
            return;
        }
        charCounter.style.display = '';
        charCountText.textContent = len + '/' + _maxChars;
        charCounter.classList.toggle('near-limit', len > 3500 && len < _maxChars);
        charCounter.classList.toggle('at-limit', len >= _maxChars);
    }

    // --- Draft management ---
    function getDraftKey() {
        var convId = ChatApp.chatArea ? ChatApp.chatArea.getCurrentConvId() : null;
        return convId ? 'chatDraft_' + convId : null;
    }

    function saveDraft() {
        var key = getDraftKey();
        if (!key) return;
        var val = textarea.value;
        if (val) {
            try { sessionStorage.setItem(key, val); } catch (e) { }
        } else {
            try { sessionStorage.removeItem(key); } catch (e) { }
        }
    }

    function restoreDraft() {
        var key = getDraftKey();
        if (!key) return;
        try {
            var val = sessionStorage.getItem(key);
            if (val) {
                textarea.value = val;
                autoResize();
                updateCharCounter();
            }
        } catch (e) { }
    }

    function clearDraft() {
        var key = getDraftKey();
        if (!key) return;
        try { sessionStorage.removeItem(key); } catch (e) { }
    }

    // --- Permission checks ---
    function checkPermissions() {
        if (!ChatApp.state.hasPermission || ChatApp.state.hasPermission('Messages.Send')) {
            textarea.disabled = false;
            textarea.placeholder = 'Type a message...';
            if (sendBtn) sendBtn.disabled = false;
            if (attachBtn) attachBtn.style.display = '';
        } else {
            textarea.disabled = true;
            textarea.placeholder = 'You don\'t have permission to send messages';
            if (sendBtn) sendBtn.disabled = true;
            if (attachBtn) attachBtn.style.display = 'none';
            return;
        }

        // Check file upload permission separately
        if (attachBtn && ChatApp.state.hasPermission && !ChatApp.state.hasPermission('Files.Upload')) {
            attachBtn.style.display = 'none';
        }
    }

    // Restore draft when conversation changes
    ChatApp.state.on('conversationSelected', function () {
        checkPermissions();
        if (_mode === 'normal') {
            setTimeout(restoreDraft, 50);
        }
    });

    // --- Send on Enter (Shift+Enter for newline) ---
    textarea.addEventListener('keydown', function (e) {
        // Mention navigation
        if (ChatApp.mention && ChatApp.mention.isOpen()) {
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp' || e.key === 'Enter' || e.key === 'Escape') {
                ChatApp.mention.handleKeydown(e);
                return;
            }
        }

        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            send();
        }

        if (e.key === 'Escape') {
            cancelMode();
        }
    });

    // --- Send button ---
    if (sendBtn) {
        sendBtn.addEventListener('click', send);
    }

    // --- Send button loading spinner ---
    function setSending(isSending) {
        if (sendBtn) sendBtn.disabled = isSending;
        var icon = sendBtn ? sendBtn.querySelector('.material-icons') : null;
        if (icon) icon.textContent = isSending ? '' : (_mode === 'edit' ? 'check' : 'send');
        var spinner = sendBtn ? sendBtn.querySelector('.send-spinner') : null;
        if (isSending && !spinner && sendBtn) {
            spinner = document.createElement('span');
            spinner.className = 'send-spinner';
            sendBtn.appendChild(spinner);
        } else if (!isSending && spinner) {
            spinner.remove();
        }
    }

    async function send() {
        const content = textarea.value.trim();
        if (!content && _mode !== 'edit') return;

        setSending(true);
        try {
            if (_mode === 'edit' && _editMessage) {
                await ChatApp.messages.edit(_editMessage.id, content);
                cancelMode();
            } else if (_mode === 'reply' && _replyToMessage) {
                await ChatApp.messages.send(content, _mentionedUserIds, _replyToMessage.id);
                cancelMode();
            } else {
                await ChatApp.messages.send(content, _mentionedUserIds);
            }

            textarea.value = '';
            _mentionedUserIds = [];
            autoResize();
            updateCharCounter();
            clearDraft();
            textarea.focus();
        } finally {
            setSending(false);
        }
    }

    // --- Reply mode ---
    function setReplyMode(msg) {
        _mode = 'reply';
        _replyToMessage = msg;
        _editMessage = null;

        replyPreviewIcon.textContent = 'reply';
        replyPreviewName.textContent = msg.senderFullName || msg.senderName || '';
        replyPreviewText.textContent = ChatApp.utils.truncateText(msg.content || '', 100);

        // Show file info if the replied message has a file
        if (msg.fileId || (msg.files && msg.files.length > 0)) {
            var fileName = msg.fileName || (msg.files && msg.files[0] ? msg.files[0].fileName : 'File');
            replyPreviewText.innerHTML += '<div class="reply-file-info"><span class="material-icons" style="font-size:14px;">attach_file</span> ' + ChatApp.utils.escapeHtml(fileName) + '</div>';
        }

        replyPreview.style.display = '';
        textarea.focus();
    }

    // --- Edit mode ---
    function setEditMode(msg) {
        _mode = 'edit';
        _editMessage = msg;
        _replyToMessage = null;

        replyPreviewIcon.textContent = 'edit';
        replyPreviewName.textContent = 'Editing message';
        replyPreviewText.textContent = ChatApp.utils.truncateText(msg.content || '', 100);
        replyPreview.style.display = '';
        textarea.value = msg.content || '';
        autoResize();
        // Change send button icon to checkmark for edit mode
        var icon = sendBtn ? sendBtn.querySelector('.material-icons') : null;
        if (icon) icon.textContent = 'check';
        textarea.focus();
    }

    function cancelMode() {
        var wasEdit = (_mode === 'edit');
        _mode = 'normal';
        _replyToMessage = null;
        _editMessage = null;
        replyPreview.style.display = 'none';
        // Restore send button icon to 'send'
        var icon = sendBtn ? sendBtn.querySelector('.material-icons') : null;
        if (icon) icon.textContent = 'send';
        if (wasEdit) {
            textarea.value = '';
            autoResize();
            updateCharCounter();
        }
    }

    if (replyPreviewClose) {
        replyPreviewClose.addEventListener('click', cancelMode);
    }

    // --- File attachment ---
    if (attachBtn && fileInput) {
        attachBtn.addEventListener('click', function () {
            fileInput.click();
        });

        fileInput.addEventListener('change', function () {
            if (!fileInput.files || fileInput.files.length === 0) return;
            var files = Array.from(fileInput.files);
            var validationError = validateFiles(files);
            if (validationError) {
                showFileError(validationError);
                fileInput.value = '';
                return;
            }
            if (ChatApp.fileUpload) {
                ChatApp.fileUpload.showFileSelection(files);
            }
            fileInput.value = ''; // Reset for re-selection
        });
    }

    // --- File validation ---
    function validateFiles(files) {
        if (files.length > maxFileCount) {
            return 'Maximum ' + maxFileCount + ' files allowed at once.';
        }
        for (var i = 0; i < files.length; i++) {
            var f = files[i];
            // Check type (by MIME or extension fallback)
            var typeOk = allowedFileTypes.indexOf(f.type) !== -1 || allowedExtensions.test(f.name);
            if (!typeOk) {
                return 'File type not allowed: ' + f.name;
            }
            if (f.size > maxFileSize) {
                return 'File too large (max 100MB): ' + f.name;
            }
        }
        return null;
    }

    function showFileError(message) {
        // Show a temporary toast-like error near the input
        var existing = document.querySelector('.file-validation-error');
        if (existing) existing.remove();
        var el = document.createElement('div');
        el.className = 'file-validation-error';
        el.innerHTML = '<span class="material-icons" style="font-size:16px;color:#e53935;">error</span> ' +
            ChatApp.utils.escapeHtml(message);
        var container = document.getElementById('messageInputContainer');
        if (container) {
            container.insertBefore(el, container.firstChild);
            setTimeout(function () { el.remove(); }, 5000);
        }
    }

    // --- Emoji picker (tabbed with search) ---
    // Pre-compute flattened unique emoji list for search
    var _allEmojis = null;
    function getAllEmojis() {
        if (_allEmojis) return _allEmojis;
        var seen = {};
        _allEmojis = [];
        Object.keys(emojiCategories).forEach(function (cat) {
            emojiCategories[cat].forEach(function (e) {
                if (!seen[e]) { seen[e] = true; _allEmojis.push(e); }
            });
        });
        return _allEmojis;
    }

    function renderEmojiGrid(category, searchTerm) {
        if (!emojiGrid) return;
        emojiGrid.innerHTML = '';
        var emojis = searchTerm ? getAllEmojis() : (emojiCategories[category] || emojiCategories.smileys);

        // Use DocumentFragment to batch DOM insertions
        var fragment = document.createDocumentFragment();
        emojis.forEach(function (emoji) {
            var btn = document.createElement('button');
            btn.className = 'emoji-item';
            btn.textContent = emoji;
            fragment.appendChild(btn);
        });
        emojiGrid.appendChild(fragment);
    }

    // Event delegation for emoji clicks (instead of per-button listeners)
    if (emojiGrid) {
        emojiGrid.addEventListener('click', function (e) {
            var btn = e.target.closest('.emoji-item');
            if (!btn) return;
            insertAtCursor(btn.textContent);
            if (emojiPicker) emojiPicker.style.display = 'none';
        });
    }

    function setActiveEmojiTab(category) {
        _activeEmojiCategory = category;
        if (emojiTabs) {
            emojiTabs.querySelectorAll('.emoji-tab').forEach(function (tab) {
                tab.classList.toggle('active', tab.dataset.category === category);
            });
        }
        renderEmojiGrid(category);
    }

    if (emojiBtn && emojiPicker && emojiGrid) {
        // Initial render
        renderEmojiGrid('smileys');

        // Tab click handlers
        if (emojiTabs) {
            emojiTabs.addEventListener('click', function (e) {
                var tab = e.target.closest('.emoji-tab');
                if (tab && tab.dataset.category) {
                    setActiveEmojiTab(tab.dataset.category);
                    if (emojiSearchInput) emojiSearchInput.value = '';
                }
            });
        }

        // Search handler
        if (emojiSearchInput) {
            emojiSearchInput.addEventListener('input', function () {
                var term = emojiSearchInput.value.trim();
                if (term) {
                    renderEmojiGrid(null, term);
                } else {
                    renderEmojiGrid(_activeEmojiCategory);
                }
            });
        }

        emojiBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            var isHidden = emojiPicker.style.display === 'none';
            emojiPicker.style.display = isHidden ? '' : 'none';
            if (isHidden) {
                renderEmojiGrid(_activeEmojiCategory);
                if (emojiSearchInput) { emojiSearchInput.value = ''; emojiSearchInput.focus(); }
            }
        });

        document.addEventListener('click', function () {
            emojiPicker.style.display = 'none';
        });

        emojiPicker.addEventListener('click', function (e) { e.stopPropagation(); });
    }

    function insertAtCursor(text) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        textarea.value = textarea.value.substring(0, start) + text + textarea.value.substring(end);
        textarea.selectionStart = textarea.selectionEnd = start + text.length;
        textarea.focus();
        autoResize();
    }

    // --- Add mention to tracked list ---
    function addMention(userId) {
        if (_mentionedUserIds.indexOf(userId) === -1) {
            _mentionedUserIds.push(userId);
        }
    }

    // --- Drag and drop file upload ---
    var chatContent = document.getElementById('chatContent') || document.querySelector('.chat-content');
    var dropZoneOverlay = null;

    function initDragDrop(target) {
        if (!target) return;

        target.addEventListener('dragenter', function (e) {
            e.preventDefault();
            e.stopPropagation();
            showDropOverlay(target);
        });

        target.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (dropZoneOverlay) dropZoneOverlay.style.display = '';
        });

        target.addEventListener('dragleave', function (e) {
            e.preventDefault();
            e.stopPropagation();
            // Only hide if leaving the target itself
            if (e.relatedTarget && target.contains(e.relatedTarget)) return;
            hideDropOverlay();
        });

        target.addEventListener('drop', function (e) {
            e.preventDefault();
            e.stopPropagation();
            hideDropOverlay();

            var files = e.dataTransfer && e.dataTransfer.files;
            if (!files || files.length === 0) return;

            var filesArr = Array.from(files);
            var err = validateFiles(filesArr);
            if (err) {
                showFileError(err);
                return;
            }
            if (ChatApp.fileUpload) {
                ChatApp.fileUpload.showFileSelection(filesArr);
            }
        });
    }

    function showDropOverlay(target) {
        if (!dropZoneOverlay) {
            dropZoneOverlay = document.createElement('div');
            dropZoneOverlay.className = 'drop-zone-overlay';
            dropZoneOverlay.innerHTML = '<div class="drop-zone-content"><span class="material-icons" style="font-size:48px;">cloud_upload</span><span>Drop files here</span></div>';
            target.style.position = target.style.position || 'relative';
            target.appendChild(dropZoneOverlay);
        }
        dropZoneOverlay.style.display = '';
    }

    function hideDropOverlay() {
        if (dropZoneOverlay) dropZoneOverlay.style.display = 'none';
    }

    // Initialize drag & drop on chat content and textarea
    initDragDrop(chatContent);
    initDragDrop(textarea);

    // --- Clipboard paste file upload ---
    textarea.addEventListener('paste', function (e) {
        var clipboardData = e.clipboardData || window.clipboardData;
        if (!clipboardData || !clipboardData.items) return;

        var files = [];
        for (var i = 0; i < clipboardData.items.length; i++) {
            var item = clipboardData.items[i];
            if (item.kind === 'file') {
                var file = item.getAsFile();
                if (file) files.push(file);
            }
        }

        if (files.length === 0) return;

        e.preventDefault();
        var err = validateFiles(files);
        if (err) {
            showFileError(err);
            return;
        }
        if (ChatApp.fileUpload) {
            ChatApp.fileUpload.showFileSelection(files);
        }
    });

    // --- Public API ---
    ChatApp.messageInput = {
        setReplyMode: setReplyMode,
        setEditMode: setEditMode,
        cancelMode: cancelMode,
        addMention: addMention,
        insertAtCursor: insertAtCursor,
        getTextarea: function () { return textarea; },
        focus: function () { if (textarea) textarea.focus(); }
    };

})();
