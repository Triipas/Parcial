namespace PortalAcademico.Models.ViewModels
{
    public class MatriculasGestionViewModel
    {
        public Curso Curso { get; set; } = null!;
        public List<MatriculaDetalleViewModel> Matriculas { get; set; } = new List<MatriculaDetalleViewModel>();
    }

    public class MatriculaDetalleViewModel
    {
        public int MatriculaId { get; set; }
        public string UsuarioEmail { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public EstadoMatricula Estado { get; set; }
    }
}