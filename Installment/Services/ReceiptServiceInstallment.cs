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

    public void Add(
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
        double currentTotal = 0;
        
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

        // ===============================
        // 2. Insert receipt using your method logic
        // ===============================
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
                    CurrentTotalAmount
                )
                VALUES
                (
                    $no,
                    $date,
                    $contractId,
                    $method,
                    $amount,
                    $currentTotalAmount
                );
                """;

            cmd.Parameters.AddWithValue("$no", receiptNo);
            cmd.Parameters.AddWithValue("$date", date);
            cmd.Parameters.AddWithValue("$contractId", contractId);
            cmd.Parameters.AddWithValue("$method", paymentMethod);
            cmd.Parameters.AddWithValue("$amount", amount);
            cmd.Parameters.AddWithValue("$currentTotalAmount", newTotal);

            cmd.ExecuteNonQuery();
        }

        // ===============================
        // 3. Update contract remaining
        // ===============================
        using (var cmd = con.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = """
                UPDATE ContractsInstallment
                SET CurrentTotalAmount = $total
                WHERE Id = $id;
                """;

            cmd.Parameters.AddWithValue("$id", contractId);
            cmd.Parameters.AddWithValue("$total", newTotal);

            cmd.ExecuteNonQuery();
        }

        // ===============================
        // 4. Close contract if finished
        // ===============================
        if (newTotal <= 0)
        {
            using var cmd = con.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = """
                UPDATE ContractsInstallment
                SET ContractState = 'منتهي'
                WHERE Id = $id;
                """;

            cmd.Parameters.AddWithValue("$id", contractId);
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
        SELECT r.Id, r.ReceiptNumber, r.ReceiptDate, r.Amount, r.PaymentMethod,r.CurrentTotalAmount,
               c.Id, c.ContractNumber,
               cus.Name
        FROM ReceiptsInstallment r
        JOIN ContractsInstallment c ON c.Id = r.ContractId
        JOIN CustomersInstallment cus ON cus.Id = c.CustomerId
        ORDER BY r.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ReceiptInstallment
            {
                Id = reader.GetInt64(0),
                ReceiptNumber = reader.GetString(1),
                ReceiptDate = reader.GetDateTime(2),
                Amount = reader.GetDouble(3),
                PaymentMethod = reader.GetString(4),
                CurrentTotalAmount = reader.GetDouble(5),

                ContractId = reader.GetInt64(6),
                ContractNumber = reader.GetString(7),

                CustomerName = reader.GetString(8),

            });
        }

        return list;
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

        // 1. Read old receipt data
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
                throw new Exception("Receipt not found.");

            oldContractId = Convert.ToInt64(reader["ContractId"]);
            oldAmount = Convert.ToDouble(reader["Amount"]);
        }

        // 2. Restore old amount to old contract
        double oldContractCurrentTotal;
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
            oldContractCurrentTotal = result == null ? 0 : Convert.ToDouble(result);
        }

        double restoredOldContractTotal = oldContractCurrentTotal + oldAmount;

        using (var cmd = con.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = """
                UPDATE ContractsInstallment
                SET CurrentTotalAmount = $total
                WHERE Id = $id;
                """;

            cmd.Parameters.AddWithValue("$id", oldContractId);
            cmd.Parameters.AddWithValue("$total", restoredOldContractTotal);

            cmd.ExecuteNonQuery();
        }

        // 3. Read target contract current total
        double targetCurrentTotal;
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
            targetCurrentTotal = result == null ? 0 : Convert.ToDouble(result);
        }

        // 4. Apply new amount to target contract
        double newTargetTotal = targetCurrentTotal - amount;
        if (newTargetTotal < 0)
            newTargetTotal = 0;

        using (var cmd = con.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = """
                UPDATE ReceiptsInstallment
                SET
                    ReceiptNumber = $no,
                    ReceiptDate = $date,
                    ContractId = $contractId,
                    PaymentMethod = $method,
                    Amount = $amount,
                    CurrentTotalAmount = $currentTotalAmount
                WHERE Id = $id;
                """;

            cmd.Parameters.AddWithValue("$id", receiptId);
            cmd.Parameters.AddWithValue("$no", receiptNo);
            cmd.Parameters.AddWithValue("$date", date);
            cmd.Parameters.AddWithValue("$contractId", contractId);
            cmd.Parameters.AddWithValue("$method", paymentMethod);
            cmd.Parameters.AddWithValue("$amount", amount);
            cmd.Parameters.AddWithValue("$currentTotalAmount", newTargetTotal);

            cmd.ExecuteNonQuery();
        }

        // 5. Update target contract total
        using (var cmd = con.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = """
                UPDATE ContractsInstallment
                SET CurrentTotalAmount = $total
                WHERE Id = $id;
                """;

            cmd.Parameters.AddWithValue("$id", contractId);
            cmd.Parameters.AddWithValue("$total", newTargetTotal);

            cmd.ExecuteNonQuery();
        }

        // 6. Fix old contract state
        using (var cmd = con.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = """
                UPDATE ContractsInstallment
                SET ContractState = CASE
                    WHEN CurrentTotalAmount <= 0 THEN 'منتهي'
                    ELSE 'نشط'
                END
                WHERE Id = $id;
                """;

            cmd.Parameters.AddWithValue("$id", oldContractId);
            cmd.ExecuteNonQuery();
        }

        // 7. Fix target contract state
        using (var cmd = con.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = """
                UPDATE ContractsInstallment
                SET ContractState = CASE
                    WHEN CurrentTotalAmount <= 0 THEN 'منتهي'
                    ELSE 'نشط'
                END
                WHERE Id = $id;
                """;

            cmd.Parameters.AddWithValue("$id", contractId);
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
    public ReceiptInstallment? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
                          SELECT
                              Id,
                              ReceiptNumber,
                              ReceiptDate,
                              ContractId,
                              PaymentMethod,
                              Amount,
                              CurrentTotalAmount
                          FROM ReceiptsInstallment
                          WHERE Id = $id;
                          """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ReceiptInstallment
        {
            Id = Convert.ToInt64(reader["Id"]),
            ReceiptNumber = reader["ReceiptNumber"]?.ToString() ?? "",
            ReceiptDate = Convert.ToDateTime(reader["ReceiptDate"]),
            ContractId = Convert.ToInt64(reader["ContractId"]),
            PaymentMethod = reader["PaymentMethod"]?.ToString() ?? "",
            Amount = Convert.ToDouble(reader["Amount"]),
            CurrentTotalAmount = Convert.ToDouble(reader["CurrentTotalAmount"])
        };
    }
    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM ReceiptsInstallment WHERE ContractId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف العقد لأنه مرتبط بسندات مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM ReceiptsInstallment WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف سند القبض لأنه مرتبط ببيانات أخرى");
        }
    }

}
