using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace RealEstateApp.Services;

public class DbService
{
    private readonly string _dbPath;

    public DbService(string dbFileName = "realestate.db")
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RealEstateApp"
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
CREATE TABLE IF NOT EXISTS Owners (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    IdentityNumber TEXT NOT NULL,
    Phone INTEGER NOT NULL,
    Address TEXT NOT NULL DEFAULT ''
);
""";

            cmd.ExecuteNonQuery();
        }

        // Units table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS Units (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        OwnerId INTEGER NOT NULL,
        UnitName TEXT NOT NULL DEFAULT '',
        City TEXT NOT NULL DEFAULT '',
        District TEXT NOT NULL DEFAULT '',
        UnitType TEXT NOT NULL DEFAULT 'سكني',
        UnitState TEXT NOT NULL DEFAULT 'شاغر',
        UnitsCount INTEGER NOT NULL DEFAULT 1,
        UnitNum INTEGER NOT NULL DEFAULT 1,
        FOREIGN KEY (OwnerId) REFERENCES Owners(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }

        // Tenants table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS Tenants (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        IdentityNumber TEXT NOT NULL,
        Phone TEXT NOT NULL,
        Address TEXT NOT NULL DEFAULT ''
    );
    """;
            cmd.ExecuteNonQuery();
        }
        // Contracts table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS Contracts (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
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
        FOREIGN KEY (UnitId) REFERENCES Units(Id),
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }
        // Receipts table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS Receipts (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ReceiptNumber TEXT UNIQUE NOT NULL,
        ReceiptDate DATETIME NOT NULL,
        ContractId INTEGER NOT NULL,
        PaymentMethod TEXT NOT NULL,
        Amount REAL NOT NULL,
        FOREIGN KEY (ContractId) REFERENCES Contracts(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }
        // Expenses table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS Expenses (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ExpensesNumber TEXT UNIQUE NOT NULL,
        ExpensesDate DATETIME NOT NULL,
        ExpensesService TEXT NOT NULL,
        ExpensesAmount REAL NOT NULL,
        ExpensesNote TEXT NOT NULL,
        UnitId INTEGER NOT NULL,
        FOREIGN KEY (UnitId) REFERENCES Units(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }

    }
}
