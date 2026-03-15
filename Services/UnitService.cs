using Microsoft.Data.Sqlite;
using RealEstateApp.Models;
using System;
using System.Collections.Generic;

namespace RealEstateApp.Services;

public class UnitService
{
    private readonly DbService _db;

    public UnitService(DbService db)
    {
        _db = db;
    }

    public List<Unit> GetAll()
    {
        var list = new List<Unit>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, UnitName, City, District, UnitState, UnitType FROM Units ORDER BY Id DESC;";


        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Unit
            {
                Id = reader.GetInt64(0),
                UnitName = reader.GetString(1),
                City = reader.GetString(2),
                District = reader.GetString(3),
                UnitState = reader.GetString(4),
                UnitType = reader.GetString(5),
            });
        }

        return list;
    }

    public long Add(long ownerId, string unitName, string city, string district, string unitType, int unitsCount, int unitNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        INSERT INTO Units (OwnerId, UnitName, City, District, UnitType, UnitState, UnitsCount, UnitNum)
        VALUES ($ownerId, $unitName, $city, $district, $unitType, 'شاغرة', $unitsCount, $unitNum);
        SELECT last_insert_rowid();
    """;

        cmd.Parameters.AddWithValue("$ownerId", ownerId);
        cmd.Parameters.AddWithValue("$unitName", unitName);
        cmd.Parameters.AddWithValue("$city", city);
        cmd.Parameters.AddWithValue("$district", district);
        cmd.Parameters.AddWithValue("$unitType", unitType);
        cmd.Parameters.AddWithValue("$unitsCount", unitsCount);
        cmd.Parameters.AddWithValue("$unitNum", unitNum);

        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(long id, long ownerId, string unitName, string city, string district, string unitType, int unitsCount, int unitNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        UPDATE Units
        SET OwnerId = $ownerId,
            UnitName = $unitName,
            City = $city,
            District = $district,
            UnitType = $unitType,
            UnitsCount = $unitsCount,
            UnitNum = $unitNum
        WHERE Id = $id;
    """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$ownerId", ownerId);
        cmd.Parameters.AddWithValue("$unitName", unitName);
        cmd.Parameters.AddWithValue("$city", city);
        cmd.Parameters.AddWithValue("$district", district);
        cmd.Parameters.AddWithValue("$unitType", unitType);
        cmd.Parameters.AddWithValue("$unitsCount", unitsCount);
        cmd.Parameters.AddWithValue("$unitNum", unitNum);

        cmd.ExecuteNonQuery();
    }

    public Unit? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    SELECT u.Id, u.OwnerId, o.Name, o.IdentityNumber, o.Phone, o.Address, u.UnitName, u.City, u.District, u.UnitType, u.UnitsCount, u.UnitNum, u.UnitState
    FROM Units u
    JOIN Owners o ON o.Id = u.OwnerId
    WHERE u.Id = $id;
    """;
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new Unit
        {
            Id = reader.GetInt64(0),
            OwnerId = reader.GetInt64(1),
            OwnerName = reader.GetString(2),
            OwnerIdentityNumber = reader.GetString(3),
            OwnerPhone = reader.GetString(4),
            OwnerAddress = reader.GetString(5),
            UnitName = reader.GetString(6),
            City = reader.GetString(7),
            District = reader.GetString(8),
            UnitType = reader.GetString(9),
            UnitsCount = reader.GetInt32(10),
            UnitNum = reader.GetInt32(11),
            UnitState = reader.GetString(12),
        };
    }
    public void UpdateUnitStates()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        UPDATE Units
        SET UnitState = 'شاغرة'
        WHERE Id NOT IN (
            SELECT UnitId
            FROM Contracts
            WHERE date(ContractEndDate) >= date('now','localtime')
        );

        UPDATE Units
        SET UnitState = 'مؤجرة'
        WHERE Id IN (
            SELECT UnitId
            FROM Contracts
            WHERE date(ContractEndDate) >= date('now','localtime')
        );
    """;

        cmd.ExecuteNonQuery();
    }
    public List<Unit> GetAvailableUnits()
    {
        var list = new List<Unit>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT Id, UnitName, UnitState
        FROM Units
        WHERE UnitState = 'شاغرة'
        ORDER BY UnitName;
    """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Unit
            {
                Id = reader.GetInt64(0),
                UnitName = reader.GetString(1),
                UnitState = reader.GetString(2)
            });
        }

        return list;
    }
    public List<Unit> GetAvailableUnitsIncluding(long? currentUnitId)
    {
        var list = new List<Unit>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    SELECT Id, UnitName, UnitState
    FROM Units
    WHERE UnitState = 'شاغرة'
       OR Id = $currentId
    ORDER BY UnitName;
    """;

        cmd.Parameters.AddWithValue("$currentId", currentUnitId ?? -1);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Unit
            {
                Id = reader.GetInt64(0),
                UnitName = reader.GetString(1),
                UnitState = reader.GetString(2)
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
