# Lessons Learned - ChatApp

## React Migration
- User is learning React from zero. Explain EVERY concept before using it.
- User types code manually to learn. Don't auto-generate large files.
- Teach one concept at a time. Don't rush.
- Always compare React concepts to .NET equivalents when possible.
- User works on 2 different PCs. Always update todo.md so progress syncs via GitHub.

## Project Rules
- Backend is complete and working. Don't modify backend code.
- Frontend was Blazor WASM, migrating to React due to UI freezing.
- WhatsApp Web style UI is the target design.

## Critical: Backend Configuration
- **Backend URL: `http://localhost:7000`** — NEVER assume a port. Always check `launchSettings.json` first.
- **CORS allowed origins:** `http://localhost:5300`, `http://localhost:5301`, `http://localhost:5173`
- **React Vite runs on default port 5173** (user added it to backend CORS).
- RULE: Before writing ANY URL/port in code, ALWAYS verify from `launchSettings.json` or config files. NEVER guess.

## Mistakes Log
| Date | Mistake | Fix | Rule |
|------|---------|-----|------|
| 2025-02-15 | Wrote `localhost:5000` for backend API | Correct port is `7000` (from launchSettings.json) | ALWAYS check launchSettings.json before writing any URL |
