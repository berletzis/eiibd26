# CLAUDE.md — Proyecto EIIBD

La documentación completa y las instrucciones para Claude Code viven en:

@Documentation/CLAUDE.md

---

## Estado al 2025 (sesión post-auditoría dependencias)

### Completado esta sesión
- Fix CRÍTICO: datos de prueba expuestos al público en sección "Validado por médicos" del glosario
- Fix 400: UsuarioLaboratorios `ActualizarResultado` — parámetros `string?` en handler
- Fix 400: UsuarioCondiciones, UsuarioTratamientos, UsuarioSintomas `EditarFechaInicio` — `DateTime?`
- Auditoría dependencias cerrada: DEP-001 eliminado, DEP-002 downgradeado a 8.0.23
- Documentación de cierre: `Documentation/dependencias-cierre/`

### Pendientes próxima sesión (en orden)
1. **A-01 URGENTE:** Eliminar datos de prueba de BD de producción (ver TODO_SIGUIENTE_SESION.md)
2. **A-02:** Auditar todos los handlers POST por parámetros no nullable (`grep OnPost.*Async.*string [^?]`)
3. **A-03:** Hook `EvaluarBadgesAutomaticosAsync` en `GlossaryService.AddValidationAsync`
4. **M-01:** Panel admin para gestionar validaciones del glosario
5. **M-02:** Dashboard médico Q&A filtrado por áreas EII

### Regla crítica — handlers Razor Pages
SIEMPRE usar `string?` y `DateTime?` en parámetros de `OnPost*Async`.
Con nullable reference types (.NET 8), `string param` es implícitamente `[Required]` → 400 si llega vacío.

### Sin migraciones
Todos los cambios de esquema se hacen con SQL directo en producción.
No usar dotnet ef database update.
