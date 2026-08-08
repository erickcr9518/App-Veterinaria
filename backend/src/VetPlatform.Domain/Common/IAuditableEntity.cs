namespace VetPlatform.Domain.Common;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    Guid? CreatedByUserId { get; set; }
    DateTime? ModifiedAtUtc { get; set; }
    Guid? ModifiedByUserId { get; set; }
}
