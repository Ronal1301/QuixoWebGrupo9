using System;
using System.Text;

namespace Quixo.Web.Models
{
    public enum CellOwner
    {
        None = 0,
        Player1 = 1,
        Player2 = 2
    }

    public enum OrientationPoint
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 3,
        Left = 4
    }

    public class TableroQuixo
    {
        public const int Size = 5;

        private readonly CellOwner[,] _cells;
        private readonly OrientationPoint[,] _orientations;

        public TableroQuixo()
        {
            _cells = new CellOwner[Size, Size];
            _orientations = new OrientationPoint[Size, Size];

            for (int f = 0; f < Size; f++)
            {
                for (int c = 0; c < Size; c++)
                {
                    _cells[f, c] = CellOwner.None;
                    _orientations[f, c] = OrientationPoint.None;
                }
            }
        }

        public CellOwner GetCell(int fila, int col) => _cells[fila, col];
        public OrientationPoint GetOrientation(int fila, int col) => _orientations[fila, col];

        // =================== SERIALIZACIÓN ===================
        public string ACompacto()
        {
            var sb = new StringBuilder(Size * Size * 2);

            for (int f = 0; f < Size; f++)
            {
                for (int c = 0; c < Size; c++)
                {
                    sb.Append(OwnerToChar(_cells[f, c]));
                    sb.Append(OrientationToChar(_orientations[f, c]));
                }
            }

            return sb.ToString();
        }

        public static TableroQuixo DesdeCompacto(string data)
        {
            var t = new TableroQuixo();

            if (string.IsNullOrEmpty(data))
                return t;

            if (data.Length == Size * Size)
            {
                int idx = 0;
                for (int f = 0; f < Size; f++)
                {
                    for (int c = 0; c < Size; c++)
                    {
                        t._cells[f, c] = CharToOwner(data[idx++]);
                        t._orientations[f, c] = OrientationPoint.None;
                    }
                }
            }
            else if (data.Length == Size * Size * 2)
            {
                int idx = 0;
                for (int f = 0; f < Size; f++)
                {
                    for (int c = 0; c < Size; c++)
                    {
                        char owner = data[idx++];
                        char orient = data[idx++];

                        t._cells[f, c] = CharToOwner(owner);
                        t._orientations[f, c] = CharToOrientation(orient);
                    }
                }
            }

            return t;
        }

        // =================== MÉTODOS NUEVOS ===================

        /// <summary>
        /// Determina si el jugador tiene al menos UN movimiento legal.
        /// Si no tiene ninguno → debe saltarse el turno.
        /// </summary>
        public bool TieneMovimientosValidos(CellOwner jugador, int? jugadorIndex, bool esPrimeraVuelta)
        {
            for (int f = 0; f < Size; f++)
            {
                for (int c = 0; c < Size; c++)
                {
                    // Solo periferia
                    if (!EsPeriferia(f, c)) continue;

                    var owner = _cells[f, c];
                    var orient = _orientations[f, c];

                    // Regla primera vuelta (solo neutros)
                    if (esPrimeraVuelta && owner != CellOwner.None)
                        continue;

                    // No se pueden tomar cubos del enemigo
                    if (owner != CellOwner.None && owner != jugador)
                        continue;

                    // Regla del punto en modo 4 jugadores (si aplica)
                    if (jugadorIndex.HasValue && owner == jugador)
                    {
                        var esperado = OrientationFromJugadorIndex(jugadorIndex.Value);
                        if (orient != esperado)
                            continue;
                    }

                    // Ahora probar si existe algún destino válido
                    if (ExisteDestinoValido(f, c))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Revisa si desde esa celda existe algún destino válido.
        /// </summary>
        private bool ExisteDestinoValido(int filaOrigen, int colOrigen)
        {
            // Intentar 4 posibles destinos (las esquinas de esa fila/columna)
            int[] extremos = { 0, Size - 1 };

            foreach (var dest in extremos)
            {
                // misma fila
                if (dest != colOrigen)
                {
                    if (EsMovimientoValidoBasico(filaOrigen, colOrigen, filaOrigen, dest))
                        return true;
                }

                // misma columna
                if (dest != filaOrigen)
                {
                    if (EsMovimientoValidoBasico(filaOrigen, colOrigen, dest, colOrigen))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reglas básicas de movimiento sin considerar dueño ni orientación (ya validadas afuera).
        /// </summary>
        private bool EsMovimientoValidoBasico(int filaOrigen, int colOrigen, int filaDestino, int colDestino)
        {
            bool mismaFila = filaOrigen == filaDestino;
            bool mismaCol = colOrigen == colDestino;

            if (!(mismaFila ^ mismaCol))
                return false;

            if (mismaFila && (colDestino == 0 || colDestino == Size - 1))
                return true;

            if (mismaCol && (filaDestino == 0 || filaDestino == Size - 1))
                return true;

            return false;
        }

        // =================== MOVER ===================
        public bool Mover(int filaOrigen, int colOrigen, int filaDestino, int colDestino,
            CellOwner jugador, out string error)
        {
            return MoverInterno(filaOrigen, colOrigen, filaDestino, colDestino,
                jugador, null, null, out error);
        }

        public bool MoverCuatroJugadores(
           int filaOrigen, int colOrigen, int filaDestino, int colDestino,
           CellOwner jugador, int jugadorIndex, OrientationPoint orientDestino,
           out string error)
        {
            return MoverInterno(filaOrigen, colOrigen, filaDestino, colDestino,
                jugador, jugadorIndex, orientDestino, out error);
        }

        private bool MoverInterno(
            int filaOrigen, int colOrigen, int filaDestino, int colDestino,
            CellOwner jugador,
            int? jugadorIndex, OrientationPoint? orientDestino,
            out string error)
        {
            error = null;

            // Validaciones existentes (NO SE TOCARON)
            if (!EsValidaPosicion(filaOrigen, colOrigen) ||
                !EsValidaPosicion(filaDestino, colDestino))
            {
                error = "Posición fuera del tablero.";
                return false;
            }

            if (filaOrigen == filaDestino && colOrigen == colDestino)
            {
                error = "El origen y el destino no pueden ser el mismo.";
                return false;
            }

            if (!EsPeriferia(filaOrigen, colOrigen))
            {
                error = "Solo se pueden retirar cubos de la periferia del tablero.";
                return false;
            }

            bool mismaFila = filaOrigen == filaDestino;
            bool mismaCol = colOrigen == colDestino;

            if (!(mismaFila ^ mismaCol))
            {
                error = "El destino debe estar en la MISMA fila o columna, y en un extremo.";
                return false;
            }

            if (mismaFila && !(colDestino == 0 || colDestino == Size - 1))
            {
                error = "El destino debe ser uno de los extremos de la fila.";
                return false;
            }

            if (mismaCol && !(filaDestino == 0 || filaDestino == Size - 1))
            {
                error = "El destino debe ser uno de los extremos de la columna.";
                return false;
            }

            var owner = _cells[filaOrigen, colOrigen];
            var orient = _orientations[filaOrigen, colOrigen];

            // No se puede tomar cubos del contrario
            if (owner != CellOwner.None && owner != jugador)
            {
                error = "No se puede retirar un cubo del símbolo del equipo contrario.";
                return false;
            }

            // Regla del punto en modo 4
            if (jugadorIndex.HasValue && owner == jugador)
            {
                var esperado = OrientationFromJugadorIndex(jugadorIndex.Value);
                if (orient != esperado)
                {
                    error = "Solo puede retirar cubos cuyo punto esté orientado hacia usted.";
                    return false;
                }
            }

            // ==================================================
            // EMPUJE (TU LÓGICA ORIGINAL, NO SE MODIFICÓ)
            // ==================================================

            _cells[filaOrigen, colOrigen] = CellOwner.None;
            _orientations[filaOrigen, colOrigen] = OrientationPoint.None;

            if (mismaFila)
            {
                // izquierda
                if (colDestino == 0)
                {
                    for (int c = colOrigen; c > 0; c--)
                    {
                        _cells[filaOrigen, c] = _cells[filaOrigen, c - 1];
                        _orientations[filaOrigen, c] = _orientations[filaOrigen, c - 1];
                    }
                    _cells[filaOrigen, 0] = jugador;
                    _orientations[filaOrigen, 0] = orientDestino ?? OrientationPoint.None;
                }
                else // derecha
                {
                    for (int c = colOrigen; c < Size - 1; c++)
                    {
                        _cells[filaOrigen, c] = _cells[filaOrigen, c + 1];
                        _orientations[filaOrigen, c] = _orientations[filaOrigen, c + 1];
                    }
                    _cells[filaOrigen, Size - 1] = jugador;
                    _orientations[filaOrigen, Size - 1] = orientDestino ?? OrientationPoint.None;
                }
            }
            else
            {
                // arriba
                if (filaDestino == 0)
                {
                    for (int f = filaOrigen; f > 0; f--)
                    {
                        _cells[f, colOrigen] = _cells[f - 1, colOrigen];
                        _orientations[f, colOrigen] = _orientations[f - 1, colOrigen];
                    }
                    _cells[0, colOrigen] = jugador;
                    _orientations[0, colOrigen] = orientDestino ?? OrientationPoint.None;
                }
                else // abajo
                {
                    for (int f = filaOrigen; f < Size - 1; f++)
                    {
                        _cells[f, colOrigen] = _cells[f + 1, colOrigen];
                        _orientations[f, colOrigen] = _orientations[f + 1, colOrigen];
                    }
                    _cells[Size - 1, colOrigen] = jugador;
                    _orientations[Size - 1, colOrigen] = orientDestino ?? OrientationPoint.None;
                }
            }

            return true;
        }

        // =================== UTILIDADES ===================

        private static bool EsValidaPosicion(int fila, int col)
            => fila >= 0 && fila < Size && col >= 0 && col < Size;

        private static bool EsPeriferia(int fila, int col)
            => fila == 0 || fila == Size - 1 || col == 0 || col == Size - 1;

        private static char OwnerToChar(CellOwner owner)
            => owner == CellOwner.Player1 ? 'O'
             : owner == CellOwner.Player2 ? 'X'
             : '.';

        private static CellOwner CharToOwner(char ch)
            => ch == 'O' ? CellOwner.Player1
             : ch == 'X' ? CellOwner.Player2
             : CellOwner.None;

        private static char OrientationToChar(OrientationPoint o)
            => o == OrientationPoint.Up ? 'U'
             : o == OrientationPoint.Right ? 'R'
             : o == OrientationPoint.Down ? 'D'
             : o == OrientationPoint.Left ? 'L'
             : 'N';

        private static OrientationPoint CharToOrientation(char ch)
            => ch == 'U' ? OrientationPoint.Up
             : ch == 'R' ? OrientationPoint.Right
             : ch == 'D' ? OrientationPoint.Down
             : ch == 'L' ? OrientationPoint.Left
             : OrientationPoint.None;

        private static OrientationPoint OrientationFromJugadorIndex(int jugadorIndex)
            => jugadorIndex == 1 ? OrientationPoint.Up
             : jugadorIndex == 2 ? OrientationPoint.Right
             : jugadorIndex == 3 ? OrientationPoint.Down
             : jugadorIndex == 4 ? OrientationPoint.Left
             : OrientationPoint.None;
    }
}
