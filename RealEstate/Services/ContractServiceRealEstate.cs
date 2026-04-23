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
        SELECT COALESCE(MAX(CAST(SUBSTR(ContractNumber, 3) AS INTEGER)), 999) + 1
        FROM Contracts
        WHERE ContractNumber LIKE 'c-%';
    """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "c-" + next;
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

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    INSERT INTO Contracts (
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
        ContractOpligation
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
        $contractOpligation
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

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
    UPDATE Contracts
    SET
        ContractEndDate = $contractEndDate,
        UnitId = $unitId,
        TenantId = $tenantId,
        RentAmount = $rent,
        ContractPayMethod = $contractPayMethod,
        ContractApartmentType = $contractApartmentType,
        ContractUnitRoomsNum = $contractUnitRoomsNum,
        ContractUnitFloorNum = $contractUnitFloorNum,
        ContractOpligation = $contractOpligation
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

        cmd.ExecuteNonQuery();
    }
    public List<ContractRealEstate> GetAll()
    {
        var list = new List<ContractRealEstate>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        SELECT c.Id, c.ContractNumber, c.ContractStartDate, c.ContractEndDate,c.ContractState,
               u.UnitName
        FROM Contracts c
        JOIN Units u ON u.Id = c.UnitId
        ORDER BY c.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ContractRealEstate
            {
                Id = reader.GetInt64(0),
                ContractNumber = reader.GetString(1),
                ContractStartDate = reader.GetDateTime(2),
                ContractEndDate = reader.GetDateTime(3),
                ContractState = reader.GetString(4),
                UnitName = reader.GetString(5),

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
FROM Contracts c
JOIN Units u ON u.Id = c.UnitId
JOIN Owners o ON o.Id = u.OwnerId
JOIN Tenants t ON t.Id = c.TenantId
WHERE c.Id = $id
LIMIT 1;
""";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ContractRealEstate
        {
            Id = reader.GetInt64(0),
            ContractNumber = reader.GetString(1),
            ContractStartDate = reader.GetDateTime(2),
            ContractEndDate = reader.GetDateTime(3),
            RentAmount = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
            ContractState = reader.GetString(5),

            UnitId = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
            TenantId = reader.IsDBNull(7) ? 0 : reader.GetInt64(7),

            ContractPayMethod = reader.IsDBNull(8) ? "شهري" : reader.GetString(8),
            ContractApartmentType = reader.IsDBNull(9) ? "غرفة مفروشة" : reader.GetString(9),
            ContractUnitRoomsNum = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
            ContractUnitFloorNum = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
            ContractOpligation = reader.IsDBNull(12)
         ? "يتحمل المؤجر مسؤولية الصيانة كاملة, يتحمل المؤجر فواتير الكهرباء والماء"
         : reader.GetString(12),

            OwnerId = reader.IsDBNull(13) ? 0 : reader.GetInt64(13),

            UnitName = reader.IsDBNull(14) ? "" : reader.GetString(14),
            City = reader.IsDBNull(15) ? "" : reader.GetString(15),
            District = reader.IsDBNull(16) ? "" : reader.GetString(16),
            UnitType = reader.IsDBNull(17) ? "" : reader.GetString(17),
            UnitsCount = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
            UnitNum = reader.IsDBNull(19) ? 0 : reader.GetInt32(19),

            OwnerName = reader.IsDBNull(20) ? "" : reader.GetString(20),
            OwnerIdentityNumber = reader.IsDBNull(21) ? "" : reader.GetString(21),
            OwnerPhone = reader.IsDBNull(22) ? "" : reader.GetString(22),
            OwnerAddress = reader.IsDBNull(23) ? "" : reader.GetString(23),

            TenantName = reader.IsDBNull(24) ? "" : reader.GetString(24),
            TenantIdentityNumber = reader.IsDBNull(25) ? "" : reader.GetString(25),
            TenantPhone = reader.IsDBNull(26) ? "" : reader.GetString(26),
            TenantAddress = reader.IsDBNull(27) ? "" : reader.GetString(27),
        };
    }
    public void UpdateContractStates()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        UPDATE Contracts
        SET ContractState = 'منتهي'
        WHERE date(ContractEndDate) < date('now')
          AND ContractState <> 'منتهي';

        UPDATE Contracts
        SET ContractState = 'جاري'
        WHERE date(ContractEndDate) >= date('now')
          AND ContractState <> 'جاري';
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
        c.UnitId,
        c.TenantId,
        t.Name,
        u.UnitName
    FROM Contracts c
    JOIN Units u ON u.Id = c.UnitId
    JOIN Tenants t ON t.Id = c.TenantId
    WHERE c.ContractNumber = $contractNum
    LIMIT 1;
    """;

        cmd.Parameters.AddWithValue("$contractNum", contractNum);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ContractRealEstate
        {
            Id = reader.GetInt64(0),
            UnitId = reader.GetInt64(1),
            TenantId = reader.GetInt64(2),
            TenantName = reader.GetString(3),
            UnitName = reader.GetString(4),
        };
    }

    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Receipts WHERE ContractId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف العقد لأنه مرتبط بسندات مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Contracts WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف العقد لأنه مرتبط ببيانات أخرى");
        }
    }

}
