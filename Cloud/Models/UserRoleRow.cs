using Postgrest.Attributes;
using Postgrest.Models;

namespace RealEstateInstallmentsManager.Models.Cloud;

[Table("user_roles")]
public class UserRoleRow : BaseModel
{
    [PrimaryKey("user_id", false)]
    public string UserId { get; set; } = "";

    [Column("role")]
    public string Role { get; set; } = "";
}