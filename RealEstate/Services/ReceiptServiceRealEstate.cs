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
        SELECT COALESCE(MAX(CAST(SUBSTR(ReceiptNumber, 3) AS INTEGER)), 999) + 1
        FROM Receipts
        WHERE ReceiptNumber LIKE 'r-%';
    """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "r-" + next;
    }

    public void Add(string receiptNo, DateTime date, long contractId, string paymentMethod, double amount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        INSERT INTO Receipts (ReceiptNumber, ReceiptDate, ContractId, PaymentMethod, Amount)
        VALUES ($no, $date, $contractId, $method, $amount);
        """;
        cmd.Parameters.AddWithValue("$no", receiptNo);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$contractId", contractId);
        cmd.Parameters.AddWithValue("$method", paymentMethod);
        cmd.Parameters.AddWithValue("$amount", amount);

        cmd.ExecuteNonQuery();
    }

    public List<ReceiptRealEstate> GetAll()
    {
        var list = new List<ReceiptRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT r.Id, r.ReceiptNumber, r.ReceiptDate, r.Amount, r.PaymentMethod,
               c.Id, c.ContractNumber,
               t.Name
        FROM Receipts r
        JOIN Contracts c ON c.Id = r.ContractId
        JOIN Tenants t ON t.Id = c.TenantId
        ORDER BY r.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ReceiptRealEstate
            {
                Id = reader.GetInt64(0),
                ReceiptNumber = reader.GetString(1),
                ReceiptDate = reader.GetDateTime(2),
                Amount = reader.GetDouble(3),
                PaymentMethod = reader.GetString(4),

                ContractId = reader.GetInt64(5),
                ContractNumber = reader.GetString(6),

                TenantName = reader.GetString(7),

            });
        }

        return list;
    }
    public void Update(long id, string receiptNo, DateTime date, long contractId, string paymentMethod, double amount)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        UPDATE Receipts
        SET ReceiptNumber = $no,
            ReceiptDate = $date,
            ContractId = $contractId,
            PaymentMethod = $method,
            Amount = $amount
        WHERE Id = $id;
    """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$no", receiptNo);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$contractId", contractId);
        cmd.Parameters.AddWithValue("$method", paymentMethod);
        cmd.Parameters.AddWithValue("$amount", amount);

        cmd.ExecuteNonQuery();
    }

    public ReceiptRealEstate? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    SELECT r.Id, r.ReceiptNumber, r.ReceiptDate, r.ContractId, r.PaymentMethod, r.Amount
    FROM Receipts r
    WHERE r.Id = $id
    LIMIT 1;
    """;
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ReceiptRealEstate
        {
            Id = reader.GetInt64(0),
            ReceiptNumber = reader.GetString(1),
            ReceiptDate = reader.GetDateTime(2),

            ContractId = reader.GetInt64(3),

            PaymentMethod = reader.GetString(4),
            Amount = reader.GetDouble(5),
        };
    }

    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Receipts WHERE ContractId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف العقد لأنه مرتبط بسندات مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Receipts WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف سند القبض لأنه مرتبط ببيانات أخرى");
        }
    }

}
