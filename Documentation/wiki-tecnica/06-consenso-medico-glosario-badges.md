# Consenso médico del glosario y sistema de badges médicos

> Wiki técnica interna — no publicar. Incluye la fórmula de consenso, el score de ranking y los umbrales de badges.

## Qué problema resuelve

El glosario tiene términos médicos que necesitan validación profesional en **dos dimensiones distintas**:

1. **Endoso del significado** (binario): "este médico confirma que la definición del término es correcta". Se cuenta cuántos profesionales lo endosaron.
2. **Nivel de relación con la EII** (graduado): un término puede tener relación **Directa**, **Indirecta** o **Secundaria** con la enfermedad. Aquí no basta un sí/no: cada médico vota un nivel y el sistema calcula el **consenso** (qué nivel gana).

Además, el sistema de **badges médicos** otorga distinciones y permisos escalonados a los profesionales según su actividad verificada en la plataforma.

## Cómo funciona por dentro

### 1. Endoso binario del significado

`MeaningValidationCount` = cantidad de registros en `ValidacionesContenidoProfesional` para el término (tipo `Termino`, estado `Validado`). Es un **conteo simple** de médicos que endosaron la definición. Los comentarios solo se muestran si el autor es un médico con badge verificado/reclamado.

### 2. Consenso graduado del nivel de relación

Enum `MedicalRelationType`: `Directa = 1` 🟢, `Indirecta = 2` 🟡, `Secundaria = 3` 🔵.

El consenso se calcula así (`GetValidationCountsAsync`):

1. Se agrupan las validaciones humanas aprobadas (`GlossaryValidations`, tipo `RelationValidation`, `Approved`) por nivel, contando votos: `HumanCount` por nivel.
2. **El voto de NINA cuenta +1:** si la IA sugirió un nivel (`MedicalRelationSuggestedId`), se suma 1 al conteo de ese nivel.
3. `Count(nivel) = votos_humanos(nivel) + (NINA sugirió ese nivel ? 1 : 0)`.
4. Se ordena por `Count` descendente. El nivel con el conteo máximo se marca `IsTopConsensus = true` (puede haber empates: todos los que igualan el máximo, con máximo > 0, se marcan top).

Es decir, **el consenso es la moda ponderada** de los votos, donde la sugerencia de NINA aporta exactamente un voto más.

### 3. Ranking de términos por calidad (`GetTopTermsByQualityAsync`)

Para el "Top" de términos se calcula un **score de calidad** que combina los votos de nivel (ponderados por importancia) con el uso real de usuarios:

```
Score = 3·directCount + 2·indirectCount + 1·secondaryCount + userCount
```

donde:
- `directCount / indirectCount / secondaryCount` = votos humanos aprobados de cada nivel **+ 1 si NINA sugirió ese nivel**.
- `userCount` = usuarios únicos que tienen ese síntoma/tratamiento vinculado (`sintomasUsuario` / `tratamientoUsuario`).

Se filtran solo términos cuyo síntoma/tratamiento vinculado tenga `RelacionEII = true` **y** que tengan al menos un usuario relacionado. Empates se desempatan por nombre. Resultado cacheado 10 minutos.

La ponderación **3-2-1** codifica que una relación Directa "vale" más que una Indirecta, y esta más que una Secundaria, en el ranking de relevancia clínica.

### 4. Badges médicos y permisos (`MedicoBadgeService`)

**Otorgamiento automático** (`EvaluarBadgesAutomaticosAsync`), con umbrales de conteo:

| Badge | Condición |
|---|---|
| `perfil_reclamado` | perfil vinculado a un usuario **y** claim aprobado (`EstatusReclamacion == Reclamado`) |
| `activo_comunidad` | **≥ 5** confirmaciones comunitarias de pacientes |
| `participante_qa` | **≥ 3** respuestas del usuario (excluye IA y eliminadas) |
| `validador_terminos` | **≥ 3** términos del glosario validados (`GlossaryValidations.Approved`) |
| `validador_contenido` | **≥ 3** contenidos validados (estado `Validado`) |
| `validador_respuestas` | **≥ 3** respuestas validadas (estado `Validado`) |

**Nivel del médico** = el máximo `Nivel` entre sus badges obtenidos (0 si no tiene). Los badges tienen un campo `Nivel` en su catálogo.

**Permisos escalonados por nivel** (`TienePermisoAsync`):

| Permiso | Nivel mínimo |
|---|---|
| editar_perfil | 1 |
| ver_comentarios_anonimos, reportar_comentarios | 2 |
| ver_nombre_paciente, responder_comentarios | 3 |
| participar_qa, validar_respuestas | 4 |
| validar_contenido | 5 |
| crear_contenido | 6 |

Cada otorgar/revocar/marcar-en-revisión queda registrado en `MedicosBadgeHistorial` con evento, actor, motivo y fecha.

## Parámetros y umbrales (valores reales)

| Parámetro | Valor | Dónde |
|---|---|---|
| Niveles de relación | Directa 1 / Indirecta 2 / Secundaria 3 | `MedicalRelationType.cs:8` |
| Voto de NINA | +1 al nivel sugerido | `GlossaryService:420`,`:740` |
| Ponderación de score | 3 / 2 / 1 + userCount | `GlossaryService:768` |
| TTL caché del Top | 10 min | `GlossaryService:783` |
| Umbral activo_comunidad | ≥ 5 | `MedicoBadgeService:179` |
| Umbrales validador/participante | ≥ 3 | `MedicoBadgeService:192`,`:199`,`:205`,`:211` |
| Permisos por nivel | 1–6 | `MedicoBadgeService:219` |

## Dónde vive

- Consenso y conteos: `eiibd26/Services/Glossary/GlossaryService.cs` — `GetValidationCountsAsync` en `:346` (voto NINA `:409`–`:424`, top-consensus `:431`–`:438`); ranking `GetTopTermsByQualityAsync` en `:615` (score `:768`).
- Enum de niveles: `eiibd26/Models/Glossary/MedicalRelationType.cs:6`.
- Badges: `eiibd26/Services/Medico/MedicoBadgeService.cs` — otorgamiento automático `:165`, nivel `:79`, permisos `:216`.

## Cómo explicarlo en una presentación

Para cada término médico juntamos dos tipos de aval de los profesionales. Uno es un simple "sí, la definición es correcta" — contamos cuántos médicos lo firman. El otro es más fino: cada médico vota qué tan relacionado está el término con la EII (directo, indirecto o secundario), y el sistema declara ganador al nivel más votado. La IA de la plataforma también emite su voto, que cuenta como uno más.

Para destacar los términos más valiosos, sumamos puntos: una relación directa vale 3, una indirecta 2, una secundaria 1, y le sumamos cuánta gente real vive con ese síntoma o toma ese tratamiento. Así, arriba quedan los términos clínicamente más relevantes y más usados.

Y a los médicos les reconocemos su participación con insignias que se ganan solas al cruzar umbrales (5 confirmaciones, 3 validaciones…), y cada insignia desbloquea permisos: ver comentarios, responder pacientes, crear contenido. Es una escalera de confianza.

## Limitaciones y supuestos

- El consenso es un **conteo de votos (moda)**, no una medida de acuerdo estadístico: 2 vs 1 y 200 vs 199 se ven igual de "ganadores".
- El voto de NINA pesa igual que el de un médico humano (+1), lo que en términos con pocos votos puede inclinar el consenso.
- Los umbrales de badges (3 y 5) son de negocio, no calibrados; cruzar el umbral es binario e irreversible salvo revocación manual.
- La ponderación 3-2-1 es una elección de diseño, no derivada de datos.
- El ranking exige `RelacionEII = true` y usuarios vinculados: términos válidos pero sin uso no aparecen en el Top.
