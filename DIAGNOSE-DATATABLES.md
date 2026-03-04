# 🩺 DIAGNÓSTICO DATATABLES ERROR - PASO A PASO

## 📍 ESTÁS AQUÍ:
```
URL: https://localhost:7002/Identity/Admin/Usuarios/Index
Error: DataTables warning: table id=usersGrid - Ajax error
```

---

## 🔧 PASO 1: Abrir DevTools y revisar Console

### Instrucciones:
1. Presiona **F12** (o Click derecho → Inspeccionar)
2. Ve a la pestaña **Console**
3. Busca mensajes que empiecen con:
   - ❌ `DataTables AJAX Error:`
   - ❌ `Error cargando estadísticas:`
   - ❌ `Error cargando condiciones:`
   - ❌ `Error cargando países:`

### ¿Qué mensajes ves?

**Ejemplo de lo que deberías ver:**
```
DataTables AJAX Error:
Status: 401 (o 403, 404, 500)
Error: Unauthorized
Thrown: ...
Response: [texto del error del servidor]
```

---

## 🔧 PASO 2: Revisar pestaña Network (Red)

### Instrucciones:
1. En DevTools → Pestaña **Network**
2. Filtra por **XHR** o **Fetch**
3. Recarga la página (F5)
4. Busca estas 4 llamadas:

#### Llamada 1: **Estadisticas**
- ¿Aparece en la lista? ⬜ Sí ⬜ No
- Status Code: _____ (ej: 200, 401, 403, 404, 500)
- Si es error, click en ella → Tab **Response** → ¿Qué dice?

#### Llamada 2: **CondicionesPadre**
- ¿Aparece en la lista? ⬜ Sí ⬜ No
- Status Code: _____
- Si es error, ¿Qué dice Response?

#### Llamada 3: **Paises**
- ¿Aparece en la lista? ⬜ Sí ⬜ No
- Status Code: _____
- Si es error, ¿Qué dice Response?

#### Llamada 4: **GridData** (LA MÁS IMPORTANTE)
- ¿Aparece en la lista? ⬜ Sí ⬜ No
- Status Code: _____
- URL completa: _____
- Si es error, ¿Qué dice Response?

---

## 🔧 PASO 3: Verificar autenticación

### ¿Estás autenticado como Administrador?

1. En la página, ¿ves tu nombre de usuario en la esquina superior derecha?
   - ⬜ Sí → Continúa al punto 2
   - ⬜ No → Ve a `/Identity/Account/Login` e inicia sesión

2. Verifica tu rol en la base de datos:
   ```sql
   SELECT u.Email, u.UserName, r.Name as RoleName
   FROM AspNetUsers u
   JOIN AspNetUserRoles ur ON u.Id = ur.UserId
   JOIN AspNetRoles r ON ur.RoleId = r.Id
   WHERE u.Email = 'tu-email@ejemplo.com'  -- Reemplaza con tu email
   ```

   - ¿Aparece el rol "Administrador"? ⬜ Sí ⬜ No

---

## 📊 INTERPRETACIÓN DE STATUS CODES:

### ✅ **200 OK** → Funciona correctamente

### ❌ **401 Unauthorized**
**Significa:** No estás autenticado

**Solución:**
1. Cierra sesión: `/Identity/Account/Logout`
2. Inicia sesión de nuevo: `/Identity/Account/Login`
3. Limpia cookies: Ctrl+Shift+Del → Cookies

### ❌ **403 Forbidden**
**Significa:** Estás autenticado pero no tienes el rol "Administrador"

**Solución:**
1. Verifica tu rol en la BD (ver query arriba)
2. Si no tienes el rol, ejecútalo en SQL:
   ```sql
   -- 1. Obtener tu UserId
   SELECT Id, Email FROM AspNetUsers WHERE Email = 'tu-email@ejemplo.com'
   
   -- 2. Obtener RoleId de Administrador
   SELECT Id, Name FROM AspNetRoles WHERE Name = 'Administrador'
   
   -- 3. Asignar rol (reemplaza los GUIDs)
   INSERT INTO AspNetUserRoles (UserId, RoleId)
   VALUES ('tu-user-guid', 'rol-administrador-guid')
   ```

### ❌ **404 Not Found**
**Significa:** La URL del handler no se está construyendo correctamente

**Solución A: Verificar que el método existe**
Abre: `Areas\Identity\Pages\Admin\Usuarios\Index.cshtml.cs`
Busca: `public async Task<IActionResult> OnGetGridDataAsync()`
- ¿Existe? ⬜ Sí ⬜ No

**Solución B: Usar URL absoluta en JavaScript**
Edita `Index.cshtml`, busca la línea:
```javascript
url: '@Url.Page(null, "GridData")',
```

Reemplázala por:
```javascript
url: '/Identity/Admin/Usuarios/Index?handler=GridData',
```

### ❌ **500 Internal Server Error**
**Significa:** Error en el código del servidor (excepción)

**Solución: Ver logs en Visual Studio**
1. En Visual Studio → Ventana **Output**
2. En el dropdown selecciona: **Debug** o **Web Server**
3. Busca el stack trace del error
4. Copia el error completo y compártelo

---

## 🚨 ERRORES ESPECÍFICOS COMUNES:

### Error: "No route matches the supplied values"
**Causa:** El Razor Page handler no se encuentra

**Solución:**
Verifica en `Index.cshtml.cs` que el método se llama exactamente:
```csharp
[IgnoreAntiforgeryToken]
[Authorize(Roles = "Administrador")]
public async Task<IActionResult> OnGetGridDataAsync()
```

Nota: 
- ✅ `OnGetGridDataAsync` (correcto)
- ❌ `OnGetGridData` (incorrecto - falta Async)
- ❌ `OnPostGridDataAsync` (incorrecto - es Post no Get)

### Error: "InvalidOperationException: The partial view..."
**Causa:** Problema con las vistas Razor

**Solución:**
Limpia y reconstruye:
```bash
dotnet clean
dotnet build
```

### Error: "SqlException: Invalid column name..."
**Causa:** Problema con la base de datos

**Solución:**
Verifica que las tablas existen:
```sql
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('AspNetUsers', 'Perfil', 'condicionUsuario', 'condiciones')
```

---

## ✅ CHECKLIST DE VERIFICACIÓN:

Marca lo que ya verificaste:

- [ ] Abrí DevTools → Console
- [ ] Vi el mensaje "DataTables AJAX Error:" en la consola
- [ ] Anoté el Status Code que aparece
- [ ] Revisé Network → XHR y vi las 4 llamadas
- [ ] Verifiqué que estoy autenticado (veo mi nombre)
- [ ] Verifiqué mi rol en la BD (tengo "Administrador")
- [ ] El método `OnGetGridDataAsync()` existe en el archivo .cs
- [ ] Revisé los logs en Visual Studio → Output

---

## 📝 SIGUIENTE PASO:

**Una vez que tengas esta información, compártela:**

1. **Status Code** de la llamada GridData: _____
2. **Mensaje completo** de la consola (Console tab)
3. **Respuesta del servidor** (Network → GridData → Response tab)
4. **Stack trace** (si es 500) desde Visual Studio Output

Con esa información podremos identificar exactamente qué está fallando.
