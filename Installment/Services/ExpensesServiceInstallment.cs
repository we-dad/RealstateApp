using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class ExpensesServiceInstallment
{
    private readonly DbServiceInstallment _db;

    public ExpensesServiceInstallment(DbServiceInstallment db)
    {
        _db = db;
    }

    public string GenerateExpensesNumber()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT COALESCE(MAX(CAST(SUBSTR(ExpensesNumber, 4) AS INTEGER)), 999) + 1
            FROM ExpensesInstallment
            WHERE ExpensesNumber LIKE 'Ie-%';
        """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "Ie-" + next + UserCodeService.GetSuffix();
    }

    public long Add(
        string expensesNo,
        DateTime date,
        long productId,
        string expensesService,
        double expensesAmount,
        string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ExpensesInstallment
            (
                ExpensesNumber,
                ExpensesDate,
                ProductId,
                ExpensesService,
                ExpensesAmount,
                ExpensesNote,
                IsDirty,
                SyncAction
            )
            VALUES
            (
                $no,
                $date,
                $productId,
                $expensesService,
                $expensesAmount,
                $expensesNote,
                1,
                'insert'
            );

            SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$no", expensesNo);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$productId", productId);
        cmd.Parameters.AddWithValue("$expensesService", expensesService);
        cmd.Parameters.AddWithValue("$expensesAmount", expensesAmount);
        cmd.Parameters.AddWithValue("$expensesNote", expensesNote);

        var newId = (long)cmd.ExecuteScalar()!;

        DataChangeNotifier.Notify();

        return newId;
    }

    public void Update(
        long id,
        string expensesNo,
        DateTime date,
        long productId,
        string expensesService,
        double expensesAmount,
        string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ExpensesInstallment
            SET ExpensesNumber  = $no,
                ExpensesDate    = $date,
                ProductId       = $productId,
                ExpensesService = $expensesService,
                ExpensesAmount  = $expensesAmount,
                ExpensesNote    = $expensesNote,
                IsDirty         = 1,
                SyncAction      = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$no", expensesNo);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$productId", productId);
        cmd.Parameters.AddWithValue("$expensesService", expensesService);
        cmd.Parameters.AddWithValue("$expensesAmount", expensesAmount);
        cmd.Parameters.AddWithValue("$expensesNote", expensesNote);

        cmd.ExecuteNonQuery();

        DataChangeNotifier.Notify();
    }

    public List<ExpensesInstallment> GetAll()
    {
        var list = new List<ExpensesInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
                e.Id,
                e.CloudId,
                e.ExpensesNumber,
                e.ExpensesDate,
                e.ExpensesService,
                e.ExpensesAmount,
                e.ExpensesNote,
                e.ProductId,
                p.CloudId,
                p.ProductName
            FROM ExpensesInstallment e
            JOIN ProductsInstallment p ON p.Id = e.ProductId
            WHERE e.SyncAction <> 'delete'
            ORDER BY e.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ExpensesInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ExpensesNumber = reader.GetString(2),
                ExpensesDate = reader.GetDateTime(3),
                ExpensesService = reader.GetString(4),
                ExpensesAmount = reader.GetDouble(5),
                ExpensesNote = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ProductId = reader.GetInt64(7),
                ProductCloudId = reader.GetInt64(8),
                ProductName = reader.GetString(9),
            });
        }

        return list;
    }

    public ExpensesInstallment? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
                e.Id,
                e.CloudId,
                e.ExpensesNumber,
                e.ExpensesDate,
                e.ExpensesService,
                e.ExpensesAmount,
                e.ExpensesNote,
                e.ProductId,
                p.CloudId,
                p.ProductName
            FROM ExpensesInstallment e
            JOIN ProductsInstallment p ON p.Id = e.ProductId
            WHERE e.Id = $id
              AND e.SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ExpensesInstallment
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            ExpensesNumber = reader.GetString(2),
            ExpensesDate = reader.GetDateTime(3),
            ExpensesService = reader.GetString(4),
            ExpensesAmount = reader.GetDouble(5),
            ExpensesNote = reader.IsDBNull(6) ? "" : reader.GetString(6),
            ProductId = reader.GetInt64(7),
            ProductCloudId = reader.GetInt64(8),
            ProductName = reader.GetString(9),
        };
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ExpensesInstallment
            SET CloudId = $cloudId,
                IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public void MarkSynced(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ExpensesInstallment
            SET IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpsertFromCloud(
        long cloudId,
        string expensesNumber,
        DateTime expensesDate,
        long productLocalId,
        string expensesService,
        double expensesAmount,
        string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM ExpensesInstallment
            WHERE CloudId = $cloudId;
        """;

        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE ExpensesInstallment
                SET ExpensesNumber = $expensesNumber,
                    ExpensesDate = $expensesDate,
                    ProductId = $productId,
                    ExpensesService = $expensesService,
                    ExpensesAmount = $expensesAmount,
                    ExpensesNote = $expensesNote
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$expensesNumber", expensesNumber);
            update.Parameters.AddWithValue("$expensesDate", expensesDate);
            update.Parameters.AddWithValue("$productId", productLocalId);
            update.Parameters.AddWithValue("$expensesService", expensesService);
            update.Parameters.AddWithValue("$expensesAmount", expensesAmount);
            update.Parameters.AddWithValue("$expensesNote", expensesNote);

            update.ExecuteNonQuery();
        }
        else
        {
            // Not found by CloudId. A local row that was created here and pushed,
            // but whose CloudId was never saved (offline, crash), would be
            // duplicated by the insert below. Adopt it instead: give it the
            // CloudId and make it an update. IsDirty stays 1, so the local
            // values are kept and pushed - nothing is overwritten.
            // The number alone is not enough (two users can pick the same one),
            // so the other fields and the parent row must match too.
            using var adopt = con.CreateCommand();
            adopt.CommandText = """
                UPDATE ExpensesInstallment
                SET CloudId = $cloudId,
                    SyncAction = 'update'
                WHERE Id = (
                    SELECT Id
                    FROM ExpensesInstallment
                    WHERE CloudId = 0
                      AND IsDirty = 1
                      AND SyncAction = 'insert'
                      AND TRIM(ExpensesNumber) = TRIM($expensesNumber)
                      AND date(ExpensesDate) = date($expensesDate)
                      AND ABS(ExpensesAmount - $expensesAmount) < 0.005
                      AND TRIM(ExpensesService) = TRIM($expensesService)
                      AND ProductId = $productLocalId
                    LIMIT 1
                );
            """;

            adopt.Parameters.AddWithValue("$cloudId", cloudId);
            adopt.Parameters.AddWithValue("$expensesNumber", expensesNumber);
            adopt.Parameters.AddWithValue("$expensesDate", expensesDate);
            adopt.Parameters.AddWithValue("$expensesAmount", expensesAmount);
            adopt.Parameters.AddWithValue("$expensesService", expensesService);
            adopt.Parameters.AddWithValue("$productLocalId", productLocalId);

            if (adopt.ExecuteNonQuery() > 0)
                return;

            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO ExpensesInstallment
                (
                    CloudId,
                    ExpensesNumber,
                    ExpensesDate,
                    ProductId,
                    ExpensesService,
                    ExpensesAmount,
                    ExpensesNote,
                    IsDirty,
                    SyncAction
                )
                VALUES
                (
                    $cloudId,
                    $expensesNumber,
                    $expensesDate,
                    $productId,
                    $expensesService,
                    $expensesAmount,
                    $expensesNote,
                    0,
                    ''
                );
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$expensesNumber", expensesNumber);
            insert.Parameters.AddWithValue("$expensesDate", expensesDate);
            insert.Parameters.AddWithValue("$productId", productLocalId);
            insert.Parameters.AddWithValue("$expensesService", expensesService);
            insert.Parameters.AddWithValue("$expensesAmount", expensesAmount);
            insert.Parameters.AddWithValue("$expensesNote", expensesNote);

            try
            {
                insert.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
            {
                // SQLITE_CONSTRAINT_UNIQUE: a different local row already uses this
                // number. Skip this row and keep pulling the rest.
                Console.WriteLine($"Skipped cloud row {cloudId} in ExpensesInstallment: number already used locally.");
            }
        }
    }

    public List<ExpensesInstallment> GetDirtyRows()
    {
        var list = new List<ExpensesInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
                e.Id,
                e.CloudId,
                e.ExpensesNumber,
                e.ExpensesDate,
                e.ExpensesService,
                e.ExpensesAmount,
                e.ExpensesNote,
                e.ProductId,
                p.CloudId,
                p.ProductName,
                e.SyncAction
           FROM ExpensesInstallment e
        LEFT JOIN ProductsInstallment p ON p.Id = e.ProductId
            WHERE e.IsDirty = 1
              AND (
                    e.SyncAction = 'delete'
                    OR p.CloudId > 0
                  );
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ExpensesInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ExpensesNumber = reader.GetString(2),
                ExpensesDate = reader.GetDateTime(3),
                ExpensesService = reader.GetString(4),
                ExpensesAmount = reader.GetDouble(5),
                ExpensesNote = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ProductId = reader.GetInt64(7),
                ProductCloudId = reader.GetInt64(8),
                ProductName = reader.GetString(9),
                SyncAction = reader.GetString(10)
            });
        }

        return list;
    }

    public long GetLocalIdByCloudId(long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id
            FROM ExpensesInstallment
            WHERE CloudId = $cloudId
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);

        var result = cmd.ExecuteScalar();
        return result == null ? 0 : Convert.ToInt64(result);
    }

    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ExpensesInstallment
            SET IsDirty = 1,
                SyncAction = 'delete'
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        DataChangeNotifier.Notify();
    }

    public void DeleteLocalPermanent(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = """
                DELETE FROM ExpensesInstallment
                WHERE Id = $id;
            """;

            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف سند الصرف لأنه مرتبط ببيانات أخرى");
        }
    }
}