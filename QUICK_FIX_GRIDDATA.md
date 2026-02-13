# 🚨 GridData 404 - Quick Fix

## Current Status
- ✅ Local code has the fix (handler alias added)
- ❌ Production server still has old code (404 error persists)

## Problem
The production server at eiibd.com doesn't recognize `?handler=GridData` because:
1. The new handler alias `OnGetGridData` isn't deployed yet
2. The file `Index.cshtml.cs` needs to be re-compiled and re-deployed

## Quick Fix Commands

### Step 1: Rebuild with Latest Changes
```powershell
cd D:\Users\berletzis\Source\Repos\eiibd\eiibd26\eiibd26
dotnet clean
dotnet publish -c Release -o ../publish
```

### Step 2: Verify Handler Exists in Published DLL
The handler should be in:
```
publish/Areas/Identity/Pages/Admin/Contenidos/Index.cshtml.cs.dll
```

### Step 3: Deploy ONLY the Updated Files
**Critical files to upload:**
- `Areas/Identity/Pages/Admin/Contenidos/Index.dll` (compiled handler)
- `Areas/Identity/Pages/Admin/Contenidos/Index.cshtml` (if not already deployed)

**Or deploy entire publish folder** to be safe.

### Step 4: Restart Application
```sh
# On production server
sudo systemctl restart eiibd
# Or stop/start IIS Application Pool
```

---

## Alternative Quick Test

If you want to test faster, try accessing the handler directly:

```
https://eiibd.com/Identity/Admin/Contenidos?handler=GridDataAsync
```

If that works, it means:
- ✅ The handler exists
- ❌ The routing doesn't recognize "GridData" without "Async"
- ✅ The alias fix will solve it

---

## What the Fix Does

### Before (production server now):
```csharp
// Only has OnGetGridDataAsync - ASP.NET Core may not route ?handler=GridData to it
public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
{
    // ... implementation
}
```

### After (local code, needs deployment):
```csharp
// Original method
public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
{
    // ... implementation
}

// NEW: Alias ensures ?handler=GridData routes correctly
public async Task<IActionResult> OnGetGridData(bool mostrarEliminados = false)
{
    return await OnGetGridDataAsync(mostrarEliminados);
}
```

---

## Why ContenidosCategorias Works

The other page works because:
1. It may have been deployed with a different ASP.NET Core routing config
2. Or the routing cache recognized it differently
3. Or there's an environment difference

Adding the explicit alias **guarantees** routing works in all cases.

---

## Expected Result After Deploy

```
✅ https://eiibd.com/Identity/Admin/Contenidos/Index
✅ DataTable loads with content rows
✅ Network shows: /Identity/Admin/Contenidos?handler=GridData → 200 OK (JSON response)
✅ Console is clean (no errors)
```

---

## Rollback if Still Fails

If it still doesn't work after deployment, try:

```csharp
// Add [HttpGet] attribute explicitly
[HttpGet]
public async Task<IActionResult> OnGetGridData(bool mostrarEliminados = false)
{
    return await OnGetGridDataAsync(mostrarEliminados);
}
```

Or rename the original method:
```csharp
// Remove Async suffix from original method
public async Task<IActionResult> OnGetGridData(bool mostrarEliminados = false)
{
    // ... existing implementation (no alias needed)
}
```

---

**⏰ Time to Fix:** 5-10 minutes (build + deploy + restart)
