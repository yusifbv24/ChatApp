/**
 * Layout Controller — replaces MainLayout.razor @code block
 * Handles sidebar, user menu, notifications, clock, network banner, auth check
 */
(function () {
    'use strict';

    // --- Config ---
    ChatApp.apiBase = document.querySelector('meta[name="api-base"]')?.content || 'http://localhost:7000';

    // --- State ---
    let sidebarOpen = false;
    let userMenuOpen = false;
    let notifPanelOpen = false;

    // --- DOM refs ---
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarBrand = document.getElementById('sidebarBrand');
    const adminNavSection = document.getElementById('adminNavSection');
    const headerClock = document.getElementById('headerClock');
    let headerAvatar = document.getElementById('headerAvatar');
    const userMenuTrigger = document.getElementById('userMenuTrigger');
    const userMenuDropdown = document.getElementById('userMenuDropdown');
    const menuAvatarLg = document.getElementById('menuAvatarLg');
    const menuUserName = document.getElementById('menuUserName');
    const menuUserEmail = document.getElementById('menuUserEmail');
    const menuProfileBtn = document.getElementById('menuProfileBtn');
    const menuSettingsBtn = document.getElementById('menuSettingsBtn');
    const logoutBtn = document.getElementById('logoutBtn');
    const notificationBtn = document.getElementById('notificationBtn');
    const notificationPanel = document.getElementById('notificationPanel');
    const closeNotifPanel = document.getElementById('closeNotifPanel');
    const notifBadge = document.getElementById('notifBadge');
    const msgUnreadBadge = document.getElementById('msgUnreadBadge');
    const networkBanner = document.getElementById('networkBanner');
    const networkBannerIcon = document.getElementById('networkBannerIcon');
    const networkBannerText = document.getElementById('networkBannerText');
    const navProfile = document.getElementById('navProfile');

    // --- Load dark mode preference ---
    ChatApp.state.loadDarkMode();

    // --- Clock (synced to minute boundary to avoid drift) ---
    var _clockInterval = null;
    function updateClock() {
        if (!headerClock) return;
        const now = new Date();
        headerClock.textContent = now.getHours().toString().padStart(2, '0') + ':' +
            now.getMinutes().toString().padStart(2, '0');
    }
    updateClock();
    // Sync to the next minute boundary, then run every 60s
    var msToNextMinute = (60 - new Date().getSeconds()) * 1000;
    setTimeout(function () {
        updateClock();
        _clockInterval = setInterval(updateClock, 60000);
    }, msToNextMinute);

    // --- Sidebar toggle ---
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            sidebarOpen = !sidebarOpen;
            sidebar.classList.toggle('sidebar-expanded', sidebarOpen);
            sidebarToggle.querySelector('.material-icons').textContent =
                sidebarOpen ? 'menu_open' : 'menu';
            sidebarToggle.title = sidebarOpen ? 'Collapse' : 'Expand';
            if (sidebarBrand) sidebarBrand.style.display = sidebarOpen ? '' : 'none';

            // Show/hide nav labels
            document.querySelectorAll('.nav-label').forEach(function (el) {
                el.style.display = sidebarOpen ? '' : 'none';
            });
        });
    }

    // --- Active nav highlighting ---
    function highlightActiveNav() {
        document.querySelectorAll('.sidebar-nav .nav-item').forEach(function (el) {
            el.classList.remove('active');
        });
        const path = window.location.pathname;
        if (path === '/') {
            document.getElementById('navFeed')?.classList.add('active');
        } else if (path.startsWith('/messages')) {
            document.getElementById('navMessages')?.classList.add('active');
        } else if (path.startsWith('/settings')) {
            document.getElementById('navSettings')?.classList.add('active');
        } else if (path.startsWith('/admin')) {
            document.getElementById('navOrganization')?.classList.add('active');
        }
    }
    highlightActiveNav();

    // --- User menu toggle ---
    if (userMenuTrigger) {
        userMenuTrigger.addEventListener('click', function (e) {
            e.stopPropagation();
            userMenuOpen = !userMenuOpen;
            userMenuDropdown.style.display = userMenuOpen ? '' : 'none';
            if (userMenuOpen) {
                notifPanelOpen = false;
                notificationPanel.style.display = 'none';
            }
        });
    }

    // --- Notification panel toggle ---
    if (notificationBtn) {
        notificationBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            notifPanelOpen = !notifPanelOpen;
            notificationPanel.style.display = notifPanelOpen ? '' : 'none';
            if (notifPanelOpen) {
                userMenuOpen = false;
                userMenuDropdown.style.display = 'none';
            }
        });
    }
    if (closeNotifPanel) {
        closeNotifPanel.addEventListener('click', function () {
            notifPanelOpen = false;
            notificationPanel.style.display = 'none';
        });
    }

    // --- Close menus on outside click ---
    document.addEventListener('click', function () {
        if (userMenuOpen) {
            userMenuOpen = false;
            userMenuDropdown.style.display = 'none';
        }
        if (notifPanelOpen) {
            notifPanelOpen = false;
            notificationPanel.style.display = 'none';
        }
    });
    // Prevent close when clicking inside panels
    if (userMenuDropdown) userMenuDropdown.addEventListener('click', function (e) { e.stopPropagation(); });
    if (notificationPanel) notificationPanel.addEventListener('click', function (e) { e.stopPropagation(); });

    // --- ESC key priority chain (10 levels, matching Blazor version) ---
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;

        // Priority 1: Image lightbox open -> close it
        var lightbox = document.getElementById('imageLightbox');
        if (lightbox && lightbox.style.display !== 'none') {
            if (ChatApp.chatArea && ChatApp.chatArea.closeLightbox) ChatApp.chatArea.closeLightbox();
            e.preventDefault();
            return;
        }

        // Priority 2: Emoji picker open -> close it
        var emojiPicker = document.getElementById('emojiPicker');
        if (emojiPicker && emojiPicker.style.display !== 'none') {
            emojiPicker.style.display = 'none';
            e.preventDefault();
            return;
        }

        // Priority 3: Context menu open -> close it
        var contextMenu = document.querySelector('.chevron-dropdown.msg-context-dropdown');
        if (contextMenu) {
            contextMenu.remove();
            e.preventDefault();
            return;
        }

        // Priority 4: Forward dialog open -> close it
        var forwardModal = document.getElementById('forwardModal');
        if (forwardModal && forwardModal.classList.contains('show')) {
            var bsModal = bootstrap.Modal.getInstance(forwardModal);
            if (bsModal) bsModal.hide();
            e.preventDefault();
            return;
        }

        // Priority 5: Selection mode active -> cancel it
        if (ChatApp.selection && ChatApp.selection.isActive()) {
            ChatApp.selection.cancelSelection();
            e.preventDefault();
            return;
        }

        // Priority 6: Search panel open -> close it
        if (ChatApp.search && ChatApp.search.isOpen()) {
            ChatApp.search.close();
            e.preventDefault();
            return;
        }

        // Priority 7: Profile panel open -> close it
        var profileOverlay = document.getElementById('profilePanelOverlay');
        if (profileOverlay && profileOverlay.style.display !== 'none') {
            if (ChatApp.profilePanel) ChatApp.profilePanel.close();
            e.preventDefault();
            return;
        }

        // Priority 8: Sidebar open -> close it
        if (ChatApp.sidebar && ChatApp.sidebar.isOpen()) {
            ChatApp.sidebar.close();
            e.preventDefault();
            return;
        }

        // Priority 9: Reply/Edit mode -> cancel it
        if (ChatApp.messageInput) {
            var replyPreview = document.getElementById('inputReplyPreview');
            if (replyPreview && replyPreview.style.display !== 'none') {
                ChatApp.messageInput.cancelMode();
                e.preventDefault();
                return;
            }
        }

        // Priority 10: Message input focused -> blur it
        var activeEl = document.activeElement;
        if (activeEl && (activeEl.tagName === 'TEXTAREA' || activeEl.tagName === 'INPUT') &&
            activeEl.closest('.message-input-container')) {
            activeEl.blur();
            e.preventDefault();
            return;
        }
    });

    // --- Logout ---
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async function () {
            userMenuOpen = false;
            userMenuDropdown.style.display = 'none';
            await ChatApp.signalR.disconnect();
            await ChatApp.auth.logout();
        });
    }

    // --- Profile panel from menu ---
    if (menuProfileBtn) {
        menuProfileBtn.addEventListener('click', function () {
            userMenuOpen = false;
            userMenuDropdown.style.display = 'none';
            if (ChatApp.profilePanel) ChatApp.profilePanel.open();
        });
    }
    if (navProfile) {
        navProfile.addEventListener('click', function () {
            if (ChatApp.profilePanel) ChatApp.profilePanel.open();
        });
    }

    // --- Update header with user info ---
    function renderUserHeader(user) {
        if (!user) return;

        // Header avatar - re-query to avoid stale reference after outerHTML swap
        headerAvatar = document.getElementById('headerAvatar');
        if (headerAvatar) {
            if (user.avatarUrl) {
                // Replace with img tag
                var img = document.createElement('img');
                img.src = user.avatarUrl;
                img.alt = user.fullName;
                img.className = 'user-menu-avatar';
                img.id = 'headerAvatar';
                headerAvatar.replaceWith(img);
            } else {
                // Show initials
                if (headerAvatar.tagName === 'IMG') {
                    var div = document.createElement('div');
                    div.className = 'user-menu-avatar';
                    div.id = 'headerAvatar';
                    headerAvatar.replaceWith(div);
                    headerAvatar = div;
                }
                headerAvatar.style.backgroundColor = ChatApp.utils.getAvatarColor(user.id);
                headerAvatar.textContent = ChatApp.utils.getInitials(user.fullName);
            }
        }

        // Menu avatar large
        if (menuAvatarLg) {
            if (user.avatarUrl) {
                menuAvatarLg.innerHTML = '<img src="' + ChatApp.utils.escapeHtml(user.avatarUrl) +
                    '" alt="' + ChatApp.utils.escapeHtml(user.fullName) + '" />' +
                    '<span class="user-status-dot"></span>';
            } else {
                menuAvatarLg.innerHTML = '<div class="avatar-placeholder-lg" style="background-color:' +
                    ChatApp.utils.getAvatarColor(user.id) + ';">' +
                    ChatApp.utils.getInitials(user.fullName) + '</div>' +
                    '<span class="user-status-dot"></span>';
            }
        }

        // Menu info
        if (menuUserName) menuUserName.textContent = user.fullName || '';
        if (menuUserEmail) menuUserEmail.textContent = user.email || '';

        // Admin nav
        if (adminNavSection) {
            adminNavSection.style.display = ChatApp.state.isAdmin ? '' : 'none';
        }
    }

    // --- Unread badge updates ---
    ChatApp.state.on('unreadMessagesChanged', function (count) {
        if (!msgUnreadBadge) return;
        if (count > 0) {
            msgUnreadBadge.textContent = count > 99 ? '99+' : count;
            msgUnreadBadge.style.display = '';
        } else {
            msgUnreadBadge.style.display = 'none';
        }
    });

    ChatApp.state.on('unreadNotificationsChanged', function (count) {
        if (!notifBadge) return;
        if (count > 0) {
            notifBadge.textContent = count > 99 ? '99+' : count;
            notifBadge.style.display = '';
        } else {
            notifBadge.style.display = 'none';
        }
    });

    // --- Network Status Banner ---
    let _bannerDismissTimer = null;
    let _wasDisconnected = false;

    function showBanner(icon, text, cssClass) {
        if (!networkBanner) return;
        if (_bannerDismissTimer) { clearTimeout(_bannerDismissTimer); _bannerDismissTimer = null; }
        networkBannerIcon.textContent = icon;
        networkBannerIcon.classList.toggle('rotating', icon === 'sync');
        networkBannerText.textContent = text;
        networkBanner.className = 'network-banner ' + cssClass;
        networkBanner.style.display = '';
    }

    function hideBanner(delayMs) {
        if (!networkBanner) return;
        if (_bannerDismissTimer) clearTimeout(_bannerDismissTimer);
        _bannerDismissTimer = setTimeout(function () {
            networkBanner.style.display = 'none';
        }, delayMs || 0);
    }

    ChatApp.signalR.on('reconnecting', function () {
        _wasDisconnected = true;
        showBanner('sync', 'Yenid\u0259n qo\u015fulur...', 'reconnecting');
    });

    ChatApp.signalR.on('disconnected', function () {
        _wasDisconnected = true;
        showBanner('wifi_off', '\u0130nternet ba\u011flant\u0131s\u0131 yoxdur', 'disconnected');
    });

    ChatApp.signalR.on('connected', function () {
        if (_wasDisconnected) {
            _wasDisconnected = false;
            showBanner('wifi', 'Ba\u011flant\u0131 b\u0259rpa olundu', 'back-online');
            hideBanner(3000);
        }
    });

    ChatApp.signalR.on('reconnected', function () {
        if (_wasDisconnected) {
            _wasDisconnected = false;
            showBanner('wifi', 'Ba\u011flant\u0131 b\u0259rpa olundu', 'back-online');
            hideBanner(3000);
        }
    });

    // --- App Initialization ---
    async function initApp() {
        // Check if we're on a page that requires auth
        const isLoginPage = window.location.pathname.startsWith('/auth/login');

        if (isLoginPage) return;  // Login page handles its own auth

        // Load current user
        const loaded = await ChatApp.auth.loadCurrentUser();
        if (!loaded) {
            window.location.href = '/auth/login';
            return;
        }

        // Render user info in header
        renderUserHeader(ChatApp.state.currentUser);

        // Initialize SignalR
        await ChatApp.signalR.initialize();
    }

    // Start initialization
    initApp().catch(function (err) {
        console.error('[Layout] Init error:', err);
    });

})();
