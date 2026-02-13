/**
 * Messages Operations — replaces Messages.MessageOperations.cs, Messages.Pinning.cs, Messages.Favorites.cs
 * Send, edit, delete, reactions, pin, favorite, mark as later
 */
(function () {
    'use strict';

    // --- Typing indicator (throttled) ---
    const sendTyping = ChatApp.utils.throttle(function () {
        const convId = ChatApp.chatArea.getCurrentConvId();
        const convType = ChatApp.chatArea.getCurrentConvType();
        if (!convId) return;

        if (convType === 'channel') {
            ChatApp.signalR.sendTypingInChannel(convId, true);
        } else {
            const conv = ChatApp.chatArea.getCurrentConv();
            if (conv && conv.otherUserId) {
                ChatApp.signalR.sendTypingInConversation(convId, conv.otherUserId, true);
            }
        }
    }, 2000);

    ChatApp.messages = {
        /**
         * Send a new message
         */
        send: async function (content, mentions, replyToId, files) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || (!content && (!files || files.length === 0))) return;

            const isChannel = convType === 'channel';

            // Optimistic UI: show pending message immediately (text-only, no files)
            var tempId = null;
            if (content && (!files || files.length === 0) && ChatApp.chatArea.addPendingMessage) {
                tempId = 'pending-' + Date.now() + '-' + Math.random().toString(36).substr(2, 5);
                ChatApp.chatArea.addPendingMessage(tempId, content);
            }

            // Upload files first if any
            let fileIds = [];
            if (files && files.length > 0) {
                for (let i = 0; i < files.length; i++) {
                    const result = await ChatApp.fileUpload.upload(files[i]);
                    if (result && result.fileId) {
                        fileIds.push(result.fileId);
                    }
                }
            }

            const payload = {
                content: content || '',
                mentionedUserIds: mentions || [],
                replyToMessageId: replyToId || null,
                fileIds: fileIds.length > 0 ? fileIds : null
            };

            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages'
                : '/api/conversations/' + convId + '/messages';

            const result = await ChatApp.api.post(endpoint, payload);
            if (result.isSuccess) {
                // Stop typing indicator
                if (isChannel) {
                    ChatApp.signalR.sendTypingInChannel(convId, false);
                }
                // Replace pending message with real one if SignalR hasn't already done it
                if (tempId && result.value && result.value.id) {
                    result.value._tempId = tempId;
                    ChatApp.chatArea.appendMessage(result.value);
                }
            } else if (tempId) {
                // Remove pending message on failure
                var pendingEl = document.querySelector('[data-message-id="' + tempId + '"]');
                if (pendingEl) pendingEl.remove();
            }
            return result;
        },

        /**
         * Edit existing message
         */
        edit: async function (messageId, newContent) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || !messageId || !newContent) return;

            const isChannel = convType === 'channel';
            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages/' + messageId
                : '/api/conversations/' + convId + '/messages/' + messageId;

            return await ChatApp.api.put(endpoint, { content: newContent });
        },

        /**
         * Delete message
         */
        deleteMessage: async function (messageId) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || !messageId) return;

            const isChannel = convType === 'channel';
            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages/' + messageId
                : '/api/conversations/' + convId + '/messages/' + messageId;

            return await ChatApp.api.del(endpoint);
        },

        /**
         * Batch delete messages
         */
        batchDelete: async function (messageIds) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || !messageIds || messageIds.length === 0) return;

            const isChannel = convType === 'channel';
            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages/batch-delete'
                : '/api/conversations/' + convId + '/messages/batch-delete';

            return await ChatApp.api.post(endpoint, { messageIds: messageIds });
        },

        /**
         * Toggle emoji reaction
         */
        toggleReaction: async function (messageId, emoji) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || !messageId) return;

            const isChannel = convType === 'channel';
            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages/' + messageId + '/reactions'
                : '/api/conversations/' + convId + '/messages/' + messageId + '/reactions';

            return await ChatApp.api.post(endpoint, { emoji: emoji });
        },

        /**
         * Toggle pin
         */
        togglePin: async function (messageId) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || !messageId) return;

            const isChannel = convType === 'channel';
            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages/' + messageId + '/toggle-pin'
                : '/api/conversations/' + convId + '/messages/' + messageId + '/toggle-pin';

            return await ChatApp.api.post(endpoint);
        },

        /**
         * Toggle favorite
         */
        toggleFavorite: async function (messageId) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || !messageId) return;

            const isChannel = convType === 'channel';
            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages/' + messageId + '/toggle-favorite'
                : '/api/conversations/' + convId + '/messages/' + messageId + '/toggle-favorite';

            return await ChatApp.api.post(endpoint);
        },

        /**
         * Mark as later
         */
        markAsLater: async function (messageId) {
            const convId = ChatApp.chatArea.getCurrentConvId();
            const convType = ChatApp.chatArea.getCurrentConvType();
            if (!convId || !messageId) return;

            const isChannel = convType === 'channel';
            const endpoint = isChannel
                ? '/api/channels/' + convId + '/messages/' + messageId + '/toggle-later'
                : '/api/conversations/' + convId + '/messages/' + messageId + '/toggle-later';

            return await ChatApp.api.post(endpoint);
        },

        /**
         * Send typing indicator
         */
        sendTyping: sendTyping
    };
})();
