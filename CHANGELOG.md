# CHANGELOG.md

Todos los cambios notables del proyecto EIIBD se documentan en este archivo.  
Formato basado en [Keep a Changelog](https://keepachangelog.com/es/1.0.0/).

---

## [Unreleased]

---

## [2025-07-xx] — Sesión post-auditoría dependencias y bugs 400

### Seguridad
- **CRÍTICO FIX:** `GlossaryService.GetValidationCountsAsync` — `MeaningComments` ya no expone texto de cuentas de prueba al público. Ahora solo muestra comentarios de médicos con badge `perfil_reclamado` o `verificado` (mismo filtro que `ComentariosMedicos`).
- Nuevo método privado `FilterCommentsByVerifiedDoctorAsync` en `GlossaryService`.

### Bug Fixes
- **400** en `UsuarioLaboratorios?handler=ActualizarResultado` — parámetros `string resultValue`, `string resultUnit`, `string notes`, `string resultDate` cambiados a `string?`.
- **400** en `UsuarioCondiciones?handler=EditarFechaInicio` — `DateTime nuevaFechaInicio` cambiado a `DateTime?`.
- **400** en `UsuarioTratamientos?handler=EditarFechaInicio` — `DateTime nuevaFechaInicio` cambiado a `DateTime?`.
- **400** en `UsuarioSintomas?handler=EditarFechaInicio` — `DateTime nuevaFechaInicio` cambiado a `DateTime?`.

### Dependencias
- **Eliminado:** `Microsoft.AspNetCore.DataProtection.Extensions 10.0.3` — paquete fantasma sin uso en código fuente (DEP-001).
- **Downgraded:** `Microsoft.VisualStudio.Web.CodeGeneration.Design` `9.0.0` → `8.0.23` para alinear con TFM `net8.0` (DEP-002).
- Confirmado: **0 vulnerabilidades** en `dotnet list package --vulnerable`.

### Documentación
- Creado `Documentation/dependencias-cierre/` con 5 archivos de cierre de auditoría.
- DEP-001 y DEP-002 marcados CERRADO en `Documentation/auditoria/09dependencias.html`.
- Creado `Documentation/RESUMEN_SESION.md`.
- Creado `Documentation/MEMORIA_PROYECTO.md`.
- Creado `Documentation/TODO_SIGUIENTE_SESION.md`.

---

## [2026-05-22] — Sistema médico y auditorías

### Added
- Sistema de badges para médicos (`perfil_reclamado`, `verificado`, `creador_contenido`).
- Dashboard médico con Q&A, validaciones de glosario, estadísticas.
- `PerfilMedico` — formulario completo con áreas EII, hospitales, avatar, privacidad.
- Directorio médico con buscador y filtros.
- Módulo de laboratorios para pacientes (`PatientLaboratoryResult`).
- Auditoría técnica completa: `Documentation/auditoria/` (01–10).

### Fixed
- `ModelState` en `PerfilMedico` — `PerfilBase.idUser` vacío causaba fallo silencioso.
- Auto-link reclamación médica por `AspNetUserId` OR email.
- Checkboxes privacidad — hidden inputs con `value=false` sobreescribían el estado real.
- Sidebar paciente mostraba opciones de admin.
- Top-menu mostraba email en vez de nombre del médico.

### Security
- Generación auditoría de seguridad `02seguridad.html`.
- Remediaciones aplicadas documentadas en `Documentation/remediacion-seguridad/`.
