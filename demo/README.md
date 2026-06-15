# Dryv Demo Projects

This folder contains four demo projects illustrating how to use **Dryv**, **DryvJS**, and **Dryvue** in different application architectures. All demos implement the same registration form with shared validation rules defined once in C#.

---

## Demo 1: ASP.NET MVC + Razor Views

**`Dryv.Demo.Razor`** — Traditional server-rendered application using ASP.NET Core MVC with Razor views.

- Uses `Dryv.AspNetCore` Tag Helper (`<dryv-client-rules>`) to render validation rules inline
- Client-side validation via **DryvJS** (loaded from CDN)
- Server-side validation via Dryv's MVC filter (automatic `ModelState` integration)

### Run

```bash
cd demo/Dryv.Demo.Razor
dotnet run
# Open http://localhost:5100
```

---

## Demo 2: REST API + Plain JavaScript SPA (DryvJS)

**`Dryv.Demo.Api.DryvJS`** — Decoupled SPA using plain JavaScript with a REST API backend.

- Backend exposes `/api/validation-rules` endpoint that returns translated JavaScript rules
- Frontend is a single `index.html` that uses **DryvJS** directly (no framework)
- Demonstrates the `DryvValidationSession` + `DryvObjectValidator` API

### Run

```bash
cd demo/Dryv.Demo.Api.DryvJS
dotnet run
# Open http://localhost:5110
```

---

## Demo 3: REST API + Vue SPA (Dryvue)

**`Dryv.Demo.Api.Dryvue`** — Decoupled Vue 3 SPA with a REST API backend.

- Backend exposes `/api/validation-rules` endpoint
- Frontend uses **Dryvue** with the `useDryv` composable for reactive validation
- Vue dev server proxies API requests to the .NET backend

### Run

```bash
# Terminal 1: Start the API
cd demo/Dryv.Demo.Api.Dryvue
dotnet run

# Terminal 2: Start the Vue dev server
cd demo/Dryv.Demo.Api.Dryvue/ClientApp
npm install
npm run dev
# Open http://localhost:5173
```

---

## Demo 4: HotChocolate GraphQL + Vue SPA (Dryvue)

**`Dryv.Demo.GraphQL.Dryvue`** — Vue 3 SPA backed by a HotChocolate GraphQL API.

- Uses `Dryv.HotChocolate` for automatic server-side validation via the `DryvValidationTypeInterceptor`
- Input types decorated with `[DryvValidation]` are validated before resolvers execute
- Validation errors are returned as GraphQL errors with code `DRYV_VALIDATION_ERROR`
- Client-side validation uses **Dryvue** (same as Demo 3)
- GraphQL Banana Cake Pop IDE available at `/graphql`

### Run

```bash
# Terminal 1: Start the GraphQL API
cd demo/Dryv.Demo.GraphQL.Dryvue
dotnet run

# Terminal 2: Start the Vue dev server
cd demo/Dryv.Demo.GraphQL.Dryvue/ClientApp
npm install
npm run dev
# Open http://localhost:5174
```

---

## Validation Rules (shared across all demos)

All demos define the same `RegistrationModel` rules in C#:

```csharp
public static readonly DryvRules Rules = DryvRules
    .For<RegistrationModel>()
    .Rule(m => m.FirstName,
        m => string.IsNullOrWhiteSpace(m.FirstName)
            ? "Please enter your first name." : null)
    .Rule(m => m.LastName,
        m => string.IsNullOrWhiteSpace(m.LastName)
            ? "Please enter your last name." : null)
    .Rule(m => m.Email,
        m => string.IsNullOrWhiteSpace(m.Email)
            ? "Please enter your email address." : null)
    .Rule(m => m.Email,
        m => !m.Email.Contains("@")
            ? "Please enter a valid email address." : null)
    .Rule(m => m.Password,
        m => string.IsNullOrWhiteSpace(m.Password)
            ? "Please enter a password." : null)
    .Rule(m => m.Password,
        m => m.Password.Trim().Length < 8
            ? "Password must be at least 8 characters." : null)
    .Rule(m => m.Password, m => m.ConfirmPassword,
        m => m.Password != m.ConfirmPassword
            ? DryvValidationResult.Error("Passwords do not match.")
            : DryvValidationResult.Success);
```

These rules are:
- Executed on the **server** during form submission (for security)
- Translated to **JavaScript** and served to the client (for instant UX)
- Written **once** — no duplication between frontend and backend
