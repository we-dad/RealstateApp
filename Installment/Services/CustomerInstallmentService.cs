using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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
        SELECT Id, Name, IdentityNumber, Phone, Address
        FROM CustomersInstallment
        ORDER BY Id DESC;
        """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CustomerInstallment
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                IdentityNumber = reader.GetString(2),
                Phone = reader.GetString(3),
                Address = reader.GetString(4),
            });
        }

        return list;
    }

    public void Add(string name, string idNumber, string phone, string address, string job,string sponserName, string sponserIdNumber, string sponserPhone, string sponserAddress, string sponserJob)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
        INSERT INTO CustomersInstallment (Name, IdentityNumber, Phone, Address,Job,SponserName, SponserIdentityNumber, SponserPhone, SponserAddress,SponserJob)
        VALUES ($name, $id, $phone, $address, $job,$sponserName, $sponserId, $sponserPhone, $sponserAddress, $sponserJob);
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

        cmd.ExecuteNonQuery();
    }
    public void Update(long id, string name, string idNumber, string phone, string address, string job,
                   string sponserName, string sponserIdNumber, string sponserPhone,
                   string sponserAddress, string sponserJob)
{
    using var con = new SqliteConnection(_db.ConnectionString);
    con.Open();

    using var cmd = con.CreateCommand();
    cmd.CommandText = """
    UPDATE CustomersInstallment
    SET 
        Name = $name,
        IdentityNumber = $idNumber,
        Phone = $phone,
        Address = $address,
        Job = $job,
        SponserName = $sponserName,
        SponserIdentityNumber = $sponserId,
        SponserPhone = $sponserPhone,
        SponserAddress = $sponserAddress,
        SponserJob = $sponserJob
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
                              Id, Name, IdentityNumber, Phone, Address, Job,
                              SponserName, SponserIdentityNumber, SponserPhone, SponserAddress, SponserJob
                          FROM CustomersInstallment
                          WHERE Id = $id
                          LIMIT 1;
                          """;

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        var customer =  new CustomerInstallment
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            IdentityNumber = reader.GetString(2),
            Phone = reader.GetString(3),
            Address = reader.GetString(4),
            Job = reader.GetString(5),
            SponserName = reader.GetString(6),
            SponserIdentityNumber = reader.GetString(7),
            SponserPhone = reader.GetString(8),
            SponserAddress = reader.GetString(9),
            SponserJob = reader.GetString(10)
        };
        customer.BoolSponser =
            !string.IsNullOrWhiteSpace(customer.SponserName);
        return customer;
    }

    public CustomerInstallment? FindByIdentity(string identityNumber)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
                          SELECT 
                              Id,
                              Name,
                              IdentityNumber,
                              Phone,
                              Address,
                              Job,
                              SponserName,
                              SponserIdentityNumber,
                              SponserPhone,
                              SponserAddress,
                              SponserJob
                          FROM CustomersInstallment
                          WHERE IdentityNumber = $id
                          LIMIT 1;
                          """;

        cmd.Parameters.AddWithValue("$id", identityNumber);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var customer =  new CustomerInstallment
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            IdentityNumber = reader.GetString(2),
            Phone = reader.GetString(3),
            Address = reader.GetString(4),
            Job = reader.GetString(5),
            SponserName = reader.GetString(6),
            SponserIdentityNumber = reader.GetString(7),
            SponserPhone = reader.GetString(8),
            SponserAddress = reader.GetString(9),
            SponserJob = reader.GetString(10),
        };
        customer.BoolSponser =
            !string.IsNullOrWhiteSpace(customer.SponserName);
        return customer;
    }
    
    public void Delete(long id)
    {
        using var con = new SqliteConnection(_db.ConnectionString);
        con.Open();

        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT EXISTS(SELECT 1 FROM Contracts WHERE InstallmentCustomerId = $id);";
            check.Parameters.AddWithValue("$id", id);

            var hasUnits = Convert.ToInt32(check.ExecuteScalar()) == 1;
            if (hasUnits)
                throw new InvalidOperationException(".لا يمكن حذف المستأجر لأنه مرتبط بعقود مسجلة");
        }
        try
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM CustomersInstallment WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(".لا يمكن حذف المستأجر لأنه مرتبط ببيانات أخرى");
        }
    }
}