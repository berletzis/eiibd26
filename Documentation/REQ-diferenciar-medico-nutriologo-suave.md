# REQ — Diferenciar médico / nutriólogo (versión SUAVE: guía, no muro)

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Objetivo:** que el tipo de profesional (médico especialista en EII vs nutriólogo) **guíe** lo que ve para validar — cada quien su terreno primero — **sin bloquear** (el traslape nutrición↔clínica en EII es real y valioso). Se apoya en la página "Mis Validaciones" ya construida.

## Contexto verificado
- Hoy NO hay diferencia funcional: ambos reciben el rol interno `Medico`; el candado de validación es solo `["Medico","Administrador"]`. El único dato es `MedicoDirectorio.Especialidad` (**texto libre**, solo se muestra, no ramifica lógica).
- `RegisterM` ya crea una ficha `MedicoDirectorio` (no pública) con especialidad + cédula al registrarse → ahí también vive el nuevo tipo.
- El TOP clínico ya existe: `GlossaryService.GetTopTermsByQualityAsync(GlossaryTermType, 10)`.
- Las notas de alimentos que valida un nutriólogo son `PlatNotaClinica` (grupos/ingredientes), validadas vía `ValidacionContenidoService` con `TipoContenido = NotaClinicaIngrediente`.

## 1. Campo nuevo: tipo de profesional
- Agregar **`TipoProfesional`** a `MedicoDirectorio` (enum-byte nullable): `1 = MedicoEspecialistaEII`, `2 = Nutriologo`, `3 = Otro`. **Nullable** — los existentes quedan `null` = "general" (ven el TOP clínico por defecto, comportamiento actual).
- **Conservar `Especialidad`** (texto libre) tal cual — el tipo es lo estructurado que maneja la lógica; la especialidad es el detalle que se muestra ("Gastroenterología pediátrica", etc.).
- **Captura/edición:**
  - **`RegisterM.cshtml` (página de registro, la que abre `/profesionaldelasalud/invitacion`):** agregar un `<select>` **"¿Eres médico especialista en EII o nutriólogo?"** (opciones: Médico especialista en EII / Nutriólogo / Otro) **justo antes del campo "Especialidad"** (que hoy va después del select de País). Enlazar a una propiedad nueva `Input.TipoProfesional` en `RegisterM.cshtml.cs`; al crear la ficha `MedicoDirectorio` en `OnPostAsync`, guardar ese valor en `MedicoDirectorio.TipoProfesional`. Es exactamente lo que la invitación les pide indicar.
  - `PerfilMedico` (editar perfil): mismo `<select>` para que el profesional pueda cambiar su tipo.
  - `Admin/DirectorioMedicos`: el admin puede fijarlo/corregirlo.
- **SQL deploy-gate** `SQL/add-medicodirectorio-tipoprofesional.sql` (idempotente, correr antes de desplegar).

## 2. TOP de "Mis Validaciones" según el tipo (guía, con fallback)
En `MisValidaciones` (PageModel), elegir la fuente del TOP según `TipoProfesional`:
- **MedicoEspecialistaEII / Otro / null** → TOP **clínico** actual: `GetTopTermsByQualityAsync(Sintoma+Tratamiento, 10)`. Sin cambio.
- **Nutriologo** → TOP de **alimentos**: notas `PlatNotaClinica` publicadas de grupos/ingredientes que el nutriólogo **aún no ha validado**, ordenadas por prioridad (las que menos validaciones tienen / más lo necesitan). **Claude Code: verificar qué ranking/consulta existe para notas; si no hay, construir una consulta simple** (publicadas, no validadas por este userId, orden por conteo de validaciones asc / fecha). Cada fila: nombre del grupo/ingrediente + link a su ficha + los mismos dos indicadores (contenido / — aquí el "nivel de relación" no aplica; usar solo el estado de validación de la nota).
- **Fallback suave (clave):** si la fila del tipo sale **flaca** (< 5, parametrizable), **agregar debajo el otro tipo** con un divisor claro: *"¿Ya cubriste lo tuyo? También puedes ayudar con esto:"* + el TOP del otro dominio. Nadie queda sin qué validar, y no se bloquea el traslape.

## 3. ABC / CTA adaptados al tipo (toque fino)
- El texto del ABC y el CTA "Empieza a validar" mencionan el terreno del tipo: nutriólogo → "valida ingredientes y grupos de alimentos"; médico → "valida síntomas y tratamientos". Si es null/Otro, texto genérico.

## 4. Lo que NO cambia
- **Sin candado duro:** el permiso de validar sigue siendo el rol `Medico` — cualquiera puede validar cualquier cosa. Esto es solo **guía/orden**, no restricción.
- El **historial** ("Mis validaciones") sigue mostrando TODO lo que el profesional validó (clínico y/o alimentos), sin filtrar por tipo.
- No renombrar el rol interno `Medico`.

## 5. Verificación
1. Un profesional marcado **Nutriólogo** ve en su TOP **ingredientes/grupos** para validar (no síntomas), con el ABC/CTA hablando de alimentos.
2. Un **Médico especialista EII** (o null/Otro) ve el TOP **clínico** actual, sin cambios.
3. Si la fila del nutriólogo sale corta (< 5), aparece el divisor y debajo el TOP clínico — nadie queda sin qué hacer.
4. El **permiso** no cambió: un nutriólogo TODAVÍA puede entrar a un término clínico y validarlo si quiere (no hay bloqueo).
5. El tipo se captura en `RegisterM`, se edita en `PerfilMedico` y el admin lo puede fijar.
6. Correr `SQL/add-medicodirectorio-tipoprofesional.sql` antes de desplegar. `dotnet publish -c Release` limpio antes del push.
