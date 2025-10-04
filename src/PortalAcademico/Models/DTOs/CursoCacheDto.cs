namespace PortalAcademico.Models.DTOs
{
    public class CursoCacheDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int CupoMaximo { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public TimeSpan HorarioFin { get; set; }
        public bool Activo { get; set; }
        
        // Solo números, NO objetos relacionados
        public int MatriculasActivas { get; set; }
        public int CuposDisponibles { get; set; }

        // Convertir desde Curso a DTO
        public static CursoCacheDto FromCurso(Curso curso)
        {
            var activas = curso.Matriculas.Count(m => 
                m.Estado == EstadoMatricula.Confirmada || 
                m.Estado == EstadoMatricula.Pendiente);

            return new CursoCacheDto
            {
                Id = curso.Id,
                Codigo = curso.Codigo,
                Nombre = curso.Nombre,
                Creditos = curso.Creditos,
                CupoMaximo = curso.CupoMaximo,
                HorarioInicio = curso.HorarioInicio,
                HorarioFin = curso.HorarioFin,
                Activo = curso.Activo,
                MatriculasActivas = activas,
                CuposDisponibles = curso.CupoMaximo - activas
            };
        }

        // Convertir de DTO a Curso (para mostrar en la vista)
        public Curso ToCurso()
        {
            var curso = new Curso
            {
                Id = this.Id,
                Codigo = this.Codigo,
                Nombre = this.Nombre,
                Creditos = this.Creditos,
                CupoMaximo = this.CupoMaximo,
                HorarioInicio = this.HorarioInicio,
                HorarioFin = this.HorarioFin,
                Activo = this.Activo,
                Matriculas = new List<Matricula>()
            };

            // Simular matrículas solo para el cálculo de CuposDisponibles
            for (int i = 0; i < this.MatriculasActivas; i++)
            {
                curso.Matriculas.Add(new Matricula 
                { 
                    Estado = EstadoMatricula.Confirmada 
                });
            }

            return curso;
        }
    }
}