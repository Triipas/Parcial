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

        public CursosController(ApplicationDbContext context, ILogger<CursosController> logger)
        {
            _context = context;
            _logger = logger;
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

                // Consulta base: solo cursos activos
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
                var curso = await _context.Cursos
                    .Include(c => c.Matriculas.Where(m => m.Estado == EstadoMatricula.Confirmada))
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

                return View(curso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el detalle del curso {CursoId}", id);
                TempData["Error"] = "Ocurrió un error al cargar el detalle del curso";
                return RedirectToAction(nameof(Catalogo));
            }
        }
    }
}