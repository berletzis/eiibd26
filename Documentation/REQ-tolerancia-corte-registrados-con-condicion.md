# REQ — Panel tolerancia: corte "Registrados con condición"

**Fecha:** 23 JUL 2026
**Scope:** solo `eiibd26.Web`, panel `Admin/Platillos/EstadisticasTolerancia`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build con vistas Razor (`dotnet publish`) antes de pushear.
**Objetivo:** agregar un cuarto corte al panel — "Registrados con condición" — junto a los que ya existen (Todos / CUCI / Crohn), con el **mismo cálculo bayesiano** (media posterior + IC 95% + n) vía `ToleranciaBayes`.

## Definición del corte (clave)
"Registrados con condición" = votos con **`UserId IS NOT NULL` Y `CondicionIdPrincipal IS NOT NULL`**, sin importar el tipo.
- Es **más amplio** que Crohn+CUCI: incluye pacientes logueados cuya condición **no clasificó** a esos dos tipos (`TipoEII IS NULL` pero con condición registrada — colitis indeterminada, etc.).
- Relación entre cortes:
  - **Todos** = registrados + anónimos (todo).
  - **Registrados con condición** ⊆ Todos (nuevo).
  - **Crohn / CUCI** ⊆ Registrados con condición (los que sí clasificaron por `TipoEII`).

## Qué cambia
- **Columna nueva "Registrados con condición"** en el grid por ingrediente: media + IC + n, con el mismo gate (n≥10 y ancho IC ≤ 40) o "insuficiente (n=…)".
- Reusar `ToleranciaBayes.Estimar(si, no)` — **NO tocar el cálculo**, solo alimentar otro filtro de votos.
- **CSV:** agregar una fila por (ingrediente, "Registrados con condición") con media, IC y si pasa el gate, igual que los otros segmentos.
- Mantener el callout existente sobre anónimos (cuentan en "Todos", no en los segmentos por tipo). Ajustarlo para aclarar que "Registrados con condición" tampoco incluye anónimos.

## Fuera de alcance
- **Sin tabla nueva ni deploy-gate SQL** — la data (`UserId`, `CondicionIdPrincipal`, `TipoEII`) ya existe.
- No tocar el cálculo bayesiano ni la encuesta pública.
- Sin segmentar por condición individual (solo "con condición, cualquiera"); el detalle por condición es futuro.

## Verificación
1. La columna "Registrados con condición" aparece junto a Todos/CUCI/Crohn con media + IC + n.
2. Su n es **≥** que (Crohn + CUCI) para el mismo ingrediente — porque incluye a los que no clasificaron. Si es menor, hay bug.
3. Su n es **≤** que "Todos" (excluye anónimos).
4. Un voto de un usuario logueado con condición que NO es Crohn ni CUCI → suma en "Todos" y en "Registrados con condición", NO en Crohn/CUCI.
5. CSV incluye el nuevo segmento.
6. `dotnet publish -c Release` limpio antes del push.
