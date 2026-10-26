using PrismaSlice.Domain.Entities;
using PrismaSlice.Domain.Enums;

namespace PrismaSlice.SeedData;

public static class SeedCatalog
{
    public static List<Department> Departments() =>
    [
        new() { Name = "Tecnología",   Code = "TECH" },
        new() { Name = "Operaciones",  Code = "OPS"  },
        new() { Name = "Comercial",    Code = "COM"  },
    ];

    // Los equipos se enlazan a departamentos por índice tras insertar.
    public static readonly (string Name, string Code, string DeptCode)[] TeamDefs =
    [
        ("Soporte TI",     "SUP",  "TECH"),
        ("Infraestructura","INFRA","TECH"),
        ("Desarrollo",     "DEV",  "TECH"),
        ("E-commerce",     "ECOM", "OPS"),
        ("Logística",      "LOG",  "OPS"),
        ("CRM",            "CRM",  "COM"),
    ];

    public static readonly (string Name, string Email, UserRole Role, string TeamCode)[] UserDefs =
    [
        ("Carolina Restrepo", "carolina.restrepo@retaildemo.co", UserRole.Admin, "SUP"),
        ("Andrés Gómez",      "andres.gomez@retaildemo.co",      UserRole.Agent, "SUP"),
        ("Valentina Ruiz",    "valentina.ruiz@retaildemo.co",    UserRole.Agent, "INFRA"),
        ("Julián Mesa",       "julian.mesa@retaildemo.co",       UserRole.Agent, "DEV"),
        ("Daniela Ospina",    "daniela.ospina@retaildemo.co",    UserRole.Agent, "ECOM"),
        ("Felipe Cardona",    "felipe.cardona@retaildemo.co",    UserRole.Agent, "LOG"),
        ("Mónica Vélez",      "monica.velez@retaildemo.co",      UserRole.Agent, "CRM"),
        ("Sebastián Arango",  "sebastian.arango@retaildemo.co",  UserRole.Client, "ECOM"),
        ("Laura Jiménez",     "laura.jimenez@retaildemo.co",     UserRole.Client, "LOG"),
        ("Camilo Torres",     "camilo.torres@retaildemo.co",     UserRole.Client, "CRM"),
        ("Natalia Duque",     "natalia.duque@retaildemo.co",     UserRole.Client, "SUP"),
        ("Ricardo Peña",      "ricardo.pena@retaildemo.co",      UserRole.Agent, "DEV"),
    ];

    public static readonly (string Name, string Color)[] LabelDefs =
    [
        ("bug",          "#e5484d"),
        ("mejora",       "#30a46c"),
        ("urgente",      "#f76808"),
        ("pos",          "#0091ff"),
        ("inventario",   "#8e4ec6"),
        ("pagos",        "#ffb224"),
        ("integración",  "#00a2c7"),
        ("capacitación", "#6e56cf"),
    ];
}