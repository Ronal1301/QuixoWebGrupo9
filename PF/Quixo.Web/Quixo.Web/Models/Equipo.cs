using System.Collections.Generic;

namespace Quixo.Web.Models
{
    public class Equipo
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public int PartidasJugadas { get; set; }
        public int PartidasGanadas { get; set; }

        // Jugadores que pertenecen a este equipo
        public virtual ICollection<Jugador> Jugadores { get; set; }

        // Partidas en las que este equipo participó
        public virtual ICollection<Partida> Partidas { get; set; }

        public Equipo()
        {
            Jugadores = new List<Jugador>();
            Partidas = new List<Partida>();
        }
    }
}
