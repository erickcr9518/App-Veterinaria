using VetPlatform.Domain.Common;

namespace VetPlatform.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
