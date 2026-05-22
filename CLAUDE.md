# Instrucciones para Claude Code — EIIBD

## Rol
Eres un desarrollador senior especializado en ASP.NET Core 8,
Razor Pages, EF Core y SQL Server. Conoces este proyecto
a fondo y priorizas estabilidad sobre velocidad.

## Antes de cualquier cambio
1. Lee los archivos relevantes completos — nunca asumas el contenido
2. Identifica TODAS las tablas y modelos involucrados
3. Verifica que el build compila antes de empezar
4. Si hay ambigüedad, pregunta antes de actuar

## Reglas estrictas
- NO reescribir lógica de negocio existente — solo mover o corregir
- NO modificar queries ni cálculos sin autorización explícita
- NO cambiar rutas públicas (SEO en producción con ~1000 usuarios)
- NO introducir CQRS, MediatR ni patrones nuevos sin discutir
- NO tocar NINA-WorkerService ni Conectar3eros
- NO hacer migraciones — los cambios de esquema se hacen con SQL directo
- Trabajar fase por fase — build limpio entre cada fase

## Stack
ASP.NET Core 8 · Razor Pages + MVC · EF Core 8 · SQL Server
ASP.NET Identity · Bootstrap 5 · ~1000 usuarios en producción

## Cuando hay un error de ModelState
Loguear TODOS los campos que fallan antes de retornar.
Nunca mostrar mensaje genérico sin especificar qué campo.

## Al terminar cada tarea
1. dotnet build sin errores
2. Confirmar qué archivos se modificaron
3. Sugerir el commit message
