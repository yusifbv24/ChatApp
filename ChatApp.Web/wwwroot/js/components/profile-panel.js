/**
 * Profile Panel — replaces ProfilePanel.razor
 * Overlay panel for viewing/editing user profiles
 */
(function () {
    'use strict';

    const overlay = document.getElementById('profilePanelOverlay');
    const container = document.getElementById('profilePanelContainer');
    let _currentUserId = null;
    let _isLoading = false;
    let _isEditingContact = false;
    let _isEditingAboutMe = false;
    let _originalContactValues = {};
    let _originalAboutMe = '';
    let _panelStack = []; // Gap #13: stack of user IDs for nested navigation

    function open(userId, nested) {
        var targetId = userId || (ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null);
        if (!targetId) return;

        // Gap #13: If nested call (clicking subordinate/supervisor), push current to stack
        if (nested && _currentUserId && _currentUserId !== targetId) {
            _panelStack.push(_currentUserId);
        } else if (!nested) {
            _panelStack = [];
        }

        _currentUserId = targetId;
        _isEditingContact = false;
        _isEditingAboutMe = false;

        overlay.style.display = '';
        loadProfile(_currentUserId);
    }

    function close() {
        overlay.style.display = 'none';
        container.innerHTML = '';
        _currentUserId = null;
        _panelStack = [];
        _isEditingContact = false;
        _isEditingAboutMe = false;
        _isLoading = false;
        _originalContactValues = {};
        _originalAboutMe = '';
        // Clean up any feedback toasts created by this panel
        var toasts = document.querySelectorAll('.pp-feedback-toast');
        toasts.forEach(function (t) { t.remove(); });
    }

    // Gap #13: Navigate back to previous profile in the stack
    function goBack() {
        if (_panelStack.length === 0) return;
        _currentUserId = _panelStack.pop();
        _isEditingContact = false;
        _isEditingAboutMe = false;
        loadProfile(_currentUserId);
    }

    async function loadProfile(userId) {
        if (_isLoading) return;
        _isLoading = true;

        // Show skeleton loading
        container.innerHTML = renderSkeleton();

        const isOwnProfile = ChatApp.state.currentUser && userId === ChatApp.state.currentUser.id;
        const endpoint = isOwnProfile ? '/api/identity/users/me' : '/api/identity/users/' + userId;
        const result = await ChatApp.api.get(endpoint);

        _isLoading = false;

        if (!result.isSuccess || !result.value) {
            container.innerHTML = '<div class="profile-panel-error"><p>Failed to load profile</p>' +
                '<button class="btn btn-sm btn-outline-secondary" onclick="ChatApp.profilePanel.close()">Close</button></div>';
            return;
        }

        renderProfile(result.value, isOwnProfile);
    }

    function renderSkeleton() {
        return '<div class="profile-panel">' +
            // Header skeleton
            '<div class="pp-header"><div class="pp-header-info"><div class="pp-skeleton" style="width:180px;height:28px;"></div></div></div>' +
            // Tabs skeleton
            '<div class="pp-tabs"><div class="pp-skeleton" style="width:70px;height:16px;margin:12px 20px;"></div><div class="pp-skeleton" style="width:70px;height:16px;margin:12px 20px;"></div></div>' +
            // Content skeleton
            '<div class="pp-content"><div class="pp-general-layout">' +
            // Left col — avatar card skeleton
            '<div class="pp-left-col">' +
            '<div class="pp-avatar-card" style="align-items:center;">' +
            '<div class="pp-avatar-card-badges" style="justify-content:space-between;width:100%;">' +
            '<div class="pp-skeleton pp-skeleton-badge" style="width:60px;"></div>' +
            '<div class="pp-skeleton pp-skeleton-badge" style="width:70px;"></div></div>' +
            '<div class="pp-skeleton pp-skeleton-avatar"></div>' +
            '<div class="pp-skeleton" style="width:140px;height:14px;margin-top:8px;"></div>' +
            '</div>' +
            // About me card skeleton
            '<div class="pp-card"><div class="pp-card-header"><div class="pp-skeleton" style="width:80px;height:16px;"></div></div>' +
            '<div class="pp-card-body"><div class="pp-skeleton" style="width:100%;height:14px;margin-bottom:8px;"></div>' +
            '<div class="pp-skeleton" style="width:75%;height:14px;"></div></div></div>' +
            '</div>' +
            // Right col — info card skeletons
            '<div class="pp-right-col">' +
            // Contact info skeleton
            '<div class="pp-card"><div class="pp-card-header"><div class="pp-skeleton" style="width:140px;height:16px;"></div></div>' +
            '<div class="pp-card-body"><div class="pp-info-list">' +
            '<div class="pp-info-field"><div class="pp-skeleton" style="width:40px;height:11px;margin-bottom:4px;"></div><div class="pp-skeleton" style="width:180px;height:14px;"></div></div>' +
            '<div class="pp-info-field"><div class="pp-skeleton" style="width:60px;height:11px;margin-bottom:4px;"></div><div class="pp-skeleton" style="width:120px;height:14px;"></div></div>' +
            '</div></div></div>' +
            // Additional info skeleton
            '<div class="pp-card"><div class="pp-card-header"><div class="pp-skeleton" style="width:160px;height:16px;"></div></div>' +
            '<div class="pp-card-body"><div class="pp-info-list">' +
            '<div class="pp-info-field"><div class="pp-skeleton" style="width:70px;height:11px;margin-bottom:4px;"></div><div class="pp-skeleton" style="width:150px;height:14px;"></div></div>' +
            '<div class="pp-info-field"><div class="pp-skeleton" style="width:50px;height:11px;margin-bottom:4px;"></div><div class="pp-skeleton" style="width:100px;height:14px;"></div></div>' +
            '<div class="pp-info-field"><div class="pp-skeleton" style="width:80px;height:11px;margin-bottom:4px;"></div><div class="pp-skeleton" style="width:90px;height:14px;"></div></div>' +
            '</div></div></div>' +
            // Supervisor skeleton
            '<div class="pp-card"><div class="pp-card-header"><div class="pp-skeleton" style="width:80px;height:16px;"></div></div>' +
            '<div class="pp-card-body"><div class="pp-skeleton-relation">' +
            '<div class="pp-skeleton pp-skeleton-sm-circle"></div>' +
            '<div style="flex:1;"><div class="pp-skeleton" style="width:120px;height:14px;margin-bottom:4px;"></div>' +
            '<div class="pp-skeleton" style="width:80px;height:11px;"></div></div></div></div></div>' +
            '</div>' +
            '</div></div></div>';
    }

    function renderProfile(user, isOwnProfile) {
        const fullName = ((user.firstName || '') + ' ' + (user.lastName || '')).trim();
        const isOnline = ChatApp.state.isUserOnline(user.id);

        let html = '<div class="profile-panel">';

        // Side action buttons
        html += '<div class="pp-side-actions visible">';
        // Gap #13: Back button when navigating nested profiles
        if (_panelStack.length > 0) {
            html += '<button class="pp-side-btn" onclick="ChatApp.profilePanel.goBack()" title="Back">' +
                '<span class="material-icons">arrow_back</span></button>';
        }
        html += '<button class="pp-side-btn" onclick="ChatApp.profilePanel.close()" title="Close">' +
            '<span class="material-icons">close</span></button>';
        if (!isOwnProfile) {
            html += '<button class="pp-side-btn chat" data-user-id="' + user.id +
                '" title="Message" onclick="ChatApp.profilePanel.startChat(\'' + user.id + '\')">' +
                '<span class="material-icons">chat</span></button>';
        }
        html += '</div>';

        // Header
        html += '<div class="pp-header">';
        html += '<div class="pp-header-info"><h1 class="pp-header-name">' + ChatApp.utils.escapeHtml(fullName) + '</h1></div>';
        html += '</div>';

        // Tabs
        html += '<div class="pp-tabs">' +
            '<button class="pp-tab active" data-tab="general">General</button>';
        if (isOwnProfile) {
            html += '<button class="pp-tab" data-tab="security">Security</button>';
        }
        html += '</div>';

        // General tab content
        html += '<div class="pp-content" id="profileTabGeneral">';
        html += '<div class="pp-general-layout">';

        // Left column — avatar + about me
        html += '<div class="pp-left-col">';
        // Avatar card
        html += '<div class="pp-avatar-card">';

        // Badges row: role left, online status right
        const roleText = user.role === 1 || user.role === 'Administrator' ? 'Admin' : 'User';
        var effectiveOnline = isOwnProfile ? true : isOnline;
        html += '<div class="pp-avatar-card-badges">';
        html += '<span class="pp-role-badge ' + (roleText === 'Admin' ? 'admin' : 'user') + '">' + roleText + '</span>';
        if (effectiveOnline) {
            html += '<span class="pp-online-badge"><span class="pp-status-dot online"></span> Online</span>';
        } else {
            html += '<div class="pp-status-group"><span class="pp-offline-badge"><span class="pp-status-dot offline"></span> Offline</span>';
            if (user.lastLoginDate) {
                html += '<span class="pp-last-seen">Last seen ' + ChatApp.utils.formatRelativeTime(user.lastLoginDate) + '</span>';
            }
            html += '</div>';
        }
        html += '</div>';

        // Gap #12: Avatar with hover overlay for own profile
        html += '<div class="pp-avatar-wrapper">';
        if (user.avatarUrl) {
            html += '<img src="' + ChatApp.utils.escapeHtml(user.avatarUrl) + '" class="pp-avatar-img" loading="lazy" alt="' +
                ChatApp.utils.escapeHtml(fullName) + '" />';
        } else {
            html += '<div class="pp-avatar-empty" style="background-color:' +
                ChatApp.utils.getAvatarColor(user.id) + ';">' +
                ChatApp.utils.getInitials(fullName) + '</div>';
        }
        if (isOwnProfile) {
            html += '<div class="pp-avatar-overlay" id="avatarOverlay">' +
                '<div class="pp-avatar-overlay-actions">' +
                '<span class="pp-avatar-overlay-btn"><span class="material-icons" style="font-size:20px;">photo_camera</span> Change photo</span>' +
                '<input type="file" accept="image/*" style="display:none;" id="avatarFileInput" onchange="ChatApp.profilePanel.uploadAvatar(this)" />';
            if (user.avatarUrl) {
                html += '<span class="pp-avatar-overlay-btn remove" onclick="event.stopPropagation(); ChatApp.profilePanel.removeAvatar();">' +
                    '<span class="material-icons" style="font-size:14px;">delete</span> Remove</span>';
            }
            html += '</div></div>';
        }
        html += '</div>'; // avatar-wrapper

        if (user.position) html += '<div style="font-size:13px;color:#6b7280;margin-top:4px;">' + ChatApp.utils.escapeHtml(user.position) + '</div>';

        html += '</div>'; // avatar card

        // About me (Gap #10: empty state, Gap #11: inline edit with Save/Cancel and char counter)
        html += '<div class="pp-card">';
        html += '<div class="pp-card-header"><h3>About Me</h3>';
        if (isOwnProfile && !_isEditingAboutMe && user.aboutMe && user.aboutMe.trim()) {
            html += '<button class="pp-edit-link" onclick="ChatApp.profilePanel.startEditAboutMe()">Edit</button>';
        }
        html += '</div>';
        html += '<div class="pp-card-body">';
        if (isOwnProfile) {
            if (_isEditingAboutMe) {
                // Edit mode with textarea, char counter, Save/Cancel
                var aboutLen = (user.aboutMe || '').length;
                html += '<textarea class="pp-inline-input pp-textarea" id="profileAboutMe" maxlength="2000" placeholder="Write something about yourself...">' +
                    ChatApp.utils.escapeHtml(user.aboutMe || '') + '</textarea>' +
                    '<div class="pp-about-footer"><div class="pp-char-counter' + (aboutLen >= 2000 ? ' limit' : '') + '"><span id="aboutCounter">' + aboutLen + '</span>/2000</div></div>' +
                    '<div class="pp-inline-actions">' +
                    '<button class="pp-inline-save" onclick="ChatApp.profilePanel.saveAboutMe()">Save</button>' +
                    '<button class="pp-inline-cancel" onclick="ChatApp.profilePanel.cancelAboutMe()">Cancel</button>' +
                    '</div>';
            } else if (user.aboutMe && user.aboutMe.trim()) {
                // Display mode with text
                html += '<p class="pp-about-text">' + ChatApp.utils.escapeHtml(user.aboutMe) + '</p>';
            } else {
                // Gap #10: Empty state
                html += '<div class="pp-about-empty-state">' +
                    '<div class="pp-about-empty-icon"><span class="material-icons" style="font-size:32px;color:#2196f3;">person</span></div>' +
                    '<p class="pp-about-empty-text">Share interesting life stories...</p>' +
                    '<button class="pp-about-tell-btn" onclick="ChatApp.profilePanel.startEditAboutMe()">TELL ABOUT YOURSELF</button>' +
                    '</div>';
            }
        } else {
            if (user.aboutMe && user.aboutMe.trim()) {
                html += '<p class="pp-about-text">' + ChatApp.utils.escapeHtml(user.aboutMe) + '</p>';
            } else {
                html += '<p class="pp-about-empty">No information provided.</p>';
            }
        }
        html += '</div></div>'; // card-body + card
        html += '</div>'; // left col

        // Right column — contact info + additional
        html += '<div class="pp-right-col">';

        // Contact info card (Gap #9: grouped edit mode, Gap #16: Last Visit)
        html += '<div class="pp-card"><div class="pp-card-header"><h3>Contact Information</h3>';
        if (isOwnProfile && !_isEditingContact) {
            html += '<button class="pp-edit-link" onclick="ChatApp.profilePanel.startEditContact()">Edit</button>';
        }
        html += '</div><div class="pp-card-body"><div class="pp-info-list">';
        html += renderInfoRow('email', 'Email', user.email, isOwnProfile && _isEditingContact, 'email');
        html += renderInfoRow('phone', 'Work Phone', user.workPhone, isOwnProfile && _isEditingContact, 'workPhone');
        // Gap #16: Last Visit (read-only)
        if (user.lastVisit) {
            html += renderInfoRow('schedule', 'Last visit', ChatApp.utils.formatLastVisit(user.lastVisit), false);
        }
        if (isOwnProfile && _isEditingContact) {
            html += '<div class="pp-inline-actions">' +
                '<button class="pp-inline-save" onclick="ChatApp.profilePanel.saveContact()">Save</button>' +
                '<button class="pp-inline-cancel" onclick="ChatApp.profilePanel.cancelContact()">Cancel</button>' +
                '</div>';
        }
        html += '</div></div></div>';

        // Additional info card (Gap #9: firstName, lastName, dateOfBirth, hiringDate, positionId in grouped edit mode)
        html += '<div class="pp-card"><div class="pp-card-header"><h3>Additional Information</h3></div><div class="pp-card-body"><div class="pp-info-list">';
        if (isOwnProfile && _isEditingContact) {
            html += renderInfoRow('person', 'First Name', user.firstName, true, 'firstName');
            html += renderInfoRow('person', 'Last Name', user.lastName, true, 'lastName');
        }
        html += renderInfoRow('business', 'Department', user.departmentName, false);
        if (isOwnProfile && user.departmentId) {
            html += '<div class="pp-info-field">' +
                '<label>Position</label>' +
                '<select class="pp-inline-input" id="profilePositionSelect" data-field="positionId"' +
                (!_isEditingContact ? ' disabled' : '') + '>' +
                '<option value="">Select position...</option></select></div>';
        } else {
            html += renderInfoRow('work', 'Position', user.position, false);
        }
        var editingContact = isOwnProfile && _isEditingContact;
        html += renderInfoRow('cake', 'Date of Birth', user.dateOfBirth ?
            (editingContact ? ChatApp.utils.formatDateForInput(user.dateOfBirth) : new Date(user.dateOfBirth).toLocaleDateString()) : '',
            editingContact, 'dateOfBirth', 'date');
        html += renderInfoRow('event', 'Hiring Date', user.hiringDate ?
            (editingContact ? ChatApp.utils.formatDateForInput(user.hiringDate) : new Date(user.hiringDate).toLocaleDateString()) : '',
            editingContact, 'hiringDate', 'date');
        html += renderInfoRow('schedule', 'Member Since', (user.createdAtUtc || user.createdAt) ?
            new Date(user.createdAtUtc || user.createdAt).toLocaleDateString() : '', false);
        html += '</div></div></div>';

        // Relations — Supervisor (Gap #15: Head of Department label)
        if (user.supervisor) {
            var supervisorLabel = user.isHeadOfDepartment ? 'Head of department' : 'Supervisor';
            html += '<div class="pp-card"><div class="pp-card-header"><h3>' + supervisorLabel + '</h3></div><div class="pp-card-body">';
            html += '<div class="pp-relation-section">' + renderRelationUser(user.supervisor) + '</div>';
            html += '</div></div>';
        }

        // Relations — Subordinates
        if (user.subordinates && user.subordinates.length > 0) {
            html += '<div class="pp-card"><div class="pp-card-header"><h3>Subordinates (' + user.subordinates.length + ')</h3></div><div class="pp-card-body">';
            html += '<div class="pp-subordinates-grid">';
            var subLimit = 5;
            user.subordinates.forEach(function (sub, idx) {
                html += '<div class="pp-subordinate-item' + (idx >= subLimit ? ' pp-subordinate-hidden' : '') + '"' +
                    (idx >= subLimit ? ' style="display:none;"' : '') + '>' + renderRelationUser(sub) + '</div>';
            });
            html += '</div>';
            if (user.subordinates.length > subLimit) {
                html += '<button class="pp-show-more-link" id="showMoreSubs">' +
                    'Show more (' + (user.subordinates.length - subLimit) + ' more)</button>';
            }
            html += '</div></div>';
        }

        html += '</div>'; // right col
        html += '</div>'; // general layout
        html += '</div>'; // general tab

        // Security tab content
        html += '<div class="pp-content" id="profileTabSecurity" style="display:none;">';
        if (isOwnProfile) {
            html += '<div class="pp-security-layout"><div class="pp-card">' +
                '<div class="pp-card-header"><h3>Change Password</h3></div>' +
                '<div class="pp-card-body">' +
                '<div class="pp-form-group"><label>Current Password</label>' +
                '<div class="pp-password-wrapper"><input type="password" class="pp-inline-input" id="currentPassword" /></div></div>' +
                '<div class="pp-form-group"><label>New Password</label>' +
                '<div class="pp-password-wrapper"><input type="password" class="pp-inline-input" id="newPassword" /></div></div>' +
                '<div class="pp-form-group"><label>Confirm New Password</label>' +
                '<div class="pp-password-wrapper"><input type="password" class="pp-inline-input" id="confirmPassword" /></div></div>' +
                '<div id="passwordError" class="pp-field-message error" style="display:none;"></div>' +
                '<div id="passwordSuccess" class="pp-field-message success" style="display:none;"></div>' +
                '<button class="pp-save-btn" onclick="ChatApp.profilePanel.changePassword()">Change Password</button>' +
                '</div></div></div>';
        }
        html += '</div>'; // security tab

        html += '</div>'; // panel

        container.innerHTML = html;

        // Tab switching
        container.querySelectorAll('.pp-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                container.querySelectorAll('.pp-tab').forEach(function (t) { t.classList.remove('active'); });
                tab.classList.add('active');
                const tabName = tab.dataset.tab;
                document.getElementById('profileTabGeneral').style.display = tabName === 'general' ? '' : 'none';
                document.getElementById('profileTabSecurity').style.display = tabName === 'security' ? '' : 'none';
            });
        });

        // Gap #11: About me counter with limit class and Escape key handler
        const aboutInput = document.getElementById('profileAboutMe');
        const aboutCounter = document.getElementById('aboutCounter');
        if (aboutInput && aboutCounter) {
            aboutInput.addEventListener('input', function () {
                var len = aboutInput.value.length;
                aboutCounter.textContent = len;
                var counterDiv = aboutCounter.parentElement;
                if (len >= 2000) {
                    counterDiv.classList.add('limit');
                } else {
                    counterDiv.classList.remove('limit');
                }
            });
            // Gap #11: Escape key cancels about me editing
            aboutInput.addEventListener('keydown', function (e) {
                if (e.key === 'Escape') {
                    ChatApp.profilePanel.cancelAboutMe();
                }
            });
        }

        // Gap #12: Avatar overlay click triggers file input
        var avatarOverlay = document.getElementById('avatarOverlay');
        var avatarFileInput = document.getElementById('avatarFileInput');
        if (avatarOverlay && avatarFileInput) {
            avatarOverlay.addEventListener('click', function (e) {
                // Only trigger file input if not clicking the remove button
                if (!e.target.closest('.pp-avatar-overlay-btn.remove')) {
                    avatarFileInput.click();
                }
            });
        }

        // Load position dropdown if own profile
        var posSelect = document.getElementById('profilePositionSelect');
        if (posSelect && user.departmentId) {
            loadPositionsByDepartment(user.departmentId).then(function (positions) {
                positions.forEach(function (pos) {
                    var opt = document.createElement('option');
                    opt.value = pos.id;
                    opt.textContent = pos.name || pos.title || '';
                    if (pos.name === user.position || pos.id === user.positionId) opt.selected = true;
                    posSelect.appendChild(opt);
                });
            });
        }

        // Password visibility toggles
        ['currentPassword', 'newPassword', 'confirmPassword'].forEach(function (fieldId) {
            var pwInput = document.getElementById(fieldId);
            if (pwInput) {
                var wrapper = pwInput.parentElement;
                var toggleBtn = document.createElement('button');
                toggleBtn.type = 'button';
                toggleBtn.className = 'pp-password-toggle';
                toggleBtn.innerHTML = '<span class="material-icons" style="font-size:18px;">visibility_off</span>';
                wrapper.appendChild(toggleBtn);
                toggleBtn.addEventListener('click', function () {
                    var isHidden = pwInput.type === 'password';
                    pwInput.type = isHidden ? 'text' : 'password';
                    toggleBtn.querySelector('.material-icons').textContent = isHidden ? 'visibility' : 'visibility_off';
                });
            }
        });

        // Subordinates "Show more" handler
        var showMoreBtn = document.getElementById('showMoreSubs');
        if (showMoreBtn) {
            showMoreBtn.addEventListener('click', function () {
                container.querySelectorAll('.pp-subordinate-hidden').forEach(function (el) {
                    el.style.display = '';
                    el.classList.remove('pp-subordinate-hidden');
                });
                showMoreBtn.style.display = 'none';
            });
        }
    }

    function renderInfoRow(icon, label, value, editable, fieldName, inputType) {
        let html = '<div class="pp-info-field">';
        html += '<label>' + label + '</label>';
        if (editable && fieldName) {
            var type = inputType || 'text';
            // Gap #9: No onchange — grouped edit mode uses batch Save
            html += '<input class="pp-inline-input" type="' + type + '" value="' +
                ChatApp.utils.escapeHtml(value || '') + '" data-field="' + fieldName +
                '" placeholder="Not set" />';
        } else {
            var valueClass = 'pp-info-value';
            if (fieldName === 'email') valueClass += ' email';
            if (!value) valueClass += ' readonly';
            html += '<div class="' + valueClass + '">' + ChatApp.utils.escapeHtml(value || 'Not set') + '</div>';
        }
        html += '</div>';
        return html;
    }

    // Gap #13: renderRelationUser uses nested open to stack panels
    function renderRelationUser(rel) {
        const fullName = ((rel.firstName || '') + ' ' + (rel.lastName || '')).trim();
        return '<div class="pp-relation-item pp-clickable" onclick="ChatApp.profilePanel.openNested(\'' + rel.id + '\')">' +
            '<div class="pp-relation-avatar">' + ChatApp.utils.renderAvatar(
                { id: rel.id, avatarUrl: rel.avatarUrl, fullName: fullName }, 'pp-relation-avatar-img') + '</div>' +
            '<div class="pp-relation-info">' +
            '<div class="pp-relation-name">' + ChatApp.utils.escapeHtml(fullName) + '</div>' +
            '<div class="pp-relation-position">' + ChatApp.utils.escapeHtml(rel.position || '') + '</div>' +
            '</div></div>';
    }

    function showFeedback(message, type) {
        // Remove any existing feedback
        var existing = container.querySelector('.pp-feedback-toast');
        if (existing) existing.remove();
        var toast = document.createElement('div');
        toast.className = 'pp-feedback-toast ' + (type || 'success');
        toast.textContent = message;
        toast.style.cssText = 'position:fixed;top:20px;left:50%;transform:translateX(-50%);z-index:9999;' +
            'padding:10px 24px;border-radius:8px;font-size:14px;font-weight:500;animation:pp-msg-in 0.25s ease-out;' +
            (type === 'error' ? 'background:#fef2f2;color:#dc2626;border:1px solid rgba(239,68,68,0.2);' : 'background:#f0fdf4;color:#059669;border:1px solid rgba(16,185,129,0.2);');
        document.body.appendChild(toast);
        setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.3s ease';
            setTimeout(function () { toast.remove(); }, 300);
        }, 3000);
    }

    async function loadPositionsByDepartment(departmentId) {
        var result = await ChatApp.api.get('/api/identity/positions/by-department/' + departmentId);
        if (result.isSuccess) return result.value || [];
        return [];
    }

    // --- Public API ---
    ChatApp.profilePanel = {
        open: function (userId) { open(userId, false); },
        // Gap #13: Nested open for subordinate/supervisor clicks
        openNested: function (userId) { open(userId, true); },
        close: close,
        // Gap #13: Back navigation
        goBack: goBack,

        startChat: function (userId) {
            close();
            ChatApp.state.setPendingChatUserId(userId);
            window.location.href = '/messages';
        },

        uploadAvatar: async function (input) {
            if (!input.files || !input.files[0]) return;
            var file = input.files[0];
            if (file.size > 10 * 1024 * 1024) {
                showFeedback('File size must be less than 10MB', 'error');
                input.value = '';
                return;
            }
            const formData = new FormData();
            formData.append('file', file);
            const result = await ChatApp.api.upload('/api/identity/users/me/avatar', formData);
            if (result.isSuccess) {
                showFeedback('Avatar updated successfully', 'success');
                await ChatApp.auth.loadCurrentUser();
                loadProfile(_currentUserId);
            } else {
                showFeedback(result.error || 'Failed to upload avatar', 'error');
            }
        },

        removeAvatar: async function () {
            const result = await ChatApp.api.del('/api/identity/users/me/avatar');
            if (result.isSuccess) {
                showFeedback('Avatar removed', 'success');
                await ChatApp.auth.loadCurrentUser();
                loadProfile(_currentUserId);
            } else {
                showFeedback(result.error || 'Failed to remove avatar', 'error');
            }
        },

        // Gap #11: About Me edit mode management
        startEditAboutMe: function () {
            _isEditingAboutMe = true;
            // Store original for cancel
            var aboutEl = container.querySelector('.pp-about-text');
            _originalAboutMe = aboutEl ? aboutEl.textContent : '';
            loadProfile(_currentUserId);
        },

        saveAboutMe: async function () {
            const input = document.getElementById('profileAboutMe');
            if (!input) return;
            var result = await ChatApp.api.put('/api/identity/users/me', { aboutMe: input.value });
            if (result.isSuccess) {
                _isEditingAboutMe = false;
                showFeedback('Saved successfully', 'success');
                loadProfile(_currentUserId);
            } else {
                showFeedback(result.error || 'Failed to save', 'error');
            }
        },

        cancelAboutMe: function () {
            _isEditingAboutMe = false;
            loadProfile(_currentUserId);
        },

        // Gap #9: Contact grouped edit mode
        startEditContact: function () {
            _isEditingContact = true;
            // Store original values for cancel
            _originalContactValues = {};
            container.querySelectorAll('[data-field]').forEach(function (el) {
                _originalContactValues[el.dataset.field] = el.value;
            });
            loadProfile(_currentUserId);
        },

        saveContact: async function () {
            var data = {};
            container.querySelectorAll('.pp-inline-input[data-field]').forEach(function (el) {
                data[el.dataset.field] = el.value;
            });
            var posSelect = document.getElementById('profilePositionSelect');
            if (posSelect && !posSelect.disabled) {
                data.positionId = posSelect.value;
            }
            var result = await ChatApp.api.put('/api/identity/users/me', data);
            if (result.isSuccess) {
                _isEditingContact = false;
                showFeedback('Saved successfully', 'success');
                await ChatApp.auth.loadCurrentUser();
                loadProfile(_currentUserId);
            } else {
                showFeedback(result.error || 'Failed to save', 'error');
            }
        },

        cancelContact: function () {
            _isEditingContact = false;
            loadProfile(_currentUserId);
        },

        saveField: async function (input) {
            const field = input.dataset.field;
            const value = input.value;
            const data = {};
            data[field] = value;
            await ChatApp.api.put('/api/identity/users/me', data);
        },

        changePassword: async function () {
            const current = document.getElementById('currentPassword');
            const newPw = document.getElementById('newPassword');
            const confirm = document.getElementById('confirmPassword');
            const errEl = document.getElementById('passwordError');
            const successEl = document.getElementById('passwordSuccess');
            errEl.textContent = '';
            errEl.style.display = 'none';
            successEl.textContent = '';
            successEl.style.display = 'none';

            if (!current.value || !newPw.value || !confirm.value) {
                errEl.textContent = 'All fields are required.';
                errEl.style.display = '';
                return;
            }
            if (newPw.value !== confirm.value) {
                errEl.textContent = 'Passwords do not match.';
                errEl.style.display = '';
                return;
            }
            // Password complexity validation matching Blazor rules
            var pwErrors = [];
            if (newPw.value.length < 8) pwErrors.push('at least 8 characters');
            if (!/[A-Z]/.test(newPw.value)) pwErrors.push('an uppercase letter');
            if (!/[a-z]/.test(newPw.value)) pwErrors.push('a lowercase letter');
            if (!/[0-9]/.test(newPw.value)) pwErrors.push('a digit');
            if (!/[^A-Za-z0-9]/.test(newPw.value)) pwErrors.push('a special character');
            if (pwErrors.length > 0) {
                errEl.textContent = 'Password must contain ' + pwErrors.join(', ') + '.';
                errEl.style.display = '';
                return;
            }

            const result = await ChatApp.api.post('/api/identity/users/change-password', {
                userId: ChatApp.state.currentUser.id,
                currentPassword: current.value,
                newPassword: newPw.value,
                confirmNewPassword: confirm.value
            });

            if (result.isSuccess) {
                successEl.textContent = 'Password changed successfully.';
                successEl.style.display = '';
                current.value = '';
                newPw.value = '';
                confirm.value = '';
            } else {
                errEl.textContent = result.error || 'Failed to change password.';
                errEl.style.display = '';
            }
        }
    };

    // Close on overlay click (not container)
    if (overlay) {
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) close();
        });
    }


})();
