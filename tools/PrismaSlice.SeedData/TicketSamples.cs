using PrismaSlice.Domain.Enums;

namespace PrismaSlice.SeedData;

// Plantilla de ticket sin resolver aún a IDs (eso pasa en el inserter).
public record TicketSeed(
    string Title,
    string Description,
    string Category,
    TicketStatus Status,
    TicketPriority Priority,
    string[] Labels);

public static class TicketSamples
{
    public static readonly TicketSeed[] Sample =
    [
        new("La caja 3 no imprime el recibo tras el pago",
            "En la tienda del centro comercial la caja registradora número 3 procesa el cobro pero la impresora térmica no emite el recibo. El cajero debe reimprimir manualmente desde el historial. Ocurre desde la actualización del POS de ayer.",
            "Punto de venta (POS)", TicketStatus.Open, TicketPriority.High,
            ["pos", "bug", "urgente"]),

        new("Diferencia de stock entre bodega y tienda online",
            "El inventario muestra 12 unidades de un producto en el sistema, pero físicamente en bodega hay 4. La tienda online sigue permitiendo compras que luego no se pueden despachar. Se necesita revisar la sincronización del inventario.",
            "Inventario", TicketStatus.InProgress, TicketPriority.Critical,
            ["inventario", "bug"]),

        new("Solicito acceso al panel de reportes de ventas",
            "Como nuevo coordinador de tienda necesito acceso de solo lectura al panel de reportes de ventas diarias para hacer seguimiento de metas. Mi jefe directo ya aprobó la solicitud por correo.",
            "Cuentas y accesos", TicketStatus.Open, TicketPriority.Low,
            ["capacitación"]),

        new("El pago con tarjeta falla en el checkout web",
            "Varios clientes reportan que al pagar con tarjeta de crédito en la tienda online reciben el error 'transacción rechazada' aunque la tarjeta tiene fondos. El pago con PSE sí funciona. Parece un problema con la pasarela.",
            "Pagos", TicketStatus.InProgress, TicketPriority.Critical,
            ["pagos", "bug", "urgente"]),

        new("Lentitud general al cargar el catálogo de productos",
            "La página de catálogo tarda más de 15 segundos en cargar en horas pico. Los clientes abandonan el carrito. Empezó a notarse después de subir las fotos en alta resolución de la nueva colección.",
            "E-commerce", TicketStatus.Open, TicketPriority.High,
            ["mejora", "urgente"]),

        new("Agregar filtro por talla en la tienda online",
            "Los clientes piden poder filtrar productos por talla directamente en el listado, sin tener que entrar a cada producto. Sería una mejora importante para la experiencia de compra.",
            "E-commerce", TicketStatus.Open, TicketPriority.Medium,
            ["mejora"]),

        new("La sincronización con la contabilidad se detuvo",
            "El proceso nocturno que envía las ventas del día al sistema contable no corrió las últimas dos noches. El área financiera no tiene los datos actualizados. Requiere revisión del job programado.",
            "Infraestructura", TicketStatus.InProgress, TicketPriority.High,
            ["integración", "bug"]),

        new("Capacitación en el nuevo módulo de devoluciones",
            "El personal de tienda necesita una sesión de capacitación sobre cómo procesar devoluciones y cambios en el nuevo módulo del POS. Solicitamos agendar para la próxima semana.",
            "Punto de venta (POS)", TicketStatus.Open, TicketPriority.Low,
            ["capacitación", "pos"]),

        new("Los correos de confirmación de pedido no llegan",
            "Desde el fin de semana los clientes no reciben el correo de confirmación tras hacer una compra online. El pedido sí queda registrado en el sistema, pero genera desconfianza en el cliente.",
            "E-commerce", TicketStatus.Open, TicketPriority.High,
            ["integración", "bug", "urgente"]),

        new("Duplicación de clientes en el CRM",
            "Al importar la base de clientes de la campaña, se crearon registros duplicados. Un mismo cliente aparece hasta tres veces con correos ligeramente distintos. Afecta el envío de comunicaciones.",
            "CRM", TicketStatus.InProgress, TicketPriority.Medium,
            ["bug"]),

        new("Solicito reporte de rotación de inventario mensual",
            "Necesito un reporte automático que muestre qué productos tienen mayor y menor rotación cada mes, para tomar decisiones de reabastecimiento. Hoy lo hacemos manual en Excel.",
            "Reportería", TicketStatus.Open, TicketPriority.Medium,
            ["mejora", "inventario"]),

        new("El lector de código de barras no reconoce productos nuevos",
            "Los productos de la última colección no son reconocidos por el lector en caja. Toca digitar el código manualmente, lo que hace lentas las filas. Parece que faltó cargar los códigos al sistema.",
            "Punto de venta (POS)", TicketStatus.Resolved, TicketPriority.Medium,
            ["pos", "inventario"]),

        new("Caída intermitente de la red en la tienda norte",
            "La conexión a internet en la tienda del norte se cae varias veces al día por unos minutos. Durante esos lapsos el POS no puede procesar pagos con tarjeta. Afecta las ventas.",
            "Infraestructura", TicketStatus.InProgress, TicketPriority.High,
            ["urgente"]),

        new("Descuento de campaña no se aplica en el carrito",
            "El cupón de la campaña de temporada no se está aplicando en el checkout. El cliente ingresa el código y el sistema dice que es inválido, aunque la campaña está vigente.",
            "Pagos", TicketStatus.Open, TicketPriority.High,
            ["pagos", "bug"]),

        new("Restablecer contraseña de usuario bloqueado",
            "Un usuario de bodega quedó bloqueado tras varios intentos fallidos de inicio de sesión. Necesita que se le restablezca la contraseña para poder registrar las entradas de mercancía de hoy.",
            "Cuentas y accesos", TicketStatus.Closed, TicketPriority.Low,
            []),
    ];
}