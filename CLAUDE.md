# CLAUDE.md — Proyecto EIIBD

La documentación completa y las instrucciones para Claude Code viven en:

@Documentation/CLAUDE.md

---

## Estado al 2026-05-22

### Completado esta semana
- Sistema médico completo: badges, reclamación, dashboard, PerfilMedico
- GitHub MCP conectado
- Fix ModelState PerfilMedico (PerfilBase.idUser vacío)
- Fix auto-link reclamación (AspNetUserId OR email)
- Fix Áreas EII guardado
- Fix checkboxes privacidad (hidden=false sobreescribía)
- Auditoría técnica generada: AUDITORIA-2026-05-22.md

### Pendientes próxima sesión (en orden)
1. C-01: Pages/Shared/_SidebarMenu.cshtml — agregar &&!IsInRole("Administrador") al bloque Paciente
2. C-02: Top-menu muestra email en vez de nombre del médico
3. Hook EvaluarBadgesAutomaticosAsync en GlossaryService.AddValidationAsync
4. Admin panel para badges manuales (verificado, creador_contenido)
5. Dashboard médico Q&A filtrado por áreas EII del médico
6. Consolidación CSS — próxima semana

### Sin migraciones
Todos los cambios de esquema se hacen con SQL directo en producción.
No usar dotnet ef database update.
