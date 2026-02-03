namespace Tenant.Domain.Entities;

public class Designation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int? Level { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
