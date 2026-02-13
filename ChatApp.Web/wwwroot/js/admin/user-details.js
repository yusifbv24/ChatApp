/**
 * User Details — replaces UserDetails.razor
 * Full user profile management for admin
 */
(function () {
    'use strict';

    let _userId = null;
    let _user = null;
    let _positions = [];
    let _departments = [];

    function init(userId) {
        _userId = userId;
        if (!_userId) return;
        loadUser();
    }

    async function loadUser() {
        const content = document.getElementById('userDetailsContent');
        content.innerHTML = '<div class="text-center py-4"><div class="spinner-border spinner-border-sm text-secondary"></div></div>';

        const [userResult, posResult, deptResult] = await Promise.all([
            ChatApp.api.get('/api/identity/users/' + _userId),
            ChatApp.api.get('/api/identity/positions'),
            ChatApp.api.get('/api/identity/departments')
        ]);

        if (!userResult.isSuccess || !userResult.value) {
            content.innerHTML = '<div class="alert alert-warning">User not found.</div>';
            return;
        }

        _user = userResult.value;
        if (posResult.isSuccess) _positions = posResult.value || [];
        if (deptResult.isSuccess) _departments = deptResult.value || [];

        renderUser();
    }

    function renderUser() {
        const content = document.getElementById('userDetailsContent');
        const u = _user;
        const fullName = ((u.firstName || '') + ' ' + (u.lastName || '')).trim();
        const isOnline = ChatApp.state.isUserOnline(u.id);
        const isAdmin = u.role === 1 || u.role === 'Administrator';
        const isActive = u.isActive !== false;

        // Permission checks
        var canUpdate = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Update');
        var canDelete = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Delete');
        var canReadPerms = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Permissions.Read');

        let html = '<div class="user-detail-card">';

        // Avatar section
        html += '<div class="user-detail-avatar-section">';
        html += '<div class="user-detail-avatar-wrapper">';
        if (u.avatarUrl) {
            html += '<img src="' + ChatApp.utils.escapeHtml(u.avatarUrl) + '" class="user-detail-avatar-img" id="userAvatarImg" style="cursor:pointer;" title="Click to view fullscreen" alt="" />';
        } else {
            html += '<div class="user-detail-avatar-placeholder" style="background-color:' +
                ChatApp.utils.getAvatarColor(u.id) + ';">' + ChatApp.utils.getInitials(fullName) + '</div>';
        }
        if (canUpdate) {
            html += '<div class="user-detail-avatar-actions">';
            html += '<button class="btn btn-sm btn-outline-primary" id="uploadAvatarBtn" title="Upload Avatar"><span class="material-icons" style="font-size:14px;">cloud_upload</span></button>';
            if (u.avatarUrl) {
                html += '<button class="btn btn-sm btn-outline-danger" id="removeAvatarBtn" title="Remove Avatar"><span class="material-icons" style="font-size:14px;">delete</span></button>';
            }
            html += '<input type="file" accept="image/*" style="display:none;" id="avatarFileInput" />';
            html += '</div>';
        }
        html += '</div>';

        // Badges
        html += '<div class="user-detail-badges">';
        html += '<span class="badge ' + (isAdmin ? 'bg-primary' : 'bg-secondary') + '">' + (isAdmin ? 'Admin' : 'User') + '</span>';
        html += '<span class="badge ' + (isActive ? 'bg-success' : 'bg-danger') + '">' + (isActive ? 'Active' : 'Inactive') + '</span>';
        if (isOnline) html += '<span class="badge bg-info">Online</span>';
        html += '</div>';

        html += '<h2>' + ChatApp.utils.escapeHtml(fullName) + '</h2>';
        if (u.position) html += '<p class="text-muted">' + ChatApp.utils.escapeHtml(u.position) + '</p>';

        // Actions

        html += '<div class="user-detail-actions">';
        if (canUpdate) {
            html += '<button class="btn btn-sm btn-outline-' + (isActive ? 'danger' : 'success') + '" id="toggleActiveBtn">' +
                (isActive ? 'Deactivate' : 'Activate') + '</button>';
            html += '<button class="btn btn-sm btn-outline-primary" id="editUserBtn">Edit</button>';
            html += '<button class="btn btn-sm btn-outline-secondary" id="changePwBtn">Change Password</button>';
        }
        if (canDelete) {
            html += '<button class="btn btn-sm btn-outline-danger" id="deleteUserBtn"><span class="material-icons" style="font-size:14px;vertical-align:middle;">delete</span> Delete</button>';
        }
        html += '</div>';
        html += '</div>';

        // Info sections
        html += '<div class="user-detail-sections">';

        // Personal
        html += '<div class="user-detail-section"><h4>Personal Information</h4>';
        html += infoRow('person', 'Full Name', fullName);
        html += infoRow('email', 'Email', u.email);
        html += infoRow('phone', 'Phone', u.workPhone);
        html += infoRow('cake', 'Date of Birth', u.dateOfBirth ? new Date(u.dateOfBirth).toLocaleDateString() : '');
        html += infoRow('event', 'Hiring Date', u.hiringDate ? new Date(u.hiringDate).toLocaleDateString() : '');
        html += infoRow('info', 'About', u.aboutMe);
        html += '</div>';

        // Organization
        html += '<div class="user-detail-section"><h4>Organization</h4>';
        html += infoRow('apartment', 'Department', u.departmentName);
        html += infoRow('work', 'Position', u.position);
        if (u.supervisorName) {
            html += infoRow('supervisor_account', 'Supervisor', u.supervisorName);
        }
        html += infoRow('admin_panel_settings', 'Role', isAdmin ? 'Administrator' : 'User');
        if (u.departmentId && canUpdate) {
            html += '<div class="user-detail-info-row">' +
                '<span class="material-icons-outlined" style="font-size:18px;color:rgba(0,0,0,0.3);">star</span>' +
                '<span class="user-detail-label">Head of Department</span>' +
                '<div class="form-check form-switch" style="margin:0;">' +
                '<input class="form-check-input" type="checkbox" id="headOfDeptToggle"' + (u.isHeadOfDepartment ? ' checked' : '') + ' />' +
                '</div></div>';
        }
        html += '</div>';

        // Account
        html += '<div class="user-detail-section"><h4>Account</h4>';
        html += infoRow('event', 'Created', u.createdDate ? new Date(u.createdDate).toLocaleDateString() : '');
        html += infoRow('update', 'Last Updated', u.updatedDate ? new Date(u.updatedDate).toLocaleDateString() : '');
        html += infoRow('login', 'Last Login', u.lastLoginDate ? ChatApp.utils.formatRelativeTime(u.lastLoginDate) : 'Never');
        html += '</div>';

        // Subordinates
        if (u.subordinates && u.subordinates.length > 0) {
            html += '<div class="user-detail-section"><h4>Subordinates (' + u.subordinates.length + ')</h4>';
            u.subordinates.forEach(function (sub) {
                const subName = ((sub.firstName || '') + ' ' + (sub.lastName || '')).trim();
                html += '<div class="admin-list-item" style="cursor:pointer;" onclick="window.location.href=\'/admin/userdetails/' + sub.id + '\'">' +
                    '<div class="admin-list-avatar">' + ChatApp.utils.renderAvatar({ id: sub.id, avatarUrl: sub.avatarUrl, fullName: subName }, 'admin-avatar-img') + '</div>' +
                    '<div class="admin-list-info"><span class="admin-list-name">' + ChatApp.utils.escapeHtml(subName) + '</span>' +
                    '<span class="admin-list-meta">' + ChatApp.utils.escapeHtml(sub.position || '') + '</span></div></div>';
            });
            html += '</div>';
        }

        // Permissions section
        if (canReadPerms && u.permissions) {
            html += '<div class="user-detail-section" id="permissionsSection"><h4>Permissions</h4>';
            html += '<div id="permissionsGrid"></div></div>';
        }

        html += '</div></div>';

        content.innerHTML = html;

        // Render permissions grid
        if (canReadPerms && u.permissions) {
            renderPermissions(u.permissions);
        }

        // Bind events
        document.getElementById('toggleActiveBtn')?.addEventListener('click', toggleActive);
        document.getElementById('editUserBtn')?.addEventListener('click', openEditDialog);
        document.getElementById('changePwBtn')?.addEventListener('click', openChangePwDialog);
        document.getElementById('deleteUserBtn')?.addEventListener('click', function () {
            showDeleteConfirmModal('Are you sure you want to delete this user?', async function () {
                var result = await ChatApp.api.del('/api/identity/users/' + _userId);
                if (result.isSuccess) {
                    window.location.href = '/admin/organizationhierarchy';
                }
            });
        });

        // Avatar handlers
        document.getElementById('userAvatarImg')?.addEventListener('click', function () {
            showAvatarLightbox(this.src);
        });
        document.getElementById('uploadAvatarBtn')?.addEventListener('click', function () {
            document.getElementById('avatarFileInput').click();
        });
        document.getElementById('avatarFileInput')?.addEventListener('change', async function () {
            if (!this.files || !this.files[0]) return;
            var formData = new FormData();
            formData.append('file', this.files[0]);
            var uploadResult = await ChatApp.api.upload('/api/files', formData);
            if (uploadResult.isSuccess && uploadResult.value) {
                var avatarUrl = uploadResult.value.downloadUrl || uploadResult.value.url || uploadResult.value;
                var result = await ChatApp.api.put('/api/identity/users/' + _userId, { avatarUrl: avatarUrl });
                if (result.isSuccess) loadUser();
            }
        });
        document.getElementById('removeAvatarBtn')?.addEventListener('click', async function () {
            var result = await ChatApp.api.put('/api/identity/users/' + _userId, { avatarUrl: '' });
            if (result.isSuccess) loadUser();
        });

        // Head of Department toggle
        document.getElementById('headOfDeptToggle')?.addEventListener('change', async function () {
            var checked = this.checked;
            var deptId = _user.departmentId;
            var result;
            if (checked) {
                result = await ChatApp.api.post('/api/identity/departments/' + deptId + '/assign-head', { userId: _userId });
            } else {
                result = await ChatApp.api.del('/api/identity/departments/' + deptId + '/remove-head');
            }
            if (result.isSuccess) {
                loadUser();
            } else {
                this.checked = !checked;
            }
        });
    }

    function infoRow(icon, label, value) {
        return '<div class="user-detail-info-row">' +
            '<span class="material-icons-outlined" style="font-size:18px;color:rgba(0,0,0,0.3);">' + icon + '</span>' +
            '<span class="user-detail-label">' + label + '</span>' +
            '<span class="user-detail-value">' + ChatApp.utils.escapeHtml(value || 'Not set') + '</span></div>';
    }

    // --- Avatar Lightbox ---
    function showAvatarLightbox(src) {
        var existing = document.getElementById('avatarLightbox');
        if (existing) existing.remove();

        var overlay = document.createElement('div');
        overlay.id = 'avatarLightbox';
        overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.85);z-index:9999;display:flex;align-items:center;justify-content:center;cursor:pointer;';
        overlay.innerHTML = '<img src="' + ChatApp.utils.escapeHtml(src) + '" style="max-width:90%;max-height:90%;border-radius:8px;box-shadow:0 4px 24px rgba(0,0,0,0.5);" />' +
            '<button style="position:absolute;top:20px;right:20px;background:rgba(255,255,255,0.2);border:none;color:#fff;font-size:24px;width:40px;height:40px;border-radius:50%;cursor:pointer;display:flex;align-items:center;justify-content:center;">' +
            '<span class="material-icons">close</span></button>';
        overlay.addEventListener('click', function () { overlay.remove(); });
        document.body.appendChild(overlay);
    }

    // --- Permissions Management ---
    var _allPermissions = {
        'Users': ['Users.Create', 'Users.Read', 'Users.Update', 'Users.Delete'],
        'Permissions': ['Permissions.Read', 'Permissions.Assign', 'Permissions.Revoke'],
        'Messages': ['Messages.Send', 'Messages.Read', 'Messages.Edit', 'Messages.Delete'],
        'Files': ['Files.Upload', 'Files.Download', 'Files.Delete'],
        'Channels': ['Channels.Create', 'Channels.Read', 'Channels.Manage', 'Channels.Delete']
    };

    function renderPermissions(userPermissions) {
        var grid = document.getElementById('permissionsGrid');
        if (!grid) return;

        var canAssign = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Permissions.Assign');
        var html = '';

        Object.keys(_allPermissions).forEach(function (module) {
            html += '<div class="perm-module-group">';
            html += '<div class="perm-module-header"><span class="material-icons" style="font-size:18px;color:#667eea;">security</span> ' + module + '</div>';
            html += '<div class="perm-module-items">';

            _allPermissions[module].forEach(function (perm) {
                var isActive = userPermissions.indexOf(perm) !== -1;
                var shortName = perm.split('.')[1];
                html += '<div class="perm-item">';
                html += '<span class="perm-item-name">' + ChatApp.utils.escapeHtml(shortName) + '</span>';
                html += '<div class="form-check form-switch">';
                html += '<input class="form-check-input perm-toggle" type="checkbox" data-perm="' + perm + '"' +
                    (isActive ? ' checked' : '') + (canAssign ? '' : ' disabled') + ' />';
                html += '</div></div>';
            });

            html += '</div></div>';
        });

        grid.innerHTML = html;

        if (canAssign) {
            grid.querySelectorAll('.perm-toggle').forEach(function (toggle) {
                toggle.addEventListener('change', function () {
                    var perm = this.dataset.perm;
                    var checked = this.checked;
                    handlePermissionToggle(perm, checked, this);
                });
            });
        }
    }

    async function handlePermissionToggle(permissionName, assign, toggleEl) {
        var result;
        if (assign) {
            result = await ChatApp.api.post('/api/identity/users/' + _userId + '/permissions', {
                permissionName: permissionName
            });
        } else {
            result = await ChatApp.api.del('/api/identity/users/' + _userId + '/permissions/' + encodeURIComponent(permissionName));
        }
        if (!result.isSuccess) {
            // Revert toggle on failure
            toggleEl.checked = !assign;
        }
    }

    // --- Delete Confirmation Modal ---
    function showDeleteConfirmModal(message, onConfirm) {
        var existing = document.getElementById('udDeleteConfirmModal');
        if (existing) existing.remove();

        var modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'udDeleteConfirmModal';
        modal.tabIndex = -1;
        modal.innerHTML =
            '<div class="modal-dialog modal-dialog-centered">' +
            '<div class="modal-content">' +
            '<div class="modal-header bg-danger text-white">' +
            '<h5 class="modal-title"><span class="material-icons" style="font-size:20px;vertical-align:middle;margin-right:6px;">warning</span>Confirm Delete</h5>' +
            '<button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body"><p>' + message + '</p><p class="text-muted small mb-0">This action cannot be undone.</p></div>' +
            '<div class="modal-footer">' +
            '<button class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>' +
            '<button class="btn btn-danger btn-sm" id="udDeleteConfirmBtn">Delete</button>' +
            '</div>' +
            '</div></div>';

        document.body.appendChild(modal);
        var bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        modal.querySelector('#udDeleteConfirmBtn').addEventListener('click', function () {
            bsModal.hide();
            onConfirm();
        });

        modal.addEventListener('hidden.bs.modal', function () { modal.remove(); });
    }

    // --- Toggle Active ---
    async function toggleActive() {
        const isActive = _user.isActive !== false;
        const endpoint = isActive
            ? '/api/identity/users/' + _userId + '/deactivate'
            : '/api/identity/users/' + _userId + '/activate';
        const result = await ChatApp.api.post(endpoint);
        if (result.isSuccess) loadUser();
    }

    // --- Edit dialog (simple prompt approach — could be enhanced to modal) ---
    async function openEditDialog() {
        // Create a simple modal dynamically
        const existing = document.getElementById('editUserModal');
        if (existing) existing.remove();

        const u = _user;
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'editUserModal';
        modal.tabIndex = -1;
        modal.innerHTML = '<div class="modal-dialog modal-lg modal-dialog-centered"><div class="modal-content">' +
            '<div class="modal-header"><h5 class="modal-title">Edit User</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>' +
            '<div class="modal-body"><div class="row g-3">' +
            '<div class="col-md-6"><label class="form-label">First Name</label><input class="form-control form-control-sm" id="euFirstName" value="' + ChatApp.utils.escapeHtml(u.firstName || '') + '" /></div>' +
            '<div class="col-md-6"><label class="form-label">Last Name</label><input class="form-control form-control-sm" id="euLastName" value="' + ChatApp.utils.escapeHtml(u.lastName || '') + '" /></div>' +
            '<div class="col-md-6"><label class="form-label">Email</label><input class="form-control form-control-sm" id="euEmail" value="' + ChatApp.utils.escapeHtml(u.email || '') + '" /></div>' +
            '<div class="col-md-6"><label class="form-label">Phone</label><input class="form-control form-control-sm" id="euPhone" value="' + ChatApp.utils.escapeHtml(u.workPhone || '') + '" /></div>' +
            '<div class="col-md-6"><label class="form-label">Department</label><select class="form-select form-select-sm" id="euDepartment"><option value="">None</option></select></div>' +
            '<div class="col-md-6"><label class="form-label">Position</label><select class="form-select form-select-sm" id="euPosition"><option value="">None</option></select></div>' +
            '<div class="col-md-6"><label class="form-label">Role</label><select class="form-select form-select-sm" id="euRole"><option value="3"' + ((!u.role || u.role === 'User' || u.role === 3) ? ' selected' : '') + '>User</option><option value="1"' + ((u.role === 1 || u.role === 'Administrator') ? ' selected' : '') + '>Administrator</option></select></div>' +
            '<div class="col-md-6"><label class="form-label">Date of Birth</label><input type="date" class="form-control form-control-sm" id="euDateOfBirth" value="' + (u.dateOfBirth ? new Date(u.dateOfBirth).toISOString().split('T')[0] : '') + '" /></div>' +
            '<div class="col-md-6"><label class="form-label">Hiring Date</label><input type="date" class="form-control form-control-sm" id="euHiringDate" value="' + (u.hiringDate ? new Date(u.hiringDate).toISOString().split('T')[0] : '') + '" /></div>' +
            '<div class="col-12"><label class="form-label">About Me</label><textarea class="form-control form-control-sm" id="euAboutMe" rows="2">' + ChatApp.utils.escapeHtml(u.aboutMe || '') + '</textarea></div>' +
            '</div><div id="euError" class="text-danger small mt-2"></div><div id="euSuccess" class="text-success small mt-2"></div></div>' +
            '<div class="modal-footer"><button class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cancel</button><button class="btn btn-primary btn-sm" id="euSubmit">Save</button></div>' +
            '</div></div>';

        document.body.appendChild(modal);

        // Populate selects
        const deptSel = modal.querySelector('#euDepartment');
        _departments.forEach(function (d) {
            deptSel.innerHTML += '<option value="' + d.id + '"' + (d.id === u.departmentId ? ' selected' : '') + '>' + ChatApp.utils.escapeHtml(d.name) + '</option>';
        });
        const posSel = modal.querySelector('#euPosition');
        _positions.forEach(function (p) {
            posSel.innerHTML += '<option value="' + p.id + '"' + (p.id === u.positionId ? ' selected' : '') + '>' + ChatApp.utils.escapeHtml(p.name) + '</option>';
        });

        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        modal.querySelector('#euSubmit').addEventListener('click', async function () {
            const result = await ChatApp.api.put('/api/identity/users/' + _userId, {
                firstName: modal.querySelector('#euFirstName').value.trim(),
                lastName: modal.querySelector('#euLastName').value.trim(),
                email: modal.querySelector('#euEmail').value.trim(),
                workPhone: modal.querySelector('#euPhone').value.trim() || null,
                departmentId: modal.querySelector('#euDepartment').value || null,
                positionId: modal.querySelector('#euPosition').value || null,
                role: parseInt(modal.querySelector('#euRole').value),
                dateOfBirth: modal.querySelector('#euDateOfBirth').value || null,
                hiringDate: modal.querySelector('#euHiringDate').value || null,
                aboutMe: modal.querySelector('#euAboutMe').value.trim() || null
            });
            if (result.isSuccess) {
                modal.querySelector('#euSuccess').textContent = 'Updated.';
                setTimeout(function () { bsModal.hide(); loadUser(); }, 800);
            } else {
                modal.querySelector('#euError').textContent = result.error || 'Failed.';
            }
        });

        modal.addEventListener('hidden.bs.modal', function () { modal.remove(); });
    }

    // --- Change password ---
    function openChangePwDialog() {
        const existing = document.getElementById('changePwModal');
        if (existing) existing.remove();

        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'changePwModal';
        modal.tabIndex = -1;
        modal.innerHTML =
            '<div class="modal-dialog modal-dialog-centered">' +
            '<div class="modal-content">' +
            '<div class="modal-header">' +
            '<h5 class="modal-title">Change Password</h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body">' +
            '<div class="mb-3">' +
            '<label class="form-label">New Password</label>' +
            '<div class="input-group">' +
            '<input type="password" class="form-control form-control-sm" id="cpNewPw" placeholder="Enter new password" />' +
            '<button class="btn btn-outline-secondary btn-sm" type="button" id="cpToggleNew" tabindex="-1">' +
            '<span class="material-icons" style="font-size:18px;">visibility_off</span>' +
            '</button>' +
            '</div>' +
            '</div>' +
            '<div class="mb-3">' +
            '<label class="form-label">Confirm Password</label>' +
            '<div class="input-group">' +
            '<input type="password" class="form-control form-control-sm" id="cpConfirmPw" placeholder="Confirm new password" />' +
            '<button class="btn btn-outline-secondary btn-sm" type="button" id="cpToggleConfirm" tabindex="-1">' +
            '<span class="material-icons" style="font-size:18px;">visibility_off</span>' +
            '</button>' +
            '</div>' +
            '</div>' +
            '<div id="cpError" class="text-danger small"></div>' +
            '<div id="cpSuccess" class="text-success small"></div>' +
            '</div>' +
            '<div class="modal-footer">' +
            '<button class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>' +
            '<button class="btn btn-primary btn-sm" id="cpSubmit">Save</button>' +
            '</div>' +
            '</div></div>';

        document.body.appendChild(modal);

        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        // Toggle password visibility
        function bindToggle(toggleId, inputId) {
            modal.querySelector('#' + toggleId).addEventListener('click', function () {
                const input = modal.querySelector('#' + inputId);
                const icon = this.querySelector('.material-icons');
                if (input.type === 'password') {
                    input.type = 'text';
                    icon.textContent = 'visibility';
                } else {
                    input.type = 'password';
                    icon.textContent = 'visibility_off';
                }
            });
        }
        bindToggle('cpToggleNew', 'cpNewPw');
        bindToggle('cpToggleConfirm', 'cpConfirmPw');

        // Submit
        modal.querySelector('#cpSubmit').addEventListener('click', async function () {
            const errorEl = modal.querySelector('#cpError');
            const successEl = modal.querySelector('#cpSuccess');
            errorEl.textContent = '';
            successEl.textContent = '';

            const newPw = modal.querySelector('#cpNewPw').value;
            const confirmPw = modal.querySelector('#cpConfirmPw').value;

            if (!newPw) { errorEl.textContent = 'Please enter a new password.'; return; }
            if (newPw.length < 6) { errorEl.textContent = 'Password must be at least 6 characters.'; return; }
            if (newPw !== confirmPw) { errorEl.textContent = 'Passwords do not match.'; return; }

            const result = await ChatApp.api.post('/api/identity/users/admin-change-password', {
                id: _userId,
                newPassword: newPw,
                confirmNewPassword: confirmPw
            });

            if (result.isSuccess) {
                successEl.textContent = 'Password changed successfully.';
                setTimeout(function () { bsModal.hide(); }, 1000);
            } else {
                errorEl.textContent = result.error || 'Failed to change password.';
            }
        });

        modal.addEventListener('hidden.bs.modal', function () { modal.remove(); });
    }

    // --- Public API ---
    ChatApp.adminUserDetails = {
        init: init
    };

})();
