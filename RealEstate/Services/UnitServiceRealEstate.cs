using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class UnitServiceRealEstate
{
    private readonly DbServiceRealEstate _db;

    public UnitServiceRealEstate(DbServiceRealEstate db)
    {
        _db = db;
    }

    public List<UnitRealEstate> GetAll()
    {
        var list = new List<UnitRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, UnitName, City, District, UnitState, UnitType
            FROM UnitsRealEstate
            WHERE SyncAction <> 'delete'
            ORDER BY Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new UnitRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                UnitName = reader.GetString(2),
                City = reader.GetString(3),
                District = reader.GetString(4),
                UnitState = reader.GetString(5),
                UnitType = reader.GetString(6),
            });
        }

        return list;
    }

    public long Add(
        long ownerId,
        string unitName,
        string city,
        string district,
        string unitType,
        int unitsCount,
        int unitNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            INSERT INTO UnitsRealEstate
            (OwnerId, UnitName, City, District, UnitType, UnitsCount, UnitNum, IsDirty, SyncAction)
            VALUES
            ($ownerId, $unitName, $city, $district, $unitType, $unitsCount, $unitNum, 1, 'insert');

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

    public void Update(
        long id,
        long ownerId,
        string unitName,
        string city,
        string district,
        string unitType,
        int unitsCount,
        int unitNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE UnitsRealEstate
            SET OwnerId = $ownerId,
                UnitName = $unitName,
                City = $city,
                District = $district,
                UnitType = $unitType,
                UnitsCount = $unitsCount,
                UnitNum = $unitNum,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
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

    public UnitRealEstate? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
                u.Id,
                u.CloudId,
                u.OwnerId,
                o.CloudId,
                o.Name,
                o.IdentityNumber,
                o.Phone,
                o.Address,
                u.UnitName,
                u.City,
                u.District,
                u.UnitType,
                u.UnitsCount,
                u.UnitNum,
                u.UnitState
            FROM UnitsRealEstate u
            JOIN OwnersRealEstate o ON o.Id = u.OwnerId
            WHERE u.Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new UnitRealEstate
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            OwnerId = reader.GetInt64(2),
            OwnerCloudId = reader.GetInt64(3),
            OwnerName = reader.GetString(4),
            OwnerIdentityNumber = reader.GetString(5),
            OwnerPhone = reader.GetString(6),
            OwnerAddress = reader.GetString(7),
            UnitName = reader.GetString(8),
            City = reader.GetString(9),
            District = reader.GetString(10),
            UnitType = reader.GetString(11),
            UnitsCount = reader.GetInt32(12),
            UnitNum = reader.GetInt32(13),
            UnitState = reader.GetString(14),
        };
    }

    public void UpdateUnitStates()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
                              UPDATE UnitsRealEstate
                              SET UnitState = 'شاغرة',
                                  IsDirty = 1,
                                  SyncAction = CASE
                                      WHEN SyncAction = 'insert' THEN 'insert'
                                      ELSE 'update'
                                  END
                              WHERE SyncAction <> 'delete'
                                AND UnitState <> 'شاغرة'
                                AND Id NOT IN (
                                  SELECT UnitId
                                  FROM ContractsRealEstate
                                  WHERE date(ContractEndDate) >= date('now','localtime')
                                    AND SyncAction <> 'delete'
                              );

                              UPDATE UnitsRealEstate
                              SET UnitState = 'مؤجرة',
                                  IsDirty = 1,
                                  SyncAction = CASE
                                      WHEN SyncAction = 'insert' THEN 'insert'
                                      ELSE 'update'
                                  END
                              WHERE SyncAction <> 'delete'
                                AND UnitState <> 'مؤجرة'
                                AND Id IN (
                                  SELECT UnitId
                                  FROM ContractsRealEstate
                                  WHERE date(ContractEndDate) >= date('now','localtime')
                                    AND SyncAction <> 'delete'
                              );
                          """;

        cmd.ExecuteNonQuery();
    }

    public List<UnitRealEstate> GetAvailableUnits()
    {
        var list = new List<UnitRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, UnitName, UnitState
            FROM UnitsRealEstate
            WHERE UnitState = 'شاغرة'
              AND SyncAction <> 'delete'
            ORDER BY UnitName;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new UnitRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                UnitName = reader.GetString(2),
                UnitState = reader.GetString(3)
            });
        }

        return list;
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            UPDATE UnitsRealEstate
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
            UPDATE UnitsRealEstate
            SET IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<UnitRealEstate> GetDirtyRows()
    {
        var list = new List<UnitRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
                Id,
                CloudId,
                OwnerId,
                UnitName,
                City,
                District,
                UnitType,
                UnitState,
                UnitsCount,
                UnitNum,
                SyncAction
            FROM UnitsRealEstate
            WHERE IsDirty = 1;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new UnitRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                OwnerId = reader.GetInt64(2),
                UnitName = reader.GetString(3),
                City = reader.GetString(4),
                District = reader.GetString(5),
                UnitType = reader.GetString(6),
                UnitState = reader.GetString(7),
                UnitsCount = reader.GetInt32(8),
                UnitNum = reader.GetInt32(9),
                SyncAction = reader.GetString(10)
            });
        }

        return list;
    }

    public long GetLocalIdByCloudId(long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id
            FROM UnitsRealEstate
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
        string unitName,
        string city,
        string district,
        string unitType,
        string unitState,
        int unitsCount,
        int unitNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM UnitsRealEstate
            WHERE CloudId = $cloudId;
        """;
        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE UnitsRealEstate
                SET OwnerId = $ownerId,
                    UnitName = $unitName,
                    City = $city,
                    District = $district,
                    UnitType = $unitType,
                    UnitState = $unitState,
                    UnitsCount = $unitsCount,
                    UnitNum = $unitNum
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$ownerId", ownerLocalId);
            update.Parameters.AddWithValue("$unitName", unitName);
            update.Parameters.AddWithValue("$city", city);
            update.Parameters.AddWithValue("$district", district);
            update.Parameters.AddWithValue("$unitType", unitType);
            update.Parameters.AddWithValue("$unitState", unitState);
            update.Parameters.AddWithValue("$unitsCount", unitsCount);
            update.Parameters.AddWithValue("$unitNum", unitNum);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO UnitsRealEstate
                (CloudId, OwnerId, UnitName, City, District, UnitType, UnitState, UnitsCount, UnitNum, IsDirty, SyncAction)
                VALUES
                ($cloudId, $ownerId, $unitName, $city, $district, $unitType, $unitState, $unitsCount, $unitNum, 0, '');
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$ownerId", ownerLocalId);
            insert.Parameters.AddWithValue("$unitName", unitName);
            insert.Parameters.AddWithValue("$city", city);
            insert.Parameters.AddWithValue("$district", district);
            insert.Parameters.AddWithValue("$unitType", unitType);
            insert.Parameters.AddWithValue("$unitState", unitState);
            insert.Parameters.AddWithValue("$unitsCount", unitsCount);
            insert.Parameters.AddWithValue("$unitNum", unitNum);

            insert.ExecuteNonQuery();
        }
    }

    public List<UnitRealEstate> GetAvailableUnitsIncluding(long? currentUnitId)
    {
        var list = new List<UnitRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, UnitName, UnitState
            FROM UnitsRealEstate
            WHERE SyncAction <> 'delete'
              AND (UnitState = 'شاغرة' OR Id = $currentId)
            ORDER BY UnitName;
        """;

        cmd.Parameters.AddWithValue("$currentId", currentUnitId ?? -1);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new UnitRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                UnitName = reader.GetString(2),
                UnitState = reader.GetString(3)
            });
        }

        return list;
    }
    
    public List<ContractRealEstate> GetContractsByUnitId(long unitId)
    {
        var list = new List<ContractRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
                              SELECT
                                  c.Id,
                                  c.CloudId,
                                  c.ContractNumber,
                                  c.ContractStartDate,
                                  c.ContractEndDate,
                                  c.RentAmount,
                                  c.ContractState,
                                  c.ContractPayMethod,
                                  t.Name
                              FROM ContractsRealEstate c
                              JOIN TenantsRealEstate t ON t.Id = c.TenantId
                              WHERE c.UnitId = $unitId
                                AND c.SyncAction <> 'delete'
                              ORDER BY c.Id DESC;
                          """;

        cmd.Parameters.AddWithValue("$unitId", unitId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ContractRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ContractNumber = reader.GetString(2),
                ContractStartDate = DateTime.Parse(reader.GetString(3)),
                ContractEndDate = DateTime.Parse(reader.GetString(4)),
                RentAmount = reader.GetDouble(5),
                ContractState = reader.GetString(6),
                ContractPayMethod = reader.GetString(7),
                TenantName = reader.GetString(8)
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
                    FROM ContractsRealEstate
                    WHERE UnitId = $id
                      AND SyncAction <> 'delete'
                );
            """;
            check.Parameters.AddWithValue("$id", id);

            var hasContracts = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasContracts)
                throw new InvalidOperationException(".لا يمكن حذف الوحدة لأنه مرتبط بعقود أو سندات صرف مسجلة");
        }

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE UnitsRealEstate
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
            DELETE FROM UnitsRealEstate
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}