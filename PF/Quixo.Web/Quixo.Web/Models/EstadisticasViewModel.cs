using System.Collections.Generic;

namespace Quixo.Web.Models
{
    public class EstadisticaFilaViewModel
    {
        public string Nombre { get; set; }
        public int PartidasGanadas { get; set; }
        public int PartidasTotales { get; set; }
        public double Efectividad { get; set; } // 0–100
    }

    public class EstadisticasViewModel
    {
        // Para modo de dos jugadores (Jugador Primero / Segundo)
        public List<EstadisticaFilaViewModel> DosJugadores { get; set; }

        // Para modo de cuatro jugadores (Equipo A / Equipo B)
        public List<EstadisticaFilaViewModel> CuatroJugadores { get; set; }

        public EstadisticasViewModel()
        {
            DosJugadores = new List<EstadisticaFilaViewModel>();
            CuatroJugadores = new List<EstadisticaFilaViewModel>();
        }
    }
}
