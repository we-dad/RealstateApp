using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class OwnerInstallmentService
{
    private readonly DbServiceInstallment _db;

    public OwnerInstallmentService(DbServiceInstallment db)
    {
        _db = db;
    }

    public List<OwnerInstallment> GetAll()
    {
        var list = new List<OwnerInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, Name, IdentityNumber, Phone, Address
            FROM OwnersInstallment
            WHERE SyncAction <> 'delete'
            ORDER BY Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new OwnerInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                Name = reader.GetString(2),
                IdentityNumber = reader.GetString(3),
                Phone = reader.GetString(4),
                Address = reader.GetString(5),
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
            INSERT INTO OwnersInstallment
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

    public void Update(long id, string name, string identityNumber, string phone, string address)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE OwnersInstallment
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

    public OwnerInstallment? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, Name, IdentityNumber, Phone, Address
            FROM OwnersInstallment
            WHERE Id = $id
              AND SyncAction <> 'delete';
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new OwnerInstallment
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
            UPDATE OwnersInstallment
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
            UPDATE OwnersInstallment
            SET IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public long GetLocalIdByCloudId(long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id
            FROM OwnersInstallment
            WHERE CloudId = $cloudId
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);

        var result = cmd.ExecuteScalar();
        return result == null ? 0 : Convert.ToInt64(result);
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
            FROM OwnersInstallment
            WHERE CloudId = $cloudId;
        """;
        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE OwnersInstallment
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
                INSERT INTO OwnersInstallment
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

    public List<OwnerInstallment> GetDirtyRows()
    {
        var list = new List<OwnerInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, Name, IdentityNumber, Phone, Address, SyncAction
            FROM OwnersInstallment
            WHERE IsDirty = 1;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new OwnerInstallment
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
                    FROM ProductsInstallment
                    WHERE OwnerId = $id
                      AND SyncAction <> 'delete'
                );
            """;

            check.Parameters.AddWithValue("$id", id);

            var hasProducts = Convert.ToInt32(check.ExecuteScalar()) == 1;

            if (hasProducts)
                throw new InvalidOperationException(".لا يمكن حذف المالك لأنه مرتبط بمنتجات مسجلة");
        }

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE OwnersInstallment
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
                DELETE FROM OwnersInstallment
                WHERE Id = $id;
            """;

            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف المالك لأنه مرتبط ببيانات أخرى");
        }
    }
}