using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace RealEstateInstallmentsManager.Models.Cloud;

#region Owners

[Table("owners_real_estate")]
public class OwnerRealEstateRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("identity_number")]
    public string IdentityNumber { get; set; } = "";

    [Column("phone")]
    public string Phone { get; set; } = "";

    [Column("address")]
    public string Address { get; set; } = "";
}

#endregion

#region Units

[Table("units_real_estate")]
public class UnitRealEstateRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("owner_id")]
    public long OwnerId { get; set; }
    
    [Column("parent_id")]
    public long ParentId { get; set; }

    [Column("unit_name")]
    public string UnitName { get; set; } = "";

    [Column("city")]
    public string City { get; set; } = "";

    [Column("district")]
    public string District { get; set; } = "";

    [Column("unit_type")]
    public string UnitType { get; set; } = "";

    [Column("unit_state")]
    public string UnitState { get; set; } = "";

    [Column("units_count")]
    public int UnitsCount { get; set; }

    [Column("unit_num")]
    public int UnitNum { get; set; }
}

#endregion

[Table("tenants_real_estate")]
public class TenantRealEstateRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("identity_number")]
    public string IdentityNumber { get; set; } = "";

    [Column("phone")]
    public string Phone { get; set; } = "";

    [Column("address")]
    public string Address { get; set; } = "";
}

[Table("contracts_real_estate")]
public class ContractRealEstateRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("contract_number")]
    public string ContractNumber { get; set; } = "";

    [Column("contract_start_date")]
    public DateTime ContractStartDate { get; set; }

    [Column("contract_end_date")]
    public DateTime ContractEndDate { get; set; }

    [Column("rent_amount")]
    public double RentAmount { get; set; }

    [Column("contract_state")]
    public string ContractState { get; set; } = "";

    [Column("contract_pay_method")]
    public string ContractPayMethod { get; set; } = "";

    [Column("contract_apartment_type")]
    public string ContractApartmentType { get; set; } = "";

    [Column("contract_unit_rooms_num")]
    public int ContractUnitRoomsNum { get; set; }

    [Column("contract_unit_floor_num")]
    public int ContractUnitFloorNum { get; set; }

    [Column("contract_opligation")]
    public string ContractOpligation { get; set; } = "";

    [Column("unit_id")]
    public long UnitId { get; set; }

    [Column("tenant_id")]
    public long TenantId { get; set; }

    [Column("signature_cloud_path")]
    public string SignatureCloudPath { get; set; } = "";

    [Column("signature_file_name")]
    public string SignatureFileName { get; set; } = "";

    [Column("signature_file_type")]
    public string SignatureFileType { get; set; } = "";
}

[Table("receipts_real_estate")]
public class ReceiptRealEstateRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("receipt_number")]
    public string ReceiptNumber { get; set; } = "";

    [Column("receipt_date")]
    public DateTime ReceiptDate { get; set; }

    [Column("contract_id")]
    public long ContractId { get; set; }

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "";

    [Column("amount")]
    public double Amount { get; set; }
}

[Table("expenses_real_estate")]
public class ExpenseRealEstateRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("expenses_number")]
    public string ExpensesNumber { get; set; } = "";

    [Column("expenses_date")]
    public DateTime ExpensesDate { get; set; }

    [Column("expenses_service")]
    public string ExpensesService { get; set; } = "";

    [Column("expenses_amount")]
    public double ExpensesAmount { get; set; }

    [Column("expenses_note")]
    public string ExpensesNote { get; set; } = "";

    [Column("unit_id")]
    public long UnitId { get; set; }
}

[Table("owners_installment")]
public class OwnerInstallmentRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("identity_number")]
    public string IdentityNumber { get; set; } = "";

    [Column("phone")]
    public string Phone { get; set; } = "";

    [Column("address")]
    public string Address { get; set; } = "";
}

[Table("products_installment")]
public class ProductInstallmentRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("owner_id")]
    public long OwnerId { get; set; }

    [Column("product_name")]
    public string ProductName { get; set; } = "";

    [Column("product_type")]
    public string ProductType { get; set; } = "";

    [Column("product_main_price")]
    public double ProductMainPrice { get; set; }

    [Column("car_plate_number")]
    public string CarPlateNumber { get; set; } = "";

    [Column("car_vin")]
    public string CarVIN { get; set; } = "";

    [Column("car_model")]
    public string CarModel { get; set; } = "";

    [Column("car_color")]
    public string CarColor { get; set; } = "";

    [Column("mobile_storage")]
    public string MobileStorage { get; set; } = "";

    [Column("mobile_color")]
    public string MobileColor { get; set; } = "";
}

[Table("customers_installment")]
public class CustomerInstallmentRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("identity_number")]
    public string IdentityNumber { get; set; } = "";

    [Column("phone")]
    public string Phone { get; set; } = "";

    [Column("address")]
    public string Address { get; set; } = "";

    [Column("job")]
    public string Job { get; set; } = "";

    [Column("sponser_name")]
    public string SponserName { get; set; } = "";

    [Column("sponser_identity_number")]
    public string SponserIdentityNumber { get; set; } = "";

    [Column("sponser_phone")]
    public string SponserPhone { get; set; } = "";

    [Column("sponser_address")]
    public string SponserAddress { get; set; } = "";

    [Column("sponser_job")]
    public string SponserJob { get; set; } = "";
}
[Table("contracts_installment")]
public class ContractInstallmentRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("contract_number")]
    public string ContractNumber { get; set; } = "";

    [Column("contract_start_date")]
    public DateTime ContractStartDate { get; set; }

    [Column("contract_end_date")]
    public DateTime ContractEndDate { get; set; }

    [Column("main_total_amount")]
    public double MainTotalAmount { get; set; }

    [Column("current_total_amount")]
    public double CurrentTotalAmount { get; set; }

    [Column("contract_period")]
    public double ContractPeriod { get; set; }

    [Column("down_payment")]
    public double DownPayment { get; set; }

    [Column("monthly_installment")]
    public double MonthlyInstallment { get; set; }

    [Column("management_fee")]
    public double ManagementFee { get; set; }

    [Column("interest_percent")]
    public double InterestPercent { get; set; }

    [Column("contract_state")]
    public string ContractState { get; set; } = "";

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("customer_id")]
    public long CustomerId { get; set; }

    [Column("signature_cloud_path")]
    public string SignatureCloudPath { get; set; } = "";

    [Column("signature_file_name")]
    public string SignatureFileName { get; set; } = "";

    [Column("signature_file_type")]
    public string SignatureFileType { get; set; } = "";
}

[Table("receipts_installment")]
public class ReceiptInstallmentRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("receipt_number")]
    public string ReceiptNumber { get; set; } = "";

    [Column("receipt_date")]
    public DateTime ReceiptDate { get; set; }

    [Column("contract_id")]
    public long ContractId { get; set; }

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "";

    [Column("amount")]
    public double Amount { get; set; }

    [Column("current_total_amount")]
    public double CurrentTotalAmount { get; set; }
}

[Table("expenses_installment")]
public class ExpenseInstallmentRow : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("expenses_number")]
    public string ExpensesNumber { get; set; } = "";

    [Column("expenses_date")]
    public DateTime ExpensesDate { get; set; }

    [Column("expenses_service")]
    public string ExpensesService { get; set; } = "";

    [Column("expenses_amount")]
    public double ExpensesAmount { get; set; }

    [Column("expenses_note")]
    public string ExpensesNote { get; set; } = "";

    [Column("product_id")]
    public long ProductId { get; set; }
}