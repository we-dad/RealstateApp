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
        cmd.CommandText = "SELECT Id, ProductName, ProductType, ProductMainPrice FROM ProductsInstallment ORDER BY Id DESC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ProductInstallment
            {
                Id = reader.GetInt64(0),
                ProductName = reader.GetString(1),
                ProductType = reader.GetString(2),
                ProductMainPrice = Convert.ToSingle(reader.GetValue(3))
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
                              MobileColor
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
                              $mobileColor
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
                              MobileColor = $mobileColor
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
                              p.OwnerId,
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
                          WHERE p.Id = $id;
                          """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ProductInstallment
        {
            Id = reader.GetInt64(0),
            OwnerId = reader.GetInt64(1),
            OwnerName = reader.GetString(2),
            OwnerIdentityNumber = reader.GetString(3),
            OwnerPhone = reader.GetString(4),
            OwnerAddress = reader.GetString(5),
            ProductName = reader.GetString(6),
            ProductType = reader.GetString(7),
            ProductMainPrice = reader.GetFloat(8),

            CarPlateNumber = reader.GetString(9),
            CarVIN = reader.GetString(10),
            CarModel = reader.GetString(11),
            CarColor = reader.GetString(12),

            MobileStorage = reader.GetString(13),
            MobileColor = reader.GetString(14),
        };
    }

    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Contracts WHERE UnitId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف الوحدة لأنه مرتبط بعقود أو سندات صرف مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Units WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف الوحدة لأنه مرتبط ببيانات أخرى");
        }
    }

}
