using System.ComponentModel.DataAnnotations;

namespace PortalAcademico.Models.ViewModels
{
    public class CursoFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(10, ErrorMessage = "El código no puede exceder 10 caracteres")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los créditos son obligatorios")]
        [Range(1, 10, ErrorMessage = "Los créditos deben estar entre 1 y 10")]
        [Display(Name = "Créditos")]
        public int Creditos { get; set; }

        [Required(ErrorMessage = "El cupo máximo es obligatorio")]
        [Range(1, 200, ErrorMessage = "El cupo máximo debe estar entre 1 y 200")]
        [Display(Name = "Cupo Máximo")]
        public int CupoMaximo { get; set; }

        [Required(ErrorMessage = "El horario de inicio es obligatorio")]
        [DataType(DataType.Time)]
        [Display(Name = "Horario Inicio")]
        public string HorarioInicioStr { get; set; } = "08:00";

        [Required(ErrorMessage = "El horario de fin es obligatorio")]
        [DataType(DataType.Time)]
        [Display(Name = "Horario Fin")]
        public string HorarioFinStr { get; set; } = "10:00";

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        // Convertir a TimeSpan
        public TimeSpan HorarioInicio => TimeSpan.Parse(HorarioInicioStr);
        public TimeSpan HorarioFin => TimeSpan.Parse(HorarioFinStr);

        // Método para crear desde Curso
        public static CursoFormViewModel FromCurso(Curso curso)
        {
            return new CursoFormViewModel
            {
                Id = curso.Id,
                Codigo = curso.Codigo,
                Nombre = curso.Nombre,
                Creditos = curso.Creditos,
                CupoMaximo = curso.CupoMaximo,
                HorarioInicioStr = curso.HorarioInicio.ToString(@"hh\:mm"),
                HorarioFinStr = curso.HorarioFin.ToString(@"hh\:mm"),
                Activo = curso.Activo
            };
        }

        // Método para convertir a Curso
        public Curso ToCurso()
        {
            return new Curso
            {
                Id = Id ?? 0,
                Codigo = Codigo,
                Nombre = Nombre,
                Creditos = Creditos,
                CupoMaximo = CupoMaximo,
                HorarioInicio = HorarioInicio,
                HorarioFin = HorarioFin,
                Activo = Activo
            };
        }
    }
}