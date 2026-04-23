using Microsoft.Data.Sqlite;
using RealEstateApp.Models;
using System;
using System.Collections.Generic;

namespace RealEstateApp.Services;

public class ContractServiceInstallment
{
    private readonly InstallmentDbService _db;

    public ContractServiceInstallment(InstallmentDbService db)
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
        FROM ContractsInstallment
        WHERE ContractNumber LIKE 'Ic-%';
    """;

        var next = Convert.ToInt32(cmd.ExecuteScalar());
        if (next < 1000) next = 1000;

        return "Ic-" + next;
    }

   public long Add(
    string contractNumber,
    DateTime contractStartDate,
    DateTime contractEndDate,
    double mainTotalAmount,
    double currentTotalAmount,
    double contractPeriod,
    double downPayment,
    double monthlyInstallment,
    float managementFee,
    float interestPercent,
    long productId,
    long customerId)
{
    using var con = new SqliteConnection(_db.ConnectionString);
    con.Open();

    using var cmd = con.CreateCommand();
    cmd.CommandText = """
        INSERT INTO ContractsInstallment (
            ContractNumber,
            ContractStartDate,
            ContractEndDate,
            MainTotalAmount,
            CurrentTotalAmount,
            ContractPeriod,
            DownPayment,
            MonthlyInstallment,
            ManagementFee,
            InterestPercent,
            ProductId,
            CustomerId
        )
        VALUES (
            $contractNumber,
            $contractStartDate,
            $contractEndDate,
            $mainTotalAmount,
            $currentTotalAmount,
            $contractPeriod,
            $downPayment,
            $monthlyInstallment,
            $managementFee,
            $interestPercent,
            $productId,
            $customerId
        );

        SELECT last_insert_rowid();
        """;

    cmd.Parameters.AddWithValue("$contractNumber", contractNumber);
    cmd.Parameters.AddWithValue("$contractStartDate", contractStartDate);
    cmd.Parameters.AddWithValue("$contractEndDate", contractEndDate);
    cmd.Parameters.AddWithValue("$mainTotalAmount", mainTotalAmount);
    cmd.Parameters.AddWithValue("$currentTotalAmount", currentTotalAmount);
    cmd.Parameters.AddWithValue("$contractPeriod", contractPeriod);
    cmd.Parameters.AddWithValue("$downPayment", downPayment);
    cmd.Parameters.AddWithValue("$monthlyInstallment", monthlyInstallment);
    cmd.Parameters.AddWithValue("$managementFee", managementFee);
    cmd.Parameters.AddWithValue("$interestPercent", interestPercent);
    cmd.Parameters.AddWithValue("$productId", productId);
    cmd.Parameters.AddWithValue("$customerId", customerId);

    return (long)cmd.ExecuteScalar()!;
}
public void Update(
    long id,
    string contractNumber,
    DateTime contractStartDate,
    DateTime contractEndDate,
    double mainTotalAmount,
    double currentTotalAmount,
    double contractPeriod,
    double downPayment,
    double monthlyInstallment,
    float managementFee,
    float interestPercent,
    long productId,
    long customerId)
{
    using var con = new SqliteConnection(_db.ConnectionString);
    con.Open();

    using var cmd = con.CreateCommand();
    cmd.CommandText = """
                      UPDATE ContractsInstallment
                      SET ContractNumber = $contractNumber,
                          ContractStartDate = $contractStartDate,
                          ContractEndDate = $contractEndDate,
                          MainTotalAmount = $$mainTotalAmount,
                          CurrentTotalAmount = $currentTotalAmount,
                          ContractPeriod = $contractPeriod,
                          DownPayment = $downPayment,
                          MonthlyInstallment = $monthlyInstallment,
                          ManagementFee = $managementFee,
                          InterestPercent = $interestPercent,
                          ProductId = $productId,
                          CustomerId = $customerId
                      WHERE Id = $id;
                      """;

    cmd.Parameters.AddWithValue("$id", id);
    cmd.Parameters.AddWithValue("$contractNumber", contractNumber);
    cmd.Parameters.AddWithValue("$contractStartDate", contractStartDate);
    cmd.Parameters.AddWithValue("$contractEndDate", contractEndDate);
    cmd.Parameters.AddWithValue("$mainTotalAmount", mainTotalAmount);
    cmd.Parameters.AddWithValue("currentTotalAmount", currentTotalAmount);
    cmd.Parameters.AddWithValue("$contractPeriod", contractPeriod);
    cmd.Parameters.AddWithValue("$downPayment", downPayment);
    cmd.Parameters.AddWithValue("$monthlyInstallment", monthlyInstallment);
    cmd.Parameters.AddWithValue("$managementFee", managementFee);
    cmd.Parameters.AddWithValue("$interestPercent", interestPercent);
    cmd.Parameters.AddWithValue("$productId", productId);
    cmd.Parameters.AddWithValue("$customerId", customerId);

    cmd.ExecuteNonQuery();
}
    public List<ContractInstallment> GetAll()
    {
        var list = new List<ContractInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
                          SELECT c.Id,
                                 c.ContractNumber,
                                 c.ContractStartDate,
                                 c.ContractEndDate,
                                 c.ContractState,
                                 u.Name
                          FROM ContractsInstallment c
                          JOIN CustomersInstallment u ON u.Id = c.CustomerId
                          ORDER BY c.Id DESC;
                          """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ContractInstallment
            {
                Id = reader.GetInt64(0),
                ContractNumber = reader.GetString(1),
                ContractStartDate = reader.GetDateTime(2),
                ContractEndDate = reader.GetDateTime(3),
                ContractState = reader.GetString(4),
                CustomerName = reader.GetString(5),
            });
        }

        return list;
    }
   
public ContractInstallment? GetById(long id)
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
    c.MainTotalAmount,
    c.CurrentTotalAmount,
    c.ContractPeriod,
    c.DownPayment,
    c.MonthlyInstallment,
    c.ManagementFee,
    c.InterestPercent,
    c.ContractState,
    c.ProductId,
    c.CustomerId,

    p.OwnerId,
    p.ProductName,
    p.ProductType,
    p.ProductMainPrice,
    p.CarPlateNumber,
    p.CarVIN,
    p.CarModel,
    p.CarColor,
    p.MobileStorage,
    p.MobileColor,

    cust.Name,
    cust.IdentityNumber,
    cust.Phone,
    cust.Address,
    cust.Job,
    cust.SponserName,
    cust.SponserIdentityNumber,
    cust.SponserPhone,
    cust.SponserAddress,
    cust.SponserJob,

    o.Name,
    o.IdentityNumber,
    o.Phone,
    o.Address

FROM ContractsInstallment c
LEFT JOIN ProductsInstallment p ON p.Id = c.ProductId
LEFT JOIN CustomersInstallment cust ON cust.Id = c.CustomerId
LEFT JOIN OwnersInstallment o ON o.Id = p.OwnerId
WHERE c.Id = $id
LIMIT 1;
""";

    cmd.Parameters.AddWithValue("$id", id);

    using var reader = cmd.ExecuteReader();
    if (!reader.Read())
        return null;

    var contract = new ContractInstallment
    {
        Id = reader.GetInt64(0),
        ContractNumber = reader.GetString(1),

        ContractStartDate = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
        ContractEndDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),

        MainTotalAmount = reader.IsDBNull(4) ? 0 : Convert.ToDouble(reader.GetValue(4)),
        CurrentTotalAmount = reader.IsDBNull(5) ? 0 : Convert.ToDouble(reader.GetValue(5)),
        ContractPeriod = reader.IsDBNull(6) ? 0 : Convert.ToDouble(reader.GetValue(6)),
        DownPayment = reader.IsDBNull(7) ? 0 : Convert.ToDouble(reader.GetValue(7)),
        MonthlyInstallment = reader.IsDBNull(8) ? 0 : Convert.ToDouble(reader.GetValue(8)),
        ManagementFee = reader.IsDBNull(9) ? 1 : Convert.ToDouble(reader.GetValue(9)),
        InterestPercent = reader.IsDBNull(10) ? 1 : Convert.ToDouble(reader.GetValue(10)),
        ContractState = reader.IsDBNull(11) ? "جاري" : reader.GetString(11),

        ProductId = reader.IsDBNull(12) ? 0 : reader.GetInt64(12),
        CustomerId = reader.IsDBNull(13) ? 0 : reader.GetInt64(13),

        OwnerId = reader.IsDBNull(14) ? 0 : reader.GetInt64(14),
        ProductName = reader.IsDBNull(15) ? "" : reader.GetString(15),
        ProductType = reader.IsDBNull(16) ? "جوالات" : reader.GetString(16),
        ProductMainPrice = reader.IsDBNull(17) ? 1 : Convert.ToDouble(reader.GetValue(17)),

        ProductCarPlateNumber = reader.IsDBNull(18) ? "" : reader.GetString(18),
        ProductCarVIN = reader.IsDBNull(19) ? "" : reader.GetString(19),
        ProductCarModel = reader.IsDBNull(20) ? "" : reader.GetString(20),
        ProductCarColor = reader.IsDBNull(21) ? "" : reader.GetString(21),
        ProductMobileStorage = reader.IsDBNull(22) ? "" : reader.GetString(22),
        ProductMobileColor = reader.IsDBNull(23) ? "" : reader.GetString(23),

        CustomerName = reader.IsDBNull(24) ? "" : reader.GetString(24),
        CustomerIdentityNumber = reader.IsDBNull(25) ? "" : reader.GetString(25),
        CustomerPhone = reader.IsDBNull(26) ? "" : reader.GetString(26),
        CustomerAddress = reader.IsDBNull(27) ? "" : reader.GetString(27),
        CustomerJob = reader.IsDBNull(28) ? "" : reader.GetString(28),
        CustomerSponserName = reader.IsDBNull(29) ? "" : reader.GetString(29),
        CustomerSponserIdentityNumber = reader.IsDBNull(30) ? "" : reader.GetString(30),
        CustomerSponserPhone = reader.IsDBNull(31) ? "" : reader.GetString(31),
        CustomerSponserAddress = reader.IsDBNull(32) ? "" : reader.GetString(32),
        CustomerSponserJob = reader.IsDBNull(33) ? "" : reader.GetString(33),

        OwnerName = reader.IsDBNull(34) ? "" : reader.GetString(34),
        OwnerIdentityNumber = reader.IsDBNull(35) ? "" : reader.GetString(35),
        OwnerPhone = reader.IsDBNull(36) ? "" : reader.GetString(36),
        OwnerAddress = reader.IsDBNull(37) ? "" : reader.GetString(37)
    };

    contract.BoolDownPayment = contract.DownPayment > 0;

    return contract;
}
    
    public void UpdateContractStates()
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        UPDATE ContractsInstallment
        SET ContractState = 'منتهي'
        WHERE date(ContractEndDate) < date('now')
          AND ContractState <> 'منتهي';

        UPDATE ContractsInstallment
        SET ContractState = 'جاري'
        WHERE date(ContractEndDate) >= date('now')
          AND ContractState <> 'جاري';
    """;

        cmd.ExecuteNonQuery();
    }
    
    public ContractInstallment? FindByContractNum(string contractNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
                          SELECT 
                              c.Id,
                              c.ProductId,
                              c.CustomerId,
                              c.MainTotalAmount,
                              c.CurrentTotalAmount,
                              c.MonthlyInstallment,
                              cus.Name,
                              p.ProductName
                          FROM ContractsInstallment c
                          JOIN ProductsInstallment p ON p.Id = c.ProductId
                          JOIN CustomersInstallment cus ON cus.Id = c.CustomerId
                          WHERE c.ContractNumber = $contractNum
                          LIMIT 1;
                          """;

        cmd.Parameters.AddWithValue("$contractNum", contractNum);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new ContractInstallment
        {
            Id = reader.GetInt64(0),
            ProductId = reader.GetInt64(1),
            CustomerId = reader.GetInt64(2),
            MainTotalAmount = reader.GetDouble(3),
            CurrentTotalAmount = reader.GetDouble(4),
            MonthlyInstallment = reader.GetDouble(5),
            CustomerName = reader.GetString(6),
            ProductName = reader.GetString(7)
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
