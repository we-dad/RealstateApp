using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace RealEstateInstallmentsManager.Services;

public class DbServiceInstallment
{
    private readonly string _dbPath;

    public DbServiceInstallment(string dbFileName = "realEstateInstallments.db")
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
CREATE TABLE IF NOT EXISTS OwnersInstallment (
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
    //products table
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = """
                          CREATE TABLE IF NOT EXISTS ProductsInstallment (
                              Id INTEGER PRIMARY KEY AUTOINCREMENT,
                              CloudId INTEGER NOT NULL DEFAULT 0,
                              OwnerId INTEGER NOT NULL,

                              ProductName TEXT NOT NULL DEFAULT '',
                              ProductType TEXT NOT NULL DEFAULT 'جوالات',
                              ProductMainPrice REAL NOT NULL DEFAULT 1,

                              CarPlateNumber TEXT NOT NULL DEFAULT '',
                              CarVIN TEXT NOT NULL DEFAULT '',
                              CarModel TEXT NOT NULL DEFAULT '',
                              CarColor TEXT NOT NULL DEFAULT '',

                              MobileStorage TEXT NOT NULL DEFAULT '',
                              MobileColor TEXT NOT NULL DEFAULT '',
                              IsDirty INTEGER NOT NULL DEFAULT 0,
                          SyncAction TEXT NOT NULL DEFAULT '',

                              FOREIGN KEY (OwnerId) REFERENCES OwnersInstallment(Id)
                          );
                          """;

        cmd.ExecuteNonQuery();
    }
    
        // Customer table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS CustomersInstallment (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        Name TEXT NOT NULL,
        IdentityNumber TEXT NOT NULL,
        Phone TEXT NOT NULL,
        Address TEXT NOT NULL DEFAULT '',
        Job TEXT NOT NULL,
        SponserName TEXT NOT NULL,
        SponserIdentityNumber TEXT NOT NULL,
        SponserPhone TEXT NOT NULL,
        SponserAddress TEXT NOT NULL DEFAULT '',
        SponserJob TEXT NOT NULL,
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
                              CREATE TABLE IF NOT EXISTS ContractsInstallment (
                                  Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                  CloudId INTEGER NOT NULL DEFAULT 0,
                                  ContractNumber TEXT UNIQUE NOT NULL,
                                  ContractStartDate DATETIME NOT NULL,
                                  ContractEndDate DATETIME NOT NULL,
                                  MainTotalAmount REAL NOT NULL,
                                  CurrentTotalAmount REAL NOT NULL,
                                  ContractPeriod REAL NOT NULL,
                                  DownPayment REAL NOT NULL DEFAULT 0,
                                  MonthlyInstallment REAL NOT NULL DEFAULT 0,
                                  ManagementFee REAL NOT NULL DEFAULT 1,
                                  InterestPercent REAL NOT NULL DEFAULT 12.5,
                                  ContractState TEXT NOT NULL DEFAULT 'جاري',

                                  ProductId INTEGER NOT NULL,
                                  CustomerId INTEGER NOT NULL,
                                  IsDirty INTEGER NOT NULL DEFAULT 0,
                              SyncAction TEXT NOT NULL DEFAULT '',

                                  FOREIGN KEY (ProductId) REFERENCES ProductsInstallment(Id),
                                  FOREIGN KEY (CustomerId) REFERENCES CustomersInstallment(Id)
                              );
                              """;
            cmd.ExecuteNonQuery();
        }
        // Receipts table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS ReceiptsInstallment (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        ReceiptNumber TEXT UNIQUE NOT NULL,
        ReceiptDate DATETIME NOT NULL,
        ContractId INTEGER NOT NULL,
        PaymentMethod TEXT NOT NULL,
        Amount REAL NOT NULL,
        CurrentTotalAmount REAL NOT NULL,
        IsDirty INTEGER NOT NULL DEFAULT 0,
    SyncAction TEXT NOT NULL DEFAULT '',
        FOREIGN KEY (ContractId) REFERENCES ContractsInstallment(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }
        // Expenses table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS ExpensesInstallment (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CloudId INTEGER NOT NULL DEFAULT 0,
        ExpensesNumber TEXT UNIQUE NOT NULL,
        ExpensesDate DATETIME NOT NULL,
        ExpensesService TEXT NOT NULL,
        ExpensesAmount REAL NOT NULL,
        ExpensesNote TEXT NOT NULL,
        ProductId INTEGER NOT NULL,
        IsDirty INTEGER NOT NULL DEFAULT 0,
    SyncAction TEXT NOT NULL DEFAULT '',
        FOREIGN KEY (ProductId) REFERENCES ProductsInstallment(Id)
    );
    """;
            cmd.ExecuteNonQuery();
        }

    }
}
