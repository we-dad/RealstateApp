# Data Model


## Entity graph

Both modules have the same shape. An owner holds assets; assets are contracted to a counterparty; contracts generate receipts; assets accrue expenses.

```
Real estate                          Installments
───────────                          ────────────
Owner                                Owner
  └─ Unit                              └─ Product
       ├─ Contract ── Tenant                └─ Contract ── Customer
       │    └─ Receipt                           └─ Receipt
       └─ Expense                           └─ Expense
```

Declared foreign keys, all on local `Id`:

| Child | Parent |
|---|---|
| `UnitsRealEstate.OwnerId` | `OwnersRealEstate.Id` |
| `ContractsRealEstate.UnitId` | `UnitsRealEstate.Id` |
| `ContractsRealEstate.TenantId` | `TenantsRealEstate.Id` |
| `ReceiptsRealEstate.ContractId` | `ContractsRealEstate.Id` |
| `ExpensesRealEstate.UnitId` | `UnitsRealEstate.Id` |
| `ProductsInstallment.OwnerId` | `OwnersInstallment.Id` |

The installment side mirrors this with `Product` and `Customer` in place of `Unit` and `Tenant`.

**Note on the contract join.** A lease points at both a unit and a tenant, so a tenant exists independently of any contract and can hold several over time. The relationship is contract-to-tenant, not unit-to-tenant, which is why a tenant's history survives a lease ending.

---

## Three modelling decisions worth knowing before you change anything

### 1. Models are wider than their tables

`ContractRealEstate` has 35 properties. `ContractsRealEstate` has 18 columns. The difference is deliberate.

Stored on the table: contract number, dates, rent, state, payment method, apartment type, room and floor counts, obligations, `UnitId`, `TenantId`, and the three sync columns.

Carried on the model but **not** stored: `UnitName`, `City`, `District`, `UnitType`, `TenantName`, `TenantIdentityNumber`, `TenantPhone`, `TenantAddress`, `OwnerName`, `OwnerIdentityNumber`, `OwnerPhone`, `OwnerAddress`.

Those are filled by the query that loads the contract. A contract object therefore arrives complete enough to render a grid row, populate a detail window, and compose a printed contract — which needs the owner and tenant identity numbers — without a second round trip or a view-model layer in between.

**What this means in practice.** If you add a displayed field, decide which side it belongs on. A stored field needs a column, a migration concern, and sync handling. A projected field needs only the query's `SELECT` and the model property. Getting this backwards is the most likely way to break something here: adding a projected field to the table means it can go stale, and adding a stored field only to the model means it silently disappears on save.

### 2. Products are one table for several kinds of thing

`ProductsInstallment` holds every product type in a single table, with type-specific columns left empty when they don't apply:

```sql
ProductType     TEXT NOT NULL DEFAULT 'جوالات',
CarPlateNumber  TEXT NOT NULL DEFAULT '',
CarVIN          TEXT NOT NULL DEFAULT '',
CarModel        TEXT NOT NULL DEFAULT '',
CarColor        TEXT NOT NULL DEFAULT '',
MobileStorage   TEXT NOT NULL DEFAULT '',
MobileColor     TEXT NOT NULL DEFAULT ''
```

A phone carries a VIN column holding an empty string; a car carries an unused storage column.

**Why.** Contracts, receipts and sync all treat a product as one row with one id regardless of kind. A separate table per type would mean either a join per query or a polymorphic key, and every one of those paths would have to know about product kinds.

**The cost.** Each new product type widens the table for every existing row, and nothing in the schema prevents a phone from being saved with a plate number. The rule that `ProductType` governs which columns are meaningful lives in the code, not in the database.

### 3. The sponsor is fields, not an entity

A customer's guarantor is stored as five columns on the customer row — `SponserName`, `SponserIdentityNumber`, `SponserPhone`, `SponserAddress`, `SponserJob` — rather than as a record of its own.

This is correct while a sponsor belongs to exactly one customer. It stops being correct the moment one person sponsors two customers: the data is duplicated, and updating a phone number means finding every row that repeats it. That is the point at which the sponsor has to become an entity with its own id.

---

## Contract attachments

Contracts in both modules carry three fields for an attached signed document:

```
SignatureCloudPath   remote storage path
SignatureFileName
SignatureFileType
```

The file lives in cloud storage; the row keeps only the path. A contract with an empty path has no attachment — which is what drives the attachment indicator in the contracts grid.

---

## Where to change what

| Change | Files to touch |
|---|---|
| Add a **stored** field | Model · `CREATE TABLE` in `Db*Service` · insert and update in the entity service · `SELECT` in every read · cloud model · sync push |
| Add a **projected** field | Model · the `SELECT` that loads it · the view binding |
| Add a validation rule | The entity service — not the view, so the rule holds for every caller |
| Change a PDF layout | `Pdf*Service` for that module only |
| Add a dashboard figure | The dashboard stats model and its query |
| Add a new entity | Table · model · service · cloud model · `Cloud*Service` · a branch in `*SyncService.PushAllDirtyAsync` |

The last row is the expensive one. A new entity touches six places because sync, storage and display are each explicit rather than generated — the trade for having no ORM and no code generation in the project.

---

## Known limitations of the model

| Area | Current state | Better approach |
|---|---|---|
| Product types | One table, unused columns per type | Type-specific tables, or a JSON attributes column |
| Sponsor | Five columns on the customer | Its own entity, once sponsors can repeat |
| Projected fields | Convention only — nothing marks them | An attribute or a separate read model, so the distinction is visible |
| Schema changes | `CREATE TABLE IF NOT EXISTS` — new tables only | A migration path for altered tables |
| Module duplication | Receipt, expense and PDF services written twice | A shared generic base for the identical parts |
