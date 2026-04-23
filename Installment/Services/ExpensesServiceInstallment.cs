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

        return "Ie-" + next;
    }

    public void Add(string ExpensesNo, DateTime date, long productId, string expensesService, double expensesAmount, string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        INSERT INTO ExpensesInstallment (ExpensesNumber, ExpensesDate, ProductId, ExpensesService, ExpensesAmount, ExpensesNote)
        VALUES ($no, $date, $productId, $expensesService, $expensesAmount, $expensesNote);
        """;
        cmd.Parameters.AddWithValue("$no", ExpensesNo);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$productId", productId);
        cmd.Parameters.AddWithValue("$expensesService", expensesService);
        cmd.Parameters.AddWithValue("$expensesAmount", expensesAmount);
        cmd.Parameters.AddWithValue("$expensesNote", expensesNote);

        cmd.ExecuteNonQuery();
    }

    public void Update(long id, string expensesNo, DateTime date, long unitId, string expensesService, double expensesAmount, string expensesNote)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        UPDATE ExpensesInstallment
        SET ExpensesNumber  = $no,
            ExpensesDate    = $date,
            UnitId          = $unitId,
            ExpensesService = $expensesService,
            ExpensesAmount  = $expensesAmount,
            ExpensesNote    = $expensesNote
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
    public List<ExpensesInstallment> GetAll()
    {
        var list = new List<ExpensesInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT e.Id, e.ExpensesNumber, e.ExpensesDate, e.ExpensesService, e.ExpensesAmount, e.ExpensesNote,
               p.ProductName
        FROM ExpensesInstallment e
        JOIN ProductsInstallment p ON p.Id = e.ProductId
        ORDER BY e.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ExpensesInstallment
            {
                Id = reader.GetInt64(0),
                ExpensesNumber = reader.GetString(1),
                ExpensesDate = reader.GetDateTime(2),
                ExpensesService = reader.GetString(3),
                ExpensesAmount = reader.GetDouble(4),
                ExpensesNote = reader.GetString(5),

                ProductName = reader.GetString(6),


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
        e.ExpensesNumber,
        e.ExpensesDate,
        e.ExpensesService,
        e.ExpensesAmount,
        e.ExpensesNote,
        e.UnitId,
        u.UnitName
    FROM ExpensesInstallment e
    JOIN Units u ON u.Id = e.UnitId
    WHERE e.Id = $id
    LIMIT 1;
    """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ExpensesRealEstate
        {
            Id = reader.GetInt64(0),
            ExpensesNumber = reader.GetString(1),
            ExpensesDate = reader.GetDateTime(2),
            ExpensesService = reader.GetString(3),
            ExpensesAmount = reader.GetDouble(4),
            ExpensesNote = reader.IsDBNull(5) ? "" : reader.GetString(5),
            UnitId = reader.GetInt64(6),
            UnitName = reader.GetString(7),
        };
    }

    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Receipts WHERE ContractID = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف العقد لأنه مرتبط بسندات مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM ExpensesInstallment WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف سند الصرف لأنه مرتبط ببيانات أخرى");
        }
    }


}
