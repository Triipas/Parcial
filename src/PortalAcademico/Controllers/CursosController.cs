using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using PortalAcademico.Models.ViewModels;

namespace PortalAcademico.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CursosController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public CursosController(
            ApplicationDbContext context, 
            ILogger<CursosController> logger,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // GET: Cursos/Catalogo
        [HttpGet]
        public async Task<IActionResult> Catalogo(CursosFiltroViewModel filtro)
        {
            try
            {
                // Validaciones server-side
                if (filtro.CreditosMin.HasValue && filtro.CreditosMin < 0)
                {
                    ModelState.AddModelError(nameof(filtro.CreditosMin), "Los créditos mínimos no pueden ser negativos");
                }

                if (filtro.CreditosMax.HasValue && filtro.CreditosMax < 0)
                {
                    ModelState.AddModelError(nameof(filtro.CreditosMax), "Los créditos máximos no pueden ser negativos");
                }

                if (filtro.CreditosMin.HasValue && filtro.CreditosMax.HasValue && 
                    filtro.CreditosMin > filtro.CreditosMax)
                {
                    ModelState.AddModelError(nameof(filtro.CreditosMax), 
                        "Los créditos máximos deben ser mayores o iguales a los créditos mínimos");
                }

                if (filtro.HorarioDesde.HasValue && filtro.HorarioHasta.HasValue && 
                    filtro.HorarioDesde >= filtro.HorarioHasta)
                {
                    ModelState.AddModelError(nameof(filtro.HorarioHasta), 
                        "El horario hasta debe ser posterior al horario desde");
                }

                // ✅ CORRECCIÓN: Incluir todas las matrículas (no solo confirmadas)
                var query = _context.Cursos
                    .Include(c => c.Matriculas)
                    .Where(c => c.Activo)
                    .AsQueryable();

                // Aplicar filtros
                if (!string.IsNullOrWhiteSpace(filtro.Nombre))
                {
                    query = query.Where(c => c.Nombre.Contains(filtro.Nombre) || 
                                           c.Codigo.Contains(filtro.Nombre));
                }

                if (filtro.CreditosMin.HasValue)
                {
                    query = query.Where(c => c.Creditos >= filtro.CreditosMin.Value);
                }

                if (filtro.CreditosMax.HasValue)
                {
                    query = query.Where(c => c.Creditos <= filtro.CreditosMax.Value);
                }

                if (filtro.HorarioDesde.HasValue)
                {
                    query = query.Where(c => c.HorarioInicio >= filtro.HorarioDesde.Value);
                }

                if (filtro.HorarioHasta.HasValue)
                {
                    query = query.Where(c => c.HorarioFin <= filtro.HorarioHasta.Value);
                }

                // Ordenar por código
                filtro.Cursos = await query.OrderBy(c => c.Codigo).ToListAsync();

                return View(filtro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el catálogo de cursos");
                TempData["Error"] = "Ocurrió un error al cargar el catálogo de cursos";
                return View(new CursosFiltroViewModel());
            }
        }

        // GET: Cursos/Detalle/5
        [HttpGet]
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // ✅ CORRECCIÓN: Incluir todas las matrículas activas (Confirmadas y Pendientes)
                var curso = await _context.Cursos
                    .Include(c => c.Matriculas.Where(m => 
                        m.Estado == EstadoMatricula.Confirmada || 
                        m.Estado == EstadoMatricula.Pendiente))
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (curso == null)
                {
                    TempData["Error"] = "El curso no existe";
                    return RedirectToAction(nameof(Catalogo));
                }

                if (!curso.Activo)
                {
                    TempData["Error"] = "El curso no está disponible";
                    return RedirectToAction(nameof(Catalogo));
                }

                // Si el usuario está autenticado, verificar si ya está inscrito
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    var yaInscrito = await _context.Matriculas
                        .AnyAsync(m => m.CursoId == id && 
                                      m.UsuarioId == userId && 
                                      m.Estado != EstadoMatricula.Cancelada);
                    
                    ViewBag.YaInscrito = yaInscrito;
                }

                return View(curso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el detalle del curso {CursoId}", id);
                TempData["Error"] = "Ocurrió un error al cargar el detalle del curso";
                return RedirectToAction(nameof(Catalogo));
            }
        }

        // POST: Cursos/Inscribirse/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inscribirse(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // ✅ CORRECCIÓN: Obtener curso con todas las matrículas activas
                var curso = await _context.Cursos
                    .Include(c => c.Matriculas)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (curso == null || !curso.Activo)
                {
                    TempData["Error"] = "El curso no existe o no está disponible";
                    return RedirectToAction(nameof(Catalogo));
                }

                // VALIDACIÓN 1: Verificar que el usuario no esté ya inscrito
                var yaInscrito = await _context.Matriculas
                    .AnyAsync(m => m.CursoId == id && 
                                  m.UsuarioId == userId && 
                                  m.Estado != EstadoMatricula.Cancelada);

                if (yaInscrito)
                {
                    TempData["Error"] = "Ya estás inscrito en este curso";
                    return RedirectToAction(nameof(Detalle), new { id });
                }

                // ✅ CORRECCIÓN: Contar matrículas Confirmadas Y Pendientes
                var matriculasActivas = curso.Matriculas
                    .Count(m => m.Estado == EstadoMatricula.Confirmada || 
                               m.Estado == EstadoMatricula.Pendiente);

                // VALIDACIÓN 2: Verificar que no se supere el cupo máximo
                if (matriculasActivas >= curso.CupoMaximo)
                {
                    TempData["Error"] = "El curso ha alcanzado su cupo máximo. No hay cupos disponibles";
                    return RedirectToAction(nameof(Detalle), new { id });
                }

                // VALIDACIÓN 3: Verificar que no se solape con otro curso
                var cursosMatriculados = await _context.Matriculas
                    .Include(m => m.Curso)
                    .Where(m => m.UsuarioId == userId && 
                               m.Estado != EstadoMatricula.Cancelada)
                    .Select(m => m.Curso)
                    .ToListAsync();

                foreach (var cursoMatriculado in cursosMatriculados)
                {
                    if (HorariosSeSuperponen(curso, cursoMatriculado))
                    {
                        TempData["Error"] = $"Este curso se solapa con el curso '{cursoMatriculado.Codigo} - {cursoMatriculado.Nombre}' " +
                                          $"que va de {cursoMatriculado.HorarioInicio:hh\\:mm} a {cursoMatriculado.HorarioFin:hh\\:mm}";
                        return RedirectToAction(nameof(Detalle), new { id });
                    }
                }

                // Crear la matrícula en estado Pendiente
                var matricula = new Matricula
                {
                    CursoId = id,
                    UsuarioId = userId,
                    FechaRegistro = DateTime.UtcNow,
                    Estado = EstadoMatricula.Pendiente
                };

                _context.Matriculas.Add(matricula);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario {UserId} inscrito en curso {CursoId} - Cupos activos: {CuposActivos}/{CupoMaximo}", 
                    userId, id, matriculasActivas + 1, curso.CupoMaximo);

                TempData["Success"] = $"¡Te has inscrito exitosamente en el curso '{curso.Codigo} - {curso.Nombre}'! " +
                                     "Tu matrícula está en estado Pendiente y será revisada por un coordinador.";

                return RedirectToAction(nameof(MisMatriculas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inscribir usuario en curso {CursoId}", id);
                TempData["Error"] = "Ocurrió un error al procesar tu inscripción. Por favor, intenta nuevamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
        }

        // GET: Cursos/MisMatriculas
        [Authorize]
        public async Task<IActionResult> MisMatriculas()
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var matriculas = await _context.Matriculas
                    .Include(m => m.Curso)
                    .Where(m => m.UsuarioId == userId)
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync();

                return View(matriculas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las matrículas del usuario");
                TempData["Error"] = "Ocurrió un error al cargar tus matrículas";
                return View(new List<Matricula>());
            }
        }

        // Método auxiliar para verificar solapamiento de horarios
        private bool HorariosSeSuperponen(Curso curso1, Curso curso2)
        {
            return (curso1.HorarioInicio < curso2.HorarioFin && curso1.HorarioFin > curso2.HorarioInicio);
        }
    }
}