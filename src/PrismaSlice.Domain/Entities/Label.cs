namespace PrismaSlice.Domain.Entities;

public class Label
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#00c8ff";

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}