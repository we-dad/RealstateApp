using System;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models.Cloud;

namespace RealEstateInstallmentsManager.Services;

public class InstallmentSyncService
{
    private readonly DbServiceInstallment _db;
    private readonly SupabaseService _supabaseService;

    public InstallmentSyncService(DbServiceInstallment db, SupabaseService supabaseService)
    {
        _db = db;
        _supabaseService = supabaseService;
    }

    public async Task PushAllDirtyAsync()
    {
        try
        {
            await PushDirtyReceiptsAsync();
            await PushDirtyExpensesAsync();
            await PushDirtyContractsAsync();
            await PushDirtyProductsAsync();
            await PushDirtyCustomersAsync();
            await PushDirtyOwnersAsync();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: installment dirty rows will sync later.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task PushDirtyReceiptsAsync()
    {
        var receiptsDB = new ReceiptServiceInstallment(_db);
        var cloudReceipts = new CloudReceiptsInstallmentService(_supabaseService);

        foreach (var receipt in receiptsDB.GetDirtyRows())
        {
            if (receipt.SyncAction == "delete")
            {
                if (receipt.CloudId > 0)
                    await cloudReceipts.DeleteReceiptAsync(receipt.CloudId);

                receiptsDB.DeleteLocalPermanent(receipt.Id);
            }
            else if (receipt.SyncAction == "insert")
            {
                if (receipt.ContractCloudId <= 0)
                    continue;

                var cloudId = await cloudReceipts.AddReceiptAsync(new ReceiptInstallmentRow
                {
                    ReceiptNumber = receipt.ReceiptNumber,
                    ReceiptDate = receipt.ReceiptDate,
                    ContractId = receipt.ContractCloudId,
                    PaymentMethod = receipt.PaymentMethod,
                    Amount = receipt.Amount,
                    CurrentTotalAmount = receipt.CurrentTotalAmount
                });

                receiptsDB.UpdateCloudId(receipt.Id, cloudId);
            }
            else if (receipt.SyncAction == "update")
            {
                if (receipt.CloudId <= 0 || receipt.ContractCloudId <= 0)
                    continue;

                await cloudReceipts.UpdateReceiptAsync(receipt.CloudId, new ReceiptInstallmentRow
                {
                    ReceiptNumber = receipt.ReceiptNumber,
                    ReceiptDate = receipt.ReceiptDate,
                    ContractId = receipt.ContractCloudId,
                    PaymentMethod = receipt.PaymentMethod,
                    Amount = receipt.Amount,
                    CurrentTotalAmount = receipt.CurrentTotalAmount
                });

                receiptsDB.MarkSynced(receipt.Id);
            }
        }
    }

    private async Task PushDirtyExpensesAsync()
    {
        var expensesDB = new ExpensesServiceInstallment(_db);
        var cloudExpenses = new CloudExpensesInstallmentService(_supabaseService);

        foreach (var expense in expensesDB.GetDirtyRows())
        {
            if (expense.SyncAction == "delete")
            {
                if (expense.CloudId > 0)
                    await cloudExpenses.DeleteExpenseAsync(expense.CloudId);

                expensesDB.DeleteLocalPermanent(expense.Id);
            }
            else if (expense.SyncAction == "insert")
            {
                if (expense.ProductCloudId <= 0)
                    continue;

                var cloudId = await cloudExpenses.AddExpenseAsync(new ExpenseInstallmentRow
                {
                    ExpensesNumber = expense.ExpensesNumber,
                    ExpensesDate = expense.ExpensesDate,
                    ProductId = expense.ProductCloudId,
                    ExpensesService = expense.ExpensesService,
                    ExpensesAmount = expense.ExpensesAmount,
                    ExpensesNote = expense.ExpensesNote
                });

                expensesDB.UpdateCloudId(expense.Id, cloudId);
            }
            else if (expense.SyncAction == "update")
            {
                if (expense.CloudId <= 0 || expense.ProductCloudId <= 0)
                    continue;

                await cloudExpenses.UpdateExpenseAsync(new ExpenseInstallmentRow
                {
                    Id = expense.CloudId,
                    ExpensesNumber = expense.ExpensesNumber,
                    ExpensesDate = expense.ExpensesDate,
                    ProductId = expense.ProductCloudId,
                    ExpensesService = expense.ExpensesService,
                    ExpensesAmount = expense.ExpensesAmount,
                    ExpensesNote = expense.ExpensesNote
                });

                expensesDB.MarkSynced(expense.Id);
            }
        }
    }

    private async Task PushDirtyContractsAsync()
    {
        var contractsDB = new ContractServiceInstallment(_db);
        var cloudContracts = new CloudContractsInstallmentService(_supabaseService);

        foreach (var contract in contractsDB.GetDirtyRows())
        {
            if (contract.SyncAction == "delete")
            {
                if (contract.CloudId > 0)
                    await cloudContracts.DeleteContractAsync(contract.CloudId);

                contractsDB.DeleteLocalPermanent(contract.Id);
            }
            else if (contract.SyncAction == "insert")
            {
                if (contract.ProductCloudId <= 0 || contract.CustomerCloudId <= 0)
                    continue;

                var cloudId = await cloudContracts.AddContractAsync(new ContractInstallmentRow
                {
                    ContractNumber = contract.ContractNumber,
                    ContractStartDate = contract.ContractStartDate,
                    ContractEndDate = contract.ContractEndDate,
                    MainTotalAmount = contract.MainTotalAmount,
                    CurrentTotalAmount = contract.CurrentTotalAmount,
                    ContractPeriod = contract.ContractPeriod,
                    DownPayment = contract.DownPayment,
                    MonthlyInstallment = contract.MonthlyInstallment,
                    ManagementFee = contract.ManagementFee,
                    InterestPercent = contract.InterestPercent,
                    ContractState = contract.ContractState,
                    ProductId = contract.ProductCloudId,
                    CustomerId = contract.CustomerCloudId,

                    SignatureCloudPath = contract.SignatureCloudPath,
                    SignatureFileName = contract.SignatureFileName,
                    SignatureFileType = contract.SignatureFileType
                });

                contractsDB.UpdateCloudId(contract.Id, cloudId);
            }
            else if (contract.SyncAction == "update")
            {
                if (contract.CloudId <= 0 || contract.ProductCloudId <= 0 || contract.CustomerCloudId <= 0)
                    continue;

                await cloudContracts.UpdateContractAsync(contract.CloudId, new ContractInstallmentRow
                {
                    ContractNumber = contract.ContractNumber,
                    ContractStartDate = contract.ContractStartDate,
                    ContractEndDate = contract.ContractEndDate,
                    MainTotalAmount = contract.MainTotalAmount,
                    CurrentTotalAmount = contract.CurrentTotalAmount,
                    ContractPeriod = contract.ContractPeriod,
                    DownPayment = contract.DownPayment,
                    MonthlyInstallment = contract.MonthlyInstallment,
                    ManagementFee = contract.ManagementFee,
                    InterestPercent = contract.InterestPercent,
                    ContractState = contract.ContractState,
                    ProductId = contract.ProductCloudId,
                    CustomerId = contract.CustomerCloudId,

                    SignatureCloudPath = contract.SignatureCloudPath,
                    SignatureFileName = contract.SignatureFileName,
                    SignatureFileType = contract.SignatureFileType
                });

                contractsDB.MarkSynced(contract.Id);
            }
        }
    }

    private async Task PushDirtyProductsAsync()
    {
        var productsDB = new ProductServiceInstallment(_db);
        var cloudProducts = new CloudProductsInstallmentService(_supabaseService);

        foreach (var product in productsDB.GetDirtyRows())
        {
            if (product.SyncAction == "delete")
            {
                if (product.CloudId > 0)
                    await cloudProducts.DeleteProductAsync(product.CloudId);

                productsDB.DeleteLocalPermanent(product.Id);
            }
            else if (product.SyncAction == "insert")
            {
                if (product.OwnerCloudId <= 0)
                    continue;

                var cloudId = await cloudProducts.AddProductAsync(new ProductInstallmentRow
                {
                    OwnerId = product.OwnerCloudId,
                    ProductName = product.ProductName,
                    ProductType = product.ProductType,
                    ProductMainPrice = product.ProductMainPrice,
                    CarPlateNumber = product.CarPlateNumber,
                    CarVIN = product.CarVIN,
                    CarModel = product.CarModel,
                    CarColor = product.CarColor,
                    MobileStorage = product.MobileStorage,
                    MobileColor = product.MobileColor
                });

                productsDB.UpdateCloudId(product.Id, cloudId);
            }
            else if (product.SyncAction == "update")
            {
                if (product.CloudId <= 0 || product.OwnerCloudId <= 0)
                    continue;

                await cloudProducts.UpdateProductAsync(product.CloudId, new ProductInstallmentRow
                {
                    OwnerId = product.OwnerCloudId,
                    ProductName = product.ProductName,
                    ProductType = product.ProductType,
                    ProductMainPrice = product.ProductMainPrice,
                    CarPlateNumber = product.CarPlateNumber,
                    CarVIN = product.CarVIN,
                    CarModel = product.CarModel,
                    CarColor = product.CarColor,
                    MobileStorage = product.MobileStorage,
                    MobileColor = product.MobileColor
                });

                productsDB.MarkSynced(product.Id);
            }
        }
    }

    private async Task PushDirtyCustomersAsync()
    {
        var customersDB = new CustomerServiceInstallment(_db);
        var cloudCustomers = new CloudCustomersInstallmentService(_supabaseService);

        foreach (var customer in customersDB.GetDirtyRows())
        {
            if (customer.SyncAction == "delete")
            {
                if (customer.CloudId > 0)
                    await cloudCustomers.DeleteCustomerAsync(customer.CloudId);

                customersDB.DeleteLocalPermanent(customer.Id);
            }
            else if (customer.SyncAction == "insert")
            {
                var cloudId = await cloudCustomers.AddCustomerAsync(new CustomerInstallmentRow
                {
                    Name = customer.Name,
                    IdentityNumber = customer.IdentityNumber,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    Job = customer.Job,
                    SponserName = customer.SponserName,
                    SponserIdentityNumber = customer.SponserIdentityNumber,
                    SponserPhone = customer.SponserPhone,
                    SponserAddress = customer.SponserAddress,
                    SponserJob = customer.SponserJob
                });

                customersDB.UpdateCloudId(customer.Id, cloudId);
            }
            else if (customer.SyncAction == "update")
            {
                if (customer.CloudId <= 0)
                    continue;

                await cloudCustomers.UpdateCustomerAsync(customer.CloudId, new CustomerInstallmentRow
                {
                    Name = customer.Name,
                    IdentityNumber = customer.IdentityNumber,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    Job = customer.Job,
                    SponserName = customer.SponserName,
                    SponserIdentityNumber = customer.SponserIdentityNumber,
                    SponserPhone = customer.SponserPhone,
                    SponserAddress = customer.SponserAddress,
                    SponserJob = customer.SponserJob
                });

                customersDB.MarkSynced(customer.Id);
            }
        }
    }

    private async Task PushDirtyOwnersAsync()
    {
        var ownersDB = new OwnerInstallmentService(_db);
        var cloudOwners = new CloudOwnersInstallmentService(_supabaseService);

        foreach (var owner in ownersDB.GetDirtyRows())
        {
            if (owner.SyncAction == "delete")
            {
                if (owner.CloudId > 0)
                    await cloudOwners.DeleteOwnerAsync(owner.CloudId);

                ownersDB.DeleteLocalPermanent(owner.Id);
            }
            else if (owner.SyncAction == "insert")
            {
                var cloudId = await cloudOwners.AddOwnerAsync(new OwnerInstallmentRow
                {
                    Name = owner.Name,
                    IdentityNumber = owner.IdentityNumber,
                    Phone = owner.Phone,
                    Address = owner.Address
                });

                ownersDB.UpdateCloudId(owner.Id, cloudId);
            }
            else if (owner.SyncAction == "update")
            {
                if (owner.CloudId <= 0)
                    continue;

                await cloudOwners.UpdateOwnerAsync(owner.CloudId, new OwnerInstallmentRow
                {
                    Name = owner.Name,
                    IdentityNumber = owner.IdentityNumber,
                    Phone = owner.Phone,
                    Address = owner.Address
                });

                ownersDB.MarkSynced(owner.Id);
            }
        }
    }
}