namespace Rascunho.Entities;

public class Ingresso
{
    public int Id { get; protected set; }

    public int EventoId { get; protected set; }
    public Evento Evento { get; protected set; } = null!;

    public int UsuarioId { get; protected set; }
    public Usuario Usuario { get; protected set; } = null!;

    public DateTime DataCompra { get; protected set; }
    public decimal ValorPago { get; protected set; }
    public string Status { get; protected set; } = string.Empty; // "Pendente", "Pago", "Cancelado"

    protected Ingresso() { }

    public Ingresso(int eventoId, int usuarioId, decimal valorPago, string status = "Pago")
    {
        EventoId = eventoId;
        UsuarioId = usuarioId;
        ValorPago = valorPago;
        Status = status;
        DataCompra = DateTime.UtcNow;
    }

    public void ConfirmarPagamento() => Status = "Pago";
    public void Cancelar() => Status = "Cancelado";
}