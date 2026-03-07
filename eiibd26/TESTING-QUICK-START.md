# ⚡ QUICK START - Testing Búsqueda Optimizada

## 🚀 En 2 Minutos

### Paso 1: Compilar
```powershell
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26
dotnet build
```
✅ Sin errores = listo para siguiente paso

### Paso 2: Ejecutar
```powershell
dotnet run
# O en Visual Studio: Presiona F5
```
✅ Espera a que esté escuchando en https://localhost:7002

### Paso 3: Ir a Búsqueda
```
https://localhost:7002/Contenidos
```

### Paso 4: Buscar
Escribe en el buscador (si existe):
- **"Diarrea"** (buscar exacto)
- **"Asma"** (buscar exacto)
- **"Diabetes"** (buscar exacto)

✅ **Esperado:** Aparece en POSICIÓN 1 de la página

### Paso 5: Validar Output
1. En Visual Studio → `View` → `Output` (Ctrl+Alt+O)
2. En el dropdown selecciona: **Debug**
3. Vuelve a hacer búsqueda
4. En Output deberías ver:
```
🔍 SEARCH: 'diarrea' | Found 42 results | PAGE 1
  → Score 10000: Diarrea
  → Score 5000: Diarrea aguda
  → Score 2000: Síntomas de diarrea
  → Score 100: Artículo sobre hidratación en diarrea
```

✅ **Si ves esto:** TODO FUNCIONA CORRECTAMENTE

---

## 🧪 Tests Rápidos (5 min)

### Test 1: Exacto en Título
```
Buscar: "Diarrea"
Esperado: Aparece PRIMERO en página 1
Score: 10000
```

### Test 2: Prefijo en Título
```
Buscar: "Diarrea"
Esperado: "Diarrea aguda" aparece segundo
Score: 5000
```

### Test 3: Límite de Palabra
```
Buscar: "Síntomas"
Esperado: "Síntomas de diarrea" aparece
Score: 2000+
```

### Test 4: Solo en Contenido Largo
```
Buscar: palabra que solo existe en contenido largo
Esperado: Aparece pero con score bajo (100)
```

### Test 5: Sin Resultados
```
Buscar: "XYZ123" (palabra que no existe)
Esperado: "0 encontrados"
```

---

## ❌ Si Algo Falla

### ❌ Build error
```
→ Verificar: ¿Compiló sin errores?
→ Solución: Clean + Build nuevamente
```

### ❌ No veo resultado exacto primero
```
→ Verificar: ¿El contenido existe en BD?
→ Solución: Buscar palabra que SABE que existe
```

### ❌ No veo Debug Output
```
→ Verificar: ¿Estás en Debug mode?
→ Solución: F5 (no Ctrl+F5)
→ Verificar: Output → Debug (no Build)
```

### ❌ Performance lenta
```
→ Normalmente: ~100-200ms
→ Si lenta: revisar BD (índices)
```

---

## 📝 Checklist Final

- [ ] Código compilado sin errores
- [ ] Aplicación ejecutándose
- [ ] Puedo acceder a /Contenidos
- [ ] Busca palabra exacta → aparece primero
- [ ] Debug output muestra Score 10000
- [ ] Sin errores en consola
- [ ] Performance < 200ms

✅ **Si todo esto está OK:** El sistema funciona perfectamente

---

## 🎯 Qué Has Logrado

```
✅ Búsqueda con scoring inteligente
✅ Resultados exactos en página 1 (garantizado)
✅ Mejor experiencia de usuario
✅ Zero ruido (solo título + contenido largo)
✅ Performance optimizado
✅ Documentación completa
```

---

**¿Necesitas ayuda?** Revisa los otros documentos o contacta. 🚀
