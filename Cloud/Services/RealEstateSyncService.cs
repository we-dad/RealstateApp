using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;

namespace RealEstateInstallmentsManager.Services;

public class RealEstateSyncService
{
    private readonly DbServiceRealEstate _db;
    private readonly SupabaseService _supabaseService;

    // ------------------------------------------------------------------
    // CHANGE 1 - one gate for the whole app.
    // Every view creates its own RealEstateSyncService, and several of
    // them fire PushAllDirtyAsync without awaiting. Two overlapping
    // pushes both read IsDirty = 1 on the same rows and both INSERT,
    // which is what produced the duplicate buildings in Supabase.
    // static => shared by all instances.
    // ------------------------------------------------------------------
    private static readonly SemaphoreSlim _pushGate = new SemaphoreSlim(1, 1);

    public RealEstateSyncService(DbServiceRealEstate db, SupabaseService supabaseService)
    {
        _db = db;
        _supabaseService = supabaseService;
    }

    public async Task PushAllDirtyAsync()
    {
        if (!AppSession.CanWriteOnline)
            return;

        // a push is already running - drop this one instead of queuing,
        // it would only re-read the same rows
        if (!await _pushGate.WaitAsync(0))
            return;

        try
        {
            // Children first, parents last
            await PushDirtyReceiptsAsync();
            await PushDirtyExpensesAsync();
            await PushDirtyContractsAsync();
            await PushDirtyUnitsAsync();
            await PushDirtyTenantsAsync();
            await PushDirtyOwnersAsync();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: real estate dirty rows will sync later.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            _pushGate.Release();
        }
    }

    private async Task PushDirtyReceiptsAsync()
    {
        var receiptsDB = new ReceiptServiceRealEstate(_db);
        var cloudReceipts = new CloudReceiptsRealEstateService(_supabaseService);

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

                var cloudId = await cloudReceipts.AddReceiptAsync(new ReceiptRealEstateRow
                {
                    ReceiptNumber = receipt.ReceiptNumber,
                    ReceiptDate = receipt.ReceiptDate,
                    ContractId = receipt.ContractCloudId,
                    PaymentMethod = receipt.PaymentMethod,
                    Amount = receipt.Amount
                });

                receiptsDB.UpdateCloudId(receipt.Id, cloudId);
            }
            else if (receipt.SyncAction == "update")
            {
                if (receipt.CloudId <= 0 || receipt.ContractCloudId <= 0)
                    continue;

                await cloudReceipts.UpdateReceiptAsync(receipt.CloudId, new ReceiptRealEstateRow
                {
                    ReceiptNumber = receipt.ReceiptNumber,
                    ReceiptDate = receipt.ReceiptDate,
                    ContractId = receipt.ContractCloudId,
                    PaymentMethod = receipt.PaymentMethod,
                    Amount = receipt.Amount
                });

                receiptsDB.MarkSynced(receipt.Id);
            }
        }
    }

    private async Task PushDirtyExpensesAsync()
    {
        var expensesDB = new ExpensesServiceRealEstate(_db);
        var cloudExpenses = new CloudExpensesRealEstateService(_supabaseService);

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
                if (expense.UnitCloudId <= 0)
                    continue;

                var cloudId = await cloudExpenses.AddExpenseAsync(new ExpenseRealEstateRow
                {
                    ExpensesNumber = expense.ExpensesNumber,
                    ExpensesDate = expense.ExpensesDate,
                    UnitId = expense.UnitCloudId,
                    ExpensesService = expense.ExpensesService,
                    ExpensesAmount = expense.ExpensesAmount,
                    ExpensesNote = expense.ExpensesNote
                });

                expensesDB.UpdateCloudId(expense.Id, cloudId);
            }
            else if (expense.SyncAction == "update")
            {
                if (expense.CloudId <= 0 || expense.UnitCloudId <= 0)
                    continue;

                await cloudExpenses.UpdateExpenseAsync(expense.CloudId, new ExpenseRealEstateRow
                {
                    ExpensesNumber = expense.ExpensesNumber,
                    ExpensesDate = expense.ExpensesDate,
                    UnitId = expense.UnitCloudId,
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
        var contractsDB = new ContractServiceRealEstate(_db);
        var cloudContracts = new CloudContractsRealEstateService(_supabaseService);

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
                if (contract.UnitCloudId <= 0 || contract.TenantCloudId <= 0)
                    continue;

                var cloudId = await cloudContracts.AddContractAsync(new ContractRealEstateRow
                {
                    ContractNumber = contract.ContractNumber,
                    ContractStartDate = contract.ContractStartDate,
                    ContractEndDate = contract.ContractEndDate,
                    RentAmount = contract.RentAmount,
                    ContractState = contract.ContractState,
                    ContractPayMethod = contract.ContractPayMethod,
                    ContractApartmentType = contract.ContractApartmentType,
                    ContractUnitRoomsNum = contract.ContractUnitRoomsNum,
                    ContractUnitFloorNum = contract.ContractUnitFloorNum,
                    ContractOpligation = contract.ContractOpligation,
                    UnitId = contract.UnitCloudId,
                    TenantId = contract.TenantCloudId,

                    SignatureCloudPath = contract.SignatureCloudPath,
                    SignatureFileName = contract.SignatureFileName,
                    SignatureFileType = contract.SignatureFileType
                });

                contractsDB.UpdateCloudId(contract.Id, cloudId);
            }
            else if (contract.SyncAction == "update")
            {
                if (contract.CloudId <= 0 || contract.UnitCloudId <= 0 || contract.TenantCloudId <= 0)
                    continue;

                await cloudContracts.UpdateContractAsync(contract.CloudId, new ContractRealEstateRow
                {
                    ContractNumber = contract.ContractNumber,
                    ContractStartDate = contract.ContractStartDate,
                    ContractEndDate = contract.ContractEndDate,
                    RentAmount = contract.RentAmount,
                    ContractState = contract.ContractState,
                    ContractPayMethod = contract.ContractPayMethod,
                    ContractApartmentType = contract.ContractApartmentType,
                    ContractUnitRoomsNum = contract.ContractUnitRoomsNum,
                    ContractUnitFloorNum = contract.ContractUnitFloorNum,
                    ContractOpligation = contract.ContractOpligation,
                    UnitId = contract.UnitCloudId,
                    TenantId = contract.TenantCloudId,

                    SignatureCloudPath = contract.SignatureCloudPath,
                    SignatureFileName = contract.SignatureFileName,
                    SignatureFileType = contract.SignatureFileType
                });

                contractsDB.MarkSynced(contract.Id);
            }
        }
    }

    // ------------------------------------------------------------------
    // CHANGE 2 - ParentId is pushed, and the order respects it.
    //
    // Inserts: a building must reach the cloud before its units, because
    //          a unit's row carries the parent's CloudId.
    // Deletes: the units must go before the building, or the cloud is
    //          left with rows pointing at a parent that no longer exists.
    //
    // Requires: a ParentId column on the Supabase units table, and a
    //           ParentId property on UnitRealEstateRow.
    // ------------------------------------------------------------------
    private async Task PushDirtyUnitsAsync()
    {
        var unitsDB = new UnitServiceRealEstate(_db);
        var ownersDB = new OwnerServiceRealEstate(_db);
        var cloudUnits = new CloudUnitsRealEstateService(_supabaseService);

        var dirtyUnits = unitsDB.GetDirtyRows()
            .OrderBy(u => u.SyncAction == "delete" ? 0 : 1)
            .ThenBy(u => u.SyncAction == "delete"
                ? (u.ParentId == 0 ? 1 : 0)      // deletes: children first
                : (u.ParentId == 0 ? 0 : 1))     // inserts: parents first
            .ToList();

        foreach (var unit in dirtyUnits)
        {
            if (unit.SyncAction == "delete")
            {
                if (unit.CloudId > 0)
                    await cloudUnits.DeleteUnitAsync(unit.CloudId);

                unitsDB.DeleteLocalPermanent(unit.Id);
                continue;
            }

            var owner = ownersDB.GetById(unit.OwnerId);

            if (owner == null || owner.CloudId <= 0)
                continue;

            // resolve the parent's cloud id; skip until the parent is up
            long parentCloudId = 0;

            if (unit.ParentId > 0)
            {
                var parent = unitsDB.GetById(unit.ParentId);

                if (parent == null || parent.CloudId <= 0)
                    continue;

                parentCloudId = parent.CloudId;
            }

            var row = new UnitRealEstateRow
            {
                OwnerId = owner.CloudId,
                ParentId = parentCloudId,
                UnitName = unit.UnitName,
                City = unit.City,
                District = unit.District,
                UnitType = unit.UnitType,
                UnitState = unit.UnitState,
                UnitsCount = unit.UnitsCount,
                UnitNum = unit.UnitNum
            };

            if (unit.SyncAction == "insert")
            {
                var cloudId = await cloudUnits.AddUnitAsync(row);
                unitsDB.UpdateCloudId(unit.Id, cloudId);
            }
            else if (unit.SyncAction == "update")
            {
                if (unit.CloudId <= 0)
                    continue;

                await cloudUnits.UpdateUnitAsync(unit.CloudId, row);
                unitsDB.MarkSynced(unit.Id);
            }
        }
    }

    private async Task PushDirtyTenantsAsync()
    {
        var tenantsDB = new TenantServiceRealEstate(_db);
        var cloudTenants = new CloudTenantsRealEstateService(_supabaseService);

        foreach (var tenant in tenantsDB.GetDirtyRows())
        {
            if (tenant.SyncAction == "delete")
            {
                if (tenant.CloudId > 0)
                    await cloudTenants.DeleteTenantAsync(tenant.CloudId);

                tenantsDB.DeleteLocalPermanent(tenant.Id);
            }
            else if (tenant.SyncAction == "insert")
            {
                var cloudId = await cloudTenants.AddTenantAsync(new TenantRealEstateRow
                {
                    Name = tenant.Name,
                    IdentityNumber = tenant.IdentityNumber,
                    Phone = tenant.Phone,
                    Address = tenant.Address
                });

                tenantsDB.UpdateCloudId(tenant.Id, cloudId);
            }
            else if (tenant.SyncAction == "update")
            {
                if (tenant.CloudId <= 0)
                    continue;

                await cloudTenants.UpdateTenantAsync(tenant.CloudId, new TenantRealEstateRow
                {
                    Name = tenant.Name,
                    IdentityNumber = tenant.IdentityNumber,
                    Phone = tenant.Phone,
                    Address = tenant.Address
                });

                tenantsDB.MarkSynced(tenant.Id);
            }
        }
    }

    private async Task PushDirtyOwnersAsync()
    {
        var ownersDB = new OwnerServiceRealEstate(_db);
        var cloudOwners = new CloudOwnersRealEstateService(_supabaseService);

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
                var cloudId = await cloudOwners.AddOwnerAsync(new OwnerRealEstateRow
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

                await cloudOwners.UpdateOwnerAsync(owner.CloudId, new OwnerRealEstateRow
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