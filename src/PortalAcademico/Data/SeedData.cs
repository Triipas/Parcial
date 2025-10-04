using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Models;

namespace PortalAcademico.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Asegurar que la base de datos existe
            await context.Database.MigrateAsync();

            // Crear rol Coordinador si no existe
            if (!await roleManager.RoleExistsAsync("Coordinador"))
            {
                await roleManager.CreateAsync(new IdentityRole("Coordinador"));
            }

            // Crear usuario Coordinador si no existe
            var coordinadorEmail = "coordinador@universidad.edu.pe";
            var coordinador = await userManager.FindByEmailAsync(coordinadorEmail);

            if (coordinador == null)
            {
                coordinador = new IdentityUser
                {
                    UserName = coordinadorEmail,
                    Email = coordinadorEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(coordinador, "Coordinador123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(coordinador, "Coordinador");
                }
            }

            // Lista de cursos semilla
            var cursosSemilla = new List<Curso>
            {
                new Curso
                {
                    Codigo = "MAT101",
                    Nombre = "Cálculo Diferencial",
                    Creditos = 4,
                    CupoMaximo = 30,
                    HorarioInicio = new TimeSpan(8, 0, 0),
                    HorarioFin = new TimeSpan(10, 0, 0),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "PROG201",
                    Nombre = "Programación Orientada a Objetos",
                    Creditos = 5,
                    CupoMaximo = 25,
                    HorarioInicio = new TimeSpan(10, 0, 0),
                    HorarioFin = new TimeSpan(12, 0, 0),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "BD301",
                    Nombre = "Base de Datos Avanzadas",
                    Creditos = 4,
                    CupoMaximo = 20,
                    HorarioInicio = new TimeSpan(14, 0, 0),
                    HorarioFin = new TimeSpan(16, 0, 0),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "WEB401",
                    Nombre = "Desarrollo Web con ASP.NET Core",
                    Creditos = 5,
                    CupoMaximo = 30,
                    HorarioInicio = new TimeSpan(16, 0, 0),
                    HorarioFin = new TimeSpan(18, 0, 0),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "IA501",
                    Nombre = "Introducción a IA",
                    Creditos = 4,
                    CupoMaximo = 1,
                    HorarioInicio = new TimeSpan(9, 0, 0),
                    HorarioFin = new TimeSpan(11, 0, 0),
                    Activo = true
                }
            };

            // Agregar solo los cursos que no existen (verificando por código)
            foreach (var cursoSemilla in cursosSemilla)
            {
                var cursoExiste = await context.Cursos
                    .AnyAsync(c => c.Codigo == cursoSemilla.Codigo);

                if (!cursoExiste)
                {
                    await context.Cursos.AddAsync(cursoSemilla);
                    Console.WriteLine($"✓ Curso agregado: {cursoSemilla.Codigo} - {cursoSemilla.Nombre}");
                }
                else
                {
                    Console.WriteLine($"○ Curso ya existe: {cursoSemilla.Codigo}");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}