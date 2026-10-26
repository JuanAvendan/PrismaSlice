using PrismaSlice.Domain.Enums;

namespace PrismaSlice.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}