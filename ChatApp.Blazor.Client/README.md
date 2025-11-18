# 🎨 ChatApp - Blazor WebAssembly Client

A modern, full-featured chat application built with Blazor WebAssembly, designed to replace tools like Bitrix24 for office communication.

## ✨ Features

### 🔐 Identity Module (Complete)
- **Authentication**
  - JWT-based login/logout
  - Automatic token refresh
  - Remember me functionality
  - Secure token storage

- **User Management**
  - User profile management
  - Password change
  - Avatar upload
  - Admin user CRUD operations
  - User activation/deactivation

- **Role-Based Access Control**
  - Role management (Create, Read, Update, Delete)
  - Permission management
  - Role assignment to users
  - Permission assignment to roles
  - Admin panel with full management capabilities

### 🎨 Modern UI/UX
- **Material Design 3** with MudBlazor
- **40+ CSS Animations** (fade, slide, bounce, pulse, glow, etc.)
- **Responsive Design** - Mobile, tablet, and desktop
- **Dark Mode Support** - System preference detection
- **Smooth Transitions** - Between all pages and components
- **Loading States** - Beautiful loading indicators
- **Toast Notifications** - User-friendly success/error messages

### ⚡ Real-Time Communication
- **SignalR Integration** - Real-time updates
- **Online/Offline Status** - User presence tracking
- **Typing Indicators** - See when users are typing
- **Live Notifications** - Instant message notifications

### 🏗️ Architecture
- **Clean Architecture** - Feature-based organization
- **SOLID Principles** - Interface segregation, DI
- **Result Pattern** - Functional error handling
- **State Management** - Reactive state updates
- **Service Layer** - Separated business logic

## 📁 Project Structure

```
ChatApp.Blazor.Client/
├── Features/                    # Feature modules
│   ├── Auth/                    # Authentication & user management
│   │   ├── Pages/              # Login, Profile
│   │   └── Services/           # AuthService, UserService
│   └── Admin/                   # Administration
│       ├── Pages/              # UserManagement, RoleManagement, PermissionManagement
│       └── Services/           # RoleService, PermissionService
│
├── Infrastructure/              # Core infrastructure
│   ├── Auth/                   # JWT authentication
│   ├── Http/                   # API client & interceptors
│   ├── SignalR/                # Real-time communication
│   └── Storage/                # Browser storage
│
├── Models/                      # DTOs and ViewModels
│   ├── Auth/                   # Authentication models
│   └── Common/                 # Shared models
│
├── Pages/                       # Public pages
│   └── Index.razor             # Dashboard
│
├── Shared/                      # Shared components
│   ├── MainLayout.razor        # Main application layout
│   └── NavMenu.razor           # Navigation menu
│
├── State/                       # State management
│   ├── AppState.cs             # Global app state
│   └── UserState.cs            # Current user state
│
└── wwwroot/                     # Static assets
    ├── css/                    # Stylesheets
    ├── js/                     # JavaScript interop
    └── appsettings.json        # Configuration
```

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or Visual Studio Code
- ChatApp backend API running

### Configuration

1. Update `wwwroot/appsettings.json`:
```json
{
  "ApiBaseAddress": "https://localhost:7000"
}
```

2. Ensure the backend API is running at the specified address.

### Run the Application

```bash
# Restore packages
dotnet restore

# Run the application
dotnet run

# Or build and run
dotnet build
dotnet run
```

The application will start at `https://localhost:5001` (or another port).

### Default Login Credentials

Check with your backend API for default admin credentials.

## 📦 NuGet Packages

```xml
<PackageReference Include="MudBlazor" Version="7.8.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
<PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
<PackageReference Include="Blazored.SessionStorage" Version="2.4.0" />
<PackageReference Include="FluentValidation" Version="11.9.0" />
```

## 🎯 Identity Module - API Coverage

### ✅ Authentication (3 endpoints)
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh token
- `POST /api/auth/logout` - User logout

### ✅ User Management (13 endpoints)
- `GET /api/users/me` - Get current user
- `PUT /api/users/me` - Update current user
- `POST /api/users/me/change-password` - Change password
- `GET /api/users` - Get all users (paginated)
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `PUT /api/users/{id}/activate` - Activate user
- `PUT /api/users/{id}/deactivate` - Deactivate user
- `DELETE /api/users/{id}` - Delete user
- `POST /api/users/change-password/{id}` - Admin change password
- `POST /api/users/{userId}/roles/{roleId}` - Assign role
- `DELETE /api/users/{userId}/roles/{roleId}` - Remove role

### ✅ Role Management (4 endpoints)
- `GET /api/roles` - Get all roles
- `POST /api/roles` - Create role
- `PUT /api/roles/{id}` - Update role
- `DELETE /api/roles/{id}` - Delete role

### ✅ Permission Management (3 endpoints)
- `GET /api/permissions` - Get all permissions
- `POST /api/permissions/roles/{roleId}/permissions/{permissionId}` - Assign permission
- `DELETE /api/permissions/roles/{roleId}/permissions/{permissionId}` - Remove permission

**Total: 23 endpoints - 100% implemented**

## 🎨 Pages & Components

### Public Pages
- **Login** (`/login`) - User authentication with animations
- **Dashboard** (`/`) - Welcome page with quick actions

### User Pages
- **Profile** (`/profile`) - User profile management
  - Edit display name, email, avatar
  - Change password
  - View roles and permissions

### Admin Pages (Requires Admin Permission)
- **User Management** (`/admin/users`)
  - View all users in table
  - Create, edit, delete users
  - Activate/deactivate users
  - Manage user roles
  - Reset user passwords

- **Role Management** (`/admin/roles`)
  - View all roles
  - Create, edit, delete roles
  - View role details and permissions
  - Assign/remove permissions from roles

- **Permission Management** (`/admin/permissions`)
  - View all permissions grouped by module
  - View permission statistics
  - See which roles have specific permissions

## 🎭 Animations

The application includes 40+ CSS animations:

```css
.animate-fade-in              /* Fade in effect */
.animate-fade-in-up           /* Fade in with upward motion */
.animate-slide-in-left        /* Slide from left */
.animate-slide-in-right       /* Slide from right */
.animate-zoom-in              /* Zoom in effect */
.animate-bounce               /* Bouncing animation */
.animate-pulse                /* Pulsing effect */
.animate-glow                 /* Glowing effect */
.animate-shake                /* Shake effect (errors) */
.hover-lift                   /* Lift on hover */
.hover-scale                  /* Scale on hover */
.hover-glow                   /* Glow on hover */
```

Use delay classes for staggered animations:
```css
.delay-100, .delay-200, .delay-300, .delay-400, .delay-500
```

## 🔐 Security

- **JWT Authentication** - Secure token-based auth
- **Automatic Token Refresh** - Seamless experience
- **Permission-Based Authorization** - Fine-grained access control
- **Secure Storage** - Browser LocalStorage with encryption
- **HTTP Interceptor** - Automatic token injection
- **CORS Protection** - Backend validation

## 🎯 State Management

### AppState
- Dark mode preference
- Online users list
- Unread notification count

### UserState
- Current user information
- User roles and permissions
- Authentication status
- Permission checking

## 📊 Performance

- **Lazy Loading** - Components load on demand
- **Optimized Rendering** - Efficient state updates
- **Caching** - Reduce API calls
- **Virtualization** - Handle large lists efficiently
- **Debouncing** - Optimize search and input

## 🛠️ Development

### Code Organization
- **Feature Folders** - Each feature is self-contained
- **Services** - Business logic separated from UI
- **Models** - DTOs match backend exactly
- **Infrastructure** - Reusable cross-cutting concerns

### Naming Conventions
- **Pages** - PascalCase (e.g., `UserManagement.razor`)
- **Components** - PascalCase (e.g., `UserAvatar.razor`)
- **Services** - Interface + Implementation (e.g., `IAuthService`, `AuthService`)
- **Models** - Request/Response/Dto suffixes

### Best Practices
- Use `@inject` for dependency injection
- Use `[Authorize]` attribute for protected pages
- Use `UserState.HasPermission()` for permission checks
- Use `Result<T>` pattern for error handling
- Use MudBlazor components for consistent UI
- Add animations with CSS classes

## 🧪 Testing

(To be implemented)
- Unit tests for services
- Integration tests for API clients
- UI tests for components

## 🚀 Deployment

### Build for Production

```bash
dotnet publish -c Release -o ./publish
```

### Deploy to IIS
1. Install .NET 8 Hosting Bundle
2. Create application pool (.NET CLR Version: No Managed Code)
3. Copy published files to wwwroot
4. Configure web.config

### Deploy to Azure
1. Create Azure Static Web App
2. Configure GitHub Actions
3. Deploy from repository

## 📝 TODO

### Future Enhancements
- [ ] Register page
- [ ] Forgot password
- [ ] Email verification
- [ ] Two-factor authentication
- [ ] Channels module
- [ ] Direct messages module
- [ ] File sharing module
- [ ] Search functionality
- [ ] Settings module
- [ ] Notifications module

## 📄 License

This project is part of the ChatApp solution.

## 👥 Authors

Built with ❤️ using Blazor WebAssembly and MudBlazor.

## 📞 Support

For issues and questions, please refer to the main ChatApp repository.

---

**Status**: Identity Module Complete ✅
**Version**: 1.0.0
**Last Updated**: 2025
