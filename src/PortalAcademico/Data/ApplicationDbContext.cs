using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Models;

namespace PortalAcademico.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Curso
            modelBuilder.Entity<Curso>(entity =>
            {
                // Índice único para el código
                entity.HasIndex(c => c.Codigo)
                    .IsUnique()
                    .HasDatabaseName("IX_Curso_Codigo_Unique");

                // Configurar tabla con restricciones
                entity.ToTable(t =>
                {
                    // Restricción: Créditos > 0
                    t.HasCheckConstraint("CK_Curso_Creditos", "[Creditos] > 0");
                    
                    // Restricción: HorarioInicio < HorarioFin
                    t.HasCheckConstraint("CK_Curso_Horario", "[HorarioInicio] < [HorarioFin]");
                    
                    // Restricción: CupoMaximo > 0
                    t.HasCheckConstraint("CK_Curso_CupoMaximo", "[CupoMaximo] > 0");
                });
            });

            // Configuración de Matricula
            modelBuilder.Entity<Matricula>(entity =>
            {
                // Un usuario no puede estar matriculado más de una vez en el mismo curso
                entity.HasIndex(m => new { m.CursoId, m.UsuarioId })
                    .IsUnique()
                    .HasDatabaseName("IX_Matricula_Curso_Usuario_Unique")
                    .HasFilter("[Estado] != 2"); // Excluir matrículas canceladas (2 = Cancelada)

                // Relación con Curso
                entity.HasOne(m => m.Curso)
                    .WithMany(c => c.Matriculas)
                    .HasForeignKey(m => m.CursoId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configurar el enum como string en la base de datos
                entity.Property(m => m.Estado)
                    .HasConversion<string>();
            });
        }
    }
}