using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortalAcademico.Models
{
    public class Matricula
    {
        public int Id { get; set; }

        [Required]
        public int CursoId { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        [Required]
        public EstadoMatricula Estado { get; set; } = EstadoMatricula.Pendiente;

        // Navegación
        [ForeignKey("CursoId")]
        public Curso? Curso { get; set; }
    }
}