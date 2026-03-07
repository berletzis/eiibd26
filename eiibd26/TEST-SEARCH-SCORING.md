# 🧪 Guía de Prueba - Sistema de Scoring Optimizado

## Resumen Rápido de Cambios

✅ **Sistema de scoring escalado** (10,000 → 1,000 → 100 → 10)
✅ **Ordenamiento por relevancia ANTES de paginar**
✅ **Debug output en consola** para validar scores
✅ **Sin cambios en BD** - puro ordenamiento en memoria

---

## 🔍 Tests Manuales

### Test 1: Búsqueda por Coincidencia Exacta
**Qué hacer:**
1. Ve a `/Contenidos`
2. Busca por: **"Diarrea"** (si existe un contenido con ese título exacto)

**Resultado esperado:**
- ✅ El contenido con título exacto "Diarrea" aparece **PRIMERO** en la página 1
- ✅ En la consola Visual Studio deberías ver:
  ```
  🔍 SEARCH: 'diarrea' | Found 42 results | PAGE 1
    → Score 10000: Diarrea
    → Score 5000: Diarrea aguda
    → Score 2000: Tratamiento de diarrea
  ```

### Test 2: Búsqueda por Prefijo
**Qué hacer:**
1. Busca por: **"Tratamiento"** (palabra que inicia varios títulos)

**Resultado esperado:**
- ✅ Contenidos que **comienzan con** "Tratamiento" aparecen primero (score 5000)
- ✅ Luego aparecen los que **contienen** "tratamiento" (score 2000 o 1000)
- ✅ Luego los que solo lo tienen en contenido (score 100)

### Test 3: Búsqueda en Contenido
**Qué hacer:**
1. Busca por una palabra que **solo existe en el contenido**, no en títulos
2. Ejemplo: **"hiperactividad"** (asumiendo que existe en algún artículo)

**Resultado esperado:**
- ✅ Aparecen resultados, pero con scores bajos (100 si está en contenido corto)
- ✅ Aparecen después de coincidencias en título

### Test 4: Pagination
**Qué hacer:**
1. Busca por: **"síntomas"** (palabra general)
2. Navega a página 2

**Resultado esperado:**
- ✅ Página 1: Contenidos con síntomas en TÍTULO (scores altos)
- ✅ Página 2+: Contenidos con síntomas principalmente en CONTENIDO (scores medios/bajos)

### Test 5: Sin Búsqueda
**Qué hacer:**
1. Ve a `/Contenidos` sin ningún término de búsqueda
2. Verifica que aparezcan por fecha (más recientes primero)

**Resultado esperado:**
- ✅ El ordenamiento es por fecha descendente
- ✅ No hay debug output en consola (no hay búsqueda)

---

## 📊 Validación de Debug Output

### Abre la Consola
1. En Visual Studio → `View` → `Output` (o presiona `Ctrl+Alt+O`)
2. Asegúrate de que "Debug" esté seleccionado en el dropdown

### Realiza una Búsqueda
- Navega a `/Contenidos?SearchQuery=diarrea`

### Verifica el Output
Deberías ver algo como:
```
🔍 SEARCH: 'diarrea' | Found 15 results | PAGE 1
  → Score 10000: Diarrea
  → Score 5000: Diarrea en adultos
  → Score 2000: Síntomas y tratamiento de la diarrea
  → Score 1000: Cómo prevenir diarrea
  → Score 100: Remedios caseros para diarrea
```

---

## 🐛 Posibles Problemas y Soluciones

### ❌ No veo el debug output
**Solución:**
1. Asegúrate de que el proyecto está en modo **Debug** (no Release)
2. Presiona `F5` para ejecutar en debug
3. Abre Output window: `View` → `Output`

### ❌ Los resultados no están ordenados como esperado
**Solución:**
1. Verifica que la búsqueda sea exacta: `?SearchQuery=miPalabra`
2. Comprueba que los contenidos existen en BD
3. Limpia caché del navegador (Ctrl+Shift+Delete)

### ❌ Solo ves 5 contenidos en la lista (de página)
**Verificación:**
- Eso es normal si `PageSize=9` y solo hay 5 que coinciden
- Navega a página 2 para ver si hay más

---

## ✅ Checklist de Validación

- [ ] Búsqueda exacta en título → aparece primero (página 1)
- [ ] Búsqueda por prefijo → aparece antes que substring
- [ ] Búsqueda en contenido corto → aparece antes que contenido largo
- [ ] Pagination funciona correctamente
- [ ] Debug output muestra scores correctos
- [ ] Sin búsqueda → ordenamiento por fecha
- [ ] No hay errores en consola (excepto debug messages)

---

## 🎯 Casos de Uso Específicos

### Caso 1: "Diarrea" (palabra común)
```
✅ Esperado: "Diarrea" (10000) → "Diarrea aguda" (5000) → ... → "Diarrea viral en contenido" (100)
```

### Caso 2: "Asma infantil" (frase)
```
✅ Esperado: Contenidos con "Asma infantil" exacto (10000) → "Asma infantil..."  (5000) → ...
```

### Caso 3: "Dolor" (palabra muy común)
```
✅ Esperado: "Dolor" exacto → "Dolor de cabeza" → "Dolor muscular" → ... → artículos que mencionan "dolor"
```

---

## 📝 Próximos Pasos (Opcional)

Si todo funciona correctamente:

1. **Performance**: Monitorea el tiempo de respuesta (Debug Tools → Performance)
2. **Analytics**: Registra qué búsquedas son más comunes
3. **Mejoras Futuras**: Implementar búsqueda booleana (+término -exclusión)

---

**Nota:** Si encuentras algún problema, verifica que:
- La compilación fue exitosa (`Run Build`)
- No hay excepciones en Output
- La caché del navegador está limpia
