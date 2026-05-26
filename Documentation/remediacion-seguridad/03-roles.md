# Bloque 3 – Roles (SEC-012)

## Análisis

**Hallazgo:** El seed de roles en `Program.cs` creaba `["Paciente", "Medico", "Admin"]`, pero todo el código de autorización usa `"Administrador"`:

- `[Authorize(Roles = "Administrador")]` — en 10+ controladores y páginas
- `User.IsInRole("Administrador")` — en `HangfireAdminAuthFilter`
- `policy.RequireRole("Administrador")` — política `AdminOnly` en `Program.cs`
- `options.Conventions.AuthorizeAreaFolder("Identity", "/Admin", "AdminOnly")`

El rol `"Admin"` seeded nunca otorgaba acceso a ningún recurso protegido. Los administradores sólo podían acceder si el rol `"Administrador"` fue creado manualmente en la DB.

## Remediación (SEC-012)

Agregar `"Administrador"` al seed de roles. Se mantiene `"Admin"` para no romper posibles registros existentes en la base de datos de producción.

```csharp
foreach (var role in new[] { "Paciente", "Medico", "Admin", "Administrador" })
```

## Nota operativa

Si existen usuarios en producción asignados al rol `"Admin"`, deben ser reasignados al rol `"Administrador"` para obtener los permisos correctos. El rol `"Admin"` puede eliminarse en una futura release una vez que se haya completado la migración de usuarios.

## Estado

| Issue | Estado |
|---|---|
| SEC-012 | ✅ RESUELTO — `"Administrador"` agregado al seed de roles |
