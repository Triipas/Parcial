using System.ComponentModel.DataAnnotations;

namespace PortalAcademico.Models.ViewModels
{
    public class CursosFiltroViewModel
    {
        [Display(Name = "Buscar por nombre")]
        public string? Nombre { get; set; }

        [Display(Name = "Créditos mínimos")]
        [Range(0, 10, ErrorMessage = "Los créditos deben estar entre 0 y 10")]
        public int? CreditosMin { get; set; }

        [Display(Name = "Créditos máximos")]
        [Range(0, 10, ErrorMessage = "Los créditos deben estar entre 0 y 10")]
        public int? CreditosMax { get; set; }

        [Display(Name = "Horario desde")]
        [DataType(DataType.Time)]
        public TimeSpan? HorarioDesde { get; set; }

        [Display(Name = "Horario hasta")]
        [DataType(DataType.Time)]
        public TimeSpan? HorarioHasta { get; set; }

        public List<Curso> Cursos { get; set; } = new List<Curso>();
    }
}