using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class CustomerServiceInstallment
{
    private readonly DbServiceInstallment _db;

    public CustomerServiceInstallment(DbServiceInstallment db)
    {
        _db = db;
    }

    public List<CustomerInstallment> GetAll()
    {
        var list = new List<CustomerInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CloudId, Name, IdentityNumber, Phone, Address
            FROM CustomersInstallment
            WHERE SyncAction <> 'delete'
            ORDER BY Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CustomerInstallment
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

    public long Add(
        string name,
        string idNumber,
        string phone,
        string address,
        string job,
        string sponserName,
        string sponserIdNumber,
        string sponserPhone,
        string sponserAddress,
        string sponserJob)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO CustomersInstallment
            (
                Name, IdentityNumber, Phone, Address, Job,
                SponserName, SponserIdentityNumber, SponserPhone, SponserAddress, SponserJob,
                IsDirty, SyncAction
            )
            VALUES
            (
                $name, $id, $phone, $address, $job,
                $sponserName, $sponserId, $sponserPhone, $sponserAddress, $sponserJob,
                1, 'insert'
            );

            SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$id", idNumber);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$address", address);
        cmd.Parameters.AddWithValue("$job", job);
        cmd.Parameters.AddWithValue("$sponserName", sponserName);
        cmd.Parameters.AddWithValue("$sponserId", sponserIdNumber);
        cmd.Parameters.AddWithValue("$sponserPhone", sponserPhone);
        cmd.Parameters.AddWithValue("$sponserAddress", sponserAddress);
        cmd.Parameters.AddWithValue("$sponserJob", sponserJob);

        return (long)cmd.ExecuteScalar()!;
    }

    public void Update(
        long id,
        string name,
        string idNumber,
        string phone,
        string address,
        string job,
        string sponserName,
        string sponserIdNumber,
        string sponserPhone,
        string sponserAddress,
        string sponserJob)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE CustomersInstallment
            SET Name = $name,
                IdentityNumber = $idNumber,
                Phone = $phone,
                Address = $address,
                Job = $job,
                SponserName = $sponserName,
                SponserIdentityNumber = $sponserId,
                SponserPhone = $sponserPhone,
                SponserAddress = $sponserAddress,
                SponserJob = $sponserJob,
                IsDirty = 1,
                SyncAction = CASE
                    WHEN SyncAction = 'insert' THEN 'insert'
                    ELSE 'update'
                END
            WHERE Id = $customerId;
        """;

        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$idNumber", idNumber);
        cmd.Parameters.AddWithValue("$phone", phone);
        cmd.Parameters.AddWithValue("$address", address);
        cmd.Parameters.AddWithValue("$job", job);
        cmd.Parameters.AddWithValue("$sponserName", sponserName);
        cmd.Parameters.AddWithValue("$sponserId", sponserIdNumber);
        cmd.Parameters.AddWithValue("$sponserPhone", sponserPhone);
        cmd.Parameters.AddWithValue("$sponserAddress", sponserAddress);
        cmd.Parameters.AddWithValue("$sponserJob", sponserJob);
        cmd.Parameters.AddWithValue("$customerId", id);

        cmd.ExecuteNonQuery();
    }

    public CustomerInstallment? GetById(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
                Id, CloudId, Name, IdentityNumber, Phone, Address, Job,
                SponserName, SponserIdentityNumber, SponserPhone, SponserAddress, SponserJob
            FROM CustomersInstallment
            WHERE Id = $id
              AND SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        var customer = new CustomerInstallment
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            Name = reader.GetString(2),
            IdentityNumber = reader.GetString(3),
            Phone = reader.GetString(4),
            Address = reader.GetString(5),
            Job = reader.GetString(6),
            SponserName = reader.GetString(7),
            SponserIdentityNumber = reader.GetString(8),
            SponserPhone = reader.GetString(9),
            SponserAddress = reader.GetString(10),
            SponserJob = reader.GetString(11)
        };

        customer.BoolSponser = !string.IsNullOrWhiteSpace(customer.SponserName);
        return customer;
    }

    public CustomerInstallment? FindByIdentity(string identityNumber)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT 
                Id, CloudId, Name, IdentityNumber, Phone, Address, Job,
                SponserName, SponserIdentityNumber, SponserPhone, SponserAddress, SponserJob
            FROM CustomersInstallment
            WHERE IdentityNumber = $id
              AND SyncAction <> 'delete'
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$id", identityNumber);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var customer = new CustomerInstallment
        {
            Id = reader.GetInt64(0),
            CloudId = reader.GetInt64(1),
            Name = reader.GetString(2),
            IdentityNumber = reader.GetString(3),
            Phone = reader.GetString(4),
            Address = reader.GetString(5),
            Job = reader.GetString(6),
            SponserName = reader.GetString(7),
            SponserIdentityNumber = reader.GetString(8),
            SponserPhone = reader.GetString(9),
            SponserAddress = reader.GetString(10),
            SponserJob = reader.GetString(11),
        };

        customer.BoolSponser = !string.IsNullOrWhiteSpace(customer.SponserName);
        return customer;
    }

    public void UpdateCloudId(long id, long cloudId)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE CustomersInstallment
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
            UPDATE CustomersInstallment
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
            FROM CustomersInstallment
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
        string address,
        string job,
        string sponserName,
        string sponserIdentityNumber,
        string sponserPhone,
        string sponserAddress,
        string sponserJob)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var check = con.CreateCommand();
        check.CommandText = """
            SELECT Id
            FROM CustomersInstallment
            WHERE CloudId = $cloudId;
        """;
        check.Parameters.AddWithValue("$cloudId", cloudId);

        var existingId = check.ExecuteScalar();

        if (existingId != null)
        {
            using var update = con.CreateCommand();
            update.CommandText = """
                UPDATE CustomersInstallment
                SET Name = $name,
                    IdentityNumber = $identityNumber,
                    Phone = $phone,
                    Address = $address,
                    Job = $job,
                    SponserName = $sponserName,
                    SponserIdentityNumber = $sponserIdentityNumber,
                    SponserPhone = $sponserPhone,
                    SponserAddress = $sponserAddress,
                    SponserJob = $sponserJob
                WHERE CloudId = $cloudId
                  AND IsDirty = 0;
            """;

            update.Parameters.AddWithValue("$cloudId", cloudId);
            update.Parameters.AddWithValue("$name", name);
            update.Parameters.AddWithValue("$identityNumber", identityNumber);
            update.Parameters.AddWithValue("$phone", phone);
            update.Parameters.AddWithValue("$address", address);
            update.Parameters.AddWithValue("$job", job);
            update.Parameters.AddWithValue("$sponserName", sponserName);
            update.Parameters.AddWithValue("$sponserIdentityNumber", sponserIdentityNumber);
            update.Parameters.AddWithValue("$sponserPhone", sponserPhone);
            update.Parameters.AddWithValue("$sponserAddress", sponserAddress);
            update.Parameters.AddWithValue("$sponserJob", sponserJob);

            update.ExecuteNonQuery();
        }
        else
        {
            using var insert = con.CreateCommand();
            insert.CommandText = """
                INSERT INTO CustomersInstallment
                (
                    CloudId, Name, IdentityNumber, Phone, Address, Job,
                    SponserName, SponserIdentityNumber, SponserPhone, SponserAddress, SponserJob,
                    IsDirty, SyncAction
                )
                VALUES
                (
                    $cloudId, $name, $identityNumber, $phone, $address, $job,
                    $sponserName, $sponserIdentityNumber, $sponserPhone, $sponserAddress, $sponserJob,
                    0, ''
                );
            """;

            insert.Parameters.AddWithValue("$cloudId", cloudId);
            insert.Parameters.AddWithValue("$name", name);
            insert.Parameters.AddWithValue("$identityNumber", identityNumber);
            insert.Parameters.AddWithValue("$phone", phone);
            insert.Parameters.AddWithValue("$address", address);
            insert.Parameters.AddWithValue("$job", job);
            insert.Parameters.AddWithValue("$sponserName", sponserName);
            insert.Parameters.AddWithValue("$sponserIdentityNumber", sponserIdentityNumber);
            insert.Parameters.AddWithValue("$sponserPhone", sponserPhone);
            insert.Parameters.AddWithValue("$sponserAddress", sponserAddress);
            insert.Parameters.AddWithValue("$sponserJob", sponserJob);

            insert.ExecuteNonQuery();
        }
    }

    public List<CustomerInstallment> GetDirtyRows()
    {
        var list = new List<CustomerInstallment>();

        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT
                Id, CloudId, Name, IdentityNumber, Phone, Address, Job,
                SponserName, SponserIdentityNumber, SponserPhone, SponserAddress, SponserJob,
                SyncAction
            FROM CustomersInstallment
            WHERE IsDirty = 1;
        """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new CustomerInstallment
            {
                Id = reader.GetInt64(0),
                CloudId = reader.GetInt64(1),
                Name = reader.GetString(2),
                IdentityNumber = reader.GetString(3),
                Phone = reader.GetString(4),
                Address = reader.GetString(5),
                Job = reader.GetString(6),
                SponserName = reader.GetString(7),
                SponserIdentityNumber = reader.GetString(8),
                SponserPhone = reader.GetString(9),
                SponserAddress = reader.GetString(10),
                SponserJob = reader.GetString(11),
                SyncAction = reader.GetString(12)
            });
        }

        return list;
    }
    
   public List<CustomerInstallment> GetCustomerRelatedData(long customerId)
{
    var list = new List<CustomerInstallment>();

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
            c.ContractPeriod,
            c.ContractState,

            r.Id,
            r.ReceiptNumber,
            r.ReceiptDate,
            r.Amount,
            r.PaymentMethod
        FROM ContractsInstallment c
        LEFT JOIN ReceiptsInstallment r
            ON r.ContractId = c.Id
           AND IFNULL(r.SyncAction, '') <> 'delete'
        WHERE c.CustomerId = $customerId
          AND IFNULL(c.SyncAction, '') <> 'delete'
        ORDER BY c.Id DESC, r.ReceiptDate DESC;
    """;

    cmd.Parameters.AddWithValue("$customerId", customerId);

    using var reader = cmd.ExecuteReader();

    while (reader.Read())
    {
        list.Add(new CustomerInstallment
        {
            ContractId = reader.GetInt64(0),
            ContractNumber = reader.GetString(1),
            ContractStartDate = reader.GetDateTime(2),
            ContractEndDate = reader.GetDateTime(3),
            ContractAmount = reader.GetDouble(4),
            ContractPeriod = reader.GetDouble(5),
            ContractState = reader.GetString(6),

            ReceiptId = reader.IsDBNull(7) ? 0 : reader.GetInt64(7),
            ReceiptNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
            ReceiptDate = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9),
            ReceiptAmount = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
            PaymentMethod = reader.IsDBNull(11) ? "" : reader.GetString(11)
        });
    }

    var contracts = list
        .Where(x => x.ContractId > 0)
        .GroupBy(x => x.ContractId)
        .Select(x => x.First())
        .ToList();

    var totalAmount = contracts.Sum(x => x.ContractAmount);
    var paidAmount = list.Where(x => x.ReceiptId > 0).Sum(x => x.ReceiptAmount);
    var receiptsCount = list.Where(x => x.ReceiptId > 0).Select(x => x.ReceiptId).Distinct().Count();

    var totalInstallments = contracts.Sum(x => (int)x.ContractPeriod);
    var paidInstallments = receiptsCount;
    var leftInstallments = Math.Max(totalInstallments - paidInstallments, 0);

    var progress = totalInstallments == 0
        ? 0
        : (double)paidInstallments / totalInstallments * 100;
    

    foreach (var item in list)
    {
        item.TotalAmount = totalAmount;
        item.PaidAmount = paidAmount;
        item.LeftAmount = totalAmount - paidAmount;
        item.ReceiptsCount = receiptsCount;

        item.TotalInstallments = totalInstallments;
        item.PaidInstallments = paidInstallments;
        item.LeftInstallments = leftInstallments;
        item.PaymentProgressPercent = progress;
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
                    WHERE CustomerId = $id
                      AND SyncAction <> 'delete'
                );
            """;
            check.Parameters.AddWithValue("$id", id);

            var hasContracts = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasContracts)
                throw new InvalidOperationException(".لا يمكن حذف العميل لأنه مرتبط بعقود مسجلة");
        }

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE CustomersInstallment
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
                DELETE FROM CustomersInstallment
                WHERE Id = $id;
            """;

            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف العميل لأنه مرتبط ببيانات أخرى");
        }
    }
}