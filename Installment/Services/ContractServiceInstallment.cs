using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class ContractServiceInstallment
{
    private readonly DbServiceInstallment _db;

    public ContractServiceInstallment(DbServiceInstallment db)
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

        var contractState = contractEndDate.Date < DateTime.Today ? "منتهي" : "جاري";

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
                ContractState,
                ProductId,
                CustomerId,
                IsDirty,
                SyncAction
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
                $contractState,
                $productId,
                $customerId,
                1,
                'insert'
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
        cmd.Parameters.AddWithValue("$contractState", contractState);
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

        var contractState = contractEndDate.Date < DateTime.Today ? "منتهي" : "جاري";

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ContractsInstallment
            SET ContractNumber = $contractNumber,
                ContractStartDate = $contractStartDate,
                ContractEndDate = $contractEndDate,
                MainTotalAmount = $mainTotalAmount,
                CurrentTotalAmount = $currentTotalAmount,
                ContractPeriod = $contractPeriod,
                DownPayment = $downPayment,
                MonthlyInstallment = $monthlyInstallment,
                ManagementFee = $managementFee,
                InterestPercent = $interestPercent,
                ContractState = $contractState,
                ProductId = $productId,
                CustomerId = $customerId,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$id", id);
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
        cmd.Parameters.AddWithValue("$contractState", contractState);
        cmd.Parameters.AddWithValue("$productId", productId);
        cmd.Parameters.AddWithValue("$customerId", customerId);

        cmd.ExecuteNonQuery();
    }

    public void UpdateSignatureCloudInfo(
        long id,
        string cloudPath,
        string fileName,
        string fileType)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();

        cmd.CommandText = """
            UPDATE ContractsInstallment
            SET SignatureCloudPath = $cloudPath,
                SignatureFileName = $fileName,
                SignatureFileType = $fileType,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE Id = $id;
        """;

        cmd.Parameters.AddWithValue("$cloudPath", cloudPath);
        cmd.Parameters.AddWithValue("$fileName", fileName);
        cmd.Parameters.AddWithValue("$fileType", fileType);
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public List<ContractInstallment> GetAll()
    {
        var list = new List<ContractInstallment>();

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
                cus.Name,
                c.SignatureCloudPath,
                c.SignatureFileName,
                c.SignatureFileType
            FROM ContractsInstallment c
            JOIN CustomersInstallment cus ON cus.Id = c.CustomerId
            WHERE c.SyncAction <> 'delete'
            ORDER BY c.Id DESC;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ContractInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ContractNumber = reader.GetString(2),
                ContractStartDate = reader.GetDateTime(3),
                ContractEndDate = reader.GetDateTime(4),
                ContractState = reader.GetString(5),
                CustomerName = reader.GetString(6),

                SignatureCloudPath = reader.GetString(7),
                SignatureFileName = reader.GetString(8),
                SignatureFileType = reader.GetString(9)
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
                c.CloudId,
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

                p.CloudId,
                cust.CloudId,

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
                o.Address,

                c.SignatureCloudPath,
                c.SignatureFileName,
                c.SignatureFileType

            FROM ContractsInstallment c
            LEFT JOIN ProductsInstallment p ON p.Id = c.ProductId
            LEFT JOIN CustomersInstallment cust ON cust.Id = c.CustomerId
            LEFT JOIN OwnersInstallment o ON o.Id = p.OwnerId
            WHERE c.Id = $id
              AND c.SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        var contract = new ContractInstallment
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            ContractNumber = reader.GetString(2),

            ContractStartDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
            ContractEndDate = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),

            MainTotalAmount = reader.IsDBNull(5) ? 0 : Convert.ToDouble(reader.GetValue(5)),
            CurrentTotalAmount = reader.IsDBNull(6) ? 0 : Convert.ToDouble(reader.GetValue(6)),
            ContractPeriod = reader.IsDBNull(7) ? 0 : Convert.ToDouble(reader.GetValue(7)),
            DownPayment = reader.IsDBNull(8) ? 0 : Convert.ToDouble(reader.GetValue(8)),
            MonthlyInstallment = reader.IsDBNull(9) ? 0 : Convert.ToDouble(reader.GetValue(9)),
            ManagementFee = reader.IsDBNull(10) ? 1 : Convert.ToDouble(reader.GetValue(10)),
            InterestPercent = reader.IsDBNull(11) ? 1 : Convert.ToDouble(reader.GetValue(11)),
            ContractState = reader.IsDBNull(12) ? "جاري" : reader.GetString(12),

            ProductId = reader.IsDBNull(13) ? 0 : reader.GetInt64(13),
            CustomerId = reader.IsDBNull(14) ? 0 : reader.GetInt64(14),

            ProductCloudId = reader.IsDBNull(15) ? 0 : reader.GetInt64(15),
            CustomerCloudId = reader.IsDBNull(16) ? 0 : reader.GetInt64(16),

            OwnerId = reader.IsDBNull(17) ? 0 : reader.GetInt64(17),
            ProductName = reader.IsDBNull(18) ? "" : reader.GetString(18),
            ProductType = reader.IsDBNull(19) ? "جوالات" : reader.GetString(19),
            ProductMainPrice = reader.IsDBNull(20) ? 1 : Convert.ToDouble(reader.GetValue(20)),

            ProductCarPlateNumber = reader.IsDBNull(21) ? "" : reader.GetString(21),
            ProductCarVIN = reader.IsDBNull(22) ? "" : reader.GetString(22),
            ProductCarModel = reader.IsDBNull(23) ? "" : reader.GetString(23),
            ProductCarColor = reader.IsDBNull(24) ? "" : reader.GetString(24),
            ProductMobileStorage = reader.IsDBNull(25) ? "" : reader.GetString(25),
            ProductMobileColor = reader.IsDBNull(26) ? "" : reader.GetString(26),

            CustomerName = reader.IsDBNull(27) ? "" : reader.GetString(27),
            CustomerIdentityNumber = reader.IsDBNull(28) ? "" : reader.GetString(28),
            CustomerPhone = reader.IsDBNull(29) ? "" : reader.GetString(29),
            CustomerAddress = reader.IsDBNull(30) ? "" : reader.GetString(30),
            CustomerJob = reader.IsDBNull(31) ? "" : reader.GetString(31),
            CustomerSponserName = reader.IsDBNull(32) ? "" : reader.GetString(32),
            CustomerSponserIdentityNumber = reader.IsDBNull(33) ? "" : reader.GetString(33),
            CustomerSponserPhone = reader.IsDBNull(34) ? "" : reader.GetString(34),
            CustomerSponserAddress = reader.IsDBNull(35) ? "" : reader.GetString(35),
            CustomerSponserJob = reader.IsDBNull(36) ? "" : reader.GetString(36),

            OwnerName = reader.IsDBNull(37) ? "" : reader.GetString(37),
            OwnerIdentityNumber = reader.IsDBNull(38) ? "" : reader.GetString(38),
            OwnerPhone = reader.IsDBNull(39) ? "" : reader.GetString(39),
            OwnerAddress = reader.IsDBNull(40) ? "" : reader.GetString(40),

            SignatureCloudPath = reader.IsDBNull(41) ? "" : reader.GetString(41),
            SignatureFileName = reader.IsDBNull(42) ? "" : reader.GetString(42),
            SignatureFileType = reader.IsDBNull(43) ? "" : reader.GetString(43)
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
            SET ContractState = 'منتهي',
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE date(ContractEndDate) < date('now')
              AND ContractState <> 'منتهي'
              AND SyncAction <> 'delete';

            UPDATE ContractsInstallment
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

    public ContractInstallment? FindByContractNum(string contractNum)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
                c.Id,
                c.CloudId,
                c.ProductId,
                c.CustomerId,
                p.CloudId,
                cus.CloudId,
                c.MainTotalAmount,
                c.CurrentTotalAmount,
                c.MonthlyInstallment,
                cus.Name,
                p.ProductName,
                c.SignatureCloudPath,
                c.SignatureFileName,
                c.SignatureFileType
            FROM ContractsInstallment c
            JOIN ProductsInstallment p ON p.Id = c.ProductId
            JOIN CustomersInstallment cus ON cus.Id = c.CustomerId
            WHERE c.ContractNumber = $contractNum
              AND c.SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$contractNum", contractNum);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new ContractInstallment
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            ProductId = reader.GetInt64(2),
            CustomerId = reader.GetInt64(3),
            ProductCloudId = reader.GetInt64(4),
            CustomerCloudId = reader.GetInt64(5),
            MainTotalAmount = reader.GetDouble(6),
            CurrentTotalAmount = reader.GetDouble(7),
            MonthlyInstallment = reader.GetDouble(8),
            CustomerName = reader.GetString(9),
            ProductName = reader.GetString(10),

            SignatureCloudPath = reader.GetString(11),
            SignatureFileName = reader.GetString(12),
            SignatureFileType = reader.GetString(13)
        };
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE ContractsInstallment
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
            UPDATE ContractsInstallment
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
            FROM ContractsInstallment
            WHERE CloudId = $cloudId
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$cloudId", cloudId);

        var result = cmd.ExecuteScalar();
        return result == null ? 0 : Convert.ToInt64(result);
    }

    public void UpsertFromCloud(
        long cloudId,
        string contractNumber,
        DateTime contractStartDate,
        DateTime contractEndDate,
        double mainTotalAmount,
        double currentTotalAmount,
        double contractPeriod,
        double downPayment,
        double monthlyInstallment,
        double managementFee,
        double interestPercent,
        string contractState,
        long productLocalId,
        long customerLocalId,
        string signatureCloudPath,
        string signatureFileName,
        string signatureFileType)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM ContractsInstallment
            WHERE CloudId = $cloudId;
        """;

        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE ContractsInstallment
                SET ContractNumber = $contractNumber,
                    ContractStartDate = $contractStartDate,
                    ContractEndDate = $contractEndDate,
                    MainTotalAmount = $mainTotalAmount,
                    CurrentTotalAmount = $currentTotalAmount,
                    ContractPeriod = $contractPeriod,
                    DownPayment = $downPayment,
                    MonthlyInstallment = $monthlyInstallment,
                    ManagementFee = $managementFee,
                    InterestPercent = $interestPercent,
                    ContractState = $contractState,
                    ProductId = $productId,
                    CustomerId = $customerId,
                    SignatureCloudPath = $signatureCloudPath,
                    SignatureFileName = $signatureFileName,
                    SignatureFileType = $signatureFileType
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$contractNumber", contractNumber);
            update.Parameters.AddWithValue("$contractStartDate", contractStartDate);
            update.Parameters.AddWithValue("$contractEndDate", contractEndDate);
            update.Parameters.AddWithValue("$mainTotalAmount", mainTotalAmount);
            update.Parameters.AddWithValue("$currentTotalAmount", currentTotalAmount);
            update.Parameters.AddWithValue("$contractPeriod", contractPeriod);
            update.Parameters.AddWithValue("$downPayment", downPayment);
            update.Parameters.AddWithValue("$monthlyInstallment", monthlyInstallment);
            update.Parameters.AddWithValue("$managementFee", managementFee);
            update.Parameters.AddWithValue("$interestPercent", interestPercent);
            update.Parameters.AddWithValue("$contractState", contractState);
            update.Parameters.AddWithValue("$productId", productLocalId);
            update.Parameters.AddWithValue("$customerId", customerLocalId);
            update.Parameters.AddWithValue("$signatureCloudPath", signatureCloudPath ?? "");
            update.Parameters.AddWithValue("$signatureFileName", signatureFileName ?? "");
            update.Parameters.AddWithValue("$signatureFileType", signatureFileType ?? "");

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO ContractsInstallment
                (
                    CloudId,
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
                    ContractState,
                    ProductId,
                    CustomerId,
                    SignatureCloudPath,
                    SignatureFileName,
                    SignatureFileType,
                    IsDirty,
                    SyncAction
                )
                VALUES
                (
                    $cloudId,
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
                    $contractState,
                    $productId,
                    $customerId,
                    $signatureCloudPath,
                    $signatureFileName,
                    $signatureFileType,
                    0,
                    ''
                );
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$contractNumber", contractNumber);
            insert.Parameters.AddWithValue("$contractStartDate", contractStartDate);
            insert.Parameters.AddWithValue("$contractEndDate", contractEndDate);
            insert.Parameters.AddWithValue("$mainTotalAmount", mainTotalAmount);
            insert.Parameters.AddWithValue("$currentTotalAmount", currentTotalAmount);
            insert.Parameters.AddWithValue("$contractPeriod", contractPeriod);
            insert.Parameters.AddWithValue("$downPayment", downPayment);
            insert.Parameters.AddWithValue("$monthlyInstallment", monthlyInstallment);
            insert.Parameters.AddWithValue("$managementFee", managementFee);
            insert.Parameters.AddWithValue("$interestPercent", interestPercent);
            insert.Parameters.AddWithValue("$contractState", contractState);
            insert.Parameters.AddWithValue("$productId", productLocalId);
            insert.Parameters.AddWithValue("$customerId", customerLocalId);
            insert.Parameters.AddWithValue("$signatureCloudPath", signatureCloudPath ?? "");
            insert.Parameters.AddWithValue("$signatureFileName", signatureFileName ?? "");
            insert.Parameters.AddWithValue("$signatureFileType", signatureFileType ?? "");

            insert.ExecuteNonQuery();
        }
    }

    public List<ContractInstallment> GetDirtyRows()
    {
        var list = new List<ContractInstallment>();

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
                p.CloudId,
                cus.CloudId,
                c.SyncAction,
                c.SignatureCloudPath,
                c.SignatureFileName,
                c.SignatureFileType
            FROM ContractsInstallment c
            LEFT JOIN ProductsInstallment p ON p.Id = c.ProductId
            LEFT JOIN CustomersInstallment cus ON cus.Id = c.CustomerId
            WHERE c.IsDirty = 1
              AND (
                    c.SyncAction = 'delete'
                    OR (p.CloudId > 0 AND cus.CloudId > 0)
                  );
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new ContractInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                ContractNumber = reader.GetString(2),
                ContractStartDate = reader.GetDateTime(3),
                ContractEndDate = reader.GetDateTime(4),
                MainTotalAmount = Convert.ToDouble(reader.GetValue(5)),
                CurrentTotalAmount = Convert.ToDouble(reader.GetValue(6)),
                ContractPeriod = Convert.ToDouble(reader.GetValue(7)),
                DownPayment = Convert.ToDouble(reader.GetValue(8)),
                MonthlyInstallment = Convert.ToDouble(reader.GetValue(9)),
                ManagementFee = Convert.ToDouble(reader.GetValue(10)),
                InterestPercent = Convert.ToDouble(reader.GetValue(11)),
                ContractState = reader.GetString(12),
                ProductId = reader.GetInt64(13),
                CustomerId = reader.GetInt64(14),
                ProductCloudId = reader.GetInt64(15),
                CustomerCloudId = reader.GetInt64(16),
                SyncAction = reader.GetString(17),

                SignatureCloudPath = reader.GetString(18),
                SignatureFileName = reader.GetString(19),
                SignatureFileType = reader.GetString(20)
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
                    FROM ReceiptsInstallment
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
            UPDATE ContractsInstallment
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
                DELETE FROM ContractsInstallment
                WHERE Id = $id;
            """;

            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف العقد لأنه مرتبط ببيانات أخرى");
        }
    }
}