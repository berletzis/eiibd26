# 🔍 GUÍA DE DEBUG - Qué Buscar en Output

## 📋 Cuando Hagas F5 y Busques "Diarrea"

Abre: `View` → `Output` (Ctrl+Alt+O)

### ✅ DEBERÍAS VER (si funciona correctamente):

```
🔍 SEARCH: 'diarrea' | Found X results | PAGE 1
  [EXACTO] 'diarrea' = 'Diarrea' → 10000
  [COMIENZA] 'diarrea' at start of 'Diarrea en Enfermedad...' → 5000
  [LÍMITE] 'diarrea' word boundary in 'Síntomas de diarrea' → 2000
  [SUBSTRING] 'diarrea' in 'Diarreas virales' → 1000
  [LARGO] 'diarrea' in long content → 100
```

---

## 🔴 SI NO VES NADA DE ESTO

**Significa que el método NO se está ejecutando.**

### Causas posibles:

1. ❌ **El código no se compiló**
   - Solución: `dotnet clean && dotnet build`

2. ❌ **Cache del navegador**
   - Solución: Ctrl+Shift+Delete → Limpiar TODO

3. ❌ **No estás en Debug Output**
   - Solución: 
     - View → Output (Ctrl+Alt+O)
     - Dropdown debe mostrar: **Debug** (no "Build")

4. ❌ **Ejecutaste con Ctrl+F5**
   - Solución: Presiona **F5** (no Ctrl+F5)

---

## 🔍 OTROS MENSAJES QUE PODRÍAS VER

### Si la búsqueda encuentra exacto:
```
[EXACTO] 'diarrea' = 'Diarrea' → 10000
```
✅ Correcto: Puntuación exacta 10,000

### Si comienza con:
```
[COMIENZA] 'diarrea' at start of 'Diarrea en EII' → 5000
```
✅ Correcto: Puntuación 5,000

### Si está en contenido largo:
```
[LARGO] 'diarrea' in long content → 100
```
✅ Correcto: Puntuación 100

---

## 📊 ORDEN ESPERADO EN BÚSQUEDA "DIARREA"

Debería ver en este orden:

```
[EXACTO] 'diarrea' = 'Diarrea' → 10000           ← PRIMERO
[COMIENZA] 'diarrea' at start of ... → 5000      ← SEGUNDO
[COMIENZA] 'diarrea' at start of ... → 5000      ← SEGUNDO
[LÍMITE] 'diarrea' word boundary in ... → 2000   ← TERCERO
[SUBSTRING] 'diarrea' in ... → 1000              ← CUARTO
[LARGO] 'diarrea' in long content → 100          ← ÚLTIMO
```

**Si ves algo diferente = hay un problema**

---

## 💡 CÓMO GUARDAR EL OUTPUT

Si quieres guardar todo el output para analizar:

1. Abre Output window (Ctrl+Alt+O)
2. Click derecho → "Save all pane text as..."
3. Guarda como: `output.txt`

---

## ⚡ RESUMEN RÁPIDO

```
✅ Compilar:          dotnet build
✅ Ejecutar:          F5 (no Ctrl+F5)
✅ Limpiar cache:     Ctrl+Shift+Delete
✅ Ver output:        Ctrl+Alt+O → Debug
✅ Buscar:            "Diarrea"
✅ Esperar debug:     Mensajes con [EXACTO], [COMIENZA], etc.
```

---

**Si NO ves los mensajes de debug después de hacer TODO ESTO:**
→ Hay un problema mayor que necesita investigación profunda
→ Ejecuta: `.\CAPTURE-BUILD-OUTPUT.ps1` y revisa el archivo

