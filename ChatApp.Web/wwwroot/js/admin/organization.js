/**
 * Organization Hierarchy — replaces OrganizationHierarchy.razor
 * Tree view with expand/collapse, search, CRUD dialogs
 */
(function () {
    'use strict';

    const treeContainer = document.getElementById('orgTree');
    const searchInput = document.getElementById('orgSearchInput');
    const expandAllBtn = document.getElementById('expandAllBtn');
    const collapseAllBtn = document.getElementById('collapseAllBtn');

    if (!treeContainer) return;

    let _hierarchy = [];
    let _departments = [];
    let _positions = [];
    let _companyId = null;
    let _expandedNodes = new Set();

    // --- Load data ---
    async function loadData() {
        const [hierResult, deptResult, posResult] = await Promise.all([
            ChatApp.api.get('/api/identity/organization/hierarchy'),
            ChatApp.api.get('/api/identity/departments'),
            ChatApp.api.get('/api/identity/positions')
        ]);

        if (hierResult.isSuccess) _hierarchy = hierResult.value || [];
        if (deptResult.isSuccess) _departments = deptResult.value || [];
        if (posResult.isSuccess) _positions = posResult.value || [];

        // Extract company ID from hierarchy root
        var companyNode = _hierarchy.find(function (n) { return n.type === 'Company' || n.type === 0; });
        if (companyNode) _companyId = companyNode.id;

        updateStatistics();
        renderTree();
        populateSelects();
    }

    function updateStatistics() {
        let deptCount = 0, userCount = 0;
        function count(nodes) {
            (nodes || []).forEach(function (n) {
                if (n.type === 'Department' || n.type === 1) deptCount++;
                if (n.type === 'User' || n.type === 2) userCount++;
                if (n.children) count(n.children);
            });
        }
        count(_hierarchy);
        document.getElementById('statDepartments').textContent = deptCount;
        document.getElementById('statUsers').textContent = userCount;
        document.getElementById('statPositions').textContent = _positions.length;
        document.getElementById('statOnline').textContent = ChatApp.state._onlineUsers.size;
    }

    // --- Render tree ---
    function renderTree(filterQuery) {
        treeContainer.innerHTML = '';
        if (_hierarchy.length === 0) {
            treeContainer.innerHTML = '<p class="text-muted text-center py-4">No organization data.</p>';
            return;
        }

        _hierarchy.forEach(function (node) {
            treeContainer.appendChild(createNode(node, 0, filterQuery));
        });
    }

    function createNode(node, level, filterQuery) {
        const isCompany = node.type === 'Company' || node.type === 0;
        const isDept = node.type === 'Department' || node.type === 1;
        const isUser = node.type === 'User' || node.type === 2;
        const hasChildren = node.children && node.children.length > 0;
        const isExpanded = _expandedNodes.has(node.id || node.name);

        // Filter
        if (filterQuery) {
            const matches = matchesSearch(node, filterQuery);
            const childMatches = hasChildren && node.children.some(function (c) { return hasMatchInTree(c, filterQuery); });
            if (!matches && !childMatches) return document.createDocumentFragment();
        }

        const el = document.createElement('div');
        el.className = 'org-tree-node' + (isCompany ? ' company-node' : isDept ? ' department-node' : ' user-node');
        el.style.paddingLeft = (level * 24) + 'px';

        let html = '<div class="org-node-header">';

        // Expand/collapse
        if (hasChildren) {
            html += '<button class="org-expand-btn" data-node-id="' + (node.id || node.name) + '">' +
                '<span class="material-icons" style="font-size:18px;">' + (isExpanded ? 'expand_more' : 'chevron_right') + '</span></button>';
        } else {
            html += '<span style="width:26px;display:inline-block;"></span>';
        }

        // Icon
        if (isCompany) {
            html += '<span class="material-icons org-node-icon" style="color:#667eea;">domain</span>';
        } else if (isDept) {
            html += '<span class="material-icons org-node-icon" style="color:#f59e0b;">apartment</span>';
        } else {
            const avatarColor = ChatApp.utils.getAvatarColor(node.id);
            if (node.avatarUrl) {
                html += '<img src="' + ChatApp.utils.escapeHtml(node.avatarUrl) + '" class="org-user-avatar" />';
            } else {
                html += '<div class="org-user-avatar-placeholder" style="background-color:' + avatarColor + ';">' +
                    ChatApp.utils.getInitials(node.name || '') + '</div>';
            }
        }

        // Name and info
        html += '<div class="org-node-info">';
        html += '<span class="org-node-name">' + ChatApp.utils.escapeHtml(node.name || '') + '</span>';
        if (isDept && node.userCount !== undefined) {
            html += '<span class="org-node-count">' + node.userCount + ' users</span>';
        }
        if (isUser && node.positionName) {
            html += '<span class="org-node-position">' + ChatApp.utils.escapeHtml(node.positionName) + '</span>';
        }
        if (isUser && node.email) {
            html += '<span class="org-node-email">' + ChatApp.utils.escapeHtml(node.email) + '</span>';
        }
        html += '</div>';

        // Action buttons
        var canCreate = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Create');
        var canUpdate = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Update');
        var canDelete = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Delete');

        html += '<div class="org-node-actions">';
        if (isDept) {
            if (canCreate) {
                html += '<button class="org-action-btn" title="Add User" data-action="addUser" data-dept-id="' + node.id + '" data-dept-name="' + ChatApp.utils.escapeHtml(node.name || '') + '">' +
                    '<span class="material-icons" style="font-size:16px;">person_add</span></button>';
                html += '<button class="org-action-btn" title="Add Sub-department" data-action="addDept" data-parent-id="' + node.id + '">' +
                    '<span class="material-icons" style="font-size:16px;">create_new_folder</span></button>';
            }
            if (canUpdate) {
                html += '<button class="org-action-btn" title="Edit" data-action="editDept" data-dept-id="' + node.id + '">' +
                    '<span class="material-icons" style="font-size:16px;">edit</span></button>';
            }
            if (canDelete) {
                html += '<button class="org-action-btn" title="Delete Department" data-action="deleteDept" data-dept-id="' + node.id + '" data-dept-name="' + ChatApp.utils.escapeHtml(node.name || '') + '">' +
                    '<span class="material-icons" style="font-size:16px;color:#ef4444;">delete</span></button>';
            }
        }
        if (isUser) {
            html += '<button class="org-action-btn" title="View Details" data-action="viewUser" data-user-id="' + node.id + '">' +
                '<span class="material-icons" style="font-size:16px;">visibility</span></button>';
            if (canDelete) {
                html += '<button class="org-action-btn" title="Delete User" data-action="deleteUser" data-user-id="' + node.id + '" data-user-name="' + ChatApp.utils.escapeHtml(node.name || '') + '">' +
                    '<span class="material-icons" style="font-size:16px;color:#ef4444;">delete</span></button>';
            }
        }
        if (isCompany) {
            if (canCreate) {
                html += '<button class="org-action-btn" title="Add Department" data-action="addDept" data-parent-id="">' +
                    '<span class="material-icons" style="font-size:16px;">create_new_folder</span></button>';
                html += '<button class="org-action-btn" title="Add User" data-action="addUser" data-dept-id="" data-dept-name="">' +
                    '<span class="material-icons" style="font-size:16px;">person_add</span></button>';
            }
        }
        html += '</div></div>';

        el.innerHTML = html;

        // Children container
        if (hasChildren) {
            const childrenContainer = document.createElement('div');
            childrenContainer.className = 'org-node-children';
            childrenContainer.style.display = isExpanded ? '' : 'none';
            node.children.forEach(function (child) {
                childrenContainer.appendChild(createNode(child, level + 1, filterQuery));
            });
            el.appendChild(childrenContainer);
        }

        // Event delegation for buttons
        el.querySelector('.org-node-header').addEventListener('click', function (e) {
            const btn = e.target.closest('button');
            if (!btn) return;

            const action = btn.dataset.action;
            const nodeId = btn.dataset.nodeId;

            if (nodeId) {
                // Toggle expand
                if (_expandedNodes.has(nodeId)) {
                    _expandedNodes.delete(nodeId);
                } else {
                    _expandedNodes.add(nodeId);
                }
                renderTree(searchInput ? searchInput.value.trim() : '');
                return;
            }

            if (action === 'addUser') openCreateUser(btn.dataset.deptId, btn.dataset.deptName);
            else if (action === 'addDept') openCreateDept(btn.dataset.parentId);
            else if (action === 'editDept') openEditDept(btn.dataset.deptId);
            else if (action === 'viewUser') window.location.href = '/admin/userdetails/' + btn.dataset.userId;
            else if (action === 'deleteDept') showDeleteConfirm('department', btn.dataset.deptId, btn.dataset.deptName);
            else if (action === 'deleteUser') showDeleteConfirm('user', btn.dataset.userId, btn.dataset.userName);
        });

        return el;
    }

    function matchesSearch(node, query) {
        const q = query.toLowerCase();
        return (node.name || '').toLowerCase().includes(q) ||
            (node.email || '').toLowerCase().includes(q) ||
            (node.positionName || '').toLowerCase().includes(q);
    }

    function hasMatchInTree(node, query) {
        if (matchesSearch(node, query)) return true;
        return (node.children || []).some(function (c) { return hasMatchInTree(c, query); });
    }

    // --- Search ---
    if (searchInput) {
        searchInput.addEventListener('input', ChatApp.utils.debounce(function () {
            const q = searchInput.value.trim();
            if (q.length >= 2) {
                // Auto-expand matching
                expandMatching(_hierarchy, q);
            }
            renderTree(q.length >= 2 ? q : '');
        }, 300));
    }

    function expandMatching(nodes, query) {
        (nodes || []).forEach(function (n) {
            if (hasMatchInTree(n, query)) {
                _expandedNodes.add(n.id || n.name);
            }
            if (n.children) expandMatching(n.children, query);
        });
    }

    // --- Expand/Collapse all ---
    if (expandAllBtn) {
        expandAllBtn.addEventListener('click', function () {
            function addAll(nodes) {
                (nodes || []).forEach(function (n) {
                    _expandedNodes.add(n.id || n.name);
                    if (n.children) addAll(n.children);
                });
            }
            addAll(_hierarchy);
            renderTree();
        });
    }

    if (collapseAllBtn) {
        collapseAllBtn.addEventListener('click', function () {
            _expandedNodes.clear();
            renderTree();
        });
    }

    // --- Populate department/position selects ---
    function populateSelects() {
        [document.getElementById('cuDepartment'), document.getElementById('cdParent')].forEach(function (sel) {
            if (!sel) return;
            const firstOpt = sel.options[0];
            sel.innerHTML = '';
            sel.appendChild(firstOpt);
            _departments.forEach(function (d) {
                const opt = document.createElement('option');
                opt.value = d.id;
                opt.textContent = d.name;
                sel.appendChild(opt);
            });
        });

        const posSel = document.getElementById('cuPosition');
        if (posSel) {
            const firstOpt = posSel.options[0];
            posSel.innerHTML = '';
            posSel.appendChild(firstOpt);
            _positions.forEach(function (p) {
                const opt = document.createElement('option');
                opt.value = p.id;
                opt.textContent = p.name + (p.departmentName ? ' (' + p.departmentName + ')' : '');
                posSel.appendChild(opt);
            });
        }
    }

    // --- Create User ---
    let _createUserModal = null;
    function openCreateUser(deptId, deptName) {
        if (!_createUserModal) _createUserModal = new bootstrap.Modal(document.getElementById('createUserModal'));
        document.getElementById('cuFirstName').value = '';
        document.getElementById('cuLastName').value = '';
        document.getElementById('cuEmail').value = '';
        document.getElementById('cuPassword').value = '';
        document.getElementById('cuDateOfBirth').value = '';
        document.getElementById('cuHiringDate').value = '';
        document.getElementById('cuAboutMe').value = '';
        if (deptId) document.getElementById('cuDepartment').value = deptId;
        document.getElementById('cuError').textContent = '';
        document.getElementById('cuSuccess').textContent = '';
        _createUserModal.show();
    }

    document.getElementById('cuSubmit')?.addEventListener('click', async function () {
        const errEl = document.getElementById('cuError');
        const sucEl = document.getElementById('cuSuccess');
        errEl.textContent = '';
        sucEl.textContent = '';

        const data = {
            firstName: document.getElementById('cuFirstName').value.trim(),
            lastName: document.getElementById('cuLastName').value.trim(),
            email: document.getElementById('cuEmail').value.trim(),
            password: document.getElementById('cuPassword').value,
            departmentId: document.getElementById('cuDepartment').value || null,
            positionId: document.getElementById('cuPosition').value || null,
            systemRole: parseInt(document.getElementById('cuRole').value),
            workPhone: document.getElementById('cuPhone').value.trim() || null,
            dateOfBirth: document.getElementById('cuDateOfBirth').value || null,
            hiringDate: document.getElementById('cuHiringDate').value || null,
            aboutMe: document.getElementById('cuAboutMe').value.trim() || null
        };

        if (!data.firstName || !data.lastName || !data.email || !data.password) {
            errEl.textContent = 'Please fill all required fields.';
            return;
        }

        const result = await ChatApp.api.post('/api/identity/users', data);
        if (result.isSuccess) {
            sucEl.textContent = 'User created successfully.';
            setTimeout(function () { _createUserModal.hide(); loadData(); }, 800);
        } else {
            errEl.textContent = result.error || 'Failed to create user.';
        }
    });

    // --- Create Department ---
    let _createDeptModal = null;
    function openCreateDept(parentId) {
        if (!_createDeptModal) _createDeptModal = new bootstrap.Modal(document.getElementById('createDeptModal'));
        document.getElementById('cdName').value = '';
        if (parentId) document.getElementById('cdParent').value = parentId;
        document.getElementById('cdError').textContent = '';
        document.getElementById('cdSuccess').textContent = '';
        _createDeptModal.show();
    }

    document.getElementById('cdSubmit')?.addEventListener('click', async function () {
        const errEl = document.getElementById('cdError');
        const sucEl = document.getElementById('cdSuccess');
        errEl.textContent = '';
        sucEl.textContent = '';

        const name = document.getElementById('cdName').value.trim();
        if (!name) { errEl.textContent = 'Name is required.'; return; }

        const parentId = document.getElementById('cdParent').value || null;

        const result = await ChatApp.api.post('/api/identity/departments', {
            name: name,
            companyId: _companyId,
            parentDepartmentId: parentId
        });

        if (result.isSuccess) {
            sucEl.textContent = 'Department created.';
            setTimeout(function () { _createDeptModal.hide(); loadData(); }, 800);
        } else {
            errEl.textContent = result.error || 'Failed to create department.';
        }
    });

    // --- Edit Department ---
    function openEditDept(deptId) {
        const dept = _departments.find(function (d) { return d.id === deptId; });
        if (!dept) return;
        // Reuse create dept modal for editing
        openCreateDept(dept.parentDepartmentId);
        document.getElementById('cdName').value = dept.name || '';
        document.querySelector('#createDeptModal .modal-title').textContent = 'Edit Department';

        // Override submit for edit
        const submitBtn = document.getElementById('cdSubmit');
        const handler = async function () {
            const errEl = document.getElementById('cdError');
            errEl.textContent = '';
            const name = document.getElementById('cdName').value.trim();
            if (!name) { errEl.textContent = 'Name is required.'; return; }

            const result = await ChatApp.api.put('/api/identity/departments/' + deptId, {
                name: name,
                parentDepartmentId: document.getElementById('cdParent').value || null
            });
            if (result.isSuccess) {
                document.getElementById('cdSuccess').textContent = 'Updated.';
                setTimeout(function () { _createDeptModal.hide(); loadData(); }, 800);
            } else {
                errEl.textContent = result.error || 'Failed.';
            }
            submitBtn.removeEventListener('click', handler);
        };
        // Remove previous listener by cloning
        const newBtn = submitBtn.cloneNode(true);
        submitBtn.parentNode.replaceChild(newBtn, submitBtn);
        newBtn.addEventListener('click', handler);
    }

    // --- Delete Confirmation Dialog ---
    function showDeleteConfirm(type, id, name) {
        var existing = document.getElementById('orgDeleteConfirmModal');
        if (existing) existing.remove();

        var entityLabel = type === 'department' ? 'Department' : 'User';
        var modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'orgDeleteConfirmModal';
        modal.tabIndex = -1;
        modal.innerHTML =
            '<div class="modal-dialog modal-dialog-centered">' +
            '<div class="modal-content">' +
            '<div class="modal-header bg-danger text-white">' +
            '<h5 class="modal-title"><span class="material-icons" style="font-size:20px;vertical-align:middle;margin-right:6px;">warning</span>Delete ' + entityLabel + '</h5>' +
            '<button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body">' +
            '<p>Are you sure you want to delete <strong>' + ChatApp.utils.escapeHtml(name || '') + '</strong>?</p>' +
            '<p class="text-muted small mb-0">This action cannot be undone.</p>' +
            '</div>' +
            '<div class="modal-footer">' +
            '<button class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>' +
            '<button class="btn btn-danger btn-sm" id="orgDeleteConfirmBtn">Delete</button>' +
            '</div>' +
            '</div></div>';

        document.body.appendChild(modal);
        var bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        modal.querySelector('#orgDeleteConfirmBtn').addEventListener('click', async function () {
            var endpoint = type === 'department'
                ? '/api/identity/departments/' + id
                : '/api/identity/users/' + id;
            var result = await ChatApp.api.del(endpoint);
            bsModal.hide();
            if (result.isSuccess) {
                loadData();
            }
        });

        modal.addEventListener('hidden.bs.modal', function () { modal.remove(); });
    }

    // --- Initial load ---
    loadData();

})();
