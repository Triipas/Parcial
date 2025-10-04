## 🚀 URL de Producción

**Aplicación desplegada**: [https://portal-academico.onrender.com](https://tu-url.onrender.com)

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