# ChatApp Project Notes

## Project Overview
This is a **modular monolith** real-time chat application built with .NET and Blazor.

## Architecture
- **Pattern**: Modular Monolith with Clean Architecture / DDD
- **Backend**: ASP.NET Core API (`ChatApp.Api`)
- **Frontend**: Blazor WebAssembly (`ChatApp.Blazor.Client`)

## Modules
Each module follows the layered structure: `Api → Application → Domain → Infrastructure`

| Module | Purpose |
|--------|---------|
| **Identity** | User authentication, roles, permissions |
| **Channels** | Group chat channels |
| **DirectMessages** | 1-on-1 messaging |
| **Files** | File uploads/attachments |
| **Notifications** | Push notifications, alerts |
| **Search** | Message/content search |
| **Settings** | User/app settings |

## Shared Projects
- `ChatApp.Shared.Kernel` - Domain primitives, base classes
- `ChatApp.Shared.Infrastructure` - Cross-cutting infrastructure

## Development Notes

### Current Focus: Messages Page (WhatsApp Web Style)
Building a modern messaging UI similar to WhatsApp Web.

### Frontend Structure
- **Messages Page**: `ChatApp.Blazor.Client/Features/Messages/Pages/Messages.razor`
- **Key Components**:
  - `ChatArea.razor` - Main message display with bubbles, typing indicators, pagination
  - `ConversationList.razor` - Unified sidebar (WhatsApp style) with DMs + Groups in single list
  - `MessageBubble.razor` - Individual message component
  - `MessageInput.razor` - Text input with typing indicator support
- **Services**: `ConversationService.cs`, `ChannelService.cs`
- **Real-time**: SignalR via `SignalRService.cs` → `/hubs/chat`
- **Styling**: `wwwroot/css/messages.css`, MudBlazor components

### Backend Structure
- **Direct Messages**: `/api/conversations` endpoints
- **Channels**: `/api/channels` endpoints
- **SignalR Hub**: `ChatHub.cs` in Shared.Infrastructure
- **Pattern**: CQRS with MediatR, FluentValidation

### Key Patterns
- Result<T> pattern for error handling
- Permission-based auth: `[RequirePermission("Messages.Read")]`
- Soft deletes (IsDeleted flag)
- UTC timestamps everywhere
- 50 messages per page pagination

---
## Session Log

### Session 1 (2025-11-29)
- Created CLAUDE.md for persistent project notes
- Explored full codebase structure
- **Goal**: Build WhatsApp Web-style messages page
- Key files identified for messaging UI work

### Session 2 (2025-11-29)
**Redesigned ConversationList component (WhatsApp style):**
- Removed tabs (Direct/Channels) - now unified single list
- Direct messages and Groups (channels) appear in same list, sorted by last activity
- Added "Chats" header with new chat button (edit icon)
- New chat button shows dropdown menu:
  - "New Message" → starts direct conversation
  - "New Group" → creates channel/group
- Group avatars show rounded square with group icon
- Direct conversation avatars show circular with online indicator
- Search placeholder updated: "Search or start new chat"

**Files modified:**
- `ChatApp.Blazor.Client/Features/Messages/Components/ConversationList.razor`
- `ChatApp.Blazor.Client/wwwroot/css/messages.css`

**Design decisions:**
- Channel = Group in WhatsApp terminology (group messaging)
- Private channels show lock icon, public show group icon
- Time format: HH:mm for today, day name for this week, dd/MM/yy older

**Fixed user search in New Message dialog:**
- Added `/api/users/search?q={term}` endpoint (no special permissions needed)
- Created `SearchUsersQuery` in Identity.Application
- Added `SearchUsersAsync` to IUserService/UserService
- Implemented debounced search (300ms) in Messages.razor.cs
- Excludes current user from search results
- Minimum 2 characters to trigger search

**Fixed isAdmin claim always false in JWT:**
- Bug: `User.IsAdmin` is a calculated property based on `UserRoles` collection
- Problem: LoginCommand & RefreshTokenCommand loaded User without including UserRoles
- Fix: Added `.Include(u => u.UserRoles)` to both commands
- Files: `LoginCommand.cs`, `RefreshTokenCommand.cs`
- Admin can now upload avatars for other users correctly

**Conversation visibility feature (WhatsApp-like):**
- New conversations are only visible to the initiator until a message is sent
- Added `InitiatedByUserId` and `HasMessages` fields to `DirectConversation` entity
- When first message sent, `HasMessages` is set to true
- Query filters: `(InitiatedByUserId == userId || HasMessages)`
- Files modified:
  - `DirectConversation.cs` - Added new fields
  - `StartConversationCommand.cs` - Track initiator
  - `SendDirectMessageCommand.cs` - Mark HasMessages on first message
  - `DirectConversationRepository.cs` - Filter query
- Migration: `AddConversationInitiatorTracking`

**Online status system:**
- Backend: `ConnectionManager` (Singleton) tracks active SignalR connections
- Users appear online only when they have an active SignalR connection
- Real-time: `UserOnline`/`UserOffline` events broadcast to all clients
- Frontend updates conversation list on status change
- Note: Both users must have the messages page open to see each other as online

**Pending conversation flow (WhatsApp-like):**
- When user searches and clicks a user, chat area shows immediately (no conversation created)
- Conversation is only created when first message is sent
- State variables: `isPendingConversation`, `pendingUser`
- Flow:
  1. Click user in search → Set pending state, show chat area
  2. Type and send message → Create conversation, send message, update list
  3. Conversation appears in sidebar only after message sent
- If user already has conversation with selected user, just selects existing one
- Files modified: `Messages.razor.cs`

**Fixed SignalR messaging issues:**
- Bug: `ChatHubConnection.SendMessageAsync` was wrapping args incorrectly
- Fix: Changed from `SendAsync(method, args)` to `SendCoreAsync(method, args)`
- Added optimistic UI: Messages appear immediately after sending (don't wait for SignalR)
- Added duplicate prevention: SignalR handlers check if message already exists by ID
- New conversation from other user triggers conversation list reload
- Files modified: `ChatHubConnection.cs`, `Messages.razor.cs`

### Session 3 (2025-11-30)
**Fixed SignalR authentication for Blazor WASM:**
- Root cause: WebSocket connections don't automatically include HttpOnly cookies
- Backend expects access token in query string for SignalR (Priority: cookie → query string → header)
- Solution:
  1. Created `/api/auth/signalr-token` endpoint that returns JWT from cookie (requires auth)
  2. Updated `ChatHubConnection` to fetch token and pass via `AccessTokenProvider`
  3. Added reconnection handling to refresh token on reconnect
- Files modified:
  - `AuthController.cs` - Added GetSignalRToken endpoint
  - `ChatHubConnection.cs` - Uses AccessTokenProvider, handles reconnects

**Added auto-scroll to chat area:**
- Chat now automatically scrolls to bottom when new messages arrive
- Added JavaScript utility `chatAppUtils.scrollToBottom` in `app.js`
- ChatArea tracks message count and scrolls on new messages
- Also scrolls on first render if messages exist
- Files modified:
  - `wwwroot/js/app.js` - Added scrollToBottom and scrollIntoView functions
  - `ChatArea.razor` - Added OnParametersSet/OnAfterRenderAsync for auto-scroll

### Session 4 (2025-11-30)
**Implemented Remember Me auto-refresh token flow:**
- When user logs in with "Remember Me" checked, preference saved to localStorage
- On page load, if access token expired but RememberMe=true, auto-refresh using refresh token
- If RememberMe=false, redirect to login (no auto-refresh)
- On logout, RememberMe flag is cleared
- Files modified:
  - `Login.razor` - Save rememberMe to localStorage
  - `CustomAuthStateProvider.cs` - Added TryGetCurrentUserAsync with auto-refresh logic

**Fixed duplicate message issue when sending:**
- Problem: Sender saw message twice (optimistic UI + SignalR broadcast)
- Solution: Skip sender's own messages in SignalR handlers (already added via optimistic UI)
- Files modified:
  - `Messages.razor.cs` - HandleNewDirectMessage/HandleNewChannelMessage skip currentUserId

**Reset notification icon for future use:**
- Changed from hardcoded `notificationCount = 3` to use `AppState.UnreadNotificationCount`
- Notification count now starts at 0, ready for future notification feature

**Added unread message badge to Messages nav icon:**
- Added `UnreadMessageCount` property to `AppState`
- Messages nav icon shows red badge with unread count
- Badge updates in real-time via AppState.OnChange subscription
- Files modified:
  - `AppState.cs` - Added UnreadMessageCount property and helper methods
  - `MainLayout.razor` - Added MudBadge to Messages nav, injected AppState

**Mark as read when entering conversation/channel:**
- When selecting a conversation/channel with unread messages:
  - Global unread count decrements by conversation's unread count
  - Conversation's local unread count set to 0
- When receiving new message from others (not in current conversation):
  - Global unread count increments
  - Conversation's local unread count increments
- Added `UnreadCount` property to `ChannelDto` (default 0)
- Files modified:
  - `Messages.razor.cs` - SelectConversation/SelectChannel update counts
  - `ChannelDto.cs` - Added UnreadCount parameter

**Auto-mark messages as read in real-time:**
- When viewing a conversation and a new message arrives via SignalR:
  - Message is automatically marked as read on the server
  - Message is displayed with IsRead = true
- Previously, messages were only marked as read when re-selecting the conversation
- Files modified:
  - `Messages.razor.cs` - HandleNewDirectMessage calls MarkAsReadAsync immediately

**Auto-focus message input when entering conversation:**
- When entering a conversation/channel, the message input textarea is automatically focused
- User can start typing immediately without clicking on the input
- Focus also triggers when switching between conversations
- Added ConversationId parameter to track conversation changes
- Files modified:
  - `MessageInput.razor` - Added ConversationId parameter, OnAfterRenderAsync for focus
  - `ChatArea.razor` - Added ConversationId parameter, passed to MessageInput
  - `Messages.razor` - Passed selectedConversationId/selectedChannelId to ChatArea

### Session 5 (2025-12-05)
**Fixed avatar upload for new users created by admin:**
- **Problem:** When admin created a user with an avatar, the file was saved to the admin's folder instead of the new user's folder
- **Root cause:** Avatar was uploaded BEFORE user creation, passing `null` as targetUserId
- **Solution:** Refactored user creation flow:
  1. Create user first (without avatar)
  2. Upload avatar to the new user's folder (using new user's ID)
  3. Update user with avatar URL
- **Backend:** Already supported `targetUserId` parameter in `/api/files/upload/profile-picture` endpoint
- Files modified:
  - `Users.razor.cs` - Refactored HandleCreateUser() method to upload avatar after user creation

**Fixed 4 critical real-time messaging issues:**

1. **New conversation not appearing in list:**
   - **Problem:** When starting a new conversation from user search, it didn't appear in the conversation list until page refresh
   - **Solution:** Reload conversation list after creating pending conversation and sending first message
   - Files modified: `Messages.razor.cs` - Added `await LoadConversationsAndChannels()` after conversation creation

2. **Mark as read not working in real-time (Race Condition):**
   - **Problem:** When both users were messaging, sender didn't see double checkmark (read receipt) until page refresh (intermittent)
   - **Root cause:** Race condition! SignalR `MessageRead` event arrives BEFORE the sender's HTTP POST completes
     - Timeline: User A sends message → Backend broadcasts to User B → User B marks as read → SignalR sends `MessageRead` to User A ⚡ → User A's HTTP response completes
     - Result: `MessageRead` event arrives but message isn't in `directMessages` list yet, so read receipt is lost
   - **Solution:** Pending read receipts pattern
     - Added `pendingReadReceipts` dictionary to store read receipts for messages that don't exist yet
     - When `MessageRead` event arrives and message not found, store it as pending
     - When message is added optimistically in `SendMessage`, check for pending read receipt and apply immediately
     - Clear pending receipts when changing conversations to prevent memory leaks
   - Files modified: `Messages.razor.cs` - Added pendingReadReceipts, updated HandleMessageRead and SendMessage

3. **JWT refresh mechanism redirecting to login:**
   - **Problem:** When access token expired, app immediately redirected to login instead of auto-refreshing with refresh token
   - **Root cause:** `RedirectToLogin` component redirected immediately without waiting for auth state provider to attempt token refresh
   - **Solution:** Modified `RedirectToLogin` to:
     - Check if RememberMe preference is set
     - Wait 500ms for auth state provider to refresh token
     - Only redirect if refresh fails or RememberMe not enabled
   - Files modified: `RedirectToLogin.razor` - Added async initialization with token refresh wait logic

4. **Online/offline status inconsistent:**
   - **Problem:** User online/offline status displayed inconsistently when navigating between pages
   - **Root cause:** Online status from backend was stale/cached, not refreshed when returning to Messages page
   - **Solution:** Query current online status from SignalR hub after loading conversations:
     - Added `RefreshOnlineStatus()` method that queries `GetOnlineStatus` for all conversation participants
     - Updates conversation list and current recipient status with real-time data
   - Files modified: `Messages.razor.cs` - Added RefreshOnlineStatus method, called from LoadConversationsAndChannels

**Implemented Reply and Forward features (WhatsApp-style):**

1. **Reply Feature:**
   - **UI Components:**
     - Added reply preview in MessageBubble showing replied-to message with sender name and truncated content
     - Added reply indicator banner in MessageInput showing "Replying to..." with cancel button
     - Green background color for reply mode input area
   - **Data Flow:**
     - Updated DTOs: Added `ReplyToMessageId`, `ReplyToContent`, `ReplyToSenderName` to DirectMessageDto and ChannelMessageDto
     - Messages.razor.cs manages reply state (isReplying, replyToMessageId, replyToSenderName, replyToContent)
     - HandleReply method sets reply state when user clicks Reply button
     - CancelReply method clears reply state
     - SendMessage includes reply data in new messages and clears reply state after sending
   - **Visual Design:**
     - Reply preview in bubbles: Gray box with blue left border, sender name in blue, content truncated to 50 chars
     - Reply indicator: White box with blue left border, close button on right
   - Files modified:
     - DTOs: `DirectMessageDto.cs`, `ChannelMessageDto.cs` - Added reply fields
     - Components: `MessageBubble.razor`, `MessageInput.razor`, `ChatArea.razor`
     - Logic: `Messages.razor.cs` - Added HandleReply, CancelReply, reply state
     - Styles: `messages.css` - Added reply-preview and reply-indicator styles

2. **Forward Feature:**
   - **UI Components:**
     - Created Forward dialog showing all available conversations and channels
     - Grouped by "Direct Messages" and "Groups" sections
     - Shows message preview at top of dialog
     - Hover effect on destination items with send icon
   - **Data Flow:**
     - Messages.razor.cs manages forward state (showForwardDialog, forwardingDirectMessage, forwardingChannelMessage)
     - HandleForward method opens dialog with selected message
     - ForwardToConversation and ForwardToChannel methods send message content to selected destination
     - CancelForward method closes dialog and clears state
   - **Visual Design:**
     - Modal dialog with message preview banner
     - List of destinations with avatars and names
     - Send icon appears on hover
   - Files modified:
     - UI: `Messages.razor` - Added forward dialog markup
     - Logic: `Messages.razor.cs` - Added HandleForward, ForwardToConversation, ForwardToChannel, CancelForward
     - Styles: `messages.css` - Added forward dialog styles

**Notes:**
- Reply and Forward are frontend-only implementations - backend currently accepts reply fields but doesn't persist them
- To fully enable Reply feature, backend needs to add ReplyToMessageId fields to database entities
- Both features work for Direct Messages and Channels

### Session 6 (2025-12-09)
**Implemented real-time message editing feature:**
- When a user edits a message, the other party receives the updated content immediately in real-time
- The edited message appears instantly in their chat area with the "(edited)" label
- The conversation list automatically refreshes to show the updated last message for both users
- Backend changes:
  - Updated `EditDirectMessageCommand.cs` and `EditChannelMessageCommand.cs` to fetch edited message DTO and broadcast it
  - Added new SignalR notification methods in `SignalRNotificationService.cs` that send complete message DTOs
- Frontend changes:
  - Updated SignalR event signatures from `Action<Guid, Guid>` to `Action<DirectMessageDto>` and `Action<ChannelMessageDto>`
  - Added new event listeners for "DirectMessageEdited" and "ChannelMessageEdited" that deserialize full message DTOs
  - Removed deprecated "MessageEdited" handler
  - Updated `HandleDirectMessageEdited` and `HandleChannelMessageEdited` to receive full DTOs and refresh conversation list

**Implemented Page Visibility API for smart read receipts:**
- **Problem:** Messages were marked as read even when user was on a different browser tab, not actually viewing the messages
- **Solution:** Use Page Visibility API to only mark messages as read when the browser tab is visible and active
- **How it works:**
  1. JavaScript tracks browser tab visibility state using `document.hidden`
  2. Blazor component subscribes to visibility changes via JavaScript interop
  3. Messages are only auto-marked as read when:
     - User is viewing the conversation (already checked)
     - AND the browser tab is visible/active (new check)
  4. If tab is hidden, messages arrive but remain unread until user returns to the tab
- **Files modified:**
  - `wwwroot/js/app.js` - Added `isPageVisible()` and `subscribeToVisibilityChange()` functions
  - `MessageInput.razor` - Removed unused `HandleFocus` method and `@onfocus` binding
  - `Messages.razor.cs` - Added:
    - Page visibility state tracking (`isPageVisible`, `visibilitySubscription`, `dotNetReference`)
    - `OnAfterRenderAsync` to subscribe to visibility changes
    - `OnVisibilityChanged` method (called by JavaScript when visibility changes)
    - Updated `HandleNewDirectMessage` to only mark as read when `isPageVisible` is true
    - Disposal of visibility subscription in `DisposeAsync`
- **User Experience:** Messages now only show as "read" (double checkmark) when the recipient actually views them with the tab active
- **Additional Fix:** When user returns to the page (tab becomes visible again):
  - If they're viewing a conversation, all unread messages in that conversation are automatically marked as read
  - Implemented in `MarkUnreadMessagesAsRead()` method called from `OnVisibilityChanged` when visibility changes from hidden to visible
  - This ensures messages that arrived while the user was away get marked as read when they return

**Auto-refocus textarea after any interaction:**
- **Problem:** When clicking on the chat area or messages (for edit, reply, etc.), the textarea loses focus and user must manually click it again
- **Solution:** Textarea now automatically refocuses after any interaction except during message sending
- **How it works:**
  1. Added public `FocusAsync()` method to MessageInput component
  2. ChatArea component has a reference to MessageInput
  3. Added click handler on messages-container div that calls `FocusAsync()`
  4. Added `OnActionCompleted` callback parameter to MessageBubble
  5. All MessageBubble actions (edit, delete, reply, forward, copy, pin, reaction, reply click) call `OnActionCompleted` after completing
  6. ChatArea passes `RefocusInput` method to MessageBubble's `OnActionCompleted` parameter
  7. Textarea remains focused at all times, providing seamless typing experience
- **Files modified:**
  - `MessageInput.razor` - Added public `FocusAsync()` method
  - `ChatArea.razor` - Added `@ref="messageInputRef"` to MessageInput, added `@onclick="HandleChatAreaClick"` to messages-container, added `HandleChatAreaClick()` and `RefocusInput()` methods, passed `OnActionCompleted="RefocusInput"` to all MessageBubble components
  - `MessageBubble.razor` - Added `OnActionCompleted` EventCallback parameter, called it in all action methods: `OnEditClick`, `OnDeleteClick`, `SelectReaction`, `HandleReplyButtonClick`, `OnCopyClick`, `OnForwardClick`, `OnPinClick`, `HandleReplyClick`
- **User Experience:**
  - User can click anywhere in the chat area (messages, load more button, etc.) and the textarea automatically refocuses
  - After any message action (copy, edit, reply, forward, add reaction, etc.), the textarea immediately refocuses
  - Allows seamless, uninterrupted typing without manual focus - just like WhatsApp Web

**WhatsApp-style hover menu (final design):**
- **Problem 1:** Message action buttons were hard to access for large messages
- **Problem 2:** More menu opened below messages at the bottom of the page, hiding edit/delete buttons
- **Solution:** Implemented WhatsApp-exact design with chevron dropdown and external react button
- **Final Design (WhatsApp-style):**
  1. **More Button (Chevron):**
     - Small chevron-down button (▼) in the upper right corner inside the bubble
     - Only visible on hover over message
     - Semi-transparent with subtle shadow
     - Opens dropdown menu with Reply, Copy, Forward, Edit, Delete, Pin options
  2. **React Button:**
     - Emoji button outside the bubble (left for own messages, right for others)
     - Visible on hover over message
     - Circular white button with shadow
     - Opens reaction picker directly at button location
     - Turns primary color on hover
  3. **Smart Menu Positioning:**
     - Dynamically calculates menu height based on number of items
     - Opens above if not enough space below AND more space above
     - Added max-height constraint (viewport height - 100px)
     - Enables vertical scrolling if menu exceeds available space
  4. **Hover State Management:**
     - Both buttons appear on mouse enter
     - Stay visible while menus are open
     - Hide on mouse leave (unless menu is open)
- **Files modified:**
  - `MessageBubble.razor`:
    - Added single `bubble-more-btn` with chevron icon inside bubble
    - Added `message-react-btn` outside bubble (like before)
    - Uses `KeyboardArrowDown` icon for more button (WhatsApp-style)
    - Enhanced `CheckMenuPosition()` with dynamic height calculation
    - Updated close methods to hide hover actions
  - `messages.css`:
    - New `.bubble-more-btn` styles (small chevron button)
    - New `.message-react-btn` styles (external emoji button)
    - Added `max-height` and `overflow-y: auto` to `.more-menu-popup`
    - Removed old `.bubble-hover-actions` styles
- **User Experience:**
  - Hover over message → see small chevron ▼ in top-right corner + emoji button outside ✅
  - Click chevron → dropdown menu opens (always fully visible) ✅
  - Click emoji button → reaction picker opens at button location ✅
  - Exact WhatsApp Web design and behavior ✅
  - Works perfectly for messages of any size ✅

**Message bubble layout refinement:**
- **Change:** Improved message bubble structure with proper spacing
- **New Layout:**
  1. **Message Content** - Text, replies, forwarded indicator
  2. **Spacing Area (20px)** - Empty space with More button (chevron) positioned here on hover
  3. **Message Footer** - Time, edited label, pinned icon, read status (double check)
- **Implementation:**
  - Restructured bubble into three sections: content → spacing → footer
  - `.message-spacing` div provides 20px gap between content and footer
  - More button (chevron) now positioned in the spacing area (on right side)
  - `.message-footer` shows time and status below the spacing
  - Removed old `.message-meta-inline` and `.message-time-inline` styles
- **Files modified:**
  - `MessageBubble.razor` - Restructured layout with `message-spacing` and `message-footer` divs
  - `messages.css` - Added `.message-spacing` and `.message-footer` styles, removed old inline styles
- **User Experience:**
  - Clean separation between message content and metadata ✅
  - More button appears in the spacing (not overlapping content) ✅
  - Time always at bottom in consistent position ✅
  - Better visual hierarchy and WhatsApp-like appearance ✅

**Implemented new table-like message bubble layout:**
- **Design Goal:** Create a 2-column layout where message content and metadata are separated
- **New Layout Structure:**
  ```
  ┌──────────────────────────────┬────┐
  │ Message content text here    │ ▼  │ ← Row 1: Chevron (on hover)
  │ Can span multiple lines      │────│
  │                              │Time│ ← Row 2: Date + status
  │                              │ ✓✓ │
  └──────────────────────────────┴────┘
  ```
- **Implementation:**
  1. **`.message-content-block`** - Flex container with 2 columns (`flex-direction: row`)
  2. **`.message-content-column`** - Left column (flex: 1) contains message text, replies, forwarded indicator
  3. **`.message-metadata-column`** - Right column with 2 rows:
     - **Row 1:** Chevron button (visible on hover) - opens more menu
     - **Row 2:** Time and status (always visible)
  4. **`.chevron-more-btn`** - Small circular button with chevron-down icon
  5. **`.chevron-more-menu`** - Menu positioned relative to chevron button (opens below/above)
- **Behavior:**
  - Hover over message wrapper → Chevron appears in metadata column
  - Click chevron → More menu opens from that point (Reply, Copy, Forward, Edit, Delete, Pin)
  - Menu intelligently positions above/below based on available space
  - React button remains outside bubble (unchanged from previous design)
- **Files modified:**
  - `MessageBubble.razor`:
    - Restructured bubble content into `.message-content-column` + `.message-metadata-column`
    - Added `showHoverActions` state and `HandleMouseEnter`/`HandleMouseLeave` methods
    - Added mouse handlers to message wrapper (`@onmouseover`/`@onmouseout`)
    - Chevron button inside metadata column (not outside bubble)
    - Renamed more menu class to `.chevron-more-menu` for clarity
    - Updated `CloseReactionPicker` and `CloseMoreMenu` to hide hover actions
  - `messages.css`:
    - Added `.message-content-block` (flex row, 2 columns)
    - Added `.message-content-column` (left column, flex: 1)
    - Added `.message-metadata-column` (right column, flex column, 2 rows, gap: 6px for spacing)
    - Added `.chevron-more-btn` styles (22px circular button, stronger background, box-shadow for visibility)
    - Added `.chevron-more-menu` styles (positioned relative to chevron)
    - Updated `.message-meta-inline` with more margin-top and padding-top for lower positioning
    - Kept `.message-actions` for React button outside bubble
- **Fixes applied:**
  - **Issue 1:** Chevron not visible on hover
    - **Fix:** Added mouse handlers to message wrapper, increased button size to 22px, stronger background colors, added box-shadow, used `!important` on visible class
  - **Issue 2:** Time and status needed to be lower
    - **Fix:** Increased gap in metadata column from 2px to 6px, added margin-top: 4px and padding-top: 2px to message-meta-inline
- **User Experience:**
  - Clean table-like separation between content and metadata ✅
  - Chevron visible and clickable on hover ✅
  - More menu opens from chevron position (not message wrapper) ✅
  - Time and status positioned lower with better spacing ✅
  - React button stays outside bubble and works perfectly ✅
  - No layout shifting or jumping on hover ✅
  - Professional, WhatsApp-inspired design ✅
