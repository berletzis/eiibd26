# Memoria de sesión — 22 JUL 2026 (lo bueno y lo malo, para retomar)

Handoff para la próxima tanda. Resume qué se hizo, qué duele, qué decidimos y qué sigue. Repo al cierre: `origin/master` en `c03301f`, árbol limpio salvo los 3 docs de seguridad de prod (fuera de git a propósito).

## Lo bueno (lo que salió y quedó respaldado)
- **Referencias por recuperación, Fase 1 / Nivel 1** (`d0a5ade`): `ReferenciaRecuperacionService` recupera links reales del índice del crawler (embeddings + coseno), nunca de la memoria del modelo. Config `ReferenciasRecuperacion` (DominiosConfiables, UmbralCoseno 0.55, TopK 5). Degrada limpio (sin Voyage key / sin dominios / nada sobre umbral → 0 candidatos). REQ en `REQ-referencias-por-recuperacion.md`.
- **Crawler CCF** (`2346a0e`… no — `1aebd90`): CCF agregado a `fuentes.json` (NINA) con scope quirúrgico (`/patientsandcaregivers`, `/es`), en + es. Robots verificado. Falta correr el worker para indexar. REQ en `REQ-nina-crawler-ccf.md`.
- **Whitelist de citas**: Mayo Clinic y My Crohn's and Colitis Team agregados a `FuentesClinicasPermitidas` (config).
- **Módulo Profesionales de la salud** (`2346a0e`): alta de validador desde admin desacoplada de pacientes, rol `MedicoPendiente`, campo `Titulo`, display limpio ("Validado por" + lista, sin "Dr." hardcodeado), URL `/profesionaldelasalud/invitacion`, filtro "Por aprobar" en admin. REQ en `REQ-profesionales-salud-alta-validacion.md`.
- **Wiki técnica**: nueva sección 11 (validación médica: autorización y alta) — gitignored, interna.

## Lo malo / fricciones (para no repetirlas)
- **401 Unauthorized de Claude API**: la key de Anthropic en `web.config` (`AiAnswer__AnthropicApiKey`) quedó **revocada** tras la rotación de seguridad — la app seguía con la vieja. Lección: al rotar keys, **propagar al `web.config` de prod y reiniciar**. El panel API Keys (read-only) sirve para verificar el prefijo.
- **web.config roto → 500 en todo el sitio**: al re-pegar la key a mano se rompió el XML y cayó el sitio entero. Lección: editar `web.config` con **editor de texto plano**, validar el XML (arrastrarlo a un navegador marca la línea rota), y tener copia para restaurar rápido — el sitio funcionaba con la key vieja (solo la IA daba 401), restaurar levanta a los pacientes primero.
- **Key pegada en el chat**: se compartió una key real en texto; el usuario la gestiona en su flujo de deploy (NO recordárselo — así lo pidió).
- **Timeouts de red en `git push`**: pasó dos veces; el primer intento se colgó, el reintento entró. Verificar el remoto antes de re-pushear.
- **El REQ no previó el choque de `PerfilMedico`**: el rol `Medico` hacía doble función (validar + editar el propio perfil). "No dar rol al registrarse" habría bloqueado también el perfil. Se resolvió con el rol `MedicoPendiente` (fail-closed). Lección: mapear TODOS los usos de un rol antes de proponer gating.

## Decisiones clave (no re-litigar)
- **Gating de validación = rol puro `MedicoPendiente`** (no un flag). Fail-closed: quien no está en `["Medico","Administrador"]` no valida, sin tocar las 3 páginas de gating ni arriesgar que un endpoint futuro olvide un flag.
- **Aprobar ≠ revelar nombre**: aprobar = puede validar; el nombre público se revela aparte con el badge `verificado`.
- **Título por combo curado** (Dr./Dra./Nut./Lic. en Nutrición/…/Otro); display `{Titulo} {Nombre}`, sin título → solo el nombre. Nunca "Dr." asumido.
- **Nutrientes/calorías**: idea futura, solo la rebanada EII (6 deficiencias), sin calorías (tarea #25). No es prioridad.
- **Mayo NO es crawleable** (WAF 403); ESPEN tampoco (bloquea IA). Para el crawler solo entra CCF.

## Pendientes al retomar (el usuario decide el orden)
1. **Prioridad declarada del usuario: seguridad** — C-2 (keyring en el servidor), #8 (limpiar HEAD + .gitignore), #9 (decisión purga de historia). Detalle en los 3 docs de seguridad fuera de git.
2. **Deploy-gate de esta sesión** (cuando despliegue): correr `SQL/add-medicodirectorio-titulo.sql`, rebuild + reinicio (sembrar rol `MedicoPendiente`), y el E2E de profesionales.
3. **Correr el worker NINA** para indexar CCF → recién ahí la recuperación (Fase 1) ofrece links de CCF.
4. **REQ pausado**: `REQ-referencias-por-recuperacion.md` Fase 2 (búsqueda en vivo — decidir API primero) y Fase 3.
5. **Referencias por recuperación**: ya implementada Fase 1; probar en vivo cuando haya índice.

## Reglas de trabajo reforzadas esta sesión
- Analizar y verificar en el código **antes** de proponer; el REQ marca qué está confirmado y qué debe confirmar Claude Code.
- **Diff antes de aplicar**; no cambiar rutas públicas (solo agregar); no tocar NINA-WorkerService ni Conectar3eros.
- Docs/REQ van en commits aparte del código; los 3 docs de seguridad de prod quedan fuera de git.
- MVP con deuda técnica consciente, pero protegiendo el piso de producción y a los pacientes.
