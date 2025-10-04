using System.ComponentModel.DataAnnotations;

namespace PortalAcademico.Models
{
    public class Curso
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(10, ErrorMessage = "El código no puede exceder 10 caracteres")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Range(1, 10, ErrorMessage = "Los créditos deben ser entre 1 y 10")]
        public int Creditos { get; set; }

        [Required]
        [Range(1, 200, ErrorMessage = "El cupo máximo debe ser entre 1 y 200")]
        public int CupoMaximo { get; set; }

        [Required(ErrorMessage = "El horario de inicio es obligatorio")]
        [DataType(DataType.Time)]
        [Display(Name = "Horario Inicio")]
        public TimeSpan HorarioInicio { get; set; }

        [Required(ErrorMessage = "El horario de fin es obligatorio")]
        [DataType(DataType.Time)]
        [Display(Name = "Horario Fin")]
        public TimeSpan HorarioFin { get; set; }

        public bool Activo { get; set; } = true;

        // Navegación
        public ICollection<Matricula> Matriculas { get; set; } = new List<Matricula>();

        // ✅ CORRECCIÓN: Contar Confirmadas Y Pendientes (las canceladas NO ocupan cupo)
        public int CuposDisponibles => CupoMaximo - Matriculas.Count(m => 
            m.Estado == EstadoMatricula.Confirmada || 
            m.Estado == EstadoMatricula.Pendiente);

        // Propiedad adicional útil
        public int MatriculasActivas => Matriculas.Count(m => 
            m.Estado == EstadoMatricula.Confirmada || 
            m.Estado == EstadoMatricula.Pendiente);
    }
}