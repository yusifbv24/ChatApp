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
