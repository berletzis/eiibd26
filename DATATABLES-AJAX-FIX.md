# 🔧 DataTables AJAX Error - Soluciones Aplicadas

## ✅ Cambios Realizados:

### 1. Agregado `[Authorize(Roles = "Administrador")]` explícitamente a todos los handlers AJAX

Según las instrucciones del proyecto, aunque la clase tenga el atributo `[Authorize]`, los métodos handler AJAX necesitan tenerlo explícitamente para que funcionen correctamente.

**Archivos modificados:**
- `eiibd26\Areas\Identity\Pages\Admin\Usuarios\Index.cshtml.cs`

**Métodos actualizados:**
- `OnGetGridDataAsync()` - Handler principal del DataTable
- `OnGetCondicionesPadreAsync()` - Handler para cargar filtro de condiciones
- `OnGetPaisesAsync()` - Handler para cargar filtro de países
- `OnGetEstadisticasAsync()` - Handler para cargar estadísticas

### 2. Agregado manejo de errores detallado en el JavaScript

**Archivos modificados:**
- `eiibd26\Areas\Identity\Pages\Admin\Usuarios\Index.cshtml`

**Mejoras:**
- ✅ Error handler en llamada AJAX de DataTables con logs detallados
- ✅ Error handlers en llamadas de estadísticas con logs en consola
- ✅ Error handlers en llamadas de condiciones y países
- ✅ Agregado `dataType: 'json'` para claridad

**Ahora verás en la consola del navegador:**
- Detalles del status HTTP (401, 403, 404, 500, etc.)
- Texto de la respuesta del servidor
- Mensaje de error específico

---

## 🔍 Cómo Verificar si Funciona:

### Paso 1: Detener y Reiniciar la Aplicación
```bash
# En Visual Studio: Detener (Shift+F5) y luego Iniciar (F5)
# O en terminal:
dotnet build
dotnet run
```

### Paso 2: Abrir DevTools
1. Presiona **F12** para abrir las herramientas de desarrollador
2. Ve a la pestaña **Network** (Red)
3. Filtra por **XHR** o **Fetch**

### Paso 3: Navegar a la Página de Usuarios
```
https://localhost:7002/Identity/Admin/Usuarios/Index
```

### Paso 4: Verificar las Llamadas AJAX
Deberías ver las siguientes llamadas en la pestaña Network:

1. **Estadisticas** (verde ✅):
   ```
   GET /Identity/Admin/Usuarios/Index?handler=Estadisticas
   Status: 200 OK
   Response: {"total":X,"perfilesCompletos":Y,...}
   ```

2. **CondicionesPadre** (verde ✅):
   ```
   GET /Identity/Admin/Usuarios/Index?handler=CondicionesPadre
   Status: 200 OK
   Response: [{"id":1,"nombre":"EII"},...]
   ```

3. **Paises** (verde ✅):
   ```
   GET /Identity/Admin/Usuarios/Index?handler=Paises
   Status: 200 OK
   Response: ["Argentina","Chile","México",...]
   ```

4. **GridData** (verde ✅):
   ```
   GET /Identity/Admin/Usuarios/Index?handler=GridData&draw=1&start=0&length=10...
   Status: 200 OK
   Response: {"draw":1,"recordsTotal":X,"recordsFiltered":Y,"data":[...]}
   ```

---

## ❌ Si AÚN NO Funciona:

### Error 1: "401 Unauthorized"
**Causa:** El usuario no está autenticado o no tiene el rol "Administrador"

**Solución:**
1. Verifica que has iniciado sesión
2. Verifica que tu usuario tiene el rol "Administrador"
3. Consulta en la base de datos:
   ```sql
   -- Ver roles del usuario actual
   SELECT u.Email, r.Name as RoleName
   FROM AspNetUsers u
   JOIN AspNetUserRoles ur ON u.Id = ur.UserId
   JOIN AspNetRoles r ON ur.RoleId = r.Id
   WHERE u.Email = 'tu-email@ejemplo.com'
   ```

### Error 2: "403 Forbidden"
**Causa:** Problema con el token antiforgery

**Solución ya aplicada:**
- ✅ `[IgnoreAntiforgeryToken]` agregado a todos los handlers GET

### Error 3: "404 Not Found"
**Causa:** La URL del handler no se está construyendo correctamente

**Solución alternativa:** Reemplazar en `Index.cshtml`:
```javascript
// ANTES:
ajax: {
    url: '@Url.Page(null, "GridData")',
    type: 'GET',
    ...
}

// DESPUÉS:
ajax: {
    url: '/Identity/Admin/Usuarios/Index?handler=GridData',
    type: 'GET',
    ...
}
```

### Error 4: "500 Internal Server Error"
**Causa:** Excepción en el servidor

**Solución:**
1. Ve a la pestaña **Console** en DevTools
2. Busca mensajes de error
3. O revisa los logs en Visual Studio (Output → Debug)

**Verifica la base de datos:**
```sql
-- Verificar que las tablas existen
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('AspNetUsers', 'Perfil', 'condicionUsuario', 'condiciones', 'EstadoAnimoUsuario')
```

---

## 🐛 Debugging Avanzado:

### Ver el SQL generado por Entity Framework
Agrega logging en `Program.cs`:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.EnableSensitiveDataLogging(); // ⚠️ Solo en desarrollo
    options.LogTo(Console.WriteLine, LogLevel.Information); // Ver queries
});
```

### Ver headers de las peticiones AJAX
En DevTools → Network → Click en una petición → Headers:
```
Request URL: https://localhost:7002/Identity/Admin/Usuarios/Index?handler=GridData&draw=1...
Request Method: GET
Status Code: 200 OK (o el error que tengas)

Request Headers:
Accept: application/json, text/javascript, */*; q=0.01
Cookie: .AspNetCore.Identity.Application=...
X-Requested-With: XMLHttpRequest
```

---

## 📝 Checklist Final:

- [ ] Reinicié la aplicación (Stop + Start)
- [ ] Limpié caché del navegador (Ctrl+Shift+Del)
- [ ] Abrí DevTools → Network antes de cargar la página
- [ ] Confirmé que estoy autenticado como Administrador
- [ ] Las 4 llamadas AJAX tienen Status 200 OK
- [ ] El DataTable muestra los datos correctamente

---

## 💡 Notas Adicionales:

### Diferencia entre GET y POST en Razor Pages:

- **GET handlers**: `OnGetGridDataAsync()` - Para obtener datos (nuestro caso)
- **POST handlers**: `OnPostGridDataAsync()` - Para enviar datos

### ¿Por qué usar GET para DataTables?
- Los parámetros de DataTables (draw, start, length, search, etc.) se envían en la query string
- GET es más apropiado para operaciones de lectura
- GET permite cachear las respuestas
- No requiere token antiforgery (simplifica la configuración)

### Alternativa POST (si GET no funciona):
1. En `Index.cshtml.cs`, cambiar el método:
   ```csharp
   [IgnoreAntiforgeryToken]
   [Authorize(Roles = "Administrador")]
   public async Task<IActionResult> OnPostGridDataAsync()
   ```

2. En `Index.cshtml`, cambiar el AJAX:
   ```javascript
   ajax: {
       url: '@Url.Page(null, "GridData")',
       type: 'POST',
       headers: {
           'RequestVerificationToken': $('meta[name="csrf-token"]').attr('content')
       },
       ...
   }
   ```

---

## ✅ Estado Actual:

✅ `[IgnoreAntiforgeryToken]` agregado a todos los handlers GET
✅ `[Authorize(Roles = "Administrador")]` agregado explícitamente
✅ Handlers usan GET (apropiado para DataTables)
✅ Compilación exitosa

**Siguiente paso:** Reiniciar la aplicación y verificar en DevTools → Network
