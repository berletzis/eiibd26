# ANÁLISIS — MedicalLink duplicado → definición no determinista en el glosario

**Fecha:** 06 AGO 2026
**Origen:** hallazgo del caso 5 (verificación del corte de validaciones). `temblor` tiene 2 filas en `GlossaryTermMedicalLink` (SintomaId 57 y 62).
**Estado:** solo análisis. NO se toca nada sin autorización (regla: no modificar queries/esquema/datos sin visto bueno).

## Causa raíz
- **Modelo:** `GlossaryTerm.MedicalLink` es navegación de **referencia** (`GlossaryTermMedicalLink?`), y `GlossaryTermMedicalLink` tiene `GlossaryTermId` + navegación inversa. Sin config fluent explícita, **EF la infiere 1:1**.
- **Esquema:** `GlossaryTermMedicalLink` **no tiene índice único** sobre `GlossaryTermId` (la FK a síntomas/tratamientos tampoco existe, por diseño desacoplado — "adapter"). Nada impide 2+ filas por término.
- **Lectura** (`GlossaryService.GetTermBySlugAsync`, L122–134): proyecta `gt.MedicalLink.SintomaId` / `.TratamientoId` y hace `FirstOrDefaultAsync()`. `gt.MedicalLink` → JOIN/subconsulta sobre la tabla; con 2 filas y **sin `ORDER BY`**, se toma una **arbitraria** (orden que devuelva el motor). El adapter (L179–187) resuelve la definición del `SintomaId`/`TratamientoId` ganador.

**Consecuencia:** para un término con links duplicados a **targets distintos** (temblor → 57 y 62), qué definición se muestra es **no determinista** y puede cambiar entre ejecuciones/planes. Afecta también `PreguntasRelacionadas`, `ExperienciasComunidad` y `RelatedUsersCount`, que cuelgan del mismo `SintomaId`/`TratamientoId` elegido (L193–206).

> Matiz EF: si el proveedor traduce cada columna como subconsulta independiente, `SintomaId` y `TratamientoId` podrían salir de filas distintas. En el patrón adapter cada fila trae solo uno, así que el riesgo práctico es "gana un target u otro"; el código chequea `SintomaId` primero (L179).

## Diagnóstico (solo lectura) — medir alcance
Ver los 3 queries en el mensaje / abajo. Interesa: (1) cuántos términos duplican, (2) si duplican al **mismo** target (duplicado exacto, fácil de depurar) o a **distintos** (como temblor 57/62, requiere decisión de cuál es el correcto), (3) links huérfanos.

```sql
SELECT COUNT(*) AS TerminosDuplicados
FROM (SELECT GlossaryTermId FROM GlossaryTermMedicalLink
      GROUP BY GlossaryTermId HAVING COUNT(*) > 1) x;

SELECT gt.Id AS TermId, gt.Nombre, gt.Slug, gt.TipoTermino,
       l.Id AS LinkId, l.SintomaId, l.TratamientoId
FROM GlossaryTermMedicalLink l
JOIN GlossaryTerm gt ON gt.Id = l.GlossaryTermId   -- tabla = GlossaryTerm (singular), no el DbSet GlossaryTerms
WHERE l.GlossaryTermId IN (
    SELECT GlossaryTermId FROM GlossaryTermMedicalLink
    GROUP BY GlossaryTermId HAVING COUNT(*) > 1)
ORDER BY gt.Id, l.Id;

SELECT COUNT(*) AS LinksSinTarget
FROM GlossaryTermMedicalLink
WHERE SintomaId IS NULL AND TratamientoId IS NULL;
```

## Hallazgos (diagnóstico corrido 06 AGO, BD 64.202.187.218)
- **24 términos** con link duplicado; cada uno tiene exactamente **2 links**; **0 links sin target**.
- **1 es síntoma:** Temblor (Id 56) → SintomaId 57 y 62 (targets distintos).
- **23 son tratamientos.** Dos familias:
  - **IDs consecutivos** (≈ duplicado exacto del tratamiento de fondo, definiciones casi idénticas): Aceite de linaza 3371/3372, Arándano 3590/3591, Arginina 3601/3602, Glucomanano 4492/4493, Huperzina A 4611/4612, Valeriana 5986/5987, Vitamina C… 6052/6053. → quedarse con cualquiera.
  - **IDs divergentes** (dos registros distintos por la misma palabra, revisar target): Abrazos 9597/2378, Actitud Positiva 2344/3, Bañera 6357/6404, Canto 2341/9740, Dieta vegetariana 8033/8042, Duchas calientes 2318/2234, Ejercicio 2565/7776, Levantamiento de pesas 7996/7800, Manejo de caso 7626/7668, Melaza 4942/8329, Modificación de la dieta 8016/8198, Papaya 5252/8341, Psicoterapia 7613/2564, Tapones 6399/6645, Terapia Física 6147/2561, Ventilador 9909/6336.
- **Regla de keeper propuesta** (query de ranking): `Eliminado ASC, TieneDescripcion DESC, RelacionEII DESC, ValidadoHumano DESC, Usuarios DESC, LinkId ASC`. `RankKeep=1` se queda; el resto se borra. Los divergentes se revisan con Berletzis antes de ejecutar.

## Opciones de arreglo (por evaluar tras ver los números)
1. **Lectura determinista (curita, rápido).** Resolver `MedicalLink` con orden explícito (p. ej. el `Id` más bajo, o el más reciente) para que **siempre** salga la misma definición. Quita el no-determinismo, pero **no garantiza que sea la correcta** si el target elegido no es el clínicamente adecuado. Toca la **consulta** del servicio → requiere autorización.
2. **Depurar datos (arreglo real).** Decidir por término cuál link es el correcto y borrar los sobrantes. Para duplicados **exactos** (mismo target) es trivial. Para targets **distintos** (temblor 57 vs 62) es **decisión clínica/de producto**: ¿cuál síntoma corresponde? Toca datos de prod → requiere autorización.
3. **Prevenir recurrencia (esquema).** Tras depurar, índice **UNIQUE sobre `GlossaryTermId`** en `GlossaryTermMedicalLink` para que no vuelva a pasar. (Y opcional: alinear la config EF a 1:1 explícita.) SQL-directo, sin migración; requiere autorización y depurar primero.

**Recomendación:** medir con el diagnóstico → si son pocos y en su mayoría duplicados exactos, hacer (2) depurar + (3) unique index de una; los de target distinto se resuelven caso por caso contigo. La (1) lectura determinista es buena red de seguridad para meter junto con (3), por si algún día se cuela otro duplicado antes del constraint.

## Fuera de alcance / cuidado
- No tocar el patrón adapter (desacople síntomas/tratamientos a propósito).
- Nada de esto se ejecuta sin tu autorización explícita (queries, datos y esquema).
- Independiente del despliegue pendiente del 6 AGO; es un tema de datos/consulta, no de las vistas ya trabajadas.
