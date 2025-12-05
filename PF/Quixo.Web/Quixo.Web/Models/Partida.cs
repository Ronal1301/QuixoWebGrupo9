using System;
using System.Collections.Generic;

namespace Quixo.Web.Models
{
    public enum ModoPartida
    {
        DosJugadores = 1,
        CuatroJugadores = 2
    }

    public enum ResultadoPartida
    {
        EnCurso = 0,
        GanoJugador1,
        GanoJugador2,
        GanoJugador3,
        GanoJugador4,
        GanoEquipoA,
        GanoEquipoB
    }

    public class Partida
    {
        public int Id { get; set; }

        public ModoPartida Modo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public TimeSpan DuracionTotal { get; set; }

        public ResultadoPartida Resultado { get; set; }

        public int? Jugador1Id { get; set; }
        public int? Jugador2Id { get; set; }
        public int? Jugador3Id { get; set; }
        public int? Jugador4Id { get; set; }

        public virtual Jugador Jugador1 { get; set; }
        public virtual Jugador Jugador2 { get; set; }
        public virtual Jugador Jugador3 { get; set; }
        public virtual Jugador Jugador4 { get; set; }

        // Relación con equipos (modo 4 jugadores)
        public int? EquipoAId { get; set; }
        public int? EquipoBId { get; set; }

        public virtual Equipo EquipoA { get; set; }
        public virtual Equipo EquipoB { get; set; }

        public virtual ICollection<Movimiento> Movimientos { get; set; }

        public virtual ICollection<EstadoTablero> EstadosTablero { get; set; }

        public Partida()
        {
            FechaCreacion = DateTime.Now;
            Resultado = ResultadoPartida.EnCurso;
            Movimientos = new List<Movimiento>();
            EstadosTablero = new List<EstadoTablero>();
        }
    }
}
