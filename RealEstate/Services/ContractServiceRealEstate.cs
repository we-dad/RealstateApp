using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class ContractServiceRealEstate
{
    private readonly DbServiceRealEstate _db;

    public ContractServiceRealEstate(DbServiceRealEstate db)
    {
        _db = db;
    }

    public string GenerateContractNumber()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT COALESCE(MAX(CAST(SUBSTR(ContractNumber, 4) AS INTEGER)), 999) + 1
            FROM ContractsRealEstate
            WHERE ContractNumber LIKE 'Rc-%';
        """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "Rc-" + next;
    }

    public long Add(
        string contractNumber,
        DateTime contractStartDate,
        DateTime contractEndDate,
        long unitId,
        long tenantId,
        double rentAmount,
        string contractPayMethod,
        string contractApartmentType,
        int contractUnitRoomsNum,
        int contractUnitFloorNum,
        string contractOpligation)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        var contractState = contractEndDate.Date < DateTime.Today ? "منتهي" : "جاري";

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ContractsRealEstate (
                ContractNumber,
                ContractStartDate,
                ContractEndDate,
                UnitId,
                TenantId,
                RentAmount,
                ContractPayMethod,
                ContractApartmentType,
                ContractUnitRoomsNum,
                ContractUnitFloorNum,
                ContractOpligation,
                ContractState,
                IsDirty,
                SyncAction
            )
            VALUES (
                $contractNumber,
                $contractStartDate,
                $contractEndDate,
                $unitId,
                $tenantId,
                $rent,
                $contractPayMethod,
                $contractApartmentType,
                $contractUnitRoomsNum,
                $contractUnitFloorNum,
                $contractOpligation,
                $contractState,
                1,
                'insert'
            );

            SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$contractNumber", contractNumber);
        cmd.Parameters.AddWithValue("$contractStartDate", contractStartDate);
        cmd.Parameters.AddWithValue("$contractEndDate", contractEndDate);
        cmd.Parameters.AddWithValue("$unitId", unitId);
        cmd.Parameters.AddWithValue("$tenantId", tenantId);
        cmd.Parameters.AddWithValue("$rent", rentAmount);
        cmd.Parameters.AddWithValue("$contractPayMethod", contractPayMethod);
        cmd.Parameters.AddWithValue("$contractApartmentType", contractApartmentType);
        cmd.Parameters.AddWithValue("$contractUnitRoomsNum", contractUnitRoomsNum);
        cmd.Parameters.AddWithValue("$contractUnitFloorNum", contractUnitFloorNum);
        cmd.Parameters.AddWithValue("$contractOpligation", contractOpligation);
        cmd.Parameters.AddWithValue("$contractState", contractState);

        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(
        long contractId,
        DateTime contractStartDate,
        DateTime contractEndDate,
        long unitId,
        long tenantId,
        double rentAmount,
        string contractPayMethod,
        string contractApartmentType,
        int contractUnitRoomsNum,
        int contractUnitFloorNum,
        string contractOpligation)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        var contractState = contractEndDate.Date < DateTime.Today ? "منتهي" : "جاري";

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ContractsRealEstate
            SET ContractStartDate = $contractStartDate,
                ContractEndDate = $contractEndDate,
                UnitId = $unitId,
                TenantId = $tenantId,
                RentAmount = $rent,
                ContractPayMethod = $contractPayMethod,
                ContractApartmentType = $contractApartmentType,
                ContractUnitRoomsNum = $contractUnitRoomsNum,
                ContractUnitFloorNum = $contractUnitFloorNum,
                ContractOpligation = $contractOpligation,
                ContractState = $contractState,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE Id = $contractId;
        """;

        cmd.Parameters.AddWithValue("$contractId", contractId);
        cmd.Parameters.AddWithValue("$contractStartDate", contractStartDate);
        cmd.Parameters.AddWithValue("$contractEndDate", contractEndDate);
        cmd.Parameters.AddWithValue("$unitId", unitId);
        cmd.Parameters.AddWithValue("$tenantId", tenantId);
        cmd.Parameters.AddWithValue("$rent", rentAmount);
        cmd.Parameters.AddWithValue("$contractPayMethod", contractPayMethod);
        cmd.Parameters.AddWithValue("$contractApartmentType", contractApartmentType);
        cmd.Parameters.AddWithValue("$contractUnitRoomsNum", contractUnitRoomsNum);
        cmd.Parameters.AddWithValue("$contractUnitFloorNum", contractUnitFloorNum);
        cmd.Parameters.AddWithValue("$contractOpligation", contractOpligation);
        cmd.Parameters.AddWithValue("$contractState", contractState);

        cmd.ExecuteNonQuery();
    }

    public List<ContractRealEstate> GetAll()
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
                c.ContractState,
                u.UnitName
            FROM ContractsRealEstate c
            JOIN UnitsRealEstate u ON u.Id = c.UnitId
            WHERE c.SyncAction <> 'delete'
            ORDER BY c.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ContractRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ContractNumber = reader.GetString(2),
                ContractStartDate = reader.GetDateTime(3),
                ContractEndDate = reader.GetDateTime(4),
                ContractState = reader.GetString(5),
                UnitName = reader.GetString(6),
            });
        }

        return list;
    }

    public ContractRealEstate? GetById(long id)
    {
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
                c.UnitId,
                c.TenantId,
                c.ContractPayMethod,
                c.ContractApartmentType,
                c.ContractUnitRoomsNum,
                c.ContractUnitFloorNum,
                c.ContractOpligation,

                u.OwnerId,
                u.UnitName,
                u.City,
                u.District,
                u.UnitType,
                u.UnitsCount,
                u.UnitNum,

                o.Name,
                o.IdentityNumber,
                o.Phone,
                o.Address,

                t.Name,
                t.IdentityNumber,
                t.Phone,
                t.Address

            FROM ContractsRealEstate c
            JOIN UnitsRealEstate u ON u.Id = c.UnitId
            JOIN OwnersRealEstate o ON o.Id = u.OwnerId
            JOIN TenantsRealEstate t ON t.Id = c.TenantId
            WHERE c.Id = $id
              AND c.SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ContractRealEstate
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            ContractNumber = reader.GetString(2),
            ContractStartDate = reader.GetDateTime(3),
            ContractEndDate = reader.GetDateTime(4),
            RentAmount = reader.GetDouble(5),
            ContractState = reader.GetString(6),

            UnitId = reader.GetInt64(7),
            TenantId = reader.GetInt64(8),

            ContractPayMethod = reader.GetString(9),
            ContractApartmentType = reader.GetString(10),
            ContractUnitRoomsNum = reader.GetInt32(11),
            ContractUnitFloorNum = reader.GetInt32(12),
            ContractOpligation = reader.GetString(13),

            OwnerId = reader.GetInt64(14),
            UnitName = reader.GetString(15),
            City = reader.GetString(16),
            District = reader.GetString(17),
            UnitType = reader.GetString(18),
            UnitsCount = reader.GetInt32(19),
            UnitNum = reader.GetInt32(20),

            OwnerName = reader.GetString(21),
            OwnerIdentityNumber = reader.GetString(22),
            OwnerPhone = reader.GetString(23),
            OwnerAddress = reader.GetString(24),

            TenantName = reader.GetString(25),
            TenantIdentityNumber = reader.GetString(26),
            TenantPhone = reader.GetString(27),
            TenantAddress = reader.GetString(28),
        };
    }

    public void UpdateContractStates()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ContractsRealEstate
            SET ContractState = 'منتهي',
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE date(ContractEndDate) < date('now')
              AND ContractState <> 'منتهي'
              AND SyncAction <> 'delete';

            UPDATE ContractsRealEstate
            SET ContractState = 'جاري',
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE date(ContractEndDate) >= date('now')
              AND ContractState <> 'جاري'
              AND SyncAction <> 'delete';
        """;

        cmd.ExecuteNonQuery();
    }

    public ContractRealEstate? FindByContractNum(string contractNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
                c.Id,
                c.CloudId,
                c.UnitId,
                c.TenantId,
                t.Name,
                u.UnitName
            FROM ContractsRealEstate c
            JOIN UnitsRealEstate u ON u.Id = c.UnitId
            JOIN TenantsRealEstate t ON t.Id = c.TenantId
            WHERE c.ContractNumber = $contractNum
              AND c.SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$contractNum", contractNum);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ContractRealEstate
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            UnitId = reader.GetInt64(2),
            TenantId = reader.GetInt64(3),
            TenantName = reader.GetString(4),
            UnitName = reader.GetString(5),
        };
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ContractsRealEstate
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
            UPDATE ContractsRealEstate
            SET IsDirty = 0,
                SyncAction = ''
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpsertFromCloud(
        long cloudId,
        string contractNumber,
        DateTime contractStartDate,
        DateTime contractEndDate,
        long unitLocalId,
        long tenantLocalId,
        double rentAmount,
        string contractState,
        string contractPayMethod,
        string contractApartmentType,
        int contractUnitRoomsNum,
        int contractUnitFloorNum,
        string contractOpligation)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM ContractsRealEstate
            WHERE CloudId = $cloudId;
        """;

        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE ContractsRealEstate
                SET ContractNumber = $contractNumber,
                    ContractStartDate = $contractStartDate,
                    ContractEndDate = $contractEndDate,
                    UnitId = $unitId,
                    TenantId = $tenantId,
                    RentAmount = $rentAmount,
                    ContractState = $contractState,
                    ContractPayMethod = $contractPayMethod,
                    ContractApartmentType = $contractApartmentType,
                    ContractUnitRoomsNum = $contractUnitRoomsNum,
                    ContractUnitFloorNum = $contractUnitFloorNum,
                    ContractOpligation = $contractOpligation
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$contractNumber", contractNumber);
            update.Parameters.AddWithValue("$contractStartDate", contractStartDate);
            update.Parameters.AddWithValue("$contractEndDate", contractEndDate);
            update.Parameters.AddWithValue("$unitId", unitLocalId);
            update.Parameters.AddWithValue("$tenantId", tenantLocalId);
            update.Parameters.AddWithValue("$rentAmount", rentAmount);
            update.Parameters.AddWithValue("$contractState", contractState);
            update.Parameters.AddWithValue("$contractPayMethod", contractPayMethod);
            update.Parameters.AddWithValue("$contractApartmentType", contractApartmentType);
            update.Parameters.AddWithValue("$contractUnitRoomsNum", contractUnitRoomsNum);
            update.Parameters.AddWithValue("$contractUnitFloorNum", contractUnitFloorNum);
            update.Parameters.AddWithValue("$contractOpligation", contractOpligation);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO ContractsRealEstate
                (
                    CloudId,
                    ContractNumber,
                    ContractStartDate,
                    ContractEndDate,
                    UnitId,
                    TenantId,
                    RentAmount,
                    ContractState,
                    ContractPayMethod,
                    ContractApartmentType,
                    ContractUnitRoomsNum,
                    ContractUnitFloorNum,
                    ContractOpligation,
                    IsDirty,
                    SyncAction
                )
                VALUES
                (
                    $cloudId,
                    $contractNumber,
                    $contractStartDate,
                    $contractEndDate,
                    $unitId,
                    $tenantId,
                    $rentAmount,
                    $contractState,
                    $contractPayMethod,
                    $contractApartmentType,
                    $contractUnitRoomsNum,
                    $contractUnitFloorNum,
                    $contractOpligation,
                    0,
                    ''
                );
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$contractNumber", contractNumber);
            insert.Parameters.AddWithValue("$contractStartDate", contractStartDate);
            insert.Parameters.AddWithValue("$contractEndDate", contractEndDate);
            insert.Parameters.AddWithValue("$unitId", unitLocalId);
            insert.Parameters.AddWithValue("$tenantId", tenantLocalId);
            insert.Parameters.AddWithValue("$rentAmount", rentAmount);
            insert.Parameters.AddWithValue("$contractState", contractState);
            insert.Parameters.AddWithValue("$contractPayMethod", contractPayMethod);
            insert.Parameters.AddWithValue("$contractApartmentType", contractApartmentType);
            insert.Parameters.AddWithValue("$contractUnitRoomsNum", contractUnitRoomsNum);
            insert.Parameters.AddWithValue("$contractUnitFloorNum", contractUnitFloorNum);
            insert.Parameters.AddWithValue("$contractOpligation", contractOpligation);

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
            FROM ContractsRealEstate
            WHERE CloudId = $cloudId
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);

        var result = cmd.ExecuteScalar();
        return result == null ? 0 : Convert.ToInt64(result);
    }

    public List<ContractRealEstate> GetDirtyRows()
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
                c.UnitId,
                c.TenantId,
                c.RentAmount,
                c.ContractState,
                c.ContractPayMethod,
                c.ContractApartmentType,
                c.ContractUnitRoomsNum,
                c.ContractUnitFloorNum,
                c.ContractOpligation,
                u.CloudId,
                t.CloudId,
                c.SyncAction
            FROM ContractsRealEstate c
            JOIN UnitsRealEstate u ON u.Id = c.UnitId
            JOIN TenantsRealEstate t ON t.Id = c.TenantId
            WHERE c.IsDirty = 1
              AND u.CloudId > 0
              AND t.CloudId > 0;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ContractRealEstate
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ContractNumber = reader.GetString(2),
                ContractStartDate = reader.GetDateTime(3),
                ContractEndDate = reader.GetDateTime(4),
                UnitId = reader.GetInt64(5),
                TenantId = reader.GetInt64(6),
                RentAmount = reader.GetDouble(7),
                ContractState = reader.GetString(8),
                ContractPayMethod = reader.GetString(9),
                ContractApartmentType = reader.GetString(10),
                ContractUnitRoomsNum = reader.GetInt32(11),
                ContractUnitFloorNum = reader.GetInt32(12),
                ContractOpligation = reader.GetString(13),
                UnitCloudId = reader.GetInt64(14),
                TenantCloudId = reader.GetInt64(15),
                SyncAction = reader.GetString(16)
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
                    FROM ReceiptsRealEstate
                    WHERE ContractId = $id
                      AND SyncAction <> 'delete'
                );
            """;

            check.Parameters.AddWithValue("$id", id);

            var hasReceipts = Convert.ToInt32(check.ExecuteScalar()) == 1;

            if (hasReceipts)
                throw new InvalidOperationException(".لا يمكن حذف العقد لأنه مرتبط بسندات مسجلة");
        }

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ContractsRealEstate
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
            DELETE FROM ContractsRealEstate
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}