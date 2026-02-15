# ChatApp Project Notes

## Arxitektura
- **Pattern**: Modular Monolith + Clean Architecture + DDD
- **Backend**: ASP.NET Core API, CQRS (MediatR), SignalR
- **Frontend**: React (Vite + JavaScript) — migrated from Blazor WASM due to UI freezing
- **UI Style**: WhatsApp Web style

## React Migration Context
- User is learning React from scratch. Teach step by step, one concept at a time.
- User types all code manually to learn. Explain every line before writing.
- Compare React concepts to .NET equivalents when helpful.
- User works on 2 PCs. Always keep `tasks/todo.md` updated so progress syncs via GitHub.
- Progress tracker: `tasks/todo.md` — read this first when resuming.
- Lessons file: `tasks/lessons.md` — read this at session start.
- When user says "Continue React migration", read `tasks/todo.md` and continue from next unchecked step.
- Backend is COMPLETE. Do NOT modify backend code.
- React project location: `C:\Users\Joseph\Desktop\ChatApp\chatapp-frontend\`

## Modullar
Identity | Channels | DirectMessages | Files | Notifications | Search | Settings

- İstifadə olunan funksiyanın optimizasiyaya ehtiyacı varsa, optimizasiya et.
- Yeni bir method əlavə edərkən, əgər köhnə və ya ona oxşar method varsa optimallaşdırmağa çalış.
- Lazımsız kodları silməyi unutma
- Həmişə kodları optimizasiya etmək, code refactor etmək və performansı yüksəltmək lazımdır.
- Yeni bir kod yazmamışdan öncə yazılan arxitekturanı, yanaşmanı təhlil et , daha sonra kod yaz.

## Workflow Orchestration

### 1. Plan Mode Default
- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately – don't keep pushing
- Use plan mode for verification steps, not just building
- Write detailed specs upfront to reduce ambiguity

### 2. Subagent Strategy
- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution

### 3. Self-Improvement Loop
- After ANY correction from the user: update `tasks/lessons.md` with the pattern
- Write rules for yourself that prevent the same mistake
- Ruthlessly iterate on these lessons until mistake rate drops
- Review lessons at session start for relevant project

### 4. Verification Before Done
- Never mark a task complete without proving it works
- Diff behavior between main and your changes when relevant
- Ask yourself: "Would a staff engineer approve this?"
- Run tests, check logs, demonstrate correctness

### 5. Demand Elegance (Balanced)
- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution"
- Skip this for simple, obvious fixes – don't over-engineer
- Challenge your own work before presenting it

### 6. Autonomous Bug Fixing
- When given a bug report: just fix it. Don't ask for hand-holding
- Point at logs, errors, failing tests – then resolve them
- Zero context switching required from the user
- Go fix failing CI tests without being told how

## Task Management

1. **Plan First**: Write plan to `tasks/todo.md` with checkable items
2. **Verify Plan**: Check in before starting implementation
3. **Track Progress**: Mark items complete as you go
4. **Explain Changes**: High-level summary at each step
5. **Document Results**: Add review section to `tasks/todo.md`
6. **Capture Lessons**: Update `tasks/lessons.md` after corrections
7. **Dont build**: Əgər sadəcə frontenddə iş görürsənsə, backend proyektini build etmə və ya əksinə.
## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.