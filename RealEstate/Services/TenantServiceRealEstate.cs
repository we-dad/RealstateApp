using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class TenantServiceRealEstate
{
    private readonly DbServiceRealEstate _db;

    public TenantServiceRealEstate(DbServiceRealEstate db)
    {
        _db = db;
    }

    public List<TenantRealEstate> GetAll()
    {
        var list = new List<TenantRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT Id, Name, IdentityNumber, Phone, Address
        FROM Tenants
        ORDER BY Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TenantRealEstate
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                IdentityNumber = reader.GetString(2),
                Phone = reader.GetString(3),
                Address = reader.GetString(4),
            });
        }

        return list;
    }

    public void Add(string name, string idNumber, string phone, string address)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        INSERT INTO Tenants (Name, IdentityNumber, Phone, Address)
        VALUES ($name, $id, $phone, $address);
        """;
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$id", idNumber);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$address", address);

        cmd.ExecuteNonQuery();
    }
    public TenantRealEstate? FindByIdentity(string identityNumber)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    SELECT Id, Name, IdentityNumber, Phone, Address
    FROM Tenants
    WHERE IdentityNumber = $id
    LIMIT 1;
    """;
        cmd.Parameters.AddWithValue("$id", identityNumber);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new TenantRealEstate
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            IdentityNumber = reader.GetString(2),
            Phone = reader.GetString(3),
            Address = reader.GetString(4),
        };
    }

    public void Update(long id, string name, string identityNumber, string phone, string address)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    UPDATE Tenants
    SET Name  = $name,
    IdentityNumber =$identityNumber,
    Phone = $phone,
    Address = $address
    WHERE Id = $id;
""";

        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$identityNumber", identityNumber);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$address", address);

        cmd.ExecuteNonQuery();
    }

    public TenantRealEstate? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT Id, Name, IdentityNumber, Phone, Address
        FROM Tenants
        WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null; // not found

        return new TenantRealEstate
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            IdentityNumber = reader.GetString(2),
            Phone = reader.GetString(3),
            Address = reader.GetString(4)
        };

    }

    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Contracts WHERE TenantId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف المستأجر لأنه مرتبط بعقود مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Tanents WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف المستأجر لأنه مرتبط ببيانات أخرى");
        }
    }
}