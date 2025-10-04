## 🚀 URL de Producción

**Aplicación desplegada**: https://parcial-7w64.onrender.com

### Credenciales de Prueba

**Coordinador:**
- Email: `coordinador@universidad.edu.pe`
- Password: `Coordinador123!`

**Estudiante de Prueba:**
- Registrarse en: `/Identity/Account/Register`

## 🔧 Configuración de Producción

### Variables de Entorno (Render.com)
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ConnectionStrings__DefaultConnection=[PostgreSQL Internal URL]
Redis__ConnectionString=[Upstash Redis URL]
Redis__InstanceName=PortalAcademico:
CacheSettings__CursosCacheDuration=60

# Portal Académico - Gestión de Cursos y Matrículas

Sistema web para gestionar cursos, estudiantes y matrículas con ASP.NET Core MVC + Identity + Redis.

## 🛠️ Stack Tecnológico

- **Framework**: ASP.NET Core MVC (.NET 8)
- **Autenticación**: ASP.NET Core Identity
- **ORM**: Entity Framework Core
- **Base de Datos**: SQLite (desarrollo) / PostgreSQL (producción)
- **Cache/Sesiones**: Redis (Redis Labs)
- **Despliegue**: Render.com
- **Control de Versiones**: GitHub

## 📋 Prerrequisitos

- .NET 8 SDK
- Git
- Cuenta GitHub
- Cuenta Render.com
- Cuenta Redis Labs (para Redis gestionado)

## 🚀 Instalación Local

### 1. Clonar el repositorio
```bash
git clone https://github.com/TU_USUARIO/portal-academico-gestion-cursos.git
cd portal-academico-gestion-cursos