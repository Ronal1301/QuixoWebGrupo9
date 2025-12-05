using System;
using System.Collections.Generic;

namespace Quixo.Web.Models
{
    // DTO para exportar una partida a XML
    public class PartidaXmlDto
    {
        public int Id { get; set; }
        public ModoPartida Modo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public ResultadoPartida Resultado { get; set; }

        // Lo guardamos como string para que sea más simple en XML (hh:mm:ss)
        public string DuracionTotal { get; set; }

        // Tablero final en formato compacto (25 caracteres)
        public string TableroFinal { get; set; }

        public List<MovimientoXmlDto> Movimientos { get; set; }
    }

    public class MovimientoXmlDto
    {
        public int NumeroMovimiento { get; set; }
        public int FilaOrigen { get; set; }
        public int ColumnaOrigen { get; set; }
        public int FilaDestino { get; set; }
        public int ColumnaDestino { get; set; }
        public string DireccionEmpuje { get; set; }
        public DateTime FechaHora { get; set; }
    }
}
