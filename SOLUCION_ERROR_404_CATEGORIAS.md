# Solución al Error 404 en Categorías

## Problema Identificado

El error 404 ocurre porque:

1. El archivo `porCategoria.cshtml` está ubicado en `Pages/Contenidos/`
2. Razor Pages genera automáticamente rutas basadas en la estructura de carpetas
3. Esto crea conflictos entre la ruta declarada `/{categorySegment}` y la ruta automática `/Contenidos/categoria/{categorySegment}`

## Soluciones Posibles

### Solución 1: Configurar Rutas en Program.cs (RECOMENDADA)

Agregar una configuración de ruta personalizada en `Program.cs` antes de `app.MapRazorPages()`:

```csharp
// En Program.cs, antes de app.MapRazorPages()
app.MapPageRoute("categoria", "/{categorySegment?}", "/Contenidos/porCategoria");
```

### Solución 2: Mover el archivo a la raíz de Pages

Mover el archivo de:
- `Pages/Contenidos/porCategoria.cshtml`

A:
- `Pages/categoria.cshtml`

Y actualizar el namespace en el archivo `.cs`:
```csharp
namespace eiibd26.Pages
{
    public class CategoriaModel : PageModel
    {
        // ... código existente
    }
}
```

### Solución 3: Actualizar todos los enlaces que apuntan incorrectamente

Si hay enlaces que están generando URLs como `/Contenidos/categoria/{slug}`, deben actualizarse para usar solo `/{slug}`.

## Cambios Ya Aplicados

✅ Se agregó comparación case-insensitive para los slugs de categorías
- Ahora `alimentacion-y-Nutricion` será encontrado aunque en la BD esté como `alimentacion-y-nutricion`

✅ Archivos actualizados:
- `eiibd26\Pages\Contenidos\porCategoria.cshtml.cs`
- `eiibd26\Pages\Home\BlogMore.cshtml.cs`

## Próximos Pasos

1. Ejecutar el script SQL `AddEsPrincipalColumn.sql` si aún no se ha ejecutado
2. Implementar la **Solución 1** (configurar ruta en Program.cs)
3. Reiniciar la aplicación
4. Probar accediendo a una categoría

## Verificación de la Base de Datos

Ejecutar esta consulta para ver los slugs existentes:

```sql
SELECT Sequence, Nombre, CategoriaSlug, Borrado, CategoriaPadre
FROM contenidosCategorias
WHERE Borrado = 0
ORDER BY Nombre;
```

Verificar que:
- Los slugs estén en minúsculas y sin espacios
- No haya registros marcados como `Borrado = 1` que deberían estar activos
