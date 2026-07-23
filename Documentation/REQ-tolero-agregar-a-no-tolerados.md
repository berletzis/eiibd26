# REQ (MVP) — Tras votar, agregar el ingrediente a "mis no tolerados"

**Fecha:** 23 JUL 2026
**Scope:** solo `eiibd26.Web`, página `Pages/Tolero/Encuesta.cshtml(.cs)`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build con vistas Razor (`dotnet publish`) antes de pushear.
**Objetivo:** después de votar en `/tolero/{slug}`, si el usuario votó **"No"**, ofrecerle un botón para **agregar ese ingrediente a su lista personal de "No tolerados"** (la que ya alimenta el filtro de platillos). Realiza la conexión encuesta ↔ perfil de alimentación.

## Por qué solo "No"
La lista personal existente es **"No tolerados"** (`PlatPerfilExclusion`). NO hay lista de "Sí tolerados". Así que solo el voto **"No"** mapea a algo accionable. Votos "Sí" / "A veces" → sin CTA (no inventar lista nueva).

## Reglas
- **Acción explícita, no automática:** votar "No" NO excluye solo; aparece un **botón** "Agregar a mis no tolerados" que el usuario decide tocar. (Un voto comunitario ≠ decisión dietética personal.)
- **Solo logueados:** la lista es por usuario (`idUsuario`). El botón aparece solo si hay sesión Y `MiVoto == No`.
  - Anónimo que votó "No": mostrar un texto suave **"Inicia sesión para guardarlo en tu lista"** (link a login con returnUrl a la encuesta). Sin botón de guardar. (MVP; opcional si complica.)
- **Efecto consciente:** agregarlo a no tolerados **cambia qué platillos verá** en el catálogo (así funciona el filtro). Es el comportamiento deseado.

## Acción (handler nuevo en `Encuesta.cshtml.cs`)
`OnPostAgregarNoToleradoAsync` (params nullable por la regla del repo, anti-forgery, PRG):
- Resuelve el ingrediente por slug (reusar `ResolverIngredienteAsync` existente).
- Exige sesión (`UsuarioActual()`); si no, redirige a login.
- Upsert sobre `PlatPerfilExclusion` respetando el único filtrado `(idUsuario, Tipo, RefId) WHERE Eliminado = 0` y el soft-delete:
  - Existe activo → no-op, feedback "Ya está en tu lista".
  - Existe pero `Eliminado = 1` → **revivir** (`Eliminado = 0`, `FechaCreacion = ahora`).
  - No existe → insert `Tipo = 'Ingrediente'`, `RefId = IngredienteId`, `idUsuario`, `FechaCreacion = UtcNow`, `Eliminado = 0`.
- PRG: redirige de vuelta a la encuesta con feedback.

## UI (en la vista, tras votar)
- Si `MiVoto == No` y logueado: botón **"Agregar '{Nombre}' a mis no tolerados"**.
- Tras agregar: estado **"✓ Agregado a tus no tolerados"** + link **"Ver mi lista"** (a `UsuarioAlimentacion`).
- Si ya estaba: **"Ya está en tu lista"** + el mismo link.
- Mantener intacto el disclaimer "experiencia de la comunidad, no consejo médico".

## Fuera de alcance (MVP)
- **Sin lista de "Sí tolerados"** — no se crea.
- **Quitar de la lista** desde aquí = nice-to-have; por ahora se quita desde el perfil (`UsuarioAlimentacion`). (Si es barato, un toggle add/quitar es aceptable, pero no obligatorio.)
- Sin agregar por "A veces".
- **Sin tabla nueva** — reusa `PlatPerfilExclusion`. Sin deploy-gate SQL.
- No tocar el cálculo bayesiano ni el conteo de votos.

## Verificación
1. Logueado, voto "No" → aparece el botón; al tocarlo, el ingrediente entra a `PlatPerfilExclusion` (Tipo='Ingrediente') y el catálogo de platillos ya lo excluye.
2. Volver a tocar / ya estaba → "Ya está en tu lista", sin duplicar (respeta el único filtrado).
3. Ingrediente antes eliminado (soft-delete) → se revive, no crea fila nueva.
4. Voto "Sí" o "A veces" → NO aparece el botón.
5. Anónimo → sin botón; ve el nudge de iniciar sesión (o nada, si se optó por simplificar).
6. `dotnet publish -c Release` limpio antes del push.
