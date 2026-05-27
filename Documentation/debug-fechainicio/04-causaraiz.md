# Causa raíz: 400 Bad Request en handler EditarFechaInicio

## Problema
`POST /Identity/Usuario/UsuarioCondiciones?handler=EditarFechaInicio` y su equivalente en UsuarioSintomas devolvían **400 Bad Request** al guardar la fecha desde el formulario inline.

## Causa raíz
**Antiforgery token duplicado en el cuerpo POST.**

### Cadena de eventos
1. El `<form asp-page-handler="EditarFechaInicio">` renderiza automáticamente un `<input type="hidden" name="__RequestVerificationToken" value="TOKEN" />` dentro del `<form>`.
2. El JS captura los datos del formulario con `new FormData(form)`, que **ya incluye** el token.
3. A continuación llama a `addAntiforgeryToken(formData)` que hace `formData.append('__RequestVerificationToken', TOKEN)`.
4. El body POST queda con **dos** entradas del mismo nombre:
   ```
   __RequestVerificationToken=TOKEN&sintId=5&nuevaFechaInicio=2024-01-01&__RequestVerificationToken=TOKEN
   ```
5. ASP.NET Core lee `IFormCollection["__RequestVerificationToken"]` → `StringValues { "TOKEN", "TOKEN" }`.
6. `StringValues.ToString()` concatena con `, ` → `"TOKEN, TOKEN"`.
7. El validador de antiforgery recibe una cadena inválida → **falla** → responde **400 Bad Request** sin llegar a ejecutar el handler.

## Archivo y línea afectados

| Archivo | Línea | Función |
|---|---|---|
| UsuarioCondiciones.cshtml | 166 | `addAntiforgeryToken` |
| UsuarioSintomas.cshtml | 276 | `addAntiforgeryToken` |
| UsuarioTratamientos.cshtml | 261 | `addAntiforgeryToken` |
| UsuarioLaboratorios.cshtml | 232 | `addAntiforgeryToken` |
| UsuarioSintomasSeguimiento.cshtml | 562 | línea inline |

## Fix aplicado
Cambiar `formData.append(...)` por `formData.set(...)`:

```js
// ANTES (bug)
if (t) formData.append('__RequestVerificationToken', t);

// DESPUÉS (fix)
if (t) formData.set('__RequestVerificationToken', t);
```

`FormData.set()` reemplaza cualquier valor existente con ese nombre en lugar de agregar uno nuevo, eliminando el duplicado.

## Payload antes del fix
```
__RequestVerificationToken=CfDJ8...abc&condUsuarioId=3&nuevaFechaInicio=2024-06-01&__RequestVerificationToken=CfDJ8...abc
```

## Payload después del fix
```
__RequestVerificationToken=CfDJ8...abc&condUsuarioId=3&nuevaFechaInicio=2024-06-01
```

## Resultado final
Handler `OnPostEditarFechaInicioAsync` ejecuta correctamente → 200 OK con `{ ok: true }`.
