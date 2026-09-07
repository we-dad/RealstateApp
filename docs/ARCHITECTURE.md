# Architecture

Technical structure of **المسار الذهبي** — an offline-first Avalonia desktop application on .NET 10, backed by local SQLite and synchronised to Supabase.

---

## Layout

```
RealEstate/          Lease management
  Models/            ContractRealEstate, UnitRealEstate, TenantRealEstate,
                     OwnerRealEstate, ReceiptRealEstate, ExpensesRealEstate
  Services/          DbServiceRealEstate (schema + connection),
                     one service per entity, PdfServiceRealEstate,
                     ReceiptServiceRealEstate
  Views/             Dashboard, contracts, units, tenants, owners, receipts,
                     expenses — each as a view plus a detail window

Installment/         Installment sales — same shape, with Product and
                     Customer in place of Unit and Tenant

Cloud/               Shared sync and access layer
  Models/            CloudModels, UserRoleRow
  Services/          SupabaseService, AuthService, AppSession, RoleService,
                     one Cloud*Service per remote table,
                     InstallmentSyncService, RealEstateSyncService
```

Both business modules are structurally parallel: models, per-entity services, a PDF service, and views. Neither references the other. `Cloud/` is the only shared layer, and it is written twice at the sync level — `InstallmentSyncService` and `RealEstateSyncService` — because their table graphs differ.

**Trade-off.** The parallel structure means the two modules can evolve independently without regression risk in the other, and a change to lease logic cannot break installments. The cost is real duplication: the receipt services, expense services and PDF services are structurally similar across modules, and a fix in one is not automatically a fix in the other.

## Storage

SQLite under the user's local application data folder, created on first run with `CREATE TABLE IF NOT EXISTS`, so schema setup and application startup are the same operation and no migration step is needed for a fresh install.

Every table carries three columns beyond its business fields:

```sql
CREATE TABLE IF NOT EXISTS OwnersRealEstate (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    CloudId        INTEGER NOT NULL DEFAULT 0,
    Name           TEXT    NOT NULL,
    IdentityNumber TEXT    NOT NULL,
    Phone          INTEGER NOT NULL,
    Address        TEXT    NOT NULL DEFAULT '',
    IsDirty        INTEGER NOT NULL DEFAULT 0,
    SyncAction     TEXT    NOT NULL DEFAULT ''
);
```

`Id` is the local key; `CloudId` is the remote key, zero until the row has been pushed. Keeping them separate is what allows a record to be created, referenced by children, and edited entirely offline before the cloud has ever seen it.

Foreign keys are declared between local ids, and each child model also carries its parent's `CloudId` — for example `ContractCloudId` on a receipt — so the sync layer can build a remote insert without a second lookup.

## Sync

`PushAllDirtyAsync` walks the dirty rows of each table in turn. Each row is dispatched on its recorded action:

| `SyncAction` | Behaviour |
|---|---|
| `insert` | Create remote row, store the returned id via `UpdateCloudId` |
| `update` | Update remote row by `CloudId`, then `MarkSynced` |
| `delete` | Delete remote row if it exists, then `DeleteLocalPermanent` |

Both `UpdateCloudId` and `MarkSynced` clear `IsDirty` and blank `SyncAction` in the same statement, so a row can never be left flagged after a successful push — which would cause it to be inserted again on the next run.

**Dependency guard.** A child row whose parent has no `CloudId` yet cannot be pushed, because the remote foreign key would be invalid:

```csharp
if (receipt.ContractCloudId <= 0)
    continue;
```

The row stays dirty and is retried on the next sync rather than being pushed with a dangling reference or dropped.

**Push order.** Tables are pushed children-first: receipts, expenses, contracts, products, customers, owners. Combined with the guard, this means a parent and its children created in the same offline session complete over two sync passes rather than one — the children are skipped on the first pass and picked up on the second. Reversing the order to parents-first would collapse this to a single pass.

**Local deletes.** A deletion marks the row `delete` rather than removing it, so the intent survives until the cloud has been told. Only after the remote delete succeeds is the local row removed permanently.

## Access control

Access is enforced in two places, and only one of them is authoritative.

`AppSession` holds the signed-in role and exposes derived permissions used to shape the interface:

```csharp
public static bool CanWriteOnline => Role == "admin" || Role == "editor";
public static bool CanReadOnline  => Role == "admin" || Role == "editor" || Role == "viewer";
```

This controls what the application offers. It is not security — anything running against the Supabase API bypasses the client entirely.

The enforcement that matters is Supabase **row-level security**: policies on the database itself decide which rows each authenticated user may read or write. Because the publishable key ships inside the executable and is extractable, RLS is the whole of the protection, not a supplement to it.

## Documents

`PdfServiceRealEstate` and `PdfServiceInstallment` compose contracts, receipts and financial statements with QuestPDF. Arabic layout is set per page and per composed block:

```csharp
page.ContentFromRightToLeft();
```

with an explicit font family, since the default font stack carries no Arabic glyph coverage. Dashboards export through the same services, so an on-screen report and its printed version come from one code path rather than two that can drift apart.

## Distribution

Velopack builds the Windows installer and drives in-place updates. The version is declared once in the project file:

```xml
<Version>1.0.31</Version>
<AssemblyVersion>1.0.31.0</AssemblyVersion>
```

and surfaced at runtime through `AppVersionService`, which reads it back from the executing assembly rather than from a second constant — so the number shown in the interface cannot disagree with the number that was built.

## Known limitations

| Area | Current state | Better approach |
|---|---|---|
| Push order | Children before parents | Parents first, so a session completes in one pass |
| Sync direction | Push-only from this client | Pull to reconcile edits made elsewhere |
| Conflict handling | Last write wins | Timestamps or version columns to detect divergence |
| Failed pushes | Skipped silently, retried next run | Surface a sync status so a stuck row is visible |
| Module duplication | Receipt, expense and PDF services written twice | Shared generic base for the structurally identical parts |
| Connection string | Composed per call | Pooled connection or a scoped unit of work |
