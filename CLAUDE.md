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

- İşləyən funksiyaya toxunma
- Yeni bir method əlavə edərkən, əgər köhnə və ya ona oxşar method varsa optimallaşdırmağa çalış.
- Lazımsız kodları silməyi unutma
- Həmişə kodları optimizasiya etmək, code refactor etmək və performansı yüksəltmək lazımdır.
- Yeni bir kod yazmamışdan öncə yazılan arxitekturanı, yanaşmanı təhlil et , daha sonra kod yaz.