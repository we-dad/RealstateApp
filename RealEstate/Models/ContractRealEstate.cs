using System;
using Avalonia.Media;

namespace RealEstateInstallmentsManager.Models;

public class ContractRealEstate
{
    //Contract
    public long Id { get; set; }
    public long CloudId { get; set; }
    public long UnitCloudId { get; set; }
    public long TenantCloudId { get; set; }
    public string SyncAction { get; set; } = "";
    public string ContractNumber { get; set; } = "";
    public DateTime ContractStartDate { get; set; }
    public DateTime ContractEndDate { get; set; }
    public double RentAmount { get; set; }
    public string ContractState { get; set; } = "جاري";

    public string ContractPayMethod { get; set; } = "شهري";
    public string ContractApartmentType { get; set; } = "غرفة مفروشة";
    public int ContractUnitRoomsNum { get; set; }
    public int ContractUnitFloorNum { get; set; }
    public string ContractOpligation { get; set; } = "يتحمل المؤجر مسؤولية الصيانة كاملة, يتحمل المؤجر فواتير الكهرباء والماء";
    public int ContractPeriod { get; set; }



    //Unit
    public long UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public string City { get; set; } = "";
    public string District { get; set; } = "";
    public string UnitType { get; set; } = "";
    public int UnitsCount { get; set; }
    public int UnitNum { get; set; }


    //Tenant
    public long TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public string TenantIdentityNumber { get; set; } = "";
    public string TenantPhone { get; set; } = "";
    public string TenantAddress { get; set; } = "";

    //Owner
    public long OwnerId { get; set; }
    public string OwnerName { get; set; } = "";
    public string OwnerIdentityNumber { get; set; } = "";
    public string OwnerPhone { get; set; } = "";
    public string OwnerAddress { get; set; } = "";

    //for state box color
    public IBrush StateColor =>
        ContractState == "منتهي" ? Brushes.Red : Brushes.Green;
}
