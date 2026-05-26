# Auditoría técnica — EIIBD · 2026-05-22

> Generada por revisión de código + BD. Última actualización del repo: commit `2c3a4fb`.

---

## 1. Archivos modificados sin commit

| Archivo | Descripción del cambio |
|---------|------------------------|
| `.claude/settings.local.json` | Permisos y herramientas de Claude Code (settings locales de sesión) |
| `eiibd26/wwwroot/uploads/medicos/medico-3a2dbe*.jpg` | Foto de perfil del médico de prueba subida pero no en gitignore |

> **Nota**: Los 22 archivos de la sesión anterior ya fueron commiteados (`67a7d92`, `7d91b4a`, `86969bf`, `6141e20`, `2c3a4fb`, `bqy05qbc2`). Solo quedan 2 archivos sin commit: settings locales y el JPG subido.

**Archivos modificados en los últimos 6 commits (contexto completo):**

| Archivo | Cambio aplicado |
|---------|-----------------|
| `Areas/Identity/Pages/Account/Login.cshtml.cs` | Redirect post-login según rol (Medico→Dashboard, resto→Usuario/Dashboard); lockout habilitado |
| `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml` | Eliminados hidden inputs que impedían guardar checkboxes; sección privacidad corregida |
| `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml.cs` | Mapa de datos, fix SaveChanges cross-contamination, fix badges try-catch, Priv* bool binding |
| `Areas/Identity/Pages/Account/Manage/_MedicoNav.cshtml` | Actualizada navegación del panel médico |
| `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml` | Panel admin directorio médicos — UI actualizada |
| `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs` | Lógica admin directorio médicos |
| `Areas/Identity/Pages/Medico/Dashboard.cshtml` | Dashboard médico con 6 secciones por nivel, badges, recomendaciones |
| `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` | Sidebar Identity: Paciente gateado con `!IsAdmin`, slug médico vinculado |
| `Models/Directorio/DirectorioViewModels.cs` | ViewModels del directorio actualizados |
| `Pages/DirectorioMedicos/Detalle.cshtml` | Detalle médico público; secciones paciente/médico separadas |
| `Pages/DirectorioMedicos/Detalle.cshtml.cs` | Fix JSON hospitales (split→deserializar); lógica roles en Detalle |
| `Pages/DirectorioMedicos/Index.cshtml.cs` | Buscador directorio médicos |
| `Pages/Glosario/Termino.cshtml` | Página de término de glosario |
| `Pages/Shared/_MedicoCard.cshtml` | Tarjeta médica en directorio |
| `Pages/Shared/_MedicoBadges.cshtml` | Partial para renderizar badges médicos |
| `Pages/Shared/_SidebarMenu.cshtml` | Sidebar público: Medico gateado, Paciente gateado, Admin gateado |
| `Pages/Shared/_TopMenuDesktop.cshtml` | Avatar top-menu desde Perfil.Avatar (prioridad sobre filesystem) |
| `Pages/Shared/_TopMenuMobile.cshtml` | Mismo fix de avatar en móvil + inject ApplicationDbContext |
| `Services/Glossary/DTOs/GlossaryValidationCountsDto.cs` | DTO para conteos de validación |
| `Services/Glossary/GlossaryService.cs` | GetValidationCountsAsync con defensas para columna AiReasoning nueva |
| `Services/Medico/MedicoBadgeService.cs` | EvaluarBadgesAutomaticosAsync; GetNivelActualAsync; OtorgarBadgeAsync |
| `wwwroot/css/directorio-medicos.css` | Estilos del directorio médico |

---

## 2. Errores críticos detectados

### 🔴 Crítico

**C-01 — Sidebar Pages/ usa rol "Paciente" sin `!IsAdmin`**
- **Archivo**: `Pages/Shared/_SidebarMenu.cshtml` línea ~265
- **Causa raíz**: `@if (User.IsInRole("Paciente"))` — si el Admin también tiene el rol Paciente asignado, verá el bloque de paciente en páginas públicas.
- **Diferencia con Identity sidebar**: `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` línea 247 ya incluye `&& !User.IsInRole("Administrador")`.
- **Fix**: Agregar `&& !User.IsInRole("Administrador")` en `Pages/Shared/_SidebarMenu.cshtml`.

**C-02 — `Perfil.Nombre` del médico está vacío — top-menu muestra email**
- **Archivo**: `Pages/Shared/_TopMenuDesktop.cshtml` línea ~33
- **Causa raíz**: Al crear el `Perfil` para un médico (vía `PerfilMedico.cshtml.cs`), `Nombre = string.Empty`. El top-menu cae al fallback `User.Identity.Name` (el email).
- **Impacto**: El médico ve su email en el menú superior en vez de su nombre del directorio.
- **Fix**: Al guardar privacidad, sincronizar también `perfilBd.Nombre` desde `MedicosDirectorio.NombreCompleto`.

**C-03 — Checkboxes de privacidad siempre guardaban `false` (ya corregido)**
- **Archivo**: `PerfilMedico.cshtml` — corregido en commit `bqy05qbc2`
- **Causa raíz**: `hidden(false)` antes del `checkbox(true)` → `BoolModelBinder.FirstValue = "false"` siempre.
- **Estado**: ✅ Corregido. Pendiente verificar en producción que los checkboxes ahora persisten.

### 🟡 Medio

**M-01 — `medicoSlug` en sidebar apunta a `/medicos/{slug}` pero la ruta es `/DirectorioMedicos/Detalle/{id}`**
- **Archivo**: `Pages/Shared/_SidebarMenu.cshtml` línea ~391 y `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` línea ~374
- **Causa raíz**: Ambos sidebars enlazan `href="/medicos/@medicoSlug"`. Si la ruta del slug-based URL no está configurada en `Program.cs`, da 404.
- **Fix**: Verificar si existe la ruta `/medicos/{slug}` o si debe ser `/DirectorioMedicos/Detalle/{id}`.

**M-02 — Dashboard médico usa `Perfil.PermitirCompartirDatosMedicos` para mostrar nombre del paciente**
- **Archivo**: `Dashboard.cshtml.cs` línea 100
- **Causa raíz**: `p.PermitirCompartirDatosMedicos == true` — si el campo era siempre `0` (bug C-03), ningún paciente permitía compartir su nombre, por lo que todos los nombres se mostraban como "Paciente anónimo".
- **Fix**: Ahora que C-03 está corregido, los nombres deberían mostrarse. Verificar después de que los pacientes actualicen sus preferencias.

**M-03 — `EvaluarBadgesAutomaticosAsync` consulta `GlossaryValidations` con `UserId` como string**
- **Archivo**: `MedicoBadgeService.cs` línea 122
- **Causa raíz**: `v.UserId == userIdStr` donde `userIdStr = perfil.UserId.Value.ToString()`. Si la columna `GlossaryValidations.UserId` es `uniqueidentifier` en BD pero string en C#, la comparación puede fallar o ser ineficiente.
- **Fix**: Verificar el tipo de `GlossaryValidations.UserId` en el modelo y en la BD; convertir a `Guid` si aplica.

**M-04 — `GetValidationCountsAsync` hace dos queries separadas para AiReasoning**
- **Archivo**: `GlossaryService.cs` líneas 351-369
- **Causa raíz**: Carga `AiReasoning` en query separada dentro de try-catch por si la columna no existe aún. Esto es un workaround temporal que debe eliminarse una vez que se haga el ALTER TABLE.
- **Fix**: Ejecutar `ALTER TABLE GlossaryTerms ADD AiReasoning NVARCHAR(MAX) NULL` y eliminar el try-catch.

### 🟢 Menor

**m-01 — Foto de médico en `wwwroot/uploads/medicos/` no está en .gitignore**
- **Archivo**: `eiibd26/wwwroot/uploads/medicos/medico-*.jpg`
- **Impacto**: Fotos subidas en desarrollo se pushean al repo. No es un error funcional pero ensucia el historial.
- **Fix**: Agregar `eiibd26/wwwroot/uploads/` a `.gitignore`.

**m-02 — `_SidebarMenu.cshtml` (Pages/) no tiene `Laboratorios` en el submenú admin**
- **Archivo**: `Pages/Shared/_SidebarMenu.cshtml` vs `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml`
- **Causa raíz**: El sidebar de Identity (para admin) sí tiene el submenú colapsable de Laboratorios, el de Pages/ no.
- **Fix**: Sincronizar ambos sidebars o extraer a un único partial compartido.

**m-03 — `_TopMenuDesktop.cshtml` hace un `try/catch` silencioso para el avatar**
- **Archivo**: `_TopMenuDesktop.cshtml` líneas 43-52
- **Causa raíz**: Si el servicio de identidad falla, el avatar se muestra como default sin log.
- **Fix**: Agregar `_logger.LogWarning` en el catch.

---

## 3. Separación de ambientes Paciente vs Médico

| URL | Rol requerido (`[Authorize]`) | Sidebar correcto | Redirect post-login | Estado |
|-----|-------------------------------|-----------------|---------------------|--------|
| `/Identity/Medico/Dashboard` | `Medico` | ✅ Muestra "Mi espacio médico" | ✅ Login.cshtml.cs → `/Identity/Medico/Dashboard` | ✅ OK |
| `/Identity/Account/Manage/PerfilMedico` | `Medico` | ✅ Muestra nav médico (_MedicoNav) | ✅ Correcto | ✅ OK |
| `/Identity/Usuario/Dashboard` | (autenticado, no rol específico) | ✅ Muestra bloque Paciente | ✅ Login → `/Identity/Usuario/Dashboard` | ✅ OK |
| `/DirectorioMedicos` (Index) | Público | ✅ Sin sidebar en páginas públicas | N/A | ✅ OK |
| `/DirectorioMedicos/Detalle/{id}` | Público | ✅ Sin sidebar | N/A | ⚠️ Parcial — Bloque `CanInteractAsPaciente` excluye médicos, pero un Admin también se excluye porque `CanInteractAsPaciente = (IsPaciente \|\| IsAdmin) && !IsMedico` — Admin puede confirmar médico |
| `/Identity/Admin/DirectorioMedicos/Index` | `Administrador` | ✅ Muestra bloque Admin | N/A | ✅ OK |
| `/Glosario/Termino/{slug}` | Público | ❌ Sin sidebar (isGlosarioPage excluye) pero usuario autenticado no ve su menú | N/A | ⚠️ Parcial |
| `/Identity/Medico/Dashboard` (como paciente) | — | — | ❌ Daría 403 Forbidden | ✅ OK (redirige a Access Denied) |
| `Pages/Shared/_SidebarMenu.cshtml` para Medico | `Medico` | ✅ Muestra "Mi espacio médico" | N/A | ✅ OK |
| `Pages/Shared/_SidebarMenu.cshtml` para Admin | `Administrador` | ⚠️ Muestra admin, pero también mostraría Paciente si Admin tiene rol Paciente | N/A | ⚠️ Parcial (C-01) |

---

## 4. Flujos que deben probarse manualmente

### F-01 — Login como médico → dashboard correcto
```
1. Ir a /Identity/Account/Login
2. Ingresar credenciales de usuario con rol "Medico"
3. Verificar redirect automático a /Identity/Medico/Dashboard
4. Verificar que el sidebar muestra "Mi espacio médico" y NO items de paciente
5. Verificar que el top-menu muestra el nombre (no el email)
   → Si muestra email: bug C-02 pendiente
6. Verificar que el Dashboard muestra Nivel y badges correctos
```

### F-02 — Login como paciente → dashboard correcto
```
1. Ir a /Identity/Account/Login
2. Ingresar credenciales de usuario con rol "Paciente"
3. Verificar redirect automático a /Identity/Usuario/Dashboard
4. Verificar que el sidebar muestra "Mi Salud", "Panel de Control", etc.
5. Verificar que NO aparece "Mi espacio médico"
6. Verificar que el top-menu muestra el avatar correcto
```

### F-03 — Médico edita perfil → todos los campos guardan
```
1. Login como médico → ir a /Identity/Account/Manage/PerfilMedico
2. Editar Biografía, agregar hospital, cambiar horarios
3. Marcar checkboxes de Privacidad (Estado y Privacidad)
4. Click "Guardar cambios"
5. Verificar mensaje de éxito "Perfil actualizado correctamente"
6. Recargar la página → verificar que:
   a. Biografía persiste ✓
   b. Hospitales persisten ✓
   c. Checkboxes de privacidad están marcados ✓ (era el bug principal)
7. En BD: SELECT PermitirTelefonoReal, PermitirCorreoNoticias... FROM Perfil WHERE idUser = '...'
   → Deben ser 1 para los campos marcados
```

### F-04 — Médico valida término de glosario → badge se actualiza
```
1. Login como médico con Nivel ≥ 5 (validador_contenido)
2. Ir a /Glosario e ir a un término
3. Hacer clic en validar
4. Verificar que GlossaryValidations tiene la entrada con Approved = true
5. Ir a /Identity/Medico/Dashboard → verificar que el badge "Validador" aparece ganado
6. Si no aparece: EvaluarBadgesAutomaticosAsync no encuentra la validación
   → Revisar tipo de UserId en GlossaryValidations (bug M-03)
```

### F-05 — Paciente ve detalle médico → NO ve bloques de paciente si es médico
```
1. Login como paciente → ir a /DirectorioMedicos/Detalle/{id}
2. Verificar que APARECE:
   - Sección "¿Recibiste atención de este médico?"
   - Sección "¿Conoces o fuiste paciente de este médico?"
3. Login como médico → ir al mismo Detalle
4. Verificar que NO APARECE ninguna de las secciones anteriores
   (CanInteractAsPaciente = false para médicos)
5. Verificar que SÍ APARECE botón "Reclamar perfil" (si no está vinculado)
   y "Ir a mi dashboard" (si es el dueño)
```

### F-06 — Médico ve su propio detalle → ve botón de dashboard, no bloques de paciente
```
1. Login como médico con perfil reclamado
2. Ir a /DirectorioMedicos/Detalle/{su_id}
3. Verificar:
   - NO aparece "¿Recibiste atención?"
   - NO aparece "¿Conoces o fuiste paciente?"
   - SÍ aparece "Ir a mi dashboard" (IsOwnerMedico = true)
   - NO aparece "Reclamar perfil" (PerfilYaVinculado = true)
4. Verificar que los hospitales se muestran como lista visual
   (no como JSON crudo ["h1","h2"]) → bug corregido en Detalle.cshtml.cs
```

### F-07 — Slug de médico en sidebar → enlace correcto
```
1. Login como médico con Slug configurado (ej: "berletzis")
2. Verificar que el sidebar muestra "Ver mi perfil público"
3. Hacer clic → verificar que la URL es /medicos/berletzis o equivalente
4. Si da 404: bug M-01 — la ruta slug no está configurada
```

---

## 5. Orden de prioridades recomendado

| # | Tarea | Archivo(s) | Estimado |
|---|-------|------------|----------|
| 1 | **Fix C-01** — Agregar `!IsAdmin` en sidebar Pages/ para bloque Paciente | `Pages/Shared/_SidebarMenu.cshtml` línea ~265 | 5 min |
| 2 | **Fix C-02** — Sincronizar `Perfil.Nombre` desde `MedicosDirectorio.NombreCompleto` al guardar PerfilMedico | `PerfilMedico.cshtml.cs` en bloque privacidad | 15 min |
| 3 | **Verificar M-01** — Confirmar que `/medicos/{slug}` existe como ruta o corregir href en sidebar | `Program.cs` + `_SidebarMenu.cshtml` | 10 min |
| 4 | **Ejecutar SQL M-04** — `ALTER TABLE GlossaryTerms ADD AiReasoning NVARCHAR(MAX) NULL` y limpiar try-catch doble en GlossaryService | `GlossaryService.cs` + SQL directo | 20 min |
| 5 | **Fix M-03** — Verificar tipo de `GlossaryValidations.UserId` y alinear con Guid | `MedicoBadgeService.cs` línea 122 + modelo | 15 min |
| 6 | **Prueba manual F-03** — Verificar que privacidad persiste tras commit | Manual (browser + sqlcmd) | 10 min |
| 7 | **Prueba manual F-04** — Verificar badge validador_contenido con fix de UserId | Manual | 10 min |
| 8 | **Fix m-01** — Agregar `eiibd26/wwwroot/uploads/` a `.gitignore` | `.gitignore` | 5 min |
| 9 | **Fix m-02** — Sincronizar submenú Laboratorios en ambos sidebars | `Pages/Shared/_SidebarMenu.cshtml` | 10 min |
| 10 | **Prueba manual F-01 a F-07** — Ciclo completo de flujos | Browser + BD | 45 min |

**Total estimado**: ~2.5 horas para fixes + pruebas.

---

## 6. Commit sugerido

```
fix: sidebar ambientes, privacidad checkboxes, hospitales JSON, avatar top-menu

- C-01 fix: _SidebarMenu (Pages/) bloque Paciente: agregar !IsAdmin para evitar
  que administradores con rol Paciente vean menú de paciente
- C-02 fix: sincronizar Perfil.Nombre desde MedicosDirectorio.NombreCompleto
  al guardar PerfilMedico (top-menu mostraba email en lugar del nombre)
- C-03 fix (ya aplicado): eliminar hidden inputs en checkboxes privacidad;
  BoolModelBinder.FirstValue siempre era "false" con el patrón invertido
- Bug hospitales JSON: Detalle.cshtml.cs usa JsonDeserialize en lugar de Split
- Bug avatar: _TopMenuDesktop y _TopMenuMobile leen Perfil.Avatar con prioridad
- Bug SaveChanges: PerfilMedico desacopla perfil antes del Save de Perfil cuando
  ModelState es inválido para evitar commits parciales
- Mapa de datos en PerfilMedico.cshtml.cs (comentario de arquitectura)
```

---

*Generado el 2026-05-22 mediante revisión de código, git log y consultas directas a BD.*
