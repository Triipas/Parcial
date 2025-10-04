using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;
using PortalAcademico.Models.ViewModels;
using PortalAcademico.Services;

namespace PortalAcademico.Controllers
{
    [Authorize(Roles = "Coordinador")]
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoordinadorController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICacheService _cacheService;

        private const string CACHE_KEY_CURSOS_ACTIVOS = "cursos:activos";

        public CoordinadorController(
            ApplicationDbContext context,
            ILogger<CoordinadorController> logger,
            UserManager<IdentityUser> userManager,
            ICacheService cacheService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _cacheService = cacheService;
        }

        // GET: /Coordinador
        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos
                .Include(c => c.Matriculas)
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            var totalMatriculas = await _context.Matriculas.CountAsync();
            var pendientes = await _context.Matriculas.CountAsync(m => m.Estado == EstadoMatricula.Pendiente);
            var confirmadas = await _context.Matriculas.CountAsync(m => m.Estado == EstadoMatricula.Confirmada);

            ViewBag.TotalCursos = cursos.Count;
            ViewBag.CursosActivos = cursos.Count(c => c.Activo);
            ViewBag.TotalMatriculas = totalMatriculas;
            ViewBag.MatriculasPendientes = pendientes;
            ViewBag.MatriculasConfirmadas = confirmadas;

            return View(cursos);
        }

        // GET: /Coordinador/CrearCurso
        public IActionResult CrearCurso()
        {
            return View(new CursoFormViewModel());
        }

        // POST: /Coordinador/CrearCurso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCurso(CursoFormViewModel model)
        {
            try
            {
                // Validación: HorarioInicio < HorarioFin
                if (model.HorarioInicio >= model.HorarioFin)
                {
                    ModelState.AddModelError(nameof(model.HorarioFinStr), 
                        "El horario de fin debe ser posterior al horario de inicio");
                }

                // Validación: Código único
                if (await _context.Cursos.AnyAsync(c => c.Codigo == model.Codigo))
                {
                    ModelState.AddModelError(nameof(model.Codigo), 
                        "Ya existe un curso con este código");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var curso = model.ToCurso();
                _context.Cursos.Add(curso);
                await _context.SaveChangesAsync();

                // ✅ INVALIDAR CACHE
                await _cacheService.RemoveAsync(CACHE_KEY_CURSOS_ACTIVOS);
                _logger.LogInformation("🗑️ Cache invalidado después de crear curso {Codigo}", curso.Codigo);

                TempData["Success"] = $"Curso '{curso.Codigo} - {curso.Nombre}' creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear curso");
                TempData["Error"] = "Ocurrió un error al crear el curso";
                return View(model);
            }
        }

        // GET: /Coordinador/EditarCurso/5
        public async Task<IActionResult> EditarCurso(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null)
            {
                TempData["Error"] = "El curso no existe";
                return RedirectToAction(nameof(Index));
            }

            var model = CursoFormViewModel.FromCurso(curso);
            return View(model);
        }

        // POST: /Coordinador/EditarCurso/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCurso(int id, CursoFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            try
            {
                // Validación: HorarioInicio < HorarioFin
                if (model.HorarioInicio >= model.HorarioFin)
                {
                    ModelState.AddModelError(nameof(model.HorarioFinStr),
                        "El horario de fin debe ser posterior al horario de inicio");
                }

                // Validación: Código único (excepto el mismo curso)
                if (await _context.Cursos.AnyAsync(c => c.Codigo == model.Codigo && c.Id != id))
                {
                    ModelState.AddModelError(nameof(model.Codigo),
                        "Ya existe otro curso con este código");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var curso = await _context.Cursos.FindAsync(id);
                if (curso == null)
                {
                    TempData["Error"] = "El curso no existe";
                    return RedirectToAction(nameof(Index));
                }

                // Actualizar propiedades
                curso.Codigo = model.Codigo;
                curso.Nombre = model.Nombre;
                curso.Creditos = model.Creditos;
                curso.CupoMaximo = model.CupoMaximo;
                curso.HorarioInicio = model.HorarioInicio;
                curso.HorarioFin = model.HorarioFin;
                curso.Activo = model.Activo;

                _context.Update(curso);
                await _context.SaveChangesAsync();

                // ✅ INVALIDAR CACHE
                await _cacheService.RemoveAsync(CACHE_KEY_CURSOS_ACTIVOS);
                _logger.LogInformation("🗑️ Cache invalidado después de editar curso {Codigo}", curso.Codigo);

                TempData["Success"] = $"Curso '{curso.Codigo}' actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar curso {CursoId}", id);
                TempData["Error"] = "Ocurrió un error al actualizar el curso";
                return View(model);
            }
        }

        // POST: /Coordinador/DesactivarCurso/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesactivarCurso(int id)
        {
            try
            {
                var curso = await _context.Cursos.FindAsync(id);
                if (curso == null)
                {
                    TempData["Error"] = "El curso no existe";
                    return RedirectToAction(nameof(Index));
                }

                curso.Activo = false;
                _context.Update(curso);
                await _context.SaveChangesAsync();

                // ✅ INVALIDAR CACHE
                await _cacheService.RemoveAsync(CACHE_KEY_CURSOS_ACTIVOS);
                _logger.LogInformation("🗑️ Cache invalidado después de desactivar curso {Codigo}", curso.Codigo);

                TempData["Success"] = $"Curso '{curso.Codigo}' desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar curso {CursoId}", id);
                TempData["Error"] = "Ocurrió un error al desactivar el curso";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Coordinador/ActivarCurso/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivarCurso(int id)
        {
            try
            {
                var curso = await _context.Cursos.FindAsync(id);
                if (curso == null)
                {
                    TempData["Error"] = "El curso no existe";
                    return RedirectToAction(nameof(Index));
                }

                curso.Activo = true;
                _context.Update(curso);
                await _context.SaveChangesAsync();

                // ✅ INVALIDAR CACHE
                await _cacheService.RemoveAsync(CACHE_KEY_CURSOS_ACTIVOS);
                _logger.LogInformation("🗑️ Cache invalidado después de activar curso {Codigo}", curso.Codigo);

                TempData["Success"] = $"Curso '{curso.Codigo}' activado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar curso {CursoId}", id);
                TempData["Error"] = "Ocurrió un error al activar el curso";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Coordinador/GestionarMatriculas/5
        public async Task<IActionResult> GestionarMatriculas(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var curso = await _context.Cursos
                    .Include(c => c.Matriculas)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (curso == null)
                {
                    TempData["Error"] = "El curso no existe";
                    return RedirectToAction(nameof(Index));
                }

                var matriculasDetalle = new List<MatriculaDetalleViewModel>();

                foreach (var matricula in curso.Matriculas.OrderByDescending(m => m.FechaRegistro))
                {
                    var usuario = await _userManager.FindByIdAsync(matricula.UsuarioId);
                    
                    matriculasDetalle.Add(new MatriculaDetalleViewModel
                    {
                        MatriculaId = matricula.Id,
                        UsuarioEmail = usuario?.Email ?? "Usuario desconocido",
                        FechaRegistro = matricula.FechaRegistro,
                        Estado = matricula.Estado
                    });
                }

                var viewModel = new MatriculasGestionViewModel
                {
                    Curso = curso,
                    Matriculas = matriculasDetalle
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar matrículas del curso {CursoId}", id);
                TempData["Error"] = "Ocurrió un error al cargar las matrículas";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Coordinador/ConfirmarMatricula/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarMatricula(int id, int cursoId)
        {
            try
            {
                var matricula = await _context.Matriculas.FindAsync(id);
                if (matricula == null)
                {
                    TempData["Error"] = "La matrícula no existe";
                    return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
                }

                if (matricula.Estado == EstadoMatricula.Confirmada)
                {
                    TempData["Warning"] = "La matrícula ya está confirmada";
                    return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
                }

                matricula.Estado = EstadoMatricula.Confirmada;
                _context.Update(matricula);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Matrícula {MatriculaId} confirmada", id);
                TempData["Success"] = "Matrícula confirmada exitosamente";
                return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar matrícula {MatriculaId}", id);
                TempData["Error"] = "Ocurrió un error al confirmar la matrícula";
                return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
            }
        }

        // POST: /Coordinador/CancelarMatricula/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarMatricula(int id, int cursoId)
        {
            try
            {
                var matricula = await _context.Matriculas.FindAsync(id);
                if (matricula == null)
                {
                    TempData["Error"] = "La matrícula no existe";
                    return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
                }

                if (matricula.Estado == EstadoMatricula.Cancelada)
                {
                    TempData["Warning"] = "La matrícula ya está cancelada";
                    return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
                }

                matricula.Estado = EstadoMatricula.Cancelada;
                _context.Update(matricula);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Matrícula {MatriculaId} cancelada", id);
                TempData["Success"] = "Matrícula cancelada exitosamente";
                return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar matrícula {MatriculaId}", id);
                TempData["Error"] = "Ocurrió un error al cancelar la matrícula";
                return RedirectToAction(nameof(GestionarMatriculas), new { id = cursoId });
            }
        }
    }
}