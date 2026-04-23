using Microsoft.Data.Sqlite;
using RealEstateApp.Models;
using System;
using System.Collections.Generic;


namespace RealEstateApp.Services;

public class OwnerInstallmentService
{
    private readonly InstallmentDbService _db;

    public OwnerInstallmentService(InstallmentDbService db)
    {
        _db = db;
    }

    public List<OwnerInstallment> GetAll()
    {
        var list = new List<OwnerInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, IdentityNumber, Phone, Address FROM OwnersInstallment ORDER BY Id DESC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new OwnerInstallment
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

    public long Add(string name, string identityNumber, string phone, string address)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    INSERT INTO OwnersInstallment (Name, Phone, IdentityNumber, Address)
    VALUES ($name, $identityNumber, $phone, $address);
    SELECT last_insert_rowid();
    """;
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$identityNumber", identityNumber);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$address", address);

        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(long id, string name, string identityNumber, string phone, string address)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    UPDATE OwnersInstallment
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

    public OwnerInstallment? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT Id, Name, IdentityNumber, Phone, Address
        FROM OwnersInstallment
        WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null; // not found

        return new OwnerInstallment
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
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Units WHERE InstallmentOwnerId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف المالك لأنه مرتبط بوحدات مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM OwnersInstallment WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف المالك لأنه مرتبط ببيانات أخرى");
        }
    }
}
