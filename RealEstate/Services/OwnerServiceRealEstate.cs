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
        cmd.CommandText = """
            SELECT Id, CloudId, Name, IdentityNumber, Phone, Address
            FROM OwnersRealEstate
            WHERE SyncAction <> 'delete'
            ORDER BY Id DESC;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new OwnerRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                Name = reader.GetString(2),
                IdentityNumber = reader.GetString(3),
                Phone = reader.GetString(4),
                Address = reader.GetString(5)
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
            INSERT INTO OwnersRealEstate
            (Name, IdentityNumber, Phone, Address, IsDirty, SyncAction)
            VALUES
            ($name, $identityNumber, $phone, $address, 1, 'insert');

            SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$identityNumber", identityNumber);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$address", address);

        return (long)cmd.ExecuteScalar()!;
    }

    public void MarkSynced(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE OwnersRealEstate
            SET IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void Update(long id, string name, string identityNumber, string phone, string address)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            UPDATE OwnersRealEstate
            SET Name = $name,
                IdentityNumber = $identityNumber,
                Phone = $phone,
                Address = $address,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
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
            SELECT Id, CloudId, Name, IdentityNumber, Phone, Address
            FROM OwnersRealEstate
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new OwnerRealEstate
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            Name = reader.GetString(2),
            IdentityNumber = reader.GetString(3),
            Phone = reader.GetString(4),
            Address = reader.GetString(5)
        };
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            UPDATE OwnersRealEstate
            SET CloudId = $cloudId,
                IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public void UpsertFromCloud(
        long cloudId,
        string name,
        string identityNumber,
        string phone,
        string address)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM OwnersRealEstate
            WHERE CloudId = $cloudId;
        """;
        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE OwnersRealEstate
                SET Name = $name,
                    IdentityNumber = $identityNumber,
                    Phone = $phone,
                    Address = $address
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$name", name);
            update.Parameters.AddWithValue("$identityNumber", identityNumber);
            update.Parameters.AddWithValue("$phone", phone);
            update.Parameters.AddWithValue("$address", address);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO OwnersRealEstate
                (CloudId, Name, IdentityNumber, Phone, Address, IsDirty, SyncAction)
                VALUES
                ($cloudId, $name, $identityNumber, $phone, $address, 0, '');
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$name", name);
            insert.Parameters.AddWithValue("$identityNumber", identityNumber);
            insert.Parameters.AddWithValue("$phone", phone);
            insert.Parameters.AddWithValue("$address", address);

            insert.ExecuteNonQuery();
        }
    }

    public long GetLocalIdByCloudId(long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            SELECT Id
            FROM OwnersRealEstate
            WHERE CloudId = $cloudId
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);

        var result = cmd.ExecuteScalar();

        if (result == null)
            return 0;

        return Convert.ToInt64(result);
    }
    
    public List<OwnerRealEstate> GetDirtyRows()
    {
        var list = new List<OwnerRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
                              SELECT Id, CloudId, Name, IdentityNumber, Phone, Address, SyncAction
                              FROM OwnersRealEstate
                              WHERE IsDirty = 1;
                          """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new OwnerRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                Name = reader.GetString(2),
                IdentityNumber = reader.GetString(3),
                Phone = reader.GetString(4),
                Address = reader.GetString(5),
                SyncAction = reader.GetString(6)
            });
        }

        return list;
    }

    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = """
                SELECT EXISTS(
                    SELECT 1
                    FROM UnitsRealEstate
                    WHERE OwnerId = $id
                      AND SyncAction <> 'delete'
                );
            """;

            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;

            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف المالك لأنه مرتبط بوحدات مسجلة");
        }

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            UPDATE OwnersRealEstate
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

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            DELETE FROM OwnersRealEstate
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}