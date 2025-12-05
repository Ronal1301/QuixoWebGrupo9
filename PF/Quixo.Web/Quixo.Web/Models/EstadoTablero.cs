namespace Quixo.Web.Models
{
    public class EstadoTablero
    {
        public int Id { get; set; }

        public int PartidaId { get; set; }
        public virtual Partida Partida { get; set; }

        // Número de movimiento asociado:
        public int NumeroMovimiento { get; set; }

        // Representación compacta del tablero 5x5
        public string TableroCompacto { get; set; }

        // Tiempo transcurrido en la partida en este momento
        public int SegundosTranscurridos { get; set; }
    }
}
