// (RECUERDA PEGAR TODO, INCLUYENDO USING)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Quixo.Web.Models;

namespace Quixo.Web.Controllers
{
    public class PartidasController : Controller
    {
        private readonly QuixoDbContext _db = new QuixoDbContext();

        // ============================
        // LISTA GENERAL
        // ============================
        public ActionResult Index()
        {
            var partidas = _db.Partidas
                .OrderByDescending(p => p.FechaCreacion)
                .ToList();

            return View(partidas);
        }

        public ActionResult Finalizadas()
        {
            var partidas = _db.Partidas
                .Where(p => p.Resultado != ResultadoPartida.EnCurso)
                .OrderByDescending(p => p.FechaCreacion)
                .ToList();

            return View(partidas);
        }

        // ============================
        // CREAR PARTIDA
        // ============================
        public ActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearPartida(ModoPartida modo)
        {
            var partida = new Partida
            {
                Modo = modo,
                FechaCreacion = DateTime.Now,
                Resultado = ResultadoPartida.EnCurso,
                DuracionTotal = TimeSpan.Zero
            };

            _db.Partidas.Add(partida);
            _db.SaveChanges();

            var tablero = new TableroQuixo();
            var estadoInicial = new EstadoTablero
            {
                PartidaId = partida.Id,
                NumeroMovimiento = 0,
                TableroCompacto = tablero.ACompacto(),
                SegundosTranscurridos = 0
            };

            _db.EstadosTablero.Add(estadoInicial);
            _db.SaveChanges();

            return RedirectToAction("Jugar", new { id = partida.Id });
        }

        // ============================
        // JUGAR
        // ============================
        public ActionResult Jugar(int id)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null)
                return HttpNotFound();

            var estado = _db.EstadosTablero
                .Where(e => e.PartidaId == id)
                .OrderByDescending(e => e.NumeroMovimiento)
                .FirstOrDefault();

            if (estado == null)
            {
                var tableroInicial = new TableroQuixo();
                estado = new EstadoTablero
                {
                    PartidaId = id,
                    NumeroMovimiento = 0,
                    TableroCompacto = tableroInicial.ACompacto(),
                    SegundosTranscurridos = 0
                };
                partida.FechaCreacion = DateTime.Now;
                _db.EstadosTablero.Add(estado);
                _db.SaveChanges();
            }

            var tablero = TableroQuixo.DesdeCompacto(estado.TableroCompacto);

            CellOwner turnoActual = CellOwner.None;

            if (partida.Resultado == ResultadoPartida.EnCurso)
            {
                int siguiente = estado.NumeroMovimiento + 1;

                if (partida.Modo == ModoPartida.DosJugadores)
                {
                    turnoActual = (siguiente % 2 == 1) ? CellOwner.Player1 : CellOwner.Player2;
                }
                else
                {
                    int idx = (siguiente - 1) % 4;
                    turnoActual = (idx == 0 || idx == 2) ? CellOwner.Player1 : CellOwner.Player2;
                }
            }

            ViewBag.Partida = partida;
            ViewBag.Estado = estado;
            ViewBag.TurnoActual = turnoActual;
            ViewBag.TiempoInicialSegundos = estado.SegundosTranscurridos;

            if (partida.Modo == ModoPartida.CuatroJugadores)
            {
                string key = $"OrientacionDestino_{id}";
                if (Session[key] == null) Session[key] = "Self";
                ViewBag.OrientacionDestino = (string)Session[key];
            }
            else
            {
                ViewBag.OrientacionDestino = null;
            }

            return View(tablero);
        }

        // ============================
        // CAMBIAR ORIENTACIÓN (MODO 4)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarOrientacion(int id, string orientacion)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null) return HttpNotFound();

            if (partida.Modo != ModoPartida.CuatroJugadores ||
                partida.Resultado != ResultadoPartida.EnCurso)
            {
                return RedirectToAction("Jugar", new { id });
            }

            string key = $"OrientacionDestino_{id}";
            Session[key] = (orientacion == "Teammate") ? "Teammate" : "Self";

            return RedirectToAction("Jugar", new { id });
        }

        // ============================
        // SELECCIONAR (MOVER)
        // ============================
        public ActionResult Seleccionar(int id, int fila, int col)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null) return HttpNotFound();

            if (partida.Resultado != ResultadoPartida.EnCurso)
            {
                TempData["Mensaje"] = "La partida ya ha finalizado.";
                return RedirectToAction("Jugar", new { id });
            }

            // =====================================
            // TURNO ACTUAL (con índice si es modo 4)
            // =====================================
            var estado = _db.EstadosTablero
                .Where(e => e.PartidaId == id)
                .OrderByDescending(e => e.NumeroMovimiento)
                .First();

            int numeroMovimiento = estado.NumeroMovimiento + 1;

            CellOwner jugadorOwner;
            string jugadorNombre;
            int? jugadorIndex = null;

            if (partida.Modo == ModoPartida.DosJugadores)
            {
                jugadorOwner = (numeroMovimiento % 2 == 1)
                    ? CellOwner.Player1 : CellOwner.Player2;

                jugadorNombre = (jugadorOwner == CellOwner.Player1) ? "Jugador 1" : "Jugador 2";
            }
            else
            {
                int idx = (numeroMovimiento - 1) % 4;
                switch (idx)
                {
                    case 0: jugadorOwner = CellOwner.Player1; jugadorNombre = "Jugador 1 (A)"; jugadorIndex = 1; break;
                    case 1: jugadorOwner = CellOwner.Player2; jugadorNombre = "Jugador 2 (B)"; jugadorIndex = 2; break;
                    case 2: jugadorOwner = CellOwner.Player1; jugadorNombre = "Jugador 3 (A)"; jugadorIndex = 3; break;
                    default: jugadorOwner = CellOwner.Player2; jugadorNombre = "Jugador 4 (B)"; jugadorIndex = 4; break;
                }
            }

            // =====================================
            // NUEVO: VALIDAR SI EL JUGADOR TIENE MOVIMIENTOS
            // =====================================
            var tableroTemp = TableroQuixo.DesdeCompacto(estado.TableroCompacto);

            bool esPrimeraVuelta =
                (partida.Modo == ModoPartida.DosJugadores && numeroMovimiento <= 2) ||
                (partida.Modo == ModoPartida.CuatroJugadores && numeroMovimiento <= 4);

            bool tieneMovimientos =
                tableroTemp.TieneMovimientosValidos(jugadorOwner, jugadorIndex, esPrimeraVuelta);

            if (!tieneMovimientos)
            {
                // SALTO DE TURNO
                var ahora = DateTime.Now;
                var elapsed = ahora - partida.FechaCreacion;

                // Registrar movimiento fantasma
                var mov = new Movimiento
                {
                    PartidaId = id,
                    NumeroMovimiento = numeroMovimiento,
                    FilaOrigen = -1,
                    ColumnaOrigen = -1,
                    FilaDestino = -1,
                    ColumnaDestino = -1,
                    DireccionEmpuje = "Turno Saltado",
                    FechaHora = ahora
                };
                _db.Movimientos.Add(mov);

                // Guardar estado igual al anterior
                var estadoNuevo = new EstadoTablero
                {
                    PartidaId = id,
                    NumeroMovimiento = numeroMovimiento,
                    TableroCompacto = estado.TableroCompacto,
                    SegundosTranscurridos = (int)elapsed.TotalSeconds
                };
                _db.EstadosTablero.Add(estadoNuevo);

                _db.SaveChanges();

                TempData["Mensaje"] = $"{jugadorNombre} no tiene movimientos válidos. Turno saltado.";
                return RedirectToAction("Jugar", new { id });
            }

            // =====================================
            // A PARTIR DE AQUÍ SIGUE TU LÓGICA NORMAL
            // =====================================

            string keyFila = $"OrigenFila_{id}";
            string keyCol = $"OrigenCol_{id}";

            if (Session[keyFila] == null || Session[keyCol] == null)
            {
                Session[keyFila] = fila;
                Session[keyCol] = col;

                TempData["Mensaje"] = $"Origen seleccionado: ({fila},{col}).";
                return RedirectToAction("Jugar", new { id });
            }

            int f_o = (int)Session[keyFila];
            int c_o = (int)Session[keyCol];
            Session[keyFila] = null;
            Session[keyCol] = null;

            var tablero = TableroQuixo.DesdeCompacto(estado.TableroCompacto);

            // Orientación (modo 4)
            OrientationPoint orientDestino = OrientationPoint.None;
            if (partida.Modo == ModoPartida.CuatroJugadores)
            {
                string pref = (string)Session[$"OrientacionDestino_{id}"];
                orientDestino = obtenerOrientacionDestino(jugadorIndex.Value, pref);
            }

            bool ok;
            string error;

            if (partida.Modo == ModoPartida.DosJugadores)
            {
                ok = tablero.Mover(f_o, c_o, fila, col, jugadorOwner, out error);
            }
            else
            {
                ok = tablero.MoverCuatroJugadores(f_o, c_o, fila, col,
                        jugadorOwner, jugadorIndex.Value, orientDestino, out error);
            }

            if (!ok)
            {
                TempData["Mensaje"] = "Movimiento inválido: " + error;
                return RedirectToAction("Jugar", new { id });
            }

            // === Guardar movimiento real ===
            var ahora2 = DateTime.Now;
            var elapsed2 = ahora2 - partida.FechaCreacion;

            var mov2 = new Movimiento
            {
                PartidaId = id,
                NumeroMovimiento = numeroMovimiento,
                FilaOrigen = f_o,
                ColumnaOrigen = c_o,
                FilaDestino = fila,
                ColumnaDestino = col,
                DireccionEmpuje = ObtenerDireccion(f_o, c_o, fila, col),
                FechaHora = ahora2
            };
            _db.Movimientos.Add(mov2);

            var estadoNuevo2 = new EstadoTablero
            {
                PartidaId = id,
                NumeroMovimiento = numeroMovimiento,
                TableroCompacto = tablero.ACompacto(),
                SegundosTranscurridos = (int)elapsed2.TotalSeconds
            };
            _db.EstadosTablero.Add(estadoNuevo2);

            // === Revisar victoria ===
            bool lineaP1 = TieneLinea(tablero, CellOwner.Player1);
            bool lineaP2 = TieneLinea(tablero, CellOwner.Player2);

            CellOwner ganador = CellOwner.None;

            if (jugadorOwner == CellOwner.Player1)
            {
                if (lineaP2) ganador = CellOwner.Player2;
                else if (lineaP1) ganador = CellOwner.Player1;
            }
            else
            {
                if (lineaP1) ganador = CellOwner.Player1;
                else if (lineaP2) ganador = CellOwner.Player2;
            }

            if (ganador != CellOwner.None)
            {
                partida.Resultado = (ganador == CellOwner.Player1)
                    ? ResultadoPartida.GanoJugador1
                    : ResultadoPartida.GanoJugador2;

                partida.DuracionTotal = elapsed2;
                _db.SaveChanges();

                TempData["Mensaje"] = (ganador == CellOwner.Player1)
                    ? "Ganador: Equipo A"
                    : "Ganador: Equipo B";

                return RedirectToAction("Jugar", new { id });
            }

            _db.SaveChanges();
            TempData["Mensaje"] = $"Movimiento realizado por {jugadorNombre}.";
            return RedirectToAction("Jugar", new { id });
        }

        // ============================
        // REINICIAR
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reiniciar(int id)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null) return HttpNotFound();

            _db.Movimientos.RemoveRange(_db.Movimientos.Where(m => m.PartidaId == id));
            _db.EstadosTablero.RemoveRange(_db.EstadosTablero.Where(e => e.PartidaId == id));

            partida.Resultado = ResultadoPartida.EnCurso;
            partida.FechaCreacion = DateTime.Now;
            partida.DuracionTotal = TimeSpan.Zero;

            var tablero = new TableroQuixo();
            var estadoInicial = new EstadoTablero
            {
                PartidaId = id,
                NumeroMovimiento = 0,
                TableroCompacto = tablero.ACompacto(),
                SegundosTranscurridos = 0
            };

            _db.EstadosTablero.Add(estadoInicial);
            _db.SaveChanges();

            TempData["Mensaje"] = "Partida reiniciada.";
            return RedirectToAction("Jugar", new { id });
        }

        // ============================
        // ELIMINAR
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null) return HttpNotFound();

            if (partida.Resultado != ResultadoPartida.EnCurso)
            {
                TempData["Mensaje"] = "Solo se pueden eliminar partidas en curso.";
                return RedirectToAction("Index");
            }

            _db.Movimientos.RemoveRange(_db.Movimientos.Where(m => m.PartidaId == id));
            _db.EstadosTablero.RemoveRange(_db.EstadosTablero.Where(e => e.PartidaId == id));
            _db.Partidas.Remove(partida);

            _db.SaveChanges();

            TempData["Mensaje"] = "Partida eliminada.";
            return RedirectToAction("Index");
        }

        // ============================
        // DETALLE
        // ============================
        public ActionResult Detalle(int id, int? movimiento)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null) return HttpNotFound();

            var estados = _db.EstadosTablero
                .Where(e => e.PartidaId == id)
                .OrderBy(e => e.NumeroMovimiento)
                .ToList();

            if (!estados.Any()) return HttpNotFound();

            int movSel = movimiento ?? estados.Max(e => e.NumeroMovimiento);

            var estadoSel = estados.FirstOrDefault(e => e.NumeroMovimiento == movSel)
                            ?? estados.First();

            var tablero = TableroQuixo.DesdeCompacto(estadoSel.TableroCompacto);

            var movs = _db.Movimientos
                .Where(m => m.PartidaId == id)
                .OrderBy(m => m.NumeroMovimiento)
                .ToList();

            ViewBag.Partida = partida;
            ViewBag.Estados = estados;
            ViewBag.EstadoSeleccionado = estadoSel;
            ViewBag.Movimientos = movs;

            return View(tablero);
        }

        // ============================
        // EXPORTAR XML
        // ============================
        public ActionResult ExportarXml(int id)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null)
                return HttpNotFound();

            ViewBag.Partida = partida;
            ViewBag.NombreSugerido = $"Partida_{partida.Id}";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportarXml(int id, string nombreArchivo)
        {
            var partida = _db.Partidas.Find(id);
            if (partida == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(nombreArchivo))
                nombreArchivo = $"Partida_{partida.Id}";

            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                nombreArchivo = nombreArchivo.Replace(c, '_');

            if (!nombreArchivo.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                nombreArchivo += ".xml";

            var movimientos = _db.Movimientos
                .Where(m => m.PartidaId == id)
                .OrderBy(m => m.NumeroMovimiento)
                .ToList();

            var estFinal = _db.EstadosTablero
                .Where(e => e.PartidaId == id)
                .OrderByDescending(e => e.NumeroMovimiento)
                .FirstOrDefault();

            var dto = new PartidaXmlDto
            {
                Id = partida.Id,
                Modo = partida.Modo,
                FechaCreacion = partida.FechaCreacion,
                Resultado = partida.Resultado,
                DuracionTotal = partida.DuracionTotal.ToString(),
                TableroFinal = estFinal != null ? estFinal.TableroCompacto : "",
                Movimientos = movimientos.Select(m => new MovimientoXmlDto
                {
                    NumeroMovimiento = m.NumeroMovimiento,
                    FilaOrigen = m.FilaOrigen,
                    ColumnaOrigen = m.ColumnaOrigen,
                    FilaDestino = m.FilaDestino,
                    ColumnaDestino = m.ColumnaDestino,
                    DireccionEmpuje = m.DireccionEmpuje,
                    FechaHora = m.FechaHora
                }).ToList()
            };

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PartidaXmlDto));

            string xml;
            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, dto);
                xml = sw.ToString();
            }

            var bytes = Encoding.UTF8.GetBytes(xml);
            return File(bytes, "application/xml", nombreArchivo);
        }

        // ============================
        // ESTADÍSTICAS
        // ============================
        public ActionResult Estadisticas()
        {
            var vm = new EstadisticasViewModel();

            // DOS JUGADORES
            var partidas2 = _db.Partidas
                .Where(p => p.Modo == ModoPartida.DosJugadores && p.Resultado != ResultadoPartida.EnCurso)
                .ToList();

            int total2 = partidas2.Count;

            vm.DosJugadores.Add(new EstadisticaFilaViewModel
            {
                Nombre = "Primero",
                PartidasGanadas = partidas2.Count(p => p.Resultado == ResultadoPartida.GanoJugador1),
                PartidasTotales = total2,
                Efectividad = total2 == 0 ? 0 : partidas2.Count(p => p.Resultado == ResultadoPartida.GanoJugador1) * 100.0 / total2
            });

            vm.DosJugadores.Add(new EstadisticaFilaViewModel
            {
                Nombre = "Segundo",
                PartidasGanadas = partidas2.Count(p => p.Resultado == ResultadoPartida.GanoJugador2),
                PartidasTotales = total2,
                Efectividad = total2 == 0 ? 0 : partidas2.Count(p => p.Resultado == ResultadoPartida.GanoJugador2) * 100.0 / total2
            });

            // CUATRO JUGADORES
            var partidas4 = _db.Partidas
                .Where(p => p.Modo == ModoPartida.CuatroJugadores && p.Resultado != ResultadoPartida.EnCurso)
                .ToList();

            int total4 = partidas4.Count;

            vm.CuatroJugadores.Add(new EstadisticaFilaViewModel
            {
                Nombre = "Equipo A",
                PartidasGanadas = partidas4.Count(p => p.Resultado == ResultadoPartida.GanoJugador1),
                PartidasTotales = total4,
                Efectividad = total4 == 0 ? 0 : partidas4.Count(p => p.Resultado == ResultadoPartida.GanoJugador1) * 100.0 / total4
            });

            vm.CuatroJugadores.Add(new EstadisticaFilaViewModel
            {
                Nombre = "Equipo B",
                PartidasGanadas = partidas4.Count(p => p.Resultado == ResultadoPartida.GanoJugador2),
                PartidasTotales = total4,
                Efectividad = total4 == 0 ? 0 : partidas4.Count(p => p.Resultado == ResultadoPartida.GanoJugador2) * 100.0 / total4
            });

            return View(vm);
        }

        // ============================
        // UTILIDADES
        // ============================
        private string ObtenerDireccion(int filaOrigen, int colOrigen, int filaDestino, int colDestino)
        {
            if (filaOrigen == filaDestino)
            {
                if (colDestino == 0) return "Izquierda";
                if (colDestino == TableroQuixo.Size - 1) return "Derecha";
            }

            if (colOrigen == colDestino)
            {
                if (filaDestino == 0) return "Arriba";
                if (filaDestino == TableroQuixo.Size - 1) return "Abajo";
            }

            return "Desconocida";
        }

        private bool TieneLinea(TableroQuixo t, CellOwner owner)
        {
            int n = TableroQuixo.Size;
            if (owner == CellOwner.None) return false;

            // Filas
            for (int f = 0; f < n; f++)
            {
                bool ok = true;
                for (int c = 0; c < n; c++)
                    if (t.GetCell(f, c) != owner) ok = false;
                if (ok) return true;
            }

            // Columnas
            for (int c = 0; c < n; c++)
            {
                bool ok = true;
                for (int f = 0; f < n; f++)
                    if (t.GetCell(f, c) != owner) ok = false;
                if (ok) return true;
            }

            // Diagonal principal
            bool diag = true;
            for (int i = 0; i < n; i++)
                if (t.GetCell(i, i) != owner) diag = false;
            if (diag) return true;

            // Diagonal secundaria
            diag = true;
            for (int i = 0; i < n; i++)
                if (t.GetCell(i, n - 1 - i) != owner) diag = false;
            if (diag) return true;

            return false;
        }

        private OrientationPoint obtenerOrientacionDestino(int jugadorIndex, string pref)
        {
            OrientationPoint self =
                jugadorIndex == 1 ? OrientationPoint.Up :
                jugadorIndex == 2 ? OrientationPoint.Right :
                jugadorIndex == 3 ? OrientationPoint.Down :
                OrientationPoint.Left;

            OrientationPoint team =
                jugadorIndex == 1 ? OrientationPoint.Down :
                jugadorIndex == 3 ? OrientationPoint.Up :
                jugadorIndex == 2 ? OrientationPoint.Left :
                OrientationPoint.Right;

            return pref == "Teammate" ? team : self;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
