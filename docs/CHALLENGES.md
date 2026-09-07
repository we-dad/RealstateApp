# Technical Challenges

Problems encountered building **المسار الذهبي**, and how they were resolved. Where a claim can be checked against the code, the file is cited.

---

## 1. The application had to keep working with no connection

**Context**
The program is used to write contracts and issue receipts. A dropped connection cannot be allowed to stop either. At the same time, data has to reach a central store so it isn't confined to one machine.

**Alternatives considered**

| Approach | Outcome |
|---|---|
| Cloud-first, with a local cache for reads | ❌ Rejected — writes still fail offline, which is the case that matters |
| Local-only, with periodic export | ❌ Rejected — no central store, and merging exports by hand is worse than not syncing |
| Local-first with per-row sync flags | ✅ Chosen |

**Implementation**
SQLite is the working database; the cloud is a replica the client pushes to. Three columns on every table carry the sync state:

```sql
CloudId    INTEGER NOT NULL DEFAULT 0,
IsDirty    INTEGER NOT NULL DEFAULT 0,
SyncAction TEXT    NOT NULL DEFAULT ''
```

Separating the local id from `CloudId` is what makes full offline operation possible. A record can be created, referenced by children, edited and even deleted before the cloud has ever seen it — because nothing in the local graph depends on a remote key existing.

**The failure mode this had to avoid**
The dangerous case is a row that pushes successfully but stays flagged dirty: the next sync inserts it again, and the cloud accumulates duplicate receipts against a customer's balance. Both completion paths clear the flags in the same statement that records the result:

```sql
UPDATE ReceiptsInstallment
SET CloudId = $cloudId, IsDirty = 0, SyncAction = ''
WHERE Id = $id;
```

Because the id assignment and the flag clearing are one write, there is no window in which a row is synced but still marked dirty.

**Ordering**
A child cannot be pushed before its parent has a remote id, or the foreign key would be invalid:

```csharp
if (receipt.ContractCloudId <= 0)
    continue;
```

Skipping leaves the row dirty for the next pass rather than pushing a broken reference or dropping the record. Combined with the current children-first push order, a contract and its receipts created in one offline session complete over two passes — correct, but one pass slower than necessary. Reversing the order would fix it.

**Accepted trade-off**
Sync is push-only and last-write-wins. The client sends its changes but does not pull to reconcile edits made elsewhere, and there is no timestamp or version column to detect divergence. For a deployment where one machine owns the data this is sound; it is the first thing that would have to change to support several machines editing concurrently.

**Lesson**
Offline-first is a data-model decision, not a networking one. Once local and remote identity are separate columns and every row records its own pending intent, the network layer becomes a loop over dirty rows. Trying to add offline support to a design that assumes a server-assigned id means rewriting the schema.

---

## 2. Arabic in generated PDF is a different problem from Arabic on screen

**Symptom**
Getting Arabic to display correctly in the application did not carry over to generated documents. Contracts and receipts came out with wrong text direction and missing glyphs.

**Diagnosis**
Two separate systems. On-screen text is laid out by the UI framework; a PDF is composed by a document library with its own layout engine and its own font handling, and it does not inherit the application's text settings. Two specific failures:

- Direction is set per composed block, not globally. A page can be right-to-left while a row inside it is still left-to-right, which puts an amount on the wrong side of its label.
- The default font stack carries no Arabic coverage, so glyphs are dropped. This is invisible during development if the layout is checked with Latin placeholder text.

**Solution**
Direction is declared on the page and again on each block that needs it, with the font family set explicitly:

```csharp
page.ContentFromRightToLeft();
page.DefaultTextStyle(x => x.FontFamily("Arial"));
```

and repeated on nested composition where a row would otherwise fall back:

```csharp
s.Item().ContentFromRightToLeft().Row(row => { ... });
```

**Related decision**
Dashboard PDF export runs through the same services that produce contracts and receipts. Keeping one document path means the on-screen report and its printed version cannot drift apart, and a layout fix applies to both.

**Lesson**
Right-to-left support has to be established per output target. Working Arabic in the interface says nothing about Arabic in generated documents, and font coverage is the failure that hides longest, because the layout looks correct while the text is silently absent.

---

## 3. Updating software that holds live business data

**Context**
The program runs on machines belonging to a working business, holding contracts and payment records in a local database. Any update mechanism has to leave that data untouched, and cannot depend on someone being available to reinstall.

**Approach**
Releases are built and distributed with Velopack, which produces the Windows installer and applies updates in place. The version is declared once in the project file and read back at runtime from the assembly:

```csharp
Assembly.GetExecutingAssembly().GetName().Version?.ToString()
```

rather than from a second constant that could be forgotten during a release. The number shown in the interface is therefore the number that was actually built.

**Why the local database survives**
The SQLite file lives in the user's local application data folder, outside the installation directory, and the schema is created with `CREATE TABLE IF NOT EXISTS` on every start. An update replaces the program; it does not touch the data, and a new table added in a later version is created on first run without a migration step.

**Accepted trade-off**
`CREATE TABLE IF NOT EXISTS` handles new tables but not changed ones. Adding a column to an existing table is silently a no-op on an installed machine, so a schema change of that kind needs a migration path that does not currently exist.

**Lesson**
Shipping to machines that already hold data changes what a release is. The version number, the install location and the schema strategy all become part of the update contract, and the safe boundary is keeping data outside the directory the installer replaces.

---

## Known limitations

| Area | Current state | Better approach |
|---|---|---|
| Sync direction | Push-only | Pull to reconcile remote edits |
| Conflicts | Last write wins | Timestamps or version columns |
| Push order | Children before parents | Parents first — one pass instead of two |
| Failed pushes | Silent skip and retry | Visible sync status for stuck rows |
| Schema changes | New tables only | A migration path for altered tables |
| Module duplication | Receipt, expense and PDF services written twice | Shared base for the identical parts |
