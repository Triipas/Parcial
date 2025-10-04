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

            // Crear cursos si no existen
            if (!await context.Cursos.AnyAsync())
            {
                var cursos = new List<Curso>
                {
                    new Curso
                    {
                        Codigo = "MAT101",
                        Nombre = "Cálculo Diferencial",
                        Creditos = 4,
                        CupoMaximo = 30,
                        HorarioInicio = new TimeSpan(8, 0, 0),  // 8:00 AM
                        HorarioFin = new TimeSpan(10, 0, 0),     // 10:00 AM
                        Activo = true
                    },
                    new Curso
                    {
                        Codigo = "PROG201",
                        Nombre = "Programación Orientada a Objetos",
                        Creditos = 5,
                        CupoMaximo = 25,
                        HorarioInicio = new TimeSpan(10, 0, 0), // 10:00 AM
                        HorarioFin = new TimeSpan(12, 0, 0),    // 12:00 PM
                        Activo = true
                    },
                    new Curso
                    {
                        Codigo = "BD301",
                        Nombre = "Base de Datos Avanzadas",
                        Creditos = 4,
                        CupoMaximo = 20,
                        HorarioInicio = new TimeSpan(14, 0, 0), // 2:00 PM
                        HorarioFin = new TimeSpan(16, 0, 0),    // 4:00 PM
                        Activo = true
                    },
                    new Curso
                    {
                        Codigo = "WEB401",
                        Nombre = "Desarrollo Web con ASP.NET Core",
                        Creditos = 5,
                        CupoMaximo = 30,
                        HorarioInicio = new TimeSpan(16, 0, 0), // 4:00 PM
                        HorarioFin = new TimeSpan(18, 0, 0),    // 6:00 PM
                        Activo = true
                    }
                };

                await context.Cursos.AddRangeAsync(cursos);
                await context.SaveChangesAsync();
            }
        }
    }
}