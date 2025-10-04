using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using PortalAcademico.Models.ViewModels;
using PortalAcademico.Models.DTOs;
using PortalAcademico.Services;

namespace PortalAcademico.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CursosController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;

        private const string CACHE_KEY_CURSOS_ACTIVOS = "cursos:activos";
        private const string SESSION_KEY_ULTIMO_CURSO = "UltimoCursoVisitado";

        public CursosController(
            ApplicationDbContext context,
            ILogger<CursosController> logger,
            UserManager<IdentityUser> userManager,
            ICacheService cacheService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _cacheService = cacheService;
            _configuration = configuration;
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

                List<Curso> cursos;

                // SI NO HAY FILTROS, INTENTAR OBTENER DEL CACHE
                if (EsFiltroVacio(filtro))
                {
                    try
                    {
                        // ✅ INTENTAR OBTENER DTOs DEL CACHE
                        var cursosCache = await _cacheService.GetAsync<List<CursoCacheDto>>(CACHE_KEY_CURSOS_ACTIVOS);

                        if (cursosCache != null && cursosCache.Any())
                        {
                            // Convertir DTOs a Cursos
                            cursos = cursosCache.Select(dto => dto.ToCurso()).ToList();
                            _logger.LogInformation("✅ CACHE HIT: {Count} cursos del cache", cursos.Count);
                            ViewBag.DesdeCache = true;
                        }
                        else
                        {
                            // ✅ CACHE MISS: Consultar BD
                            var cursosDb = await ObtenerCursosActivosAsync();

                            // Convertir a DTOs para guardar
                            var cursosDto = cursosDb.Select(c => CursoCacheDto.FromCurso(c)).ToList();

                            // Guardar DTOs en cache
                            var cacheDuration = _configuration.GetValue<int>("CacheSettings:CursosCacheDuration", 60);
                            await _cacheService.SetAsync(CACHE_KEY_CURSOS_ACTIVOS, cursosDto, TimeSpan.FromSeconds(cacheDuration));

                            cursos = cursosDb;
                            _logger.LogInformation("💾 CACHE MISS: {Count} cursos de BD, guardados en cache por {Duration}s",
                                cursos.Count, cacheDuration);
                            ViewBag.DesdeCache = false;
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogError(cacheEx, "❌ Error con cache, usando BD directamente");
                        cursos = await ObtenerCursosActivosAsync();
                        ViewBag.DesdeCache = false;
                    }
                }
                else
                {
                    // HAY FILTROS: Consultar BD directamente
                    var query = _context.Cursos
                        .Include(c => c.Matriculas)
                        .Where(c => c.Activo)
                        .AsQueryable();

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

                    cursos = await query.OrderBy(c => c.Codigo).ToListAsync();
                    _logger.LogInformation("🔍 FILTROS APLICADOS: {Count} cursos", cursos.Count);
                    ViewBag.DesdeCache = false;
                }

                filtro.Cursos = cursos;
                return View(filtro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al cargar el catálogo de cursos");
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

                // ✅ GUARDAR ÚLTIMO CURSO VISITADO EN SESIÓN
                var cursoInfo = new
                {
                    Id = curso.Id,
                    Codigo = curso.Codigo,
                    Nombre = curso.Nombre
                };
                HttpContext.Session.SetString(SESSION_KEY_ULTIMO_CURSO,
                    System.Text.Json.JsonSerializer.Serialize(cursoInfo));

                _logger.LogDebug("Guardado en sesión: Último curso visitado = {Codigo}", curso.Codigo);

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

                // VALIDACIÓN 2: Verificar que no se supere el cupo máximo
                var matriculasActivas = curso.Matriculas
                    .Count(m => m.Estado == EstadoMatricula.Confirmada ||
                               m.Estado == EstadoMatricula.Pendiente);

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

                _logger.LogInformation("Usuario {UserId} inscrito en curso {CursoId}", userId, id);

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

        // ✅ MÉTODO PARA INVALIDAR CACHE
        public async Task InvalidarCacheCursos()
        {
            await _cacheService.RemoveAsync(CACHE_KEY_CURSOS_ACTIVOS);
            _logger.LogInformation("🗑️ Cache de cursos invalidado");
        }

        // Métodos auxiliares
        private bool HorariosSeSuperponen(Curso curso1, Curso curso2)
        {
            return (curso1.HorarioInicio < curso2.HorarioFin && curso1.HorarioFin > curso2.HorarioInicio);
        }

        private bool EsFiltroVacio(CursosFiltroViewModel filtro)
        {
            return string.IsNullOrWhiteSpace(filtro.Nombre) &&
                   !filtro.CreditosMin.HasValue &&
                   !filtro.CreditosMax.HasValue &&
                   !filtro.HorarioDesde.HasValue &&
                   !filtro.HorarioHasta.HasValue;
        }

        private async Task<List<Curso>> ObtenerCursosActivosAsync()
        {
            return await _context.Cursos
                .Include(c => c.Matriculas)
                .Where(c => c.Activo)
                .OrderBy(c => c.Codigo)
                .ToListAsync();
        }
    }
}