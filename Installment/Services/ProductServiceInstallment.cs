using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class ProductServiceInstallment
{
    private readonly DbServiceInstallment _db;

    public ProductServiceInstallment(DbServiceInstallment db)
    {
        _db = db;
    }

    public List<ProductInstallment> GetAll()
    {
        var list = new List<ProductInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, ProductName, ProductType, ProductMainPrice
            FROM ProductsInstallment
            WHERE SyncAction <> 'delete'
            ORDER BY Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ProductInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ProductName = reader.GetString(2),
                ProductType = reader.GetString(3),
                ProductMainPrice = Convert.ToSingle(reader.GetValue(4))
            });
        }

        return list;
    }

    public long Add(
        long ownerId,
        string productName,
        string productType,
        float productMainPrice,
        string carPlateNumber,
        string carVIN,
        string carModel,
        string carColor,
        string mobileStorage,
        string mobileColor)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ProductsInstallment
            (
                OwnerId,
                ProductName,
                ProductType,
                ProductMainPrice,
                CarPlateNumber,
                CarVIN,
                CarModel,
                CarColor,
                MobileStorage,
                MobileColor,
                IsDirty,
                SyncAction
            )
            VALUES
            (
                $ownerId,
                $productName,
                $productType,
                $productMainPrice,
                $carPlateNumber,
                $carVIN,
                $carModel,
                $carColor,
                $mobileStorage,
                $mobileColor,
                1,
                'insert'
            );

            SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$ownerId", ownerId);
        cmd.Parameters.AddWithValue("$productName", productName);
        cmd.Parameters.AddWithValue("$productType", productType);
        cmd.Parameters.AddWithValue("$productMainPrice", productMainPrice);
        cmd.Parameters.AddWithValue("$carPlateNumber", carPlateNumber);
        cmd.Parameters.AddWithValue("$carVIN", carVIN);
        cmd.Parameters.AddWithValue("$carModel", carModel);
        cmd.Parameters.AddWithValue("$carColor", carColor);
        cmd.Parameters.AddWithValue("$mobileStorage", mobileStorage);
        cmd.Parameters.AddWithValue("$mobileColor", mobileColor);

        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(
        long id,
        long ownerId,
        string productName,
        string productType,
        float productMainPrice,
        string carPlateNumber,
        string carVIN,
        string carModel,
        string carColor,
        string mobileStorage,
        string mobileColor)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ProductsInstallment
            SET OwnerId = $ownerId,
                ProductName = $productName,
                ProductType = $productType,
                ProductMainPrice = $productMainPrice,
                CarPlateNumber = $carPlateNumber,
                CarVIN = $carVIN,
                CarModel = $carModel,
                CarColor = $carColor,
                MobileStorage = $mobileStorage,
                MobileColor = $mobileColor,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$ownerId", ownerId);
        cmd.Parameters.AddWithValue("$productName", productName);
        cmd.Parameters.AddWithValue("$productType", productType);
        cmd.Parameters.AddWithValue("$productMainPrice", productMainPrice);
        cmd.Parameters.AddWithValue("$carPlateNumber", carPlateNumber);
        cmd.Parameters.AddWithValue("$carVIN", carVIN);
        cmd.Parameters.AddWithValue("$carModel", carModel);
        cmd.Parameters.AddWithValue("$carColor", carColor);
        cmd.Parameters.AddWithValue("$mobileStorage", mobileStorage);
        cmd.Parameters.AddWithValue("$mobileColor", mobileColor);

        cmd.ExecuteNonQuery();
    }

    public ProductInstallment? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
                p.Id,
                p.CloudId,
                p.OwnerId,
                o.CloudId,
                o.Name,
                o.IdentityNumber,
                o.Phone,
                o.Address,
                p.ProductName,
                p.ProductType,
                p.ProductMainPrice,
                p.CarPlateNumber,
                p.CarVIN,
                p.CarModel,
                p.CarColor,
                p.MobileStorage,
                p.MobileColor
            FROM ProductsInstallment p
            JOIN OwnersInstallment o ON o.Id = p.OwnerId
            WHERE p.Id = $id
              AND p.SyncAction <> 'delete';
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ProductInstallment
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            OwnerId = reader.GetInt64(2),
            OwnerCloudId = reader.GetInt64(3),
            OwnerName = reader.GetString(4),
            OwnerIdentityNumber = reader.GetString(5),
            OwnerPhone = reader.GetString(6),
            OwnerAddress = reader.GetString(7),
            ProductName = reader.GetString(8),
            ProductType = reader.GetString(9),
            ProductMainPrice = Convert.ToSingle(reader.GetValue(10)),
            CarPlateNumber = reader.GetString(11),
            CarVIN = reader.GetString(12),
            CarModel = reader.GetString(13),
            CarColor = reader.GetString(14),
            MobileStorage = reader.GetString(15),
            MobileColor = reader.GetString(16),
        };
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ProductsInstallment
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
            UPDATE ProductsInstallment
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
            FROM ProductsInstallment
            WHERE CloudId = $cloudId
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);

        var result = cmd.ExecuteScalar();
        return result == null ? 0 : Convert.ToInt64(result);
    }

    public void UpsertFromCloud(
        long cloudId,
        long ownerLocalId,
        string productName,
        string productType,
        double productMainPrice,
        string carPlateNumber,
        string carVIN,
        string carModel,
        string carColor,
        string mobileStorage,
        string mobileColor)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM ProductsInstallment
            WHERE CloudId = $cloudId;
        """;

        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE ProductsInstallment
                SET OwnerId = $ownerId,
                    ProductName = $productName,
                    ProductType = $productType,
                    ProductMainPrice = $productMainPrice,
                    CarPlateNumber = $carPlateNumber,
                    CarVIN = $carVIN,
                    CarModel = $carModel,
                    CarColor = $carColor,
                    MobileStorage = $mobileStorage,
                    MobileColor = $mobileColor
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$ownerId", ownerLocalId);
            update.Parameters.AddWithValue("$productName", productName);
            update.Parameters.AddWithValue("$productType", productType);
            update.Parameters.AddWithValue("$productMainPrice", productMainPrice);
            update.Parameters.AddWithValue("$carPlateNumber", carPlateNumber);
            update.Parameters.AddWithValue("$carVIN", carVIN);
            update.Parameters.AddWithValue("$carModel", carModel);
            update.Parameters.AddWithValue("$carColor", carColor);
            update.Parameters.AddWithValue("$mobileStorage", mobileStorage);
            update.Parameters.AddWithValue("$mobileColor", mobileColor);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO ProductsInstallment
                (
                    CloudId,
                    OwnerId,
                    ProductName,
                    ProductType,
                    ProductMainPrice,
                    CarPlateNumber,
                    CarVIN,
                    CarModel,
                    CarColor,
                    MobileStorage,
                    MobileColor,
                    IsDirty,
                    SyncAction
                )
                VALUES
                (
                    $cloudId,
                    $ownerId,
                    $productName,
                    $productType,
                    $productMainPrice,
                    $carPlateNumber,
                    $carVIN,
                    $carModel,
                    $carColor,
                    $mobileStorage,
                    $mobileColor,
                    0,
                    ''
                );
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$ownerId", ownerLocalId);
            insert.Parameters.AddWithValue("$productName", productName);
            insert.Parameters.AddWithValue("$productType", productType);
            insert.Parameters.AddWithValue("$productMainPrice", productMainPrice);
            insert.Parameters.AddWithValue("$carPlateNumber", carPlateNumber);
            insert.Parameters.AddWithValue("$carVIN", carVIN);
            insert.Parameters.AddWithValue("$carModel", carModel);
            insert.Parameters.AddWithValue("$carColor", carColor);
            insert.Parameters.AddWithValue("$mobileStorage", mobileStorage);
            insert.Parameters.AddWithValue("$mobileColor", mobileColor);

            insert.ExecuteNonQuery();
        }
    }

    public List<ProductInstallment> GetDirtyRows()
    {
        var list = new List<ProductInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
                p.Id,
                p.CloudId,
                p.OwnerId,
                o.CloudId,
                p.ProductName,
                p.ProductType,
                p.ProductMainPrice,
                p.CarPlateNumber,
                p.CarVIN,
                p.CarModel,
                p.CarColor,
                p.MobileStorage,
                p.MobileColor,
                p.SyncAction
            FROM ProductsInstallment p
        LEFT JOIN OwnersInstallment o ON o.Id = p.OwnerId
            WHERE p.IsDirty = 1
              AND (
                    p.SyncAction = 'delete'
                    OR o.CloudId > 0
                  );
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ProductInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                OwnerId = reader.GetInt64(2),
                OwnerCloudId = reader.GetInt64(3),
                ProductName = reader.GetString(4),
                ProductType = reader.GetString(5),
                ProductMainPrice = Convert.ToSingle(reader.GetValue(6)),
                CarPlateNumber = reader.GetString(7),
                CarVIN = reader.GetString(8),
                CarModel = reader.GetString(9),
                CarColor = reader.GetString(10),
                MobileStorage = reader.GetString(11),
                MobileColor = reader.GetString(12),
                SyncAction = reader.GetString(13)
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
                    FROM ContractsInstallment
                    WHERE ProductId = $id
                      AND SyncAction <> 'delete'
                );
            """;

            check.Parameters.AddWithValue("$id", id);

            var hasContracts = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasContracts)
                throw new InvalidOperationException(".لا يمكن حذف المنتج لأنه مرتبط بعقود مسجلة");
        }

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ProductsInstallment
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
                DELETE FROM ProductsInstallment
                WHERE Id = $id;
            """;

            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف المنتج لأنه مرتبط ببيانات أخرى");
        }
    }
}