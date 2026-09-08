# Database Architecture

**المسار الذهبي** runs on two databases at once: a local SQLite file that the application writes to directly, and a Supabase (PostgreSQL) mirror it synchronises to. This document describes both, and the mapping between them.

---

## Why two databases

The application must keep working with no network connection — a dropped link cannot stop a contract from being written or a receipt from being issued. So SQLite is the working database and the cloud is a replica, not the other way around.

| | Local — SQLite | Cloud — Supabase |
|---|---|---|
| Role | Working database, always available | Replica and central store |
| Location | `LocalApplicationData`, outside the install directory | Managed PostgreSQL |
| Written by | The application, always | The sync layer, when connected |
| Naming | `PascalCase` | `snake_case` |
| Tables | 12 | 13 |
| Access control | None — it's a local file | Row-level security policies |

Neither is authoritative for everything. Business data originates locally; identity and roles originate in the cloud.

## Entity graph

Both business modules have the same shape. An owner holds assets; assets are contracted to a counterparty; contracts generate receipts; assets accrue expenses.

```
Real estate                             Installments
───────────                             ────────────
Owner                                   Owner
  └─ Unit                                 └─ Product
       ├─ Contract ── Tenant                   └─ Contract ── Customer
       │    └─ Receipt                              └─ Receipt
       └─ Expense                              └─ Expense
```

**A contract points at both sides.** A lease references a unit *and* a tenant, so a tenant exists independently of any contract and can hold several over time. The relationship is contract-to-tenant, not unit-to-tenant — which is why a tenant's history survives a lease ending. The installment side works the same way with products and customers.

## Local schema

Twelve tables, six per module, all mirroring each other.

### Real estate

```sql
OwnersRealEstate     Id · Name · IdentityNumber · Phone · Address
UnitsRealEstate      Id · OwnerId → Owners · UnitName · City · District
                     UnitType · UnitState · UnitsCount · UnitNum
TenantsRealEstate    Id · Name · IdentityNumber · Phone · Address
ContractsRealEstate  Id · ContractNumber (UNIQUE) · Start/EndDate · RentAmount
                     ContractState · PayMethod · ApartmentType
                     RoomsNum · FloorNum · Opligation
                     UnitId → Units · TenantId → Tenants
ReceiptsRealEstate   Id · ReceiptNumber (UNIQUE) · ReceiptDate
                     ContractId → Contracts · PaymentMethod · Amount
ExpensesRealEstate   Id · ExpensesNumber (UNIQUE) · ExpensesDate
                     ExpensesService · Amount · Note · UnitId → Units
```

### Installments

```sql
OwnersInstallment    Id · Name · IdentityNumber · Phone · Address
ProductsInstallment  Id · OwnerId → Owners · ProductName · ProductType
                     ProductMainPrice
                     CarPlateNumber · CarVIN · CarModel · CarColor
                     MobileStorage · MobileColor
CustomersInstallment Id · Name · IdentityNumber · Phone · Address · Job
                     SponserName · SponserIdentityNumber · SponserPhone
                     SponserAddress · SponserJob
ContractsInstallment Id · ContractNumber (UNIQUE) · Start/EndDate
                     MainTotalAmount · CurrentTotalAmount · ContractPeriod
                     DownPayment · MonthlyInstallment
                     ManagementFee · InterestPercent (default 12.5)
                     ContractState
                     ProductId → Products · CustomerId → Customers
ReceiptsInstallment  Id · ReceiptNumber (UNIQUE) · ReceiptDate
                     ContractId → Contracts · PaymentMethod · Amount
                     CurrentTotalAmount
ExpensesInstallment  Id · ExpensesNumber (UNIQUE) · ExpensesDate
                     ExpensesService · Amount · Note · ProductId → Products
```

Every document table — contracts, receipts, expenses — carries a `UNIQUE` human-readable number (`Rc-1009`, `Ir-1106`, `Ic-1115`) separate from its primary key. The primary key is for joins; the document number is what appears on a printed contract and what a person quotes on the phone. Making it `UNIQUE` at the database level means a duplicate can't be created even if two code paths race.

## The three sync columns

Every local table carries three columns beyond its business fields:

```sql
CloudId    INTEGER NOT NULL DEFAULT 0,
IsDirty    INTEGER NOT NULL DEFAULT 0,
SyncAction TEXT    NOT NULL DEFAULT ''
```

**These exist only locally.** The cloud tables have no equivalent, because they are client bookkeeping rather than business data — the cloud has no need to know which of its rows a particular machine still considers pending.

| Column | Meaning |
|---|---|
| `CloudId` | The row's primary key in the cloud, or `0` if it has never been pushed |
| `IsDirty` | `1` when the row has local changes not yet sent |
| `SyncAction` | What kind of change: `insert`, `update` or `delete` |

**Separating `Id` from `CloudId` is what makes offline operation possible.** A record can be created, referenced by children, edited and even deleted before the cloud has ever seen it, because nothing in the local graph depends on a remote key existing. A design that used a server-assigned id as the primary key could not do this without a rewrite.

Each child row also stores its parent's `CloudId` — `ContractCloudId` on a receipt, for instance — so the sync layer can build a remote insert without a second lookup, and can tell at a glance whether the parent is ready to be referenced.

## Cloud schema

The same twelve tables in `snake_case`, plus one that exists only in the cloud.

### `user_roles` — cloud only

```sql
user_roles ( user_id uuid PRIMARY KEY, role text NOT NULL )
```

Roles live where they are enforced. The application reads a role into `AppSession` to decide which buttons to show, but that is presentation — the enforcement is Supabase **row-level security**, and RLS policies evaluate against this table. Storing roles locally would put the security decision on the client, where anything talking to the API directly would bypass it.

### Two columns the cloud has and SQLite doesn't

**`units_real_estate.parent_id`** — reserved for a sub-unit hierarchy. Today a building's occupancy is tracked with `UnitsCount` and `UnitNum` ("2 vacant of 7"); `parent_id` is the path to modelling each apartment as its own row belonging to a parent building. The column is defined ahead of the feature so the cloud schema doesn't need altering when it lands.

**`contracts_*.signature_cloud_path`, `signature_file_name`, `signature_file_type`** — the attached signed contract. The file lives in cloud storage and the row keeps only its path, which is why these are cloud-side: an attachment has no meaning without the storage it points into. The local database tracks the contract; the cloud tracks the document attached to it.

## Naming and type mapping

The two schemas are the same model in two dialects, translated in the `Cloud*Service` layer:

| Local | Cloud |
|---|---|
| `OwnersRealEstate` | `owners_real_estate` |
| `IdentityNumber` | `identity_number` |
| `ContractStartDate` | `contract_start_date` |
| `MainTotalAmount` | `main_total_amount` |
| `REAL` (money) | `double precision` |
| `DATETIME` | `timestamp without time zone` |
| `INTEGER PRIMARY KEY AUTOINCREMENT` | `bigint GENERATED ALWAYS AS IDENTITY` |

Because the translation is written by hand rather than generated, adding a field means editing both sides plus the mapping between them. That is the cost of having no ORM — and the reason a new column is a six-file change.

## Two modelling decisions worth knowing

### Products are one table for several kinds of thing

`ProductsInstallment` holds every product type in a single table, with type-specific columns left empty when they don't apply. A phone carries a `CarVIN` holding an empty string; a car carries an unused `MobileStorage`.

**Why.** Contracts, receipts and sync all treat a product as one row with one id regardless of kind. A separate table per type would mean either a join on every query or a polymorphic key, and every one of those paths would have to know about product kinds.

**The cost.** Each new product type widens the table for every existing row, and nothing in the schema stops a phone being saved with a plate number. The rule that `ProductType` governs which columns are meaningful lives in the code, not in the database.

### The sponsor is columns, not an entity

A customer's guarantor is five columns on the customer row rather than a record of its own. This is correct while a sponsor belongs to exactly one customer. It stops being correct the moment one person sponsors two customers: the data is duplicated, and updating a phone number means finding every row that repeats it. That is the point at which the sponsor has to become an entity with its own id.

## Schema creation and change

Tables are created with `CREATE TABLE IF NOT EXISTS` on every application start, so setup and startup are the same operation and a fresh install needs no migration step. A table added in a later version appears on first run.

**The limitation.** This handles new tables but not changed ones. Adding a column to an existing table is silently a no-op on a machine that already has that table, so a change of that kind needs a migration path the project does not currently have.

## Where to change what

| Change | What it touches |
|---|---|
| Add a business field | Local `CREATE TABLE` · model · entity service (insert, update, every `SELECT`) · cloud table · cloud model · `Cloud*Service` mapping · sync push |
| Add a computed/display field | Model · the `SELECT` that fills it · the view binding — no schema change |
| Add a validation rule | The entity service, so it holds for every caller |
| Add an entity | All of the above, plus a branch in `*SyncService.PushAllDirtyAsync` |

## Known limitations of the schema

Sync behaviour and its limits are covered in [CHALLENGES.md](CHALLENGES.md).

| Area | Current state | Better approach |
|---|---|---|
| Schema changes | New tables only | A migration path for altered tables |
| Product types | One table, unused columns per type | Type-specific tables, or a JSON attributes column |
| Sponsor | Five columns on the customer | Its own entity, once sponsors can repeat |
| Mapping | Hand-written on both sides | Generated from a single schema definition |
