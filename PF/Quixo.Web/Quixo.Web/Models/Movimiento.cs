using System;

namespace Quixo.Web.Models
{
    public class Movimiento
    {
        public int Id { get; set; }

        public int NumeroMovimiento { get; set; }

        public int PartidaId { get; set; }
        public virtual Partida Partida { get; set; }

        public int? JugadorId { get; set; }
        public virtual Jugador Jugador { get; set; }

        public int FilaOrigen { get; set; }
        public int ColumnaOrigen { get; set; }

        public int FilaDestino { get; set; }
        public int ColumnaDestino { get; set; }

        public string DireccionEmpuje { get; set; }

        public DateTime FechaHora { get; set; }

        public Movimiento()
        {
            FechaHora = DateTime.Now;
        }
    }
}
