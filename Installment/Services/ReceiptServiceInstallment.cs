using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class ReceiptServiceInstallment
{
    private readonly DbServiceInstallment _db;

    public ReceiptServiceInstallment(DbServiceInstallment db)
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
            FROM ReceiptsInstallment
            WHERE ReceiptNumber LIKE 'Ir-%';
        """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "Ir-" + next;
    }

    public long Add(
        string receiptNo,
        DateTime date,
        long contractId,
        string paymentMethod,
        double amount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var tran = con.BeginTransaction();

        try
        {
            double currentTotal;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    SELECT CurrentTotalAmount
                    FROM ContractsInstallment
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", contractId);

                var result = cmd.ExecuteScalar();
                currentTotal = result == null ? 0 : Convert.ToDouble(result);
            }

            double newTotal = currentTotal - amount;
            if (newTotal < 0)
                newTotal = 0;

            long receiptLocalId;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    INSERT INTO ReceiptsInstallment
                    (
                        ReceiptNumber,
                        ReceiptDate,
                        ContractId,
                        PaymentMethod,
                        Amount,
                        CurrentTotalAmount,
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
                        $currentTotalAmount,
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
                cmd.Parameters.AddWithValue("$currentTotalAmount", newTotal);

                receiptLocalId = (long)cmd.ExecuteScalar()!;
            }

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    UPDATE ContractsInstallment
                    SET CurrentTotalAmount = $total,
                        ContractState = CASE
                            WHEN $total <= 0 THEN 'منتهي'
                            ELSE ContractState
                        END
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", contractId);
                cmd.Parameters.AddWithValue("$total", newTotal);

                cmd.ExecuteNonQuery();
            }

            tran.Commit();
            return receiptLocalId;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    public void Update(
        long receiptId,
        string receiptNo,
        DateTime date,
        long contractId,
        string paymentMethod,
        double amount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var tran = con.BeginTransaction();

        try
        {
            long oldContractId;
            double oldAmount;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    SELECT ContractId, Amount
                    FROM ReceiptsInstallment
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", receiptId);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    throw new Exception("Receipt not found");

                oldContractId = Convert.ToInt64(reader["ContractId"]);
                oldAmount = Convert.ToDouble(reader["Amount"]);
            }

            double oldCurrent;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    SELECT CurrentTotalAmount
                    FROM ContractsInstallment
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", oldContractId);

                var result = cmd.ExecuteScalar();
                oldCurrent = result == null ? 0 : Convert.ToDouble(result);
            }

            double restored = oldCurrent + oldAmount;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    UPDATE ContractsInstallment
                    SET CurrentTotalAmount = $total,
                        ContractState = 'جاري'
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", oldContractId);
                cmd.Parameters.AddWithValue("$total", restored);

                cmd.ExecuteNonQuery();
            }

            double newCurrent;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    SELECT CurrentTotalAmount
                    FROM ContractsInstallment
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", contractId);

                var result = cmd.ExecuteScalar();
                newCurrent = result == null ? 0 : Convert.ToDouble(result);
            }

            double newTotal = newCurrent - amount;
            if (newTotal < 0)
                newTotal = 0;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    UPDATE ReceiptsInstallment
                    SET ReceiptNumber = $no,
                        ReceiptDate = $date,
                        ContractId = $contractId,
                        PaymentMethod = $method,
                        Amount = $amount,
                        CurrentTotalAmount = $currentTotalAmount,
                        IsDirty = 1,
                        SyncAction = CASE
                            WHEN SyncAction = 'insert' THEN 'insert'
                            ELSE 'update'
                        END
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", receiptId);
                cmd.Parameters.AddWithValue("$no", receiptNo);
                cmd.Parameters.AddWithValue("$date", date);
                cmd.Parameters.AddWithValue("$contractId", contractId);
                cmd.Parameters.AddWithValue("$method", paymentMethod);
                cmd.Parameters.AddWithValue("$amount", amount);
                cmd.Parameters.AddWithValue("$currentTotalAmount", newTotal);

                cmd.ExecuteNonQuery();
            }

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    UPDATE ContractsInstallment
                    SET CurrentTotalAmount = $total,
                        ContractState = CASE
                            WHEN $total <= 0 THEN 'منتهي'
                            ELSE 'جاري'
                        END
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", contractId);
                cmd.Parameters.AddWithValue("$total", newTotal);

                cmd.ExecuteNonQuery();
            }

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    public List<ReceiptInstallment> GetAll()
    {
        var list = new List<ReceiptInstallment>();

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
                r.CurrentTotalAmount,
                c.Id,
                c.CloudId,
                c.ContractNumber,
                cus.Name
            FROM ReceiptsInstallment r
            JOIN ContractsInstallment c ON c.Id = r.ContractId
            JOIN CustomersInstallment cus ON cus.Id = c.CustomerId
            WHERE r.SyncAction <> 'delete'
            ORDER BY r.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ReceiptInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ReceiptNumber = reader.GetString(2),
                ReceiptDate = reader.GetDateTime(3),
                Amount = reader.GetDouble(4),
                PaymentMethod = reader.GetString(5),
                CurrentTotalAmount = reader.GetDouble(6),
                ContractId = reader.GetInt64(7),
                ContractCloudId = reader.GetInt64(8),
                ContractNumber = reader.GetString(9),
                CustomerName = reader.GetString(10),
            });
        }

        return list;
    }

    public ReceiptInstallment? GetById(long id)
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
                r.CurrentTotalAmount
            FROM ReceiptsInstallment r
            JOIN ContractsInstallment c ON c.Id = r.ContractId
            WHERE r.Id = $id
              AND r.SyncAction <> 'delete';
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ReceiptInstallment
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            ReceiptNumber = reader.GetString(2),
            ReceiptDate = reader.GetDateTime(3),
            ContractId = reader.GetInt64(4),
            ContractCloudId = reader.GetInt64(5),
            PaymentMethod = reader.GetString(6),
            Amount = reader.GetDouble(7),
            CurrentTotalAmount = reader.GetDouble(8)
        };
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ReceiptsInstallment
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
            UPDATE ReceiptsInstallment
            SET IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpsertFromCloud(
        long cloudId,
        string receiptNumber,
        DateTime receiptDate,
        long contractLocalId,
        string paymentMethod,
        double amount,
        double currentTotalAmount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM ReceiptsInstallment
            WHERE CloudId = $cloudId;
        """;

        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE ReceiptsInstallment
                SET ReceiptNumber = $receiptNumber,
                    ReceiptDate = $receiptDate,
                    ContractId = $contractId,
                    PaymentMethod = $paymentMethod,
                    Amount = $amount,
                    CurrentTotalAmount = $currentTotalAmount
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$receiptNumber", receiptNumber);
            update.Parameters.AddWithValue("$receiptDate", receiptDate);
            update.Parameters.AddWithValue("$contractId", contractLocalId);
            update.Parameters.AddWithValue("$paymentMethod", paymentMethod);
            update.Parameters.AddWithValue("$amount", amount);
            update.Parameters.AddWithValue("$currentTotalAmount", currentTotalAmount);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO ReceiptsInstallment
                (
                    CloudId,
                    ReceiptNumber,
                    ReceiptDate,
                    ContractId,
                    PaymentMethod,
                    Amount,
                    CurrentTotalAmount,
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
                    $currentTotalAmount,
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
            insert.Parameters.AddWithValue("$currentTotalAmount", currentTotalAmount);

            insert.ExecuteNonQuery();
        }
    }

    public List<ReceiptInstallment> GetDirtyRows()
    {
        var list = new List<ReceiptInstallment>();

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
                r.CurrentTotalAmount,
                r.SyncAction
            FROM ReceiptsInstallment r
        LEFT JOIN ContractsInstallment c ON c.Id = r.ContractId
            WHERE r.IsDirty = 1
              AND (
                    r.SyncAction = 'delete'
                    OR c.CloudId > 0
                  );
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ReceiptInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ReceiptNumber = reader.GetString(2),
                ReceiptDate = reader.GetDateTime(3),
                ContractId = reader.GetInt64(4),
                ContractCloudId = reader.GetInt64(5),
                PaymentMethod = reader.GetString(6),
                Amount = reader.GetDouble(7),
                CurrentTotalAmount = reader.GetDouble(8),
                SyncAction = reader.GetString(9)
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
            FROM ReceiptsInstallment
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

        using var tran = con.BeginTransaction();

        try
        {
            long contractId;
            double amount;

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    SELECT ContractId, Amount
                    FROM ReceiptsInstallment
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", id);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return;

                contractId = reader.GetInt64(0);
                amount = reader.GetDouble(1);
            }

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    UPDATE ReceiptsInstallment
                    SET IsDirty = 1,
                        SyncAction = 'delete'
                    WHERE Id = $id;
                """;

                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = """
                    UPDATE ContractsInstallment
                    SET CurrentTotalAmount = CurrentTotalAmount + $amount,
                        ContractState = 'جاري'
                    WHERE Id = $contractId;
                """;

                cmd.Parameters.AddWithValue("$amount", amount);
                cmd.Parameters.AddWithValue("$contractId", contractId);
                cmd.ExecuteNonQuery();
            }

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }

    public void DeleteLocalPermanent(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = """
                DELETE FROM ReceiptsInstallment
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