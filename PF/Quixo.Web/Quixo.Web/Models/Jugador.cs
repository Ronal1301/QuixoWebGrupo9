using System.Collections.Generic;

namespace Quixo.Web.Models
{
    public class Jugador
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        // algún nickname o código
        public string Alias { get; set; }

        public int PartidasJugadas { get; set; }
        public int PartidasGanadas { get; set; }

        public virtual ICollection<Partida> PartidasComoJugador1 { get; set; }
        public virtual ICollection<Partida> PartidasComoJugador2 { get; set; }
        public virtual ICollection<Partida> PartidasComoJugador3 { get; set; }
        public virtual ICollection<Partida> PartidasComoJugador4 { get; set; }

        public Jugador()
        {
            PartidasComoJugador1 = new List<Partida>();
            PartidasComoJugador2 = new List<Partida>();
            PartidasComoJugador3 = new List<Partida>();
            PartidasComoJugador4 = new List<Partida>();
        }
    }
}
