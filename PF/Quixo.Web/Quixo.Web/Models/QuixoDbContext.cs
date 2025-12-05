using System.Data.Entity;

namespace Quixo.Web.Models
{
    public class QuixoDbContext : DbContext
    {
        // Nombre de la conexión (lo usaremos en Web.config)
        public QuixoDbContext() : base("QuixoConnection")
        {
        }

        public DbSet<Jugador> Jugadores { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Partida> Partidas { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }
        public DbSet<EstadoTablero> EstadosTablero { get; set; }
    }
}
