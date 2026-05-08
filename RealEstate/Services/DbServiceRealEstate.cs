using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace RealEstateInstallmentsManager.Services;

public class DbServiceRealEstate
{
    private readonly string _dbPath;

    public DbServiceRealEstate(string dbFileName = "realEstateInstallments.db")
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RealEstateInstallmentsManager"
        );

        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, dbFileName);
    }

    public string ConnectionString =>
        new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            ForeignKeys = true
        }.ToString();

    public void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        // Create table owners
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
CREATE TABLE IF NOT EXISTS OwnersRealEstate (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CloudId INTEGER NOT NULL DEFAULT 0,
    Name TEXT NOT NULL,
    IdentityNumber TEXT NOT NULL,
    Phone INTEGER NOT NULL,
    Address TEXT NOT NULL DEFAULT '',
    IsDirty INTEGER NOT NULL DEFAULT 0,
SyncAction TEXT NOT NULL DEFAULT ''
);
""";

            cmd.ExecuteNonQuery();
        }

        // Units table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS UnitsRealEstate (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        OwnerId INTEGER NOT NULL,
        UnitName TEXT NOT NULL DEFAULT '',
        City TEXT NOT NULL DEFAULT '',
        District TEXT NOT NULL DEFAULT '',
        UnitType TEXT NOT NULL DEFAULT 'سكني',
        UnitState TEXT NOT NULL DEFAULT 'شاغرة',
        UnitsCount INTEGER NOT NULL DEFAULT 1,
        UnitNum INTEGER NOT NULL DEFAULT 1,
        IsDirty INTEGER NOT NULL DEFAULT 0,
    SyncAction TEXT NOT NULL DEFAULT '',
        FOREIGN KEY (OwnerId) REFERENCES OwnersRealEstate(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }

        // Tenants table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS TenantsRealEstate (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        Name TEXT NOT NULL,
        IdentityNumber TEXT NOT NULL,
        Phone TEXT NOT NULL,
        Address TEXT NOT NULL DEFAULT '',
        IsDirty INTEGER NOT NULL DEFAULT 0,
    SyncAction TEXT NOT NULL DEFAULT ''
    );
    """;
            cmd.ExecuteNonQuery();
        }
        // Contracts table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS ContractsRealEstate (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        ContractNumber TEXT UNIQUE NOT NULL,
        ContractStartDate DATETIME NOT NULL,
        ContractEndDate DATETIME NOT NULL,
        RentAmount REAL NOT NULL,
        ContractState TEXT NOT NULL DEFAULT 'جاري',
        ContractPayMethod TEXT NOT NULL DEFAULT 'شهري',
        ContractApartmentType TEXT NOT NULL DEFAULT 'غرفة مفروشة',
        ContractUnitRoomsNum INTEGER NOT NULL,
        ContractUnitFloorNum INTEGER NOT NULL,
        ContractOpligation TEXT NOT NULL DEFAULT 'يتحمل المؤجر مسؤولية الصيانة كاملة, يتحمل المؤجر فواتير الكهرباء والماء',
        UnitId INTEGER NOT NULL,
        TenantId INTEGER NOT NULL,
        IsDirty INTEGER NOT NULL DEFAULT 0,
    SyncAction TEXT NOT NULL DEFAULT '',
        FOREIGN KEY (UnitId) REFERENCES UnitsRealEstate(Id),
        FOREIGN KEY (TenantId) REFERENCES TenantsRealEstate(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }
        // Receipts table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS ReceiptsRealEstate (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        ReceiptNumber TEXT UNIQUE NOT NULL,
        ReceiptDate DATETIME NOT NULL,
        ContractId INTEGER NOT NULL,
        PaymentMethod TEXT NOT NULL,
        Amount REAL NOT NULL,
        IsDirty INTEGER NOT NULL DEFAULT 0,
    SyncAction TEXT NOT NULL DEFAULT '',
        FOREIGN KEY (ContractId) REFERENCES ContractsRealEstate(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }
        // Expenses table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS ExpensesRealEstate (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        ExpensesNumber TEXT UNIQUE NOT NULL,
        ExpensesDate DATETIME NOT NULL,
        ExpensesService TEXT NOT NULL,
        ExpensesAmount REAL NOT NULL,
        ExpensesNote TEXT NOT NULL,
        UnitId INTEGER NOT NULL,
        IsDirty INTEGER NOT NULL DEFAULT 0,
    SyncAction TEXT NOT NULL DEFAULT '',
        FOREIGN KEY (UnitId) REFERENCES UnitsRealEstate(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }

    }
}
