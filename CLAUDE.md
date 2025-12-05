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
