/**
 * Positions Management — replaces Positions.razor
 * Position CRUD with department grouping
 */
(function () {
    'use strict';

    const groupsContainer = document.getElementById('positionGroups');
    const searchInput = document.getElementById('posSearchInput');
    const newPositionBtn = document.getElementById('newPositionBtn');

    if (!groupsContainer) return;

    let _positions = [];
    let _departments = [];
    let _editingId = null;
    let _bsModal = null;

    async function loadData() {
        const [posResult, deptResult] = await Promise.all([
            ChatApp.api.get('/api/identity/positions'),
            ChatApp.api.get('/api/identity/departments')
        ]);

        if (posResult.isSuccess) _positions = posResult.value || [];
        if (deptResult.isSuccess) _departments = deptResult.value || [];

        updateStats();
        populateDeptSelect();
        render();
    }

    function updateStats() {
        document.getElementById('posStatTotal').textContent = _positions.length;
        const deptIds = new Set(_positions.map(function (p) { return p.departmentId; }).filter(Boolean));
        document.getElementById('posStatDepts').textContent = deptIds.size;
    }

    function populateDeptSelect() {
        const sel = document.getElementById('posDepartment');
        if (!sel) return;
        sel.innerHTML = '<option value="">No Department</option>';
        _departments.forEach(function (d) {
            sel.innerHTML += '<option value="' + d.id + '">' + ChatApp.utils.escapeHtml(d.name) + '</option>';
        });
    }

    function render(filterQuery) {
        const filtered = filterQuery
            ? _positions.filter(function (p) {
                const q = filterQuery.toLowerCase();
                return (p.name || '').toLowerCase().includes(q) ||
                    (p.description || '').toLowerCase().includes(q) ||
                    (p.departmentName || '').toLowerCase().includes(q);
            })
            : _positions;

        // Group by department
        const groups = {};
        filtered.forEach(function (p) {
            const key = p.departmentName || 'No Department';
            if (!groups[key]) groups[key] = [];
            groups[key].push(p);
        });

        groupsContainer.innerHTML = '';
        if (Object.keys(groups).length === 0) {
            groupsContainer.innerHTML = '<p class="text-muted text-center py-4">No positions found.</p>';
            return;
        }

        Object.keys(groups).sort().forEach(function (deptName) {
            const group = document.createElement('div');
            group.className = 'position-group-card';

            let html = '<div class="position-group-header">' +
                '<span class="material-icons" style="font-size:20px;color:#f59e0b;">apartment</span>' +
                '<span class="position-group-name">' + ChatApp.utils.escapeHtml(deptName) + '</span>' +
                '<span class="position-group-count">' + groups[deptName].length + ' positions</span></div>';

            html += '<div class="position-group-items">';
            groups[deptName].forEach(function (pos) {
                html += '<div class="position-item">' +
                    '<div class="position-item-info">' +
                    '<span class="position-item-name">' + ChatApp.utils.escapeHtml(pos.name || '') + '</span>' +
                    (pos.description ? '<span class="position-item-desc">' + ChatApp.utils.escapeHtml(pos.description) + '</span>' : '') +
                    '<span class="position-item-date">' + (pos.createdDate ? new Date(pos.createdDate).toLocaleDateString() : '') + '</span>' +
                    '</div>' +
                    '<div class="position-item-actions">' +
                    (canUpdate ? '<button class="org-action-btn" title="Edit" data-action="edit" data-id="' + pos.id + '">' +
                    '<span class="material-icons" style="font-size:16px;">edit</span></button>' : '') +
                    (canDelete ? '<button class="org-action-btn" title="Delete" data-action="delete" data-id="' + pos.id + '" data-name="' + ChatApp.utils.escapeHtml(pos.name || '') + '">' +
                    '<span class="material-icons" style="font-size:16px;color:#ef4444;">delete</span></button>' : '') +
                    '</div></div>';
            });
            html += '</div>';

            group.innerHTML = html;

            // Event delegation
            group.addEventListener('click', function (e) {
                const btn = e.target.closest('button[data-action]');
                if (!btn) return;
                const action = btn.dataset.action;
                const id = btn.dataset.id;
                if (action === 'edit') openEdit(id);
                else if (action === 'delete') handleDelete(id);
            });

            groupsContainer.appendChild(group);
        });
    }

    // --- Search ---
    if (searchInput) {
        searchInput.addEventListener('input', ChatApp.utils.debounce(function () {
            render(searchInput.value.trim());
        }, 300));
    }

    // --- Permission checks ---
    var canCreate = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Create');
    var canUpdate = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Update');
    var canDelete = !ChatApp.state.hasPermission || ChatApp.state.hasPermission('Users.Delete');

    // --- New Position ---
    if (newPositionBtn) {
        if (!canCreate) {
            newPositionBtn.style.display = 'none';
        } else {
            newPositionBtn.addEventListener('click', function () { openCreate(); });
        }
    }

    function openCreate() {
        _editingId = null;
        document.getElementById('posModalTitle').textContent = 'Create Position';
        document.getElementById('posName').value = '';
        document.getElementById('posDepartment').value = '';
        document.getElementById('posDescription').value = '';
        document.getElementById('posError').textContent = '';
        document.getElementById('posSuccess').textContent = '';
        if (!_bsModal) _bsModal = new bootstrap.Modal(document.getElementById('positionModal'));
        _bsModal.show();
    }

    function openEdit(id) {
        const pos = _positions.find(function (p) { return p.id === id; });
        if (!pos) return;
        _editingId = id;
        document.getElementById('posModalTitle').textContent = 'Edit Position';
        document.getElementById('posName').value = pos.name || '';
        document.getElementById('posDepartment').value = pos.departmentId || '';
        document.getElementById('posDescription').value = pos.description || '';
        document.getElementById('posError').textContent = '';
        document.getElementById('posSuccess').textContent = '';
        if (!_bsModal) _bsModal = new bootstrap.Modal(document.getElementById('positionModal'));
        _bsModal.show();
    }

    // --- Submit ---
    document.getElementById('posSubmit')?.addEventListener('click', async function () {
        const errEl = document.getElementById('posError');
        const sucEl = document.getElementById('posSuccess');
        errEl.textContent = '';
        sucEl.textContent = '';

        const name = document.getElementById('posName').value.trim();
        if (!name) { errEl.textContent = 'Name is required.'; return; }

        const data = {
            name: name,
            departmentId: document.getElementById('posDepartment').value || null,
            description: document.getElementById('posDescription').value.trim() || null
        };

        let result;
        if (_editingId) {
            result = await ChatApp.api.put('/api/identity/positions/' + _editingId, data);
        } else {
            result = await ChatApp.api.post('/api/identity/positions', data);
        }

        if (result.isSuccess) {
            sucEl.textContent = _editingId ? 'Updated.' : 'Created.';
            setTimeout(function () { _bsModal.hide(); loadData(); }, 800);
        } else {
            errEl.textContent = result.error || 'Operation failed.';
        }
    });

    // --- Delete (styled confirmation) ---
    function handleDelete(id) {
        var pos = _positions.find(function (p) { return p.id === id; });
        var posName = pos ? pos.name : '';

        var existing = document.getElementById('posDeleteConfirmModal');
        if (existing) existing.remove();

        var modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'posDeleteConfirmModal';
        modal.tabIndex = -1;
        modal.innerHTML =
            '<div class="modal-dialog modal-dialog-centered">' +
            '<div class="modal-content">' +
            '<div class="modal-header bg-danger text-white">' +
            '<h5 class="modal-title"><span class="material-icons" style="font-size:20px;vertical-align:middle;margin-right:6px;">warning</span>Delete Position</h5>' +
            '<button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body">' +
            '<p>Are you sure you want to delete <strong>' + ChatApp.utils.escapeHtml(posName) + '</strong>?</p>' +
            '<p class="text-muted small mb-0">This action cannot be undone.</p>' +
            '</div>' +
            '<div class="modal-footer">' +
            '<button class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>' +
            '<button class="btn btn-danger btn-sm" id="posDeleteConfirmBtn">Delete</button>' +
            '</div>' +
            '</div></div>';

        document.body.appendChild(modal);
        var bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        modal.querySelector('#posDeleteConfirmBtn').addEventListener('click', async function () {
            var result = await ChatApp.api.del('/api/identity/positions/' + id);
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
