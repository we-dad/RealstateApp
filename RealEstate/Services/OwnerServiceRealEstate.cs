using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;


namespace RealEstateInstallmentsManager.Services;

public class OwnerServiceRealEstate
{
    private readonly DbServiceRealEstate _db;

    public OwnerServiceRealEstate(DbServiceRealEstate db)
    {
        _db = db;
    }

    public List<OwnerRealEstate> GetAll()
    {
        var list = new List<OwnerRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, IdentityNumber, Phone, Address FROM Owners ORDER BY Id DESC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new OwnerRealEstate
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
    INSERT INTO Owners (Name, Phone, IdentityNumber, Address)
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
    UPDATE Owners
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

    public OwnerRealEstate? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT Id, Name, IdentityNumber, Phone, Address
        FROM Owners
        WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null; // not found

        return new OwnerRealEstate
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
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Units WHERE OwnerId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف المالك لأنه مرتبط بوحدات مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Owners WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف المالك لأنه مرتبط ببيانات أخرى");
        }
    }
}
