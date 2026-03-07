# ⚡ PRUEBA INMEDIATA - Copia los pasos exactos

## 1️⃣ COMPILAR
```powershell
dotnet build
```
Debe terminar con: **"Build succeeded"**

---

## 2️⃣ LIMPIAR CACHE NAVEGADOR
```
Ctrl+Shift+Delete
→ Todos los tiempos
→ Marca TODO
→ Limpiar
```

---

## 3️⃣ EJECUTAR
```
En Visual Studio: F5
Espera a que aparezca el navegador
```

---

## 4️⃣ ABRIR OUTPUT WINDOW
```
Ctrl+Alt+O

En dropdown selecciona: DEBUG (no Build)
```

---

## 5️⃣ BUSCAR "DIARREA"
```
1. Ve a: /Contenidos
2. Busca: "Diarrea"
3. Presiona: Enter
```

---

## 6️⃣ REVISAR OUTPUT

**Deberías ver UNA de estas opciones:**

### ✅ OPCIÓN A: Funciona correctamente
```
🔍 SEARCH: 'diarrea' | Found 42 results | PAGE 1
  [EXACTO] 'diarrea' = 'Diarrea' → 10000
  [COMIENZA] 'diarrea' at start... → 5000
  ...
```

### ❌ OPCIÓN B: No funciona
```
(NADA - No ves mensajes de debug)
```

### ⚠️ OPCIÓN C: Error
```
error xxxxxxx
```

---

## ✅ RESULTADO ESPERADO

**Búsqueda "Diarrea" debe mostrar:**

```
Página 1:
  1. Diarrea (o muy parecido)
  2. Diarrea en EII (o similar)
  3. Síntomas de diarrea
  ...

Página 2+:
  Artículos con "diarrea" en contenido largo
```

---

## 📋 DESPUÉS DE PROBAR

**Di exactamente UNA de estas 3 cosas:**

1. ✅ "Funciona, veo los debug messages [EXACTO], [COMIENZA], etc"
2. ❌ "No funciona, en Output no veo nada"
3. ⚠️ "Error en Output, veo: [escribe el error aquí]"

---

**Es importante que hagas EXACTAMENTE estos 6 pasos en orden** 🎯
