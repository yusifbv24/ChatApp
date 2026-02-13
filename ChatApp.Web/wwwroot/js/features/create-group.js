/**
 * Create Group Panel — full group/channel creation with member picker
 * Replaces the simple Bootstrap modal with a full panel experience
 */
(function () {
    'use strict';

    const panel = document.getElementById('createGroupPanel');
    const chatContent = document.getElementById('chatContent');
    const chatEmptyState = document.getElementById('chatEmptyState');
    const groupNameInput = document.getElementById('groupNameInput');
    const groupNameCounter = document.getElementById('groupNameCounter');
    const groupDescInput = document.getElementById('groupDescInput');
    const groupDescCounter = document.getElementById('groupDescCounter');
    const groupAvatarInput = document.getElementById('groupAvatarInput');
    const groupAvatarPreview = document.getElementById('groupAvatarPreview');
    const groupAvatarIcon = document.getElementById('groupAvatarIcon');
    const groupAvatarOverlay = document.getElementById('groupAvatarOverlay');
    const groupAvatarLabel = document.getElementById('groupAvatarLabel');
    const membersInputContainer = document.getElementById('membersInputContainer');
    const memberChipsContainer = document.getElementById('memberChipsContainer');
    const addMemberTrigger = document.getElementById('addMemberTrigger');
    const inlineMemberSearch = document.getElementById('inlineMemberSearch');
    const memberPickerDropdown = document.getElementById('memberPickerDropdown');
    const pickerContent = document.getElementById('pickerContent');
    const pickerTabRecent = document.getElementById('pickerTabRecent');
    const pickerTabDepts = document.getElementById('pickerTabDepts');
    const pickerCloseBtn = document.getElementById('pickerCloseBtn');
    const settingsToggle = document.getElementById('settingsToggle');
    const settingsContent = document.getElementById('settingsContent');
    const settingsExpandIcon = document.getElementById('settingsExpandIcon');
    const createGroupSubmit = document.getElementById('createGroupSubmit');
    const createGroupCancel = document.getElementById('createGroupCancel');
    const creatorChipAvatar = document.getElementById('creatorChipAvatar');
    const creatorChipName = document.getElementById('creatorChipName');

    if (!panel) return;

    let _mode = 'channel'; // 'channel' | 'dm'
    let _selectedMembers = []; // [{id, fullName, avatarUrl, email}]
    let _searchResults = [];
    let _departments = [];
    let _departmentUsersCache = {};
    let _expandedDeptIds = new Set();
    let _selectedDeptIds = new Set();
    let _currentTab = 'recent';
    let _isSearching = false;
    let _isLoadingDepts = false;
    let _isCreating = false;
    let _avatarFile = null;
    let _avatarPreviewUrl = null;
    let _showSettings = false;
    let _searchDebounceTimer = null;

    // --- Open / Close ---
    function openPanel() {
        // Reset state
        _selectedMembers = [];
        _searchResults = [];
        _expandedDeptIds.clear();
        _selectedDeptIds.clear();
        _currentTab = 'recent';
        _isCreating = false;
        _avatarFile = null;
        _avatarPreviewUrl = null;
        _showSettings = false;

        // Reset form
        groupNameInput.value = '';
        groupNameCounter.textContent = '0/100';
        groupNameCounter.classList.remove('exceeded');
        if (groupDescInput) { groupDescInput.value = ''; }
        if (groupDescCounter) { groupDescCounter.textContent = '0/500'; }
        document.querySelectorAll('input[name="chatType"][value="1"]').forEach(function(r) { r.checked = true; });

        // Reset avatar
        groupAvatarPreview.style.display = 'none';
        groupAvatarIcon.style.display = '';
        groupAvatarOverlay.style.display = 'none';
        groupAvatarLabel.classList.remove('has-image');

        // Reset members UI
        memberChipsContainer.innerHTML = '';
        closeMemberPicker();
        settingsContent.style.display = 'none';
        settingsExpandIcon.textContent = 'expand_more';

        // Set creator chip
        var user = ChatApp.state.currentUser;
        if (user) {
            creatorChipName.textContent = user.fullName || '';
            if (user.avatarUrl) {
                creatorChipAvatar.innerHTML = '<img src="' + ChatApp.utils.escapeHtml(user.avatarUrl) + '" class="chip-avatar" />';
            } else {
                creatorChipAvatar.style.backgroundColor = ChatApp.utils.getAvatarColor(user.id);
                creatorChipAvatar.innerHTML = '<span class="material-icons creator-icon" style="font-size:14px;">person</span>';
            }
        }

        // Initialize mode tabs
        _mode = 'channel';
        initModeTabs();
        updateModeView();

        // Show panel, hide others
        chatEmptyState.style.display = 'none';
        chatContent.style.display = 'none';
        panel.style.display = '';

        updateSubmitButton();
        groupNameInput.focus();
    }

    // --- Mode Tabs (New Channel vs New Message) ---
    function initModeTabs() {
        var existing = panel.querySelector('.create-mode-tabs');
        if (existing) existing.remove();

        var tabsContainer = document.createElement('div');
        tabsContainer.className = 'create-mode-tabs';
        tabsContainer.style.cssText = 'display:flex;gap:0;margin-bottom:16px;border-bottom:1px solid #e0e0e0;';

        var channelTab = document.createElement('button');
        channelTab.className = 'create-mode-tab active';
        channelTab.type = 'button';
        channelTab.textContent = 'New Channel';
        channelTab.style.cssText = 'flex:1;padding:10px;border:none;background:none;cursor:pointer;font-size:14px;font-weight:500;border-bottom:2px solid transparent;';

        var dmTab = document.createElement('button');
        dmTab.className = 'create-mode-tab';
        dmTab.type = 'button';
        dmTab.textContent = 'New Message';
        dmTab.style.cssText = 'flex:1;padding:10px;border:none;background:none;cursor:pointer;font-size:14px;font-weight:500;border-bottom:2px solid transparent;';

        function setActiveTab(mode) {
            _mode = mode;
            channelTab.classList.toggle('active', mode === 'channel');
            dmTab.classList.toggle('active', mode === 'dm');
            channelTab.style.borderBottomColor = mode === 'channel' ? '#4caf50' : 'transparent';
            channelTab.style.color = mode === 'channel' ? '#4caf50' : '#666';
            dmTab.style.borderBottomColor = mode === 'dm' ? '#4caf50' : 'transparent';
            dmTab.style.color = mode === 'dm' ? '#4caf50' : '#666';
            updateModeView();
        }

        channelTab.addEventListener('click', function () { setActiveTab('channel'); });
        dmTab.addEventListener('click', function () { setActiveTab('dm'); });

        tabsContainer.appendChild(channelTab);
        tabsContainer.appendChild(dmTab);

        // Insert at top of panel content
        var panelBody = panel.querySelector('.create-group-body') || panel.firstElementChild;
        if (panelBody) {
            panelBody.insertBefore(tabsContainer, panelBody.firstChild);
        } else {
            panel.insertBefore(tabsContainer, panel.firstChild);
        }

        // Set initial active state
        channelTab.style.borderBottomColor = '#4caf50';
        channelTab.style.color = '#4caf50';
    }

    function updateModeView() {
        // Get/create DM search section
        var dmSection = panel.querySelector('.dm-search-section');
        // Get channel form elements
        var channelFormElements = panel.querySelectorAll('.create-group-form, .group-form-section, .create-group-actions');

        if (_mode === 'dm') {
            // Hide channel form
            channelFormElements.forEach(function (el) { el.style.display = 'none'; });
            // Show DM section
            if (!dmSection) {
                dmSection = createDmSection();
                var tabsEl = panel.querySelector('.create-mode-tabs');
                if (tabsEl && tabsEl.nextSibling) {
                    tabsEl.parentNode.insertBefore(dmSection, tabsEl.nextSibling);
                } else {
                    (panel.querySelector('.create-group-body') || panel).appendChild(dmSection);
                }
            }
            dmSection.style.display = '';
            var dmInput = dmSection.querySelector('.dm-user-search');
            if (dmInput) dmInput.focus();
        } else {
            // Show channel form
            channelFormElements.forEach(function (el) { el.style.display = ''; });
            // Hide DM section
            if (dmSection) dmSection.style.display = 'none';
            groupNameInput.focus();
        }
    }

    function createDmSection() {
        var section = document.createElement('div');
        section.className = 'dm-search-section';
        section.style.cssText = 'padding:0 16px;';

        section.innerHTML = '<div style="margin-bottom:12px;">' +
            '<input type="text" class="dm-user-search form-control" placeholder="Search users to start a conversation..." style="width:100%;padding:10px 12px;border:1px solid #e0e0e0;border-radius:8px;font-size:14px;" />' +
            '</div>' +
            '<div class="dm-user-results" style="max-height:400px;overflow-y:auto;"></div>';

        var searchInput = section.querySelector('.dm-user-search');
        var resultsDiv = section.querySelector('.dm-user-results');

        var dmSearchTimer = null;
        searchInput.addEventListener('input', function () {
            if (dmSearchTimer) clearTimeout(dmSearchTimer);
            dmSearchTimer = setTimeout(function () {
                var q = searchInput.value.trim();
                if (q.length < 2) {
                    resultsDiv.innerHTML = '<div style="text-align:center;color:#999;padding:24px 0;">Type to search for users...</div>';
                    return;
                }
                searchDmUsers(q, resultsDiv);
            }, 300);
        });

        resultsDiv.innerHTML = '<div style="text-align:center;color:#999;padding:24px 0;">Type to search for users...</div>';

        return section;
    }

    async function searchDmUsers(query, resultsDiv) {
        resultsDiv.innerHTML = '<div style="text-align:center;padding:16px;"><div class="spinner-border spinner-border-sm text-secondary"></div></div>';

        var result = await ChatApp.api.get('/api/identity/users/search?query=' + encodeURIComponent(query));
        if (!result.isSuccess) {
            resultsDiv.innerHTML = '<div style="text-align:center;color:#999;padding:16px;">Search failed</div>';
            return;
        }

        var currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
        var users = (result.value || []).filter(function (u) { return u.id !== currentUserId; });

        if (users.length === 0) {
            resultsDiv.innerHTML = '<div style="text-align:center;color:#999;padding:16px;">No users found</div>';
            return;
        }

        resultsDiv.innerHTML = '';
        users.forEach(function (user) {
            var fullName = user.fullName || ((user.firstName || '') + ' ' + (user.lastName || '')).trim();
            var avatarColor = ChatApp.utils.getAvatarColor(user.id);
            var avatarHtml = user.avatarUrl
                ? '<img src="' + ChatApp.utils.escapeHtml(user.avatarUrl) + '" style="width:36px;height:36px;border-radius:50%;object-fit:cover;" />'
                : '<div style="width:36px;height:36px;border-radius:50%;background-color:' + avatarColor + ';display:flex;align-items:center;justify-content:center;"><span class="material-icons" style="font-size:16px;color:#fff;">person</span></div>';

            var item = document.createElement('div');
            item.style.cssText = 'display:flex;align-items:center;gap:12px;padding:10px 8px;cursor:pointer;border-radius:8px;transition:background 0.15s;';
            item.innerHTML = avatarHtml + '<span style="font-size:14px;font-weight:500;">' + ChatApp.utils.escapeHtml(fullName) + '</span>';

            item.addEventListener('mouseenter', function () { item.style.background = '#f5f5f5'; });
            item.addEventListener('mouseleave', function () { item.style.background = ''; });

            item.addEventListener('click', function () {
                startDirectMessage(user.id);
            });

            resultsDiv.appendChild(item);
        });
    }

    async function startDirectMessage(userId) {
        var result = await ChatApp.api.post('/api/conversations', { otherUserId: userId });
        if (result.isSuccess && result.value) {
            var convId = result.value.id || result.value;
            closePanel();
            // Reload conversation list and navigate to the conversation
            if (ChatApp.conversationList && ChatApp.conversationList.reload) {
                ChatApp.conversationList.reload();
            }
            if (ChatApp.conversationList && ChatApp.conversationList.selectById) {
                setTimeout(function() {
                    ChatApp.conversationList.selectById(convId);
                }, 500);
            }
        }
    }

    function closePanel() {
        panel.style.display = 'none';
        // Show empty state if no conversation selected
        if (!ChatApp.chatArea.getCurrentConvId()) {
            chatEmptyState.style.display = '';
        } else {
            chatContent.style.display = '';
        }
        _avatarFile = null;
        _avatarPreviewUrl = null;
    }

    // --- Name char counter ---
    groupNameInput.addEventListener('input', function () {
        var len = groupNameInput.value.length;
        groupNameCounter.textContent = len + '/100';
        groupNameCounter.classList.toggle('exceeded', len > 100);
        groupNameInput.classList.toggle('invalid', len > 100);
        updateSubmitButton();
    });

    // --- Description char counter ---
    if (groupDescInput) {
        groupDescInput.addEventListener('input', function () {
            var len = groupDescInput.value.length;
            groupDescCounter.textContent = len + '/500';
            groupDescCounter.classList.toggle('exceeded', len > 500);
            groupDescInput.classList.toggle('invalid', len > 500);
            updateSubmitButton();
        });
    }

    // --- Avatar ---
    groupAvatarInput.addEventListener('change', function () {
        var file = groupAvatarInput.files[0];
        if (!file) return;
        if (!file.type.startsWith('image/')) return;
        if (file.size > 5 * 1024 * 1024) { alert('Image must be less than 5MB'); return; }

        _avatarFile = file;
        _avatarPreviewUrl = URL.createObjectURL(file);
        groupAvatarPreview.src = _avatarPreviewUrl;
        groupAvatarPreview.style.display = '';
        groupAvatarIcon.style.display = 'none';
        groupAvatarOverlay.style.display = '';
        groupAvatarLabel.classList.add('has-image');
        groupAvatarInput.value = '';
    });

    // --- Settings toggle ---
    settingsToggle.addEventListener('click', function () {
        _showSettings = !_showSettings;
        settingsContent.style.display = _showSettings ? '' : 'none';
        settingsExpandIcon.textContent = _showSettings ? 'expand_less' : 'expand_more';
    });

    // --- Member Picker ---
    addMemberTrigger.addEventListener('click', function () {
        addMemberTrigger.style.display = 'none';
        inlineMemberSearch.style.display = '';
        inlineMemberSearch.value = '';
        inlineMemberSearch.focus();
        memberPickerDropdown.style.display = '';
        setPickerTab('recent');
        pickerContent.innerHTML = '<div class="picker-hint">Type to search users...</div>';
    });

    inlineMemberSearch.addEventListener('input', function () {
        var query = inlineMemberSearch.value.trim();
        if (_searchDebounceTimer) clearTimeout(_searchDebounceTimer);
        _searchDebounceTimer = setTimeout(function () {
            if (_currentTab === 'recent') {
                if (query.length < 2) {
                    pickerContent.innerHTML = '<div class="picker-hint">Type to search users...</div>';
                    return;
                }
                searchUsers(query);
            } else {
                renderDepartments(query);
            }
        }, 300);
    });

    // Tab switching
    pickerTabRecent.addEventListener('click', function () { setPickerTab('recent'); });
    pickerTabDepts.addEventListener('click', function () { setPickerTab('departments'); });

    function setPickerTab(tab) {
        _currentTab = tab;
        pickerTabRecent.classList.toggle('active', tab === 'recent');
        pickerTabDepts.classList.toggle('active', tab === 'departments');
        if (tab === 'departments' && _departments.length === 0) {
            loadDepartments();
        } else if (tab === 'departments') {
            renderDepartments(inlineMemberSearch.value.trim());
        } else {
            var q = inlineMemberSearch.value.trim();
            if (q.length >= 2) { searchUsers(q); }
            else { pickerContent.innerHTML = '<div class="picker-hint">Type to search users...</div>'; }
        }
    }

    pickerCloseBtn.addEventListener('click', closeMemberPicker);

    function closeMemberPicker() {
        memberPickerDropdown.style.display = 'none';
        inlineMemberSearch.style.display = 'none';
        addMemberTrigger.style.display = '';
    }

    // --- Search users ---
    async function searchUsers(query) {
        _isSearching = true;
        pickerContent.innerHTML = '<div class="picker-loading"><div class="spinner-border spinner-border-sm"></div> Searching...</div>';

        var result = await ChatApp.api.get('/api/identity/users/search?query=' + encodeURIComponent(query));
        _isSearching = false;

        if (result.isSuccess && result.value) {
            var currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
            _searchResults = (result.value || []).filter(function (u) {
                return u.id !== currentUserId;
            });
            renderUserResults();
        }
    }

    function renderUserResults() {
        if (_searchResults.length === 0) {
            pickerContent.innerHTML = '<div class="picker-empty">No users found</div>';
            return;
        }
        pickerContent.innerHTML = '';
        var list = document.createElement('div');
        list.className = 'picker-list';

        _searchResults.forEach(function (user) {
            var fullName = user.fullName || ((user.firstName || '') + ' ' + (user.lastName || '')).trim();
            var isSelected = _selectedMembers.some(function (m) { return m.id === user.id; });

            var item = document.createElement('div');
            item.className = 'picker-item' + (isSelected ? ' selected' : '');

            var avatarColor = ChatApp.utils.getAvatarColor(user.id);
            var avatarHtml = user.avatarUrl
                ? '<img src="' + ChatApp.utils.escapeHtml(user.avatarUrl) + '" class="picker-avatar-img" />'
                : '<span class="material-icons" style="font-size:16px;color:#fff;">person</span>';

            item.innerHTML = '<div class="picker-item-avatar" style="background-color:' + avatarColor + ';">' + avatarHtml + '</div>' +
                '<span class="picker-item-name">' + ChatApp.utils.escapeHtml(fullName) + '</span>' +
                (isSelected ? '<span class="material-icons picker-item-check" style="font-size:18px;color:#4caf50;">check</span>' : '');

            item.addEventListener('click', function () {
                toggleMember({ id: user.id, fullName: fullName, avatarUrl: user.avatarUrl, email: user.email });
            });

            list.appendChild(item);
        });

        pickerContent.appendChild(list);
    }

    // --- Load departments ---
    async function loadDepartments() {
        _isLoadingDepts = true;
        pickerContent.innerHTML = '<div class="picker-loading"><div class="spinner-border spinner-border-sm"></div> Loading...</div>';

        var result = await ChatApp.api.get('/api/identity/departments');
        _isLoadingDepts = false;

        if (result.isSuccess && result.value) {
            _departments = result.value || [];
            renderDepartments('');
        }
    }

    function renderDepartments(filterQuery) {
        var filtered = _departments;
        if (filterQuery) {
            var q = filterQuery.toLowerCase();
            filtered = _departments.filter(function (d) { return d.name.toLowerCase().indexOf(q) !== -1; });
        }

        if (filtered.length === 0) {
            pickerContent.innerHTML = '<div class="picker-empty">No departments found</div>';
            return;
        }

        pickerContent.innerHTML = '';
        var list = document.createElement('div');
        list.className = 'picker-list dept-list';

        filtered.forEach(function (dept) {
            var isDeptSelected = _selectedDeptIds.has(dept.id);
            var isExpanded = _expandedDeptIds.has(dept.id);

            // Department header
            var deptItem = document.createElement('div');
            deptItem.className = 'picker-item dept-item' + (isDeptSelected ? ' selected' : '');
            deptItem.innerHTML = '<div class="picker-item-avatar dept-avatar"><span class="material-icons" style="font-size:16px;color:#fff;">groups</span></div>' +
                '<div class="picker-item-info"><span class="dept-label">Department</span><span class="picker-item-name">' +
                ChatApp.utils.escapeHtml(dept.name) + '</span></div>' +
                '<button class="dept-expand-btn"><span class="material-icons" style="font-size:18px;">' + (isExpanded ? 'expand_less' : 'expand_more') + '</span></button>';

            // Click on dept header = select entire dept
            deptItem.addEventListener('click', function (e) {
                if (e.target.closest('.dept-expand-btn')) return;
                toggleDepartmentSelection(dept);
            });

            // Expand/collapse
            deptItem.querySelector('.dept-expand-btn').addEventListener('click', function (e) {
                e.stopPropagation();
                toggleDepartmentExpand(dept.id);
            });

            list.appendChild(deptItem);

            // Expanded users
            if (isExpanded) {
                var users = _departmentUsersCache[dept.id];
                if (users) {
                    var currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
                    users.forEach(function (u) {
                        if (u.userId === currentUserId || u.id === currentUserId) return;
                        var userId = u.userId || u.id;
                        var fullName = u.fullName || ((u.firstName || '') + ' ' + (u.lastName || '')).trim();
                        var isSelected = _selectedMembers.some(function (m) { return m.id === userId; });

                        var userItem = document.createElement('div');
                        userItem.className = 'picker-item user-item' + (isSelected ? ' selected' : '');

                        var avatarColor = ChatApp.utils.getAvatarColor(userId);
                        var avatarHtml = u.avatarUrl
                            ? '<img src="' + ChatApp.utils.escapeHtml(u.avatarUrl) + '" class="picker-avatar-img" />'
                            : '<span class="material-icons" style="font-size:16px;color:#fff;">person</span>';

                        userItem.innerHTML = '<div class="picker-item-avatar" style="background-color:' + avatarColor + ';">' + avatarHtml + '</div>' +
                            '<span class="picker-item-name">' + ChatApp.utils.escapeHtml(fullName) + '</span>' +
                            (isSelected ? '<span class="material-icons picker-item-check" style="font-size:18px;color:#4caf50;">check</span>' : '');

                        userItem.addEventListener('click', function () {
                            toggleMember({ id: userId, fullName: fullName, avatarUrl: u.avatarUrl, email: u.email });
                        });

                        list.appendChild(userItem);
                    });
                } else {
                    var loadingItem = document.createElement('div');
                    loadingItem.className = 'picker-loading-small';
                    loadingItem.innerHTML = '<div class="spinner-border spinner-border-sm"></div>';
                    list.appendChild(loadingItem);
                }
            }
        });

        pickerContent.appendChild(list);
    }

    async function toggleDepartmentExpand(deptId) {
        if (_expandedDeptIds.has(deptId)) {
            _expandedDeptIds.delete(deptId);
        } else {
            _expandedDeptIds.add(deptId);
            if (!_departmentUsersCache[deptId]) {
                await loadDepartmentUsers(deptId);
            }
        }
        renderDepartments(inlineMemberSearch.value.trim());
    }

    async function loadDepartmentUsers(deptId) {
        var result = await ChatApp.api.get('/api/identity/users/department-users?pageNumber=1&pageSize=100');
        if (result.isSuccess && result.value) {
            var items = result.value.items || result.value || [];
            _departmentUsersCache[deptId] = items.filter(function (u) {
                return u.departmentId === deptId;
            });
            renderDepartments(inlineMemberSearch.value.trim());
        }
    }

    function toggleDepartmentSelection(dept) {
        if (_selectedDeptIds.has(dept.id)) {
            // Deselect: remove all dept users
            _selectedDeptIds.delete(dept.id);
            var deptUsers = _departmentUsersCache[dept.id] || [];
            deptUsers.forEach(function (u) {
                var uid = u.userId || u.id;
                _selectedMembers = _selectedMembers.filter(function (m) { return m.id !== uid; });
            });
        } else {
            // Select: add all dept users
            _selectedDeptIds.add(dept.id);
            if (!_departmentUsersCache[dept.id]) {
                loadDepartmentUsers(dept.id).then(function () {
                    addDeptUsersToSelection(dept.id);
                    renderMemberChips();
                    updateSubmitButton();
                    renderDepartments(inlineMemberSearch.value.trim());
                });
                renderDepartments(inlineMemberSearch.value.trim());
                return;
            }
            addDeptUsersToSelection(dept.id);
        }
        renderMemberChips();
        updateSubmitButton();
        renderDepartments(inlineMemberSearch.value.trim());
    }

    function addDeptUsersToSelection(deptId) {
        var currentUserId = ChatApp.state.currentUser ? ChatApp.state.currentUser.id : null;
        var deptUsers = _departmentUsersCache[deptId] || [];
        deptUsers.forEach(function (u) {
            var uid = u.userId || u.id;
            if (uid === currentUserId) return;
            if (_selectedMembers.some(function (m) { return m.id === uid; })) return;
            _selectedMembers.push({
                id: uid,
                fullName: u.fullName || ((u.firstName || '') + ' ' + (u.lastName || '')).trim(),
                avatarUrl: u.avatarUrl,
                email: u.email
            });
        });
    }

    // --- Toggle member ---
    function toggleMember(user) {
        var idx = _selectedMembers.findIndex(function (m) { return m.id === user.id; });
        if (idx !== -1) {
            _selectedMembers.splice(idx, 1);
        } else {
            _selectedMembers.push(user);
        }
        renderMemberChips();
        updateSubmitButton();
        // Re-render picker to update checkmarks
        if (_currentTab === 'recent') { renderUserResults(); }
        else { renderDepartments(inlineMemberSearch.value.trim()); }
        inlineMemberSearch.value = '';
        inlineMemberSearch.focus();
    }

    // --- Render member chips ---
    function renderMemberChips() {
        memberChipsContainer.innerHTML = '';
        _selectedMembers.forEach(function (member) {
            var chip = document.createElement('div');
            chip.className = 'selected-member-chip';

            var avatarColor = ChatApp.utils.getAvatarColor(member.id);
            var avatarHtml = member.avatarUrl
                ? '<img src="' + ChatApp.utils.escapeHtml(member.avatarUrl) + '" class="chip-avatar" />'
                : '<div class="chip-avatar-placeholder" style="background-color:' + avatarColor + ';"><span class="material-icons member-icon" style="font-size:14px;">person</span></div>';

            chip.innerHTML = avatarHtml +
                '<span class="chip-name">' + ChatApp.utils.escapeHtml(member.fullName) + '</span>' +
                '<button class="chip-remove"><span class="material-icons" style="font-size:14px;">close</span></button>';

            chip.querySelector('.chip-remove').addEventListener('click', function (e) {
                e.stopPropagation();
                _selectedMembers = _selectedMembers.filter(function (m) { return m.id !== member.id; });
                renderMemberChips();
                updateSubmitButton();
                if (_currentTab === 'recent') { renderUserResults(); }
                else { renderDepartments(inlineMemberSearch.value.trim()); }
            });

            memberChipsContainer.appendChild(chip);
        });
    }

    // --- Validation ---
    function updateSubmitButton() {
        var name = groupNameInput.value.trim();
        var descLen = groupDescInput ? groupDescInput.value.length : 0;
        var invalid = !name || name.length > 100 || descLen > 500;
        createGroupSubmit.disabled = invalid || _isCreating;
    }

    // --- Create channel ---
    createGroupSubmit.addEventListener('click', async function () {
        var name = groupNameInput.value.trim();
        if (!name || _isCreating) return;

        _isCreating = true;
        createGroupSubmit.disabled = true;
        createGroupSubmit.innerHTML = '<div class="loading-spinner small"></div>';

        try {
            var channelType = parseInt(document.querySelector('input[name="chatType"]:checked').value);
            var desc = groupDescInput ? groupDescInput.value.trim() : '';

            // 1. Create channel
            var result = await ChatApp.api.post('/api/channels', {
                name: name,
                description: desc || null,
                channelType: channelType
            });

            if (!result.isSuccess) {
                alert(result.error || 'Failed to create channel');
                return;
            }

            var channelId = result.value.channelId || result.value.id || result.value;

            // 2. Add members
            for (var i = 0; i < _selectedMembers.length; i++) {
                await ChatApp.api.post('/api/channels/' + channelId + '/members', {
                    userId: _selectedMembers[i].id
                });
            }

            // 3. Upload avatar if selected
            if (_avatarFile) {
                var formData = new FormData();
                formData.append('File', _avatarFile);
                var uploadResult = await ChatApp.api.upload('/api/files/upload/channel-avatar/' + channelId, formData);
                if (uploadResult.isSuccess && uploadResult.value && uploadResult.value.downloadUrl) {
                    await ChatApp.api.put('/api/channels/' + channelId, {
                        avatarUrl: uploadResult.value.downloadUrl
                    });
                }
            }

            // 4. Close panel and reload
            closePanel();
            ChatApp.conversationList.reload();

        } catch (err) {
            console.error('[CreateGroup] Error:', err);
            alert('An error occurred while creating the channel.');
        } finally {
            _isCreating = false;
            createGroupSubmit.disabled = false;
            createGroupSubmit.innerHTML = '<span>CREATE CHAT</span>';
        }
    });

    createGroupCancel.addEventListener('click', closePanel);

    // --- Public API ---
    ChatApp.createGroup = {
        open: openPanel,
        close: closePanel,
        openNewMessage: function () {
            openPanel();
            // Switch to DM mode
            _mode = 'dm';
            var tabs = panel.querySelectorAll('.create-mode-tab');
            if (tabs.length >= 2) {
                tabs[0].classList.remove('active');
                tabs[0].style.borderBottomColor = 'transparent';
                tabs[0].style.color = '#666';
                tabs[1].classList.add('active');
                tabs[1].style.borderBottomColor = '#4caf50';
                tabs[1].style.color = '#4caf50';
            }
            updateModeView();
        }
    };

})();
