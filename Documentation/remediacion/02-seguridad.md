# 02 - Seguridad

## SEC-008: [IgnoreAntiforgeryToken] en datos clínicos

### Problema
`UsuarioLaboratorios.cshtml.cs` y `UsuarioCondiciones.cshtml.cs` tenían `[IgnoreAntiforgeryToken]` a nivel de clase, exponiendo operaciones POST sobre datos clínicos de pacientes (condiciones, laboratorios, fechas de inicio, diagnóstico principal) a ataques CSRF.

### Causa raíz
El atributo fue agregado para evitar errores de token en AJAX, en lugar de implementar correctamente el manejo de antiforgery en cliente.

### Solución
- Eliminado `[IgnoreAntiforgeryToken]` de ambas páginas.
- `UsuarioCondiciones.cshtml` actualizado para incluir `@Html.AntiForgeryToken()` en el formulario y helpers JS `getAntiforgeryToken()` / `addAntiforgeryToken()` en todos los `fetch()` POST.
- Validado que la UI no se rompe: los POST de agregar, editar, toggle y eliminar condiciones incluyen el token correctamente.

### Impacto
- Los endpoints de datos clínicos del paciente requieren token CSRF válido.
- Se elimina el riesgo de que un sitio malicioso ejecute operaciones sobre los datos del paciente.

### Archivos modificados
- `eiibd26/Areas/Identity/Pages/Usuario/UsuarioLaboratorios.cshtml.cs`
- `eiibd26/Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml.cs`
- `eiibd26/Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml`

---

## SEC-010/011: IDOR en datos clínicos (Estado de ánimo, síntomas)

### Problema
`EstadoAnimoUsuarioController.Nuevo()` y `DashboardController.AddSymptom()` aceptaban IDs de FK (condición, síntoma, tratamiento) provistos por el cliente sin verificar que perteneciesen al usuario autenticado. Un atacante podía asociar registros clínicos a entidades de otro paciente.

### Causa raíz
Ausencia de validación de ownership para FK clínicas en los controladores de escritura.

### Solución
- Creado `ClinicalOwnershipValidator` (servicio reutilizable) con métodos `OwnsCondicionAsync`, `OwnsSintomaAsync`, `OwnsTratamientoAsync`, `OwnsEstadoAnimoAsync` y `ValidateEstadoAnimoRelationsAsync`.
- Registrado como `AddScoped` en `Program.cs`.
- `EstadoAnimoUsuarioController.Nuevo()` valida todas las FK opcionales antes de persistir.
- `DashboardController.AddSymptom()` valida que `sintomaUsuarioId` pertenece al usuario antes de insertar.

### Impacto
- El cliente no puede asignar ownership a entidades ajenas.
- Patrón reutilizable para futuros endpoints clínicos.

### Archivos modificados
- `eiibd26/Services/ClinicalOwnershipValidator.cs` (creado)
- `eiibd26/Program.cs`
- `eiibd26/Controllers/EstadoAnimoUsuarioController.cs`
- `eiibd26/Controllers/DashboardController.cs`
