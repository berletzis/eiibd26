# REQ — Cantidad de ingrediente no acepta decimales (0.25 se guarda como 25)

**Scope:** solo `eiibd26.Web` — `Program.cs` (registro de model binders) + una clase nueva del binder. **Cero BD, cero cambios de esquema, reversible.** NO tocar NINA-WorkerService ni Conectar3eros.

## Síntoma
En el editor de platillos (`Areas/Identity/Pages/Admin/Platillos/Detalle.cshtml`), al capturar la **Cantidad** de un ingrediente, un valor decimal como `0.25` (1/4 de taza) se **guarda como `25`**. Cualquier `N/100` termina como `N`.

## Causa raíz (confirmada)
NO es el input ni el modelo:
- El modelo ya es decimal: `PlatPlatilloIngrediente.Cantidad` es `decimal?`.
- El input ya permite decimales: `Detalle.cshtml:253` → `type="number" step="0.001" min="0"`.

Es un **choque entre HTML5 `type=number` y la localización de la app**:
- Un `<input type="number">` **siempre** envía el decimal con **punto** (`0.25`) en su `value` del DOM, sin importar el idioma del navegador (así lo define el estándar).
- La app usa `app.UseRequestLocalization` con `SupportedCultures = { es-MX, es-ES, es }` y `DefaultRequestCulture = es-MX` (`Program.cs:560-567`).
- El **model binding** de ASP.NET Core parsea los números con la **cultura de la request**. Si la request resuelve a **es-ES** o **es** (por el `Accept-Language` del navegador), el **punto es separador de miles** → `0.25` se parsea como `25`. (En es-MX el separador decimal es punto y no se rompe; el bug aparece cuando la cultura cae en es-ES/es.)

## Cambio recomendado (robusto, NO toca la localización de display)
Registrar **model binders invariantes** para `decimal`, `decimal?`, `double`, `double?`, `float`, `float?`, de modo que los números que vienen de formularios (inputs HTML `type=number`, siempre con punto) se parseen con `CultureInfo.InvariantCulture`, independientemente de la cultura de la request.

Sketch:

```csharp
// InvariantDecimalModelBinder.cs (eiibd26.Web)
public sealed class InvariantDecimalModelBinder : IModelBinder
{
    private readonly Type _type;
    public InvariantDecimalModelBinder(Type type) => _type = Nullable.GetUnderlyingType(type) ?? type;

    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var v = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (v == ValueProviderResult.None) return Task.CompletedTask;
        ctx.ModelState.SetModelValue(ctx.ModelName, v);
        var s = v.FirstValue;
        if (string.IsNullOrWhiteSpace(s)) { ctx.Result = ModelBindingResult.Success(null); return Task.CompletedTask; }

        // Los inputs type=number postean con punto; parsear SIEMPRE invariante.
        bool ok = _type == typeof(decimal)
            ? decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                ? Set(ctx, d) : Fail(ctx, s)
            : _type == typeof(double)
            ? double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var db)
                ? Set(ctx, db) : Fail(ctx, s)
            : float.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var f)
                ? Set(ctx, f) : Fail(ctx, s);
        return Task.CompletedTask;
    }
    // Set(...) => ctx.Result = ModelBindingResult.Success(value); return true;
    // Fail(...) => ctx.ModelState.TryAddModelError(ctx.ModelName, "Número inválido"); return false;
}

// InvariantDecimalModelBinderProvider.cs
public sealed class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext c)
    {
        var t = Nullable.GetUnderlyingType(c.Metadata.ModelType) ?? c.Metadata.ModelType;
        return (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            ? new InvariantDecimalModelBinder(c.Metadata.ModelType) : null;
    }
}
```

Registro (Program.cs, donde se configura Razor Pages/MVC):

```csharp
builder.Services.AddRazorPages().AddMvcOptions(o =>
    o.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider()));
```

(Si usan `AddControllersWithViews()`/`AddControllers()` en paralelo, aplicar el mismo `AddMvcOptions` ahí también.)

### Por qué esta opción y no otra
- **Alternativa más simple pero frágil:** dejar solo `es-MX` en `SupportedCultures` (es-MX usa punto decimal). Funciona, pero depende de que ninguna request caiga en es-ES/es y no arregla el problema de raíz si mañana se agrega otra cultura. El binder invariante es el fix correcto y localizado al problema (parseo de formularios), sin afectar cómo se muestran fechas/números en la UI.

## Puntos a verificar además del binder
1. **Hidratación de filas existentes (display):** el JS arma el modelo de filas desde el servidor (`Detalle.cshtml` ~línea 27 `cantidad = r.Cantidad`, y ~línea 324 `q('.inp-cantidad').value = d.cantidad`). Confirmar que esa cantidad se serializa **invariante** (vía `System.Text.Json`/`Json.Serialize`, no `ToString()` con cultura es-ES) para que `0.25` no llegue al JS como `"0,25"` (→ input vacío/NaN). Si ya usa Json.Serialize, no hay nada que hacer.
2. **Round-trip:** capturar `0.25`, guardar, recargar el platillo → debe seguir mostrando `0.25` (no `25`, no vacío).

## Datos ya dañados (IMPORTANTE — requiere decisión, no automatizar)
Los ingredientes capturados antes del fix donde `N/100` se guardó como `N` (p. ej. `1/4` → `25`) quedaron **mal en la BD**. NO auto-corregir con un UPDATE masivo (dividir /100 a ciegas borraría cantidades legítimamente enteras como `25 g`). Se identifican cruzando `Cantidad` contra `TextoOriginal` (que sí conserva "1/4 taza"). Propuesta: listar los platillos afectados y re-capturarlos a mano, o un UPDATE acotado SOLO a los casos claramente detectables — con autorización explícita de Berletzis antes de correr nada.

## Verificación
- Capturar un ingrediente con Cantidad `0.25`, guardar, recargar → `0.25` persiste.
- Capturar `1.5`, `0.75`, `2` → persisten correctos.
- `dotnet build -c Release` limpio; el editor de platillos abre y guarda sin error.
