using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class ReceiptServiceRealEstate
{
    private readonly DbServiceRealEstate _db;

    public ReceiptServiceRealEstate(DbServiceRealEstate db)
    {
        _db = db;
    }

    public string GenerateReceiptNumber()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT COALESCE(MAX(CAST(SUBSTR(ReceiptNumber, 4) AS INTEGER)), 999) + 1
            FROM ReceiptsRealEstate
            WHERE ReceiptNumber LIKE 'Rr-%';
        """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "Rr-" + next;
    }

    public long Add(string receiptNo, DateTime date, long contractId, string paymentMethod, double amount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ReceiptsRealEstate
            (
                ReceiptNumber,
                ReceiptDate,
                ContractId,
                PaymentMethod,
                Amount,
                IsDirty,
                SyncAction
            )
            VALUES
            (
                $no,
                $date,
                $contractId,
                $method,
                $amount,
                1,
                'insert'
            );

            SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$no", receiptNo);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$contractId", contractId);
        cmd.Parameters.AddWithValue("$method", paymentMethod);
        cmd.Parameters.AddWithValue("$amount", amount);

        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(long id, DateTime date, long contractId, string paymentMethod, double amount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ReceiptsRealEstate
            SET ReceiptDate = $date,
                ContractId = $contractId,
                PaymentMethod = $method,
                Amount = $amount,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$contractId", contractId);
        cmd.Parameters.AddWithValue("$method", paymentMethod);
        cmd.Parameters.AddWithValue("$amount", amount);

        cmd.ExecuteNonQuery();
    }

    public void UpsertFromCloud(
        long cloudId,
        string receiptNumber,
        DateTime receiptDate,
        long contractLocalId,
        string paymentMethod,
        double amount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM ReceiptsRealEstate
            WHERE CloudId = $cloudId;
        """;

        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE ReceiptsRealEstate
                SET ReceiptNumber = $receiptNumber,
                    ReceiptDate = $receiptDate,
                    ContractId = $contractId,
                    PaymentMethod = $paymentMethod,
                    Amount = $amount
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$receiptNumber", receiptNumber);
            update.Parameters.AddWithValue("$receiptDate", receiptDate);
            update.Parameters.AddWithValue("$contractId", contractLocalId);
            update.Parameters.AddWithValue("$paymentMethod", paymentMethod);
            update.Parameters.AddWithValue("$amount", amount);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO ReceiptsRealEstate
                (
                    CloudId,
                    ReceiptNumber,
                    ReceiptDate,
                    ContractId,
                    PaymentMethod,
                    Amount,
                    IsDirty,
                    SyncAction
                )
                VALUES
                (
                    $cloudId,
                    $receiptNumber,
                    $receiptDate,
                    $contractId,
                    $paymentMethod,
                    $amount,
                    0,
                    ''
                );
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$receiptNumber", receiptNumber);
            insert.Parameters.AddWithValue("$receiptDate", receiptDate);
            insert.Parameters.AddWithValue("$contractId", contractLocalId);
            insert.Parameters.AddWithValue("$paymentMethod", paymentMethod);
            insert.Parameters.AddWithValue("$amount", amount);

            insert.ExecuteNonQuery();
        }
    }

    public List<ReceiptRealEstate> GetAll()
    {
        var list = new List<ReceiptRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
                              SELECT 
                                  r.Id,
                                  r.CloudId,
                                  r.ReceiptNumber,
                                  r.ReceiptDate,
                                  r.Amount,
                                  r.PaymentMethod,
                                  c.Id,
                                  c.CloudId,
                                  c.ContractNumber,
                                  t.Name,
                                  (
                                      SELECT COUNT(*)
                                      FROM ReceiptsRealEstate r2
                                      WHERE r2.ContractId = r.ContractId
                                        AND r2.Id <= r.Id
                                        AND IFNULL(r2.SyncAction, '') <> 'delete'
                                  ) AS ReceiptOrderInContract
                              FROM ReceiptsRealEstate r
                              JOIN ContractsRealEstate c ON c.Id = r.ContractId
                              JOIN TenantsRealEstate t ON t.Id = c.TenantId
                              WHERE IFNULL(r.SyncAction, '') <> 'delete'
                              ORDER BY r.Id DESC;
                          """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ReceiptRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ReceiptNumber = reader.GetString(2),
                ReceiptDate = reader.GetDateTime(3),
                Amount = reader.GetDouble(4),
                PaymentMethod = reader.GetString(5),
                ContractId = reader.GetInt64(6),
                ContractCloudId = reader.GetInt64(7),
                ContractNumber = reader.GetString(8),
                TenantName = reader.GetString(9),
                ReceiptsCount = reader.GetInt32(10)
            });
        }

        return list;
    }

    public ReceiptRealEstate? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
                              SELECT 
                                  r.Id,
                                  r.CloudId,
                                  r.ReceiptNumber,
                                  r.ReceiptDate,
                                  r.ContractId,
                                  c.CloudId,
                                  r.PaymentMethod,
                                  r.Amount,
                                  (
                                      SELECT COUNT(*)
                                      FROM ReceiptsRealEstate r2
                                      WHERE r2.ContractId = r.ContractId
                                        AND r2.Id <= r.Id
                                        AND IFNULL(r2.SyncAction, '') <> 'delete'
                                  ) AS ReceiptOrderInContract
                              FROM ReceiptsRealEstate r
                              JOIN ContractsRealEstate c ON c.Id = r.ContractId
                              WHERE r.Id = $id
                                AND IFNULL(r.SyncAction, '') <> 'delete'
                              LIMIT 1;
                          """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ReceiptRealEstate
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            ReceiptNumber = reader.GetString(2),
            ReceiptDate = reader.GetDateTime(3),
            ContractId = reader.GetInt64(4),
            ContractCloudId = reader.GetInt64(5),
            PaymentMethod = reader.GetString(6),
            Amount = reader.GetDouble(7),
            ReceiptsCount = reader.GetInt32(8)
        };
    }
    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ReceiptsRealEstate
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
            UPDATE ReceiptsRealEstate
            SET IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<ReceiptRealEstate> GetDirtyRows()
    {
        var list = new List<ReceiptRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
                r.Id,
                r.CloudId,
                r.ReceiptNumber,
                r.ReceiptDate,
                r.ContractId,
                c.CloudId,
                r.PaymentMethod,
                r.Amount,
                r.SyncAction
            FROM ReceiptsRealEstate r
            JOIN ContractsRealEstate c ON c.Id = r.ContractId
            WHERE r.IsDirty = 1
              AND (
                    r.SyncAction = 'delete'
                    OR c.CloudId > 0
                  );
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ReceiptRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ReceiptNumber = reader.GetString(2),
                ReceiptDate = reader.GetDateTime(3),
                ContractId = reader.GetInt64(4),
                ContractCloudId = reader.GetInt64(5),
                PaymentMethod = reader.GetString(6),
                Amount = reader.GetDouble(7),
                SyncAction = reader.GetString(8)
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
            FROM ReceiptsRealEstate
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
            UPDATE ReceiptsRealEstate
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
                DELETE FROM ReceiptsRealEstate
                WHERE Id = $id;
            """;

            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف سند القبض لأنه مرتبط ببيانات أخرى");
        }
    }
}