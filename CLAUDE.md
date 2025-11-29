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
