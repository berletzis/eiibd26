# REQ — Rúbrica v2: pseudoterapias→Basura + afinar bienestar (autocuidado→Válido)

Extiende `REQ-nina-limpieza-tratamientos-triage.md`. **Scope:** `SintomasTratamientosAiService` (prompt de triage) + un reset SQL de la rama de prueba + un retry. Solo `eiibd26.Web`.
**Ejecución (AUTORIZADO por Berletzis, sin pedir permisos):** aplica los cambios de rúbrica, el retry y el reset; build + `publish`; re-corre la sonda de SOLO LECTURA sobre la rama 2. **GATE intacto:** NO desactivar nada (sigue dry-run); "Aplicar Basura" lo dispara Berletzis tras revisar.

Decisiones de producto (Berletzis, 07 AGO), tras el dry-run de "Cambios en el Estilo de Vida" (17 válidos / 12 basura / 70 dudosos):

## 1. Pseudoterapias → BASURA
En `BuildTriageSystemPrompt`, agregar a **BASURA**: prácticas **sin base científica ni mecanismo documentado**.
- Ejemplos: ACMOS, "Códigos curativos", Método Sedona, Técnica Perrin, filtros de armónicos/CEM (STETZERiZER), sanación energética, biodescodificación.
- Señales: lenguaje pseudocientífico vago (armonía, energía, sinergia, frecuencias, "códigos") sin evidencia ni mecanismo plausible.
- **Deslinde (NO confundir):** terapias complementarias con uso reconocido para confort o alivio sintomático (baño de Epsom, calor/frío, ropa de compresión) **NO** son pseudoterapia → siguen Válido/Dudoso. La línea: pseudociencia = sin mecanismo plausible **ni** uso reconocido.
- Es una política editorial fuerte para un sitio médico → el dry-run→revisar→aplicar la protege; Berletzis revisa este sub-bucket antes de aplicar.

## 2. Afinar el bienestar reportado (partir el 70% Dudoso)
- **Autocuidado plausible / manejo cotidiano de salud → VÁLIDO** (relación EII baja): descanso, dormir, siestas, duchas/baños calientes, manejo del estrés, evitar alcohol, restricción/adecuación de fluidos, actividad física según tolerado, rutina/higiene intestinal, evitar el calor.
- **Ocio / eventos de vida sin nexo claro con salud → DUDOSO** (revisión humana): viajes ("Viaje a Nueva Zelanda"), espectáculos/figuras ("Ver a Joel Osteen"), peregrinaje, hobbies sin relación, aprendizaje reportado ("Estudiar idiomas", "Tejido de punto").
- **Firme:** nada de ocio/aprendizaje/bienestar reportado por pacientes va a Basura. Solo van a Basura las pseudoterapias (punto 1), los no-tratamientos claros (recordatorios/alarmas, productos/alimentos/cosméticos, códigos/ensayos, condiciones médicas mal metidas) y ruido.

## 3. Retry en la llamada a la IA
El error "An error occurred while sending the request" reapareció (2ª vez) y se auto-recupera (queda sin sellar → se reintenta al siguiente lote). Agregar un **retry corto** (1–2 intentos con backoff) en la llamada de clasificación para no depender de una segunda pasada.

## 4. Reset + re-run de la rama de prueba
Los 100 de "Cambios en el Estilo de Vida" (id 2) ya están sellados con la rúbrica vieja. Como **nada se desactivó** (dry-run), resetear su estado para re-clasificar con v2:
```sql
UPDATE dbo.tratamientos
SET RevisionLimpiezaEstado = NULL, RevisionLimpiezaConfianza = NULL,
    RevisionLimpiezaMotivo = NULL, RevisionLimpiezaFecha = NULL
WHERE idPadre = 2 AND RevisionLimpiezaEstado IS NOT NULL AND Eliminado = 0;
```
(No toca `Eliminado` — nadie fue desactivado.) Luego re-correr la sonda read-only sobre la rama 2 (Berletzis re-corre el dry-run por el panel para ver el split nuevo).

## Verificación (sonda read-only sobre rama 2)
- Pseudoterapias (ACMOS, Códigos curativos, Método Sedona, Técnica Perrin, STETZERiZER) → **Basura**.
- Autocuidado (Descanso, Dormir, Duchas calientes, Manejo del estrés, Evitar alcohol) → **Válido**.
- Ocio/eventos (Viaje a Nueva Zelanda, Ver a Joel Osteen, Estudiar idiomas) → **Dudoso**, nunca Basura.
- Baño de Epsom / ropa de compresión → siguen Válido/Dudoso (no pseudoterapia).
- El bucket Dudoso baja bastante respecto al 70% anterior; Basura sube con las pseudoterapias.
- Build + `publish -c Release` limpios.
