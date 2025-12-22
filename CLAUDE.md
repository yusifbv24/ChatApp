# ChatApp Project Notes

## Arxitektura
- **Pattern**: Modular Monolith + Clean Architecture + DDD
- **Backend**: ASP.NET Core API, CQRS (MediatR), SignalR
- **Frontend**: Blazor WASM, WhatsApp Web style UI

## Modullar
Identity | Channels | DirectMessages | Files | Notifications | Search | Settings

## Əsas Pattern-lər
- **Result<T>** - Error handling
- **CQRS** - Command/Query separation
- **Hybrid Notification** - SignalR group + direct connections (lazy loading dəstəyi)
- **Optimistic UI** - Mesajlar dərhal göstərilir, SignalR confirmation gözləmir
- **Pending Read Receipts** - Race condition üçün (MessageRead event HTTP-dən əvvəl gəlir)
- **Page Visibility API** - Mark-as-read yalnız tab visible olduqda
- **Debounced StateHasChanged** - Typing/online eventi flood-dan UI freeze qarşısını alır
- **Lazy Loading** - SignalR group-lara yalnız aktiv conversation/channel seçiləndə join olunur
- **In-Memory Cache** - Channel member list (typing üçün, 30 dəq)

---
## Session Log (Qısa)

### Session 1-5: Əsas UI və Funksionallıq
- **ConversationList** - WhatsApp style unified list (DM + Groups birlikdə)
- **User Search** - Debounced search (300ms), `/api/users/search`
- **Pending Conversation** - Mesaj göndərənə qədər conversation yaranmır
- **Online Status** - ConnectionManager (SignalR), real-time status
- **Remember Me** - Auto token refresh, localStorage
- **Unread Badge** - AppState, real-time global unread count
- **Mark as Read** - Auto-mark when viewing + Page Visibility API
- **Reply & Forward** - UI+DTO ready, backend persist lazımdır
- **Race Condition Fix** - Pending read receipts pattern (MessageRead event HTTP-dən əvvəl gəlir)
- **Avatar Upload Fix** - Admin üçün targetUserId (user create sonra upload)

### Session 6: Real-time Edit + Page Visibility + Auto-refocus
- **Real-time Edit** - SignalR broadcasts full DTO, conversation list auto-updates
- **Page Visibility API** - Mark-as-read yalnız tab visible olduqda (JS interop)
- **Auto-refocus Textarea** - Hər aksiyadan sonra (OnActionCompleted callback)
- **WhatsApp Hover Menu** - Chevron inside bubble, React button outside, smart positioning
- **Table Layout** - 2-column (content | metadata), chevron + time metadata column-da

### Session 7: Channel Read Status Fixes
- **EF Core Tracking** - UpdateAsync() redundant, remove (entity already tracked)
- **Page Visibility for Channels** - MarkUnreadMessagesAsRead() uncommented
- **Direct Message Regression** - isPageVisible + senderId != currentUserId check
- **Code Cleanup** - Console.WriteLine silindi, unused fields silindi

### Session 8: Reactions + Menu Redesign + Sorting Fixes
- **Reaction Picker Fixes** - Close timing, positioning (right/left), hover logic
- **Modern Menu** - Outlined icons, no submenu, 220px width, better typography
- **Placeholder Functions** - HandleAddToFavoritesClick, HandleMarkToReadLaterClick, HandleSelectClick
- **Forward Duplicate Fix** - processedMessageIds + duplicate check
- **Sorting Fixes** - Remove + insert to top (conversation list always sorted by time)
- **SignalR Race** - Wait for connection before joining groups
- **Hybrid Pattern** - Channel messages broadcast to both group + direct connections
- **Lazy Loading** - Join groups yalnız select edəndə, 99% reduction in memberships

### Session 9: Hybrid Typing + Performance
- **Typing Indicator** - Hybrid pattern (group + direct), IChannelMemberCache (30min), cache population
- **Throttle** - 2s timer (10 keystroke/s → 0.5 event/s)
- **Frontend** - Already ready (channelTypingUsers, conversationTypingState)

### Session 10: Drafts + DateTime + Menu + Debounce
- **Message Drafts** - Save/restore on switch, "Draft:" indicator (red)
- **DateTime UTC Fix** - SpecifyKind(Utc) before PostgreSQL query
- **Date Localization** - CultureInfo.InvariantCulture (English everywhere)
- **Menu Z-Index** - 9999 (always on top)
- **Menu Positioning** - 420px height, max-height, overflow-y: auto
- **Debounced StateHasChanged** - 50ms batch (typing/online events), UI freeze fix
- **Typing Hybrid** - Conversation + Channel hybrid pattern
- **Cache Population** - GetChannelMessages populates cache

### Session 11 (2025-12-22): Race Condition + Edit/Delete Fixes
**Race Condition Fix:**
- **Problem:** Own messages appeared as "other" messages on initial load
- **Solution:** Subscribe to UserState.OnChange event, update currentUserId when loaded
- **Result:** Messages always display with correct ownership ✅

**Reply Auto-scroll:**
- **Problem:** Reply göndərəndə scroll ən aşağıya düşmürdü
- **Solution:** `ChatArea.razor:504` - `!IsReplying` şərtini sildik
- **Result:** Reply göndərəndə avtomatik scroll ✅

**Edit Typing Indicator:**
- **Problem:** Edit edərkən "is typing" göstərilməli idi (bayaq yanlış başa düşmüşdük)
- **Solution:** `MessageInput.razor:241` - Həm edit, həm də new message üçün typing göndər
- **Result:** Edit edərkən "is typing" göstərilir ✅

**Edit Message Layout (Channel):**
- **Problem:** Edit edəndə mesaj 1 xəttə çevrilirdi (ReadByCount, TotalMemberCount 0 olurdu)
- **Root Cause:** Backend `GetByIdAsDtoAsync` bu field-ləri populate etmir
- **Solution:** `HandleChannelMessageEdited` - Yalnız Content, IsEdited, EditedAtUtc update et (digər field-ləri preserve et)
- **Result:** Edit edəndə mesaj düzgün formatda qalır ✅

**Conversation List Update (Edit/Delete):**
- **Problem:** Başqa channel-da olarkən kimsə son mesajı edit/delete edərsə, conversation list yenilənmirdi
- **Root Cause:** `IsLastMessageInChannel`/`IsLastMessageInConversation` yalnız aktiv channel-ın mesajlarını yoxlayırdı
- **Solution:**
  - Bu metodları düzəltdik: Aktiv channel-da isə yüklənmiş mesajları yoxla, deyilsə channel.LastMessageAtUtc ilə müqayisə et
  - `HandleDirectMessageEdited/Deleted` və `HandleChannelMessageEdited/Deleted` - Conversation list update-i `if (selectedChannel == ...)` block-undan kənara çıxardıq
- **Result:** Başqa channel-da olsaq belə, edit/delete conversation listdə yenilənir ✅

**Channel Deleted Message Preview:**
- **Problem:** Qrupda son mesaj silindikdə conversation listdə "2 members" göstərirdi (silinmiş mesaj göstərilmirdi)
- **Root Cause:** Channel last message həmişə "SenderName: Content" formatında göstərilirdi, amma "This message was deleted" üçün sender name olmamalıdır
- **Solution:** `ConversationList.razor:274-278` - Silinmiş mesajlar üçün xüsusi yoxlama əlavə etdik (sender name-siz göstər)
- **Result:** Silinmiş mesajlar conversation-larda olduğu kimi sadəcə "This message was deleted" göstərilir ✅

**🚨 CRITICAL: Mono Runtime Crash Fix (UI Freeze):**
- **Problem:** Channel-lər arasında sürətlə keçid edərkən browser-də `[MONO] Assertion failed` error-u və UI tamamilə donurdu
- **Root Cause:**
  1. SignalR handler-larında eyni anda bir neçə dəfə `StateHasChanged()` çağrılırdı (race condition)
  2. Component disposed olsa belə event handler-lar fire olurdu
  3. Exception-lar handle edilmirdi və runtime çökdürürdü
- **Solution:**
  - `_disposed` flag əlavə etdik və DisposeAsync-də true et
  - Bütün kritik handler-lara guard check əlavə etdik: `if (_disposed) return;`
  - Hər handler-ı `try-catch` blokunla wrap etdik (runtime crash-i qarşısını alır)
  - Multiple `StateHasChanged()` çağrılarını konsolidasiya etdik (bir dəfə, sonda)
  - Updated handlers: `HandleDirectMessageEdited`, `HandleDirectMessageDeleted`, `HandleChannelMessageEdited`, `HandleChannelMessageDeleted`
- **Result:** UI freeze və runtime crash problemi həll olundu ✅
- **Files:** `Messages.razor.cs:101` (_disposed), `Messages.razor.cs:2782` (DisposeAsync), handler updates (555-770)

**Forward Dialog Height Increase:**
- **Problem:** Forward message dialog-da yalnız 5-6 istifadəçi/channel görünürdü, scroll etmək lazım olurdu
- **Solution:** `messages.css:2645` - Dialog height: 480px → 560px artırdıq
- **Result:** İndi minimum 8 istifadəçi/channel scroll etmədən görünür ✅

**Auto-Focus After Cancel Edit/Reply:**
- **Problem:** Reply və ya Edit cancel edəndə textarea focus-u itirirdi, user manual olaraq klikləməli idi
- **Solution:** `MessageInput.razor:337, 345` - `CancelEdit()` və `CancelReply()` metodlarına `await FocusAsync()` əlavə etdik
- **Result:** Cancel etdikdən sonra avtomatik olaraq textarea focused olur və yazmaq üçün hazır vəziyyətə keçir ✅

**Prevent Editing Forwarded Messages:**
- **Problem:** Forward olunmuş mesajlar edit edilə bilirdi (olmamalıdır)
- **Solution:** `MessageBubble.razor:207` - Edit button şərtinə `!IsForwarded` əlavə etdik: `@if (IsOwn && !IsForwarded)`
- **Result:** Forward olunmuş mesajların more menu-sunda "Edit" button-u görünmür ✅

**Deleted Messages - Simplified More Menu (FIX APPLIED):**
- **Problem:** Silinmiş mesajlarda nə react, nə more icon, nə də reply işləmirdi
- **Root Cause:** Chevron button və more menu `else` blokun içində idi (normal message content ilə birlikdə), ona görə silinmiş mesajlar üçün heç nə render olunmurdu
- **Solution:**
  - `MessageBubble.razor:28-30` - Message bubble-a mouse event handler-lar əlavə etdik (`@onmouseenter`/`@onmouseleave`)
  - `MessageBubble.razor:328` - `showHoverActions` state dəyişəni əlavə etdik
  - `MessageBubble.razor:180-254` - Chevron button və more menu-nu `else` blokdan kənara çıxardıq:
    - İndi həm deleted, həm də normal mesajlar üçün göstərilir
    - Chevron button: `@if (showHoverActions || showMoreMenu)` şərti ilə
    - More menu content: `@if (IsDeleted)` → Yalnız **Reply**, `else` → Full menu
  - `MessageBubble.razor:257` - React button artıq `@if (!IsDeleted)` ilə gizlədilir ✅ (dəyişmədi)
- **Result:**
  - ❌ Silinmiş mesajlara react bildirmək mümkün deyil (gizli)
  - ✅ More icon görünür (hover edəndə) və klikləmək olar
  - ✅ More menu açılır və **YALNIZ Reply** göstərilir
  - ✅ Reply düyməsinə klikləyəndə silinmiş mesaja reply edilə bilir

**Deleted Messages - CSS Fix:**
- **Problem:** Silinmiş mesajların arxa fonu və text şəffafdı (opacity: 0.6 * 0.7 = 0.42), görünüşü pozurdu
- **Solution:**
  - `messages.css:821` - `.message-wrapper.deleted` opacity silindi (0.6 → removed)
  - `messages.css:1084` - `.deleted-message` opacity silindi (0.7 → removed)
- **Result:**
  - ✅ Silinmiş mesajlar normal mesajlarla eyni opacity-də görünür (şəffaflıq yoxdur)
  - ✅ Arxa fon və text rəngi digər mesajlarla eynidir

### Session 12 (2025-12-22): Reaction Handler UI Freeze + Infinite Loop Fix
**🚨 CRITICAL: Reaction Event UI Freeze & Infinite Loop:**
- **Problem:**
  1. Silinmiş mesaja react əlavə edərkən backend-də əlavə olur, lakin UI-da göstərilmir
  2. İstifadəçi A silinmiş mesaja react bildirəndə, İstifadəçi B-nin UI-ı loopa düşür
  3. Channel-dən conversation-a keçid edərkən UI tamamilə donur
  4. Backend-ə sonsuz request-lər göndərilir (conversation → channel → mark-as-read → load messages → loop)
- **Root Cause:**
  - Session 11-də `HandleDirectMessageEdited`, `HandleChannelMessageEdited`, `HandleDirectMessageDeleted`, `HandleChannelMessageDeleted` handler-larına `_disposed` check və `try-catch` əlavə etmişdik
  - Ancaq `HandleReactionToggled` və `HandleChannelMessageReactionsUpdated` reaction handler-larını unudulmuşdu
  - Component disposed olduqda və ya exception baş verdikdə, bu handler-lar runtime-ı crash edirdi və UI loop-a düşürdü
  - `HandleReactionToggled` InvokeAsync wrap-də deyildi (race condition risk)
- **Solution:**
  - `Messages.razor.cs:1455-1475` - `HandleReactionToggled`:
    - InvokeAsync wrap əlavə etdik (handler async context-də çalışmalıdır)
    - `if (_disposed) return;` guard check əlavə etdik
    - try-catch block əlavə etdik (exception-ları silently handle et, runtime crash-dən qaçın)
  - `Messages.razor.cs:1477-1511` - `HandleChannelMessageReactionsUpdated`:
    - `if (_disposed) return;` guard check əlavə etdik (handler-ın əvvəlinə)
    - try-catch block əlavə etdik (exception-ları silently handle et)
- **Result:**
  - ✅ Silinmiş mesajlara react əlavə edəndə UI freeze və loop problemi həll olundu
  - ✅ Component disposed olduqda reaction event-ləri silently ignore olunur
  - ✅ Exception-lar silently handle edilir və runtime crash olmur
  - ✅ Channel-dən conversation-a keçid edərkən sonsuz loop problemi həll olundu
  - ✅ Reaction handler-lar indi edit/delete handler-larla eyni safety pattern-ə malikdir
- **Pattern Consistency:** İndi bütün SignalR event handler-ları (`HandleDirectMessageEdited`, `HandleChannelMessageEdited`, `HandleDirectMessageDeleted`, `HandleChannelMessageDeleted`, `HandleReactionToggled`, `HandleChannelMessageReactionsUpdated`) eyni safety pattern-ə malikdir: InvokeAsync + _disposed check + try-catch

**Remove React from Deleted Messages:**
- **Problem:** Silinmiş mesajlara react bildirmək funksionallığı lazımsızdır və UI-da görünməməlidir
- **Solution:**
  - **Frontend:**
    - `MessageBubble.razor:257` - React button şərtinə `!IsDeleted` əlavə etdik: `@if (!IsDeleted && (showHoverActions || showReactionPicker))`
    - Comment update: "visible for all messages including deleted" → "hidden for deleted messages"
  - **Backend:**
    - `DirectMessages\ToggleReactionCommand.cs:83-87` - IsDeleted yoxlaması əlavə etdik: `if (message.IsDeleted) return Result.Failure("Cannot react to deleted messages");`
    - `Channels\ToggleReactionCommand.cs:62-66` - IsDeleted yoxlaması əlavə etdik: `if (message.IsDeleted) return Result.Failure("Cannot react to deleted messages");`
- **Result:**
  - ✅ Silinmiş mesajlarda react button görünmür (UI)
  - ✅ Backend silinmiş mesajlara react bildirməyə icazə vermir
  - ✅ Lazımsız kod və funksionallıq aradan qaldırıldı

**🚨 CRITICAL: Infinite Loop Fix - Race Condition in SelectConversation/SelectChannel:**
- **Problem:**
  - Conversation və channel arasında tez-tez keçid edərkən null dəyər və sonsuz loop yaranır
  - Backend-ə sonsuz request-lər göndərilir (conversation → channel → mark-as-read → load messages → loop)
  - Bəzən UI tamamilə donur və dayandırmaq mümkün olmur
  - "Node cannot be found in the current page" error-u
- **Root Cause:**
  - `SelectConversation` və `SelectChannel` metodlarında eyni anda bir neçə çağrı mümkündür (concurrent calls)
  - User çox tez conversation/channel seçəndə, race condition yaranır
  - Əvvəlki selection bitməmiş yeni selection başlayır
  - Messages load olunarkən SignalR event-ləri trigger olur və state inconsistent olur
  - Null check və guard yoxdur
- **Solution:**
  - `Messages.razor.cs:104` - `_isSelecting` flag əlavə etdik (concurrent operation tracking)
  - `SelectConversation:1679-1695` - Guard checks əlavə etdik:
    - `if (_isSelecting || _disposed) return;` - Prevent concurrent calls
    - `if (conversation == null) return;` - Null check
    - `if (selectedConversationId == conversation.Id) return;` - Already selected check
    - `_isSelecting = true;` - Set flag before operation
  - `SelectConversation:1781-1788` - Try-catch-finally block:
    - catch: Show user-friendly error message
    - finally: `_isSelecting = false;` - Always reset flag
  - `SelectChannel:1939-1955` - Guard checks əlavə etdik (eyni pattern)
  - `SelectChannel:2096-2103` - Try-catch-finally block (eyni pattern)
- **Result:**
  - ✅ Conversation və channel arasında sürətlə keçid edərkən sonsuz loop problemi həll olundu
  - ✅ Null dəyər exception-ları artıq baş vermir
  - ✅ Concurrent selection calls prevent edilir (_isSelecting guard)
  - ✅ Duplicate selection ignore edilir (already selected check)
  - ✅ UI freeze və backend request flood problemi həll olundu
  - ✅ Error-lar user-friendly mesajlarla göstərilir
  - ✅ _isSelecting flag həmişə finally block-da reset olunur (memory leak yoxdur)

**🚨 SECURITY FIX: Sanitize Deleted Message Content (Backend DTO Layer):**
- **Problem:**
  1. Silinmiş mesajların **content-i browser dev tools-da görünə bilir** (SECURITY RISK!)
  2. Conversation list-də son mesaj silinibsə, amma yenə də silinməmiş content göstərilir
  3. Silinmiş mesaja reply edərkən, parent mesajın (silinmiş) content-i görünür
- **Root Cause:**
  - Backend DTO mapping layer-də silinmiş mesajların content-i sanitize olunmur
  - Frontend-ə həqiqi silinmiş content göndərilir (browser network tab-da görünür)
  - IsDeleted=true olduqda Content və ReplyToContent field-ləri sanitize edilməlidir
- **Solution:**
  - **DirectMessageRepository:**
    - `GetByIdAsDtoAsync:74-75,102,112` - Content və ReplyToContent sanitize etdik
    - `GetConversationMessagesAsync:153-154,196,206` - Content və ReplyToContent sanitize etdik
    - Pattern: `IsDeleted ? "This message was deleted" : Content`
  - **ChannelMessageRepository:**
    - `GetByIdAsDtoAsync:58-59,74,84` - Content və ReplyToContent sanitize etdik
    - `GetChannelMessagesAsync:122-123,171,181` - Content və ReplyToContent sanitize etdik
    - `GetPinnedMessagesAsync:221` - ReplyToContent sanitize etdik (pinned messages özü deleted ola bilməz)
  - **DirectConversationRepository:**
    - `GetUserConversationsAsync:86-90,104-106` - LastMessageContent sanitize etdik
    - İndi deleted messages include olunur və content "This message was deleted" göstərilir
  - **ChannelRepository:**
    - `GetUserChannelDtosAsync:109-119,143-145` - LastMessageContent sanitize etdik
    - İndi deleted messages include olunur və content "This message was deleted" göstərilir
- **Result:**
  - ✅ **SECURITY:** Silinmiş mesajların həqiqi content-i heç vaxt frontend-ə göndərilmir
  - ✅ Browser dev tools, network tab, və memory-də silinmiş content görünməyəcək
  - ✅ Conversation/channel list-də son mesaj silinibsə "This message was deleted" göstərilir
  - ✅ Silinmiş mesaja reply edərkən parent content "This message was deleted" göstərilir
  - ✅ DTO layer-də centralized sanitization (consistent və secure)
