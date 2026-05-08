using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class ExpensesServiceRealEstate
{
    private readonly DbServiceRealEstate _db;

    public ExpensesServiceRealEstate(DbServiceRealEstate db)
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
            FROM ExpensesRealEstate
            WHERE ExpensesNumber LIKE 'Re-%';
        """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "Re-" + next;
    }

    public long Add(
        string expensesNo,
        DateTime date,
        long unitId,
        string expensesService,
        double expensesAmount,
        string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ExpensesRealEstate
            (
                ExpensesNumber,
                ExpensesDate,
                UnitId,
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
                $unitId,
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
        cmd.Parameters.AddWithValue("$unitId", unitId);
        cmd.Parameters.AddWithValue("$expensesService", expensesService);
        cmd.Parameters.AddWithValue("$expensesAmount", expensesAmount);
        cmd.Parameters.AddWithValue("$expensesNote", expensesNote);

        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(
        long id,
        string expensesNo,
        DateTime date,
        long unitId,
        string expensesService,
        double expensesAmount,
        string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ExpensesRealEstate
            SET ExpensesNumber  = $no,
                ExpensesDate    = $date,
                UnitId          = $unitId,
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
        cmd.Parameters.AddWithValue("$unitId", unitId);
        cmd.Parameters.AddWithValue("$expensesService", expensesService);
        cmd.Parameters.AddWithValue("$expensesAmount", expensesAmount);
        cmd.Parameters.AddWithValue("$expensesNote", expensesNote);

        cmd.ExecuteNonQuery();
    }

    public List<ExpensesRealEstate> GetAll()
    {
        var list = new List<ExpensesRealEstate>();

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
                e.UnitId,
                u.CloudId,
                u.UnitName
            FROM ExpensesRealEstate e
            JOIN UnitsRealEstate u ON u.Id = e.UnitId
            WHERE e.SyncAction <> 'delete'
            ORDER BY e.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ExpensesRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ExpensesNumber = reader.GetString(2),
                ExpensesDate = reader.GetDateTime(3),
                ExpensesService = reader.GetString(4),
                ExpensesAmount = reader.GetDouble(5),
                ExpensesNote = reader.IsDBNull(6) ? "" : reader.GetString(6),
                UnitId = reader.GetInt64(7),
                UnitCloudId = reader.GetInt64(8),
                UnitName = reader.GetString(9)
            });
        }

        return list;
    }

    public ExpensesRealEstate? GetById(long id)
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
                e.UnitId,
                u.CloudId,
                u.UnitName
            FROM ExpensesRealEstate e
            JOIN UnitsRealEstate u ON u.Id = e.UnitId
            WHERE e.Id = $id
              AND e.SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ExpensesRealEstate
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            ExpensesNumber = reader.GetString(2),
            ExpensesDate = reader.GetDateTime(3),
            ExpensesService = reader.GetString(4),
            ExpensesAmount = reader.GetDouble(5),
            ExpensesNote = reader.IsDBNull(6) ? "" : reader.GetString(6),
            UnitId = reader.GetInt64(7),
            UnitCloudId = reader.GetInt64(8),
            UnitName = reader.GetString(9),
        };
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            UPDATE ExpensesRealEstate
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
            UPDATE ExpensesRealEstate
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
        long unitLocalId,
        string expensesService,
        double expensesAmount,
        string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM ExpensesRealEstate
            WHERE CloudId = $cloudId;
        """;

        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE ExpensesRealEstate
                SET ExpensesNumber = $expensesNumber,
                    ExpensesDate = $expensesDate,
                    UnitId = $unitId,
                    ExpensesService = $expensesService,
                    ExpensesAmount = $expensesAmount,
                    ExpensesNote = $expensesNote
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$expensesNumber", expensesNumber);
            update.Parameters.AddWithValue("$expensesDate", expensesDate);
            update.Parameters.AddWithValue("$unitId", unitLocalId);
            update.Parameters.AddWithValue("$expensesService", expensesService);
            update.Parameters.AddWithValue("$expensesAmount", expensesAmount);
            update.Parameters.AddWithValue("$expensesNote", expensesNote);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO ExpensesRealEstate
                (
                    CloudId,
                    ExpensesNumber,
                    ExpensesDate,
                    UnitId,
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
                    $unitId,
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
            insert.Parameters.AddWithValue("$unitId", unitLocalId);
            insert.Parameters.AddWithValue("$expensesService", expensesService);
            insert.Parameters.AddWithValue("$expensesAmount", expensesAmount);
            insert.Parameters.AddWithValue("$expensesNote", expensesNote);

            insert.ExecuteNonQuery();
        }
    }

    public List<ExpensesRealEstate> GetDirtyRows()
    {
        var list = new List<ExpensesRealEstate>();

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
                e.UnitId,
                u.CloudId,
                u.UnitName,
                e.SyncAction
            FROM ExpensesRealEstate e
            JOIN UnitsRealEstate u ON u.Id = e.UnitId
            WHERE e.IsDirty = 1
              AND (
                    e.SyncAction = 'delete'
                    OR u.CloudId > 0
                  );
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ExpensesRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ExpensesNumber = reader.GetString(2),
                ExpensesDate = reader.GetDateTime(3),
                ExpensesService = reader.GetString(4),
                ExpensesAmount = reader.GetDouble(5),
                ExpensesNote = reader.IsDBNull(6) ? "" : reader.GetString(6),
                UnitId = reader.GetInt64(7),
                UnitCloudId = reader.GetInt64(8),
                UnitName = reader.GetString(9),
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
            FROM ExpensesRealEstate
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
            UPDATE ExpensesRealEstate
            SET IsDirty = 1,
                SyncAction = 'delete'
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteLocalPermanent(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = """
                DELETE FROM ExpensesRealEstate
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