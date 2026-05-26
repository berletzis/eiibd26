# 01 - Riesgo Clínico

## ARCH-003: Contaminación IA con contenido no-EII

### Problema
`NinaModelRouterService.GenerarRespuestaSimple()` contenía respuestas hardcodeadas sobre VIH, HIV, SIDA, transmisión y ETS. La plataforma es exclusiva de Enfermedades Inflamatorias Intestinales.

### Causa raíz
El router de IA fue desarrollado con contenido médico genérico en lugar de conocimiento específico de EII. No existía ninguna barrera de dominio.

### Solución
- Creado `IBDKnowledgeTemplates.cs`: repositorio de conocimiento EII-only con templates para CUCI, Crohn, biológicos, brotes, colonoscopia, medicamentos, nutrición y seguimiento.
- `GenerarRespuestaSimple()` ahora delega a `IBDKnowledgeTemplates.TryResolve()`.
- Si no hay template EII aplicable, retorna `string.Empty` y escala a Haiku (no inventa contenido).
- Prompt del sistema actualizado a dominio EII exclusivo.
- `HighRiskKeywords` y `ClassifyQuestionAsync` usan terminología EII.

### Impacto
- Nina ya no puede producir información clínica sobre enfermedades fuera del dominio EII.
- Las preguntas fuera de dominio son redirigidas apropiadamente.

### Archivos modificados
- `eiibd26/Services/AI/NinaModelRouterService.cs`
- `eiibd26/Services/AI/IBDKnowledgeTemplates.cs` (creado)
