/**
 * File Upload — replaces Messages.FileUploads.cs + FileSelectionPanel.razor
 * File selection, preview, upload with progress, multi-file support
 * Includes: validation, cancel, retry
 */
(function () {
    'use strict';

    // --- Constants ---
    var ALLOWED_EXTENSIONS = [
        '.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg', '.bmp',
        '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx',
        '.txt', '.csv', '.rtf', '.odt', '.ods',
        '.zip', '.rar', '.7z', '.tar', '.gz',
        '.mp4', '.mp3', '.wav', '.avi', '.mov', '.mkv'
    ];
    var MAX_FILE_SIZE = 100 * 1024 * 1024; // 100MB
    var MAX_FILE_COUNT = 10;

    // --- DOM References ---
    const fileSelectionPanel = document.getElementById('fileSelectionPanel');
    const fileSelectionList = document.getElementById('fileSelectionList');
    const fileSelectionClose = document.getElementById('fileSelectionClose');
    const fileMessageInput = document.getElementById('fileMessageInput');
    const fileSendBtn = document.getElementById('fileSendBtn');

    if (!fileSelectionPanel) return;

    // --- State ---
    let _selectedFiles = [];  // { file, id, status: 'pending'|'uploading'|'done'|'failed', progress: 0, result: null }
    var _xhrMap = {};         // fileId -> XMLHttpRequest (for cancel support)

    // --- Validation ---
    function validateFiles(files) {
        var truncated = false;
        if (files.length > MAX_FILE_COUNT) {
            truncated = true;
            files = Array.prototype.slice.call(files, 0, MAX_FILE_COUNT);
        }

        var validFiles = [];
        var errors = [];

        if (truncated) {
            errors.push('Maximum ' + MAX_FILE_COUNT + ' files allowed. Extra files were removed.');
        }

        files.forEach(function (f) {
            var dotIndex = f.name.lastIndexOf('.');
            var ext = dotIndex !== -1 ? f.name.substring(dotIndex).toLowerCase() : '';
            if (ALLOWED_EXTENSIONS.indexOf(ext) === -1) {
                errors.push(f.name + ': unsupported file type');
                return;
            }
            if (f.size > MAX_FILE_SIZE) {
                errors.push(f.name + ': exceeds 100MB limit');
                return;
            }
            if (f.size === 0) {
                errors.push(f.name + ': file is empty');
                return;
            }
            validFiles.push(f);
        });

        return { validFiles: validFiles, errors: errors };
    }

    function showFileError(msg) {
        var errorDiv = document.getElementById('fileValidationError');
        if (errorDiv) {
            errorDiv.textContent = msg;
            errorDiv.style.display = '';
            setTimeout(function () { errorDiv.style.display = 'none'; }, 5000);
        } else {
            alert(msg);
        }
    }

    // --- File Selection ---
    function showFileSelection(files) {
        var filesArray = Array.prototype.slice.call(files);
        var validation = validateFiles(filesArray);

        if (validation.errors.length > 0) {
            showFileError(validation.errors.join('\n'));
        }

        if (validation.validFiles.length === 0) return;

        _selectedFiles = validation.validFiles.map(function (f) {
            return {
                file: f,
                id: ChatApp.utils.generateId(),
                status: 'pending',
                progress: 0,
                result: null
            };
        });
        renderFiles();
        fileSelectionPanel.style.display = '';
        if (fileMessageInput) fileMessageInput.focus();
    }

    // --- Render ---
    function renderFiles() {
        fileSelectionList.innerHTML = '';
        _selectedFiles.forEach(function (sf) {
            var el = document.createElement('div');
            el.className = 'file-selection-item';
            el.dataset.id = sf.id;

            var isImage = /\.(jpg|jpeg|png|gif|webp|svg)$/i.test(sf.file.name);
            var previewHtml = '';
            if (isImage) {
                var url = URL.createObjectURL(sf.file);
                previewHtml = '<img src="' + url + '" class="file-preview-thumb" alt="" />';
            } else {
                previewHtml = '<div class="file-preview-icon"><span class="material-icons">description</span></div>';
            }

            // Build status-specific HTML
            var statusHtml = '';
            if (sf.status === 'uploading') {
                statusHtml = '<div class="file-progress"><div class="file-progress-bar" style="width:' + sf.progress + '%"></div></div>';
            } else if (sf.status === 'failed') {
                statusHtml = '<span class="file-error text-danger">Upload failed</span>';
            } else if (sf.status === 'done') {
                statusHtml = '<span class="file-done text-success" style="font-size:12px;"><span class="material-icons" style="font-size:14px;vertical-align:middle;">check_circle</span> Uploaded</span>';
            }

            // Build action buttons based on status
            var actionHtml = '';
            if (sf.status === 'uploading') {
                actionHtml = '<button class="file-cancel-btn" title="Cancel upload">' +
                    '<span class="material-icons" style="font-size:16px;">close</span></button>';
            } else if (sf.status === 'failed') {
                actionHtml = '<button class="file-retry-btn" title="Retry upload">' +
                    '<span class="material-icons" style="font-size:16px;">refresh</span></button>' +
                    '<button class="file-remove-btn" title="Remove">' +
                    '<span class="material-icons" style="font-size:16px;">close</span></button>';
            } else if (sf.status === 'pending') {
                actionHtml = '<button class="file-remove-btn" title="Remove">' +
                    '<span class="material-icons" style="font-size:16px;">close</span></button>';
            }
            // For 'done' status, no action buttons needed

            el.innerHTML = previewHtml +
                '<div class="file-info">' +
                '<span class="file-name">' + ChatApp.utils.escapeHtml(sf.file.name) + '</span>' +
                '<span class="file-size">' + ChatApp.utils.formatFileSize(sf.file.size) + '</span>' +
                statusHtml +
                '</div>' +
                '<div class="file-actions">' + actionHtml + '</div>';

            // Bind cancel button
            var cancelBtn = el.querySelector('.file-cancel-btn');
            if (cancelBtn) {
                cancelBtn.addEventListener('click', function () {
                    cancelUpload(sf.id);
                });
            }

            // Bind retry button
            var retryBtn = el.querySelector('.file-retry-btn');
            if (retryBtn) {
                retryBtn.addEventListener('click', function () {
                    retryUpload(sf.id);
                });
            }

            // Bind remove button
            var removeBtn = el.querySelector('.file-remove-btn');
            if (removeBtn) {
                removeBtn.addEventListener('click', function () {
                    removeFile(sf.id);
                });
            }

            fileSelectionList.appendChild(el);
        });
    }

    // --- Actions ---
    function removeFile(fileId) {
        // Abort if uploading
        if (_xhrMap[fileId]) {
            _xhrMap[fileId].abort();
            delete _xhrMap[fileId];
        }
        _selectedFiles = _selectedFiles.filter(function (f) { return f.id !== fileId; });
        if (_selectedFiles.length === 0) {
            closePanel();
        } else {
            renderFiles();
        }
    }

    function cancelUpload(fileId) {
        if (_xhrMap[fileId]) {
            _xhrMap[fileId].abort();
            delete _xhrMap[fileId];
        }
        var sf = _selectedFiles.find(function (f) { return f.id === fileId; });
        if (sf) {
            sf.status = 'pending';
            sf.progress = 0;
        }
        renderFiles();
    }

    async function retryUpload(fileId) {
        var sf = _selectedFiles.find(function (f) { return f.id === fileId; });
        if (!sf || sf.status !== 'failed') return;

        sf.status = 'uploading';
        sf.progress = 0;
        renderFiles();

        var result = await upload(sf.file, function (pct) {
            sf.progress = pct;
            renderFiles();
        }, fileId);

        if (result && result.fileId) {
            sf.status = 'done';
            sf.result = result;
        } else {
            sf.status = 'failed';
        }
        renderFiles();
    }

    function closePanel() {
        // Abort any active uploads
        Object.keys(_xhrMap).forEach(function (fileId) {
            if (_xhrMap[fileId]) {
                _xhrMap[fileId].abort();
            }
        });
        _xhrMap = {};

        fileSelectionPanel.style.display = 'none';
        _selectedFiles = [];
        if (fileMessageInput) fileMessageInput.value = '';
    }

    if (fileSelectionClose) {
        fileSelectionClose.addEventListener('click', closePanel);
    }

    // --- Send with files ---
    if (fileSendBtn) {
        fileSendBtn.addEventListener('click', async function () {
            if (_selectedFiles.length === 0) return;
            fileSendBtn.disabled = true;

            var content = fileMessageInput ? fileMessageInput.value.trim() : '';
            var tempId = ChatApp.utils.generateId();

            // Collect raw File objects for the pending bubble
            var rawFiles = _selectedFiles.map(function (sf) {
                sf.file._trackId = sf.id;
                return sf.file;
            });

            // Show pending upload bubble in chat immediately
            var pendingBubble = null;
            if (ChatApp.chatArea && ChatApp.chatArea.addPendingUpload) {
                pendingBubble = ChatApp.chatArea.addPendingUpload(tempId, content, rawFiles, function () {
                    // Cancel all uploads
                    Object.keys(_xhrMap).forEach(function (fid) {
                        if (_xhrMap[fid]) _xhrMap[fid].abort();
                    });
                    _xhrMap = {};
                    if (pendingBubble) pendingBubble.complete();
                    _selectedFiles.forEach(function (sf) {
                        sf.status = 'pending';
                        sf.progress = 0;
                    });
                    renderFiles();
                    fileSendBtn.disabled = false;
                });
            }

            // Close panel immediately so user sees the chat with pending bubble
            fileSelectionPanel.style.display = 'none';

            // Upload all pending/failed files
            var fileIds = [];
            var uploadFailed = false;
            for (var i = 0; i < _selectedFiles.length; i++) {
                var sf = _selectedFiles[i];

                // Skip already uploaded files, collect their IDs
                if (sf.status === 'done' && sf.result && sf.result.fileId) {
                    fileIds.push(sf.result.fileId);
                    if (pendingBubble) pendingBubble.updateProgress(100, sf.id);
                    continue;
                }

                sf.status = 'uploading';
                sf.progress = 0;

                var result = await upload(sf.file, (function (fileId) {
                    return function (pct) {
                        sf.progress = pct;
                        if (pendingBubble) pendingBubble.updateProgress(pct, fileId);
                    };
                })(sf.id), sf.id);

                if (result && result.fileId) {
                    sf.status = 'done';
                    sf.result = result;
                    fileIds.push(result.fileId);
                    if (pendingBubble) pendingBubble.updateProgress(100, sf.id);
                } else {
                    sf.status = 'failed';
                    uploadFailed = true;
                }
            }

            if (fileIds.length > 0 && !uploadFailed) {
                // Send message with files
                var convId = ChatApp.chatArea.getCurrentConvId();
                var convType = ChatApp.chatArea.getCurrentConvType();
                var isChannel = convType === 'channel';
                var endpoint = isChannel
                    ? '/api/channels/' + convId + '/messages'
                    : '/api/conversations/' + convId + '/messages';

                var sendResult = await ChatApp.api.post(endpoint, {
                    content: content,
                    fileIds: fileIds
                });

                if (pendingBubble) pendingBubble.complete();
                closePanel();
            } else if (uploadFailed) {
                if (pendingBubble) pendingBubble.fail('Some files failed to upload');
                // Re-show file selection panel for retry
                fileSelectionPanel.style.display = '';
                renderFiles();
            } else {
                // No files uploaded successfully
                if (pendingBubble) pendingBubble.fail('Upload failed');
                fileSelectionPanel.style.display = '';
                renderFiles();
            }

            fileSendBtn.disabled = false;
        });
    }

    /**
     * Upload single file with progress callback
     * @param {File} file
     * @param {Function} onProgress
     * @param {string} [fileId] - optional file ID for XHR tracking (cancel support)
     */
    async function upload(file, onProgress, fileId) {
        var formData = new FormData();
        formData.append('file', file);

        var result = await ChatApp.api.upload('/api/files', formData, onProgress, function (xhr) {
            if (fileId) {
                _xhrMap[fileId] = xhr;
            }
        });

        // Clean up XHR reference
        if (fileId) {
            delete _xhrMap[fileId];
        }

        if (result.isSuccess) {
            return result.value;
        }
        return null;
    }

    // --- Public API ---
    ChatApp.fileUpload = {
        showFileSelection: showFileSelection,
        upload: async function (file) {
            return await upload(file, null);
        },
        closePanel: closePanel
    };

})();
