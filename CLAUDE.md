# CLAUDE.md

## Project
RealEstateInstallmentsManager: a desktop app for managing real-estate rentals and installment sales. Built with Avalonia UI 11.3 on .NET 10. The UI is in Arabic.

## Communication
- The developer is learning. Explain changes in simple Arabic.
- Before any large change, present a plan and wait for approval.

## Build and run
- Build: `dotnet build RealEstateInstallmentsManager.sln` (the root has both a .sln and a .csproj; naming the file is not strictly required — `dotnet build`/`dotnet run` with no arguments also resolves fine since there's only one project — but naming it keeps intent explicit).
- Run: `dotnet run --project RealEstateInstallmentsManager.csproj`
- Build after every change and fix all errors before finishing a task.
- There are no automated tests yet.
- Development machine is macOS. The app also targets Windows (`app.ico`, `app.manifest`); `Assets/app.icns` is the macOS/Avalonia icon.

## Structure
The .csproj is at the repo root and automatically includes every folder below it.
- `Main/`: startup and shell (`Program`, `App`, `LoginView`, `MainMenuView`, `MainWindow`, `AppVersionService`, `app.manifest`)
- `Assets/`: app icons (`app.ico` for Windows, `app.icns` for macOS)
- `Installment/`: installment sales module (Models, Services, Views) covering contracts, customers, owners, products, receipts, expenses, dashboard, and PDF export. Local database access is in `DbServiceInstallment`.
- `RealEstate/`: real-estate rental module (Models, Services, Views) covering contracts, owners, tenants, units, receipts, expenses, dashboard, and PDF export (`PdfServiceRealEstate`). Local database access is in `DbServiceRealEstate`.
- `Cloud/`: Supabase integration. Auth (`AuthService`, `AppSession`), roles (`RoleService`, `UserRoleRow`), client (`SupabaseService`), per-entity cloud services, and sync (`InstallmentSyncService`, `RealEstateSyncService`).

## Architecture
- Data is stored locally in SQLite and synced with Supabase.
- **SQLite connection**: `DbServiceRealEstate` and `DbServiceInstallment` are separate classes, but by default they both resolve to the *same physical file* — `realEstateInstallments.db` under `%LocalApplicationData%/RealEstateInstallmentsManager/` — so the local database is one SQLite file holding both modules' tables (12 tables total, 6 per module), not two separate files. The connection string is built with `SqliteConnectionStringBuilder { DataSource = <path>, ForeignKeys = true }`. `Initialize()` runs `CREATE TABLE IF NOT EXISTS` for that module's own tables; it's called once at startup for `DbServiceRealEstate` (`Main/Program.cs`) and again from the constructor/load of every individual view in both modules, which is safe only because table creation is idempotent. There's no shared/pooled connection object — every read/write method in the entity services opens its own short-lived `new SqliteConnection(_db.ConnectionString)`, uses it, and disposes it.
- Views use code-behind (`.axaml.cs`). Do not convert to MVVM unless asked.
- Naming: every type ends with its module name (for example `CustomerInstallment`, `ContractRealEstate`). Follow this for new files.
- Each entity usually has a list view (`XView`) and an add/edit window (`XWindowView`) — the dashboard views are the exception, with no window counterpart.
- Keep the right-to-left layout and Arabic text intact.

## Rules
- Never write secrets (Supabase keys, passwords, tokens) in code, README, or this file.
- Do not add, remove, or update NuGet packages without permission.
- Do not change the Supabase database schema or delete the local SQLite database.
- Do not change business logic (installment calculations, amounts, dates) unless the task asks for it.
- Work on a feature branch. Never commit directly to `master`. Never force push.
- Commit after each finished task with a clear English message. Push only when asked.

## Known issues
- NuGet warnings for vulnerable versions of: Microsoft.IdentityModel.JsonWebTokens, System.IdentityModel.Tokens.Jwt, SQLitePCLRaw.lib.e_sqlite3, Tmds.DBus.Protocol. These are tracked as a separate task.

## Planning files
- `ROADMAP.md`: project plan (to be created)
- `PROGRESS.md`: progress log (to be created)
