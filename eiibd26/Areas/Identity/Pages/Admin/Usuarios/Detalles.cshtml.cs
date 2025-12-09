using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace eiibd26.Areas.Identity.Pages.Admin.Usuarios
{
    [Authorize(Roles = "Administrador")]
    [IgnoreAntiforgeryToken]
    public class DetallesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DetallesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public ApplicationUser Usuario { get; set; }
        public Perfil Perfil { get; set; }

        public List<CondicionDetalleView> Condiciones { get; set; } = new();
        public List<SintomaDetalleView> Sintomas { get; set; } = new();
        public List<TratamientoDetalleView> Tratamientos { get; set; } = new();

        public bool HashIsValid { get; set; }

        public class CondicionDetalleView
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int? IdPadre { get; set; }
            public DateTime FechaInicio { get; set; }
            public bool EsPadre => IdPadre == null;
            public List<CondicionDetalleView> Hijos { get; set; } = new();
        }
        public class SintomaDetalleView
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int? IdPadre { get; set; }
            public DateTime FechaCreado { get; set; }
            public bool EsPadre => IdPadre == null;
            public List<SintomaDetalleView> Hijos { get; set; } = new();
        }
        public class TratamientoDetalleView
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int? IdPadre { get; set; }
            public DateTime FechaDeInicio { get; set; }
            public bool EsPadre => IdPadre == null;
            public List<TratamientoDetalleView> Hijos { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    ViewData["Error"] = "El parámetro id está vacío.";
                    return Page();
                }

                if (!Guid.TryParse(id, out Guid userIdGuid))
                {
                    ViewData["Error"] = $"El parámetro id '{id}' no es un GUID válido.";
                    return Page();
                }

                Usuario = await _db.Users.FirstOrDefaultAsync(u => u.Id == userIdGuid);
                if (Usuario == null)
                {
                    Perfil = null;
                    ViewData["Error"] = "Usuario no encontrado.";
                    return Page();
                }
                Perfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == userIdGuid);

                // Condiciones
                var condicionesRaw = await (from cu in _db.condicionUsuario
                                            join c in _db.condiciones on cu.idCondicion equals c.id
                                            where cu.idUsuario == userIdGuid && !cu.Eliminado
                                            select new CondicionDetalleView
                                            {
                                                Id = cu.idCondicion.Value,
                                                Nombre = c.nombre,
                                                IdPadre = c.idPadre,
                                                FechaInicio = cu.fechaInicio ?? DateTime.UtcNow,
                                            }).ToListAsync();
                var padres = condicionesRaw.Where(x => x.IdPadre == null).ToList();
                foreach (var padre in padres)
                    padre.Hijos = condicionesRaw.Where(x => x.IdPadre == padre.Id).ToList();
                var huerfanos = condicionesRaw.Where(x => x.IdPadre != null && !padres.Any(p => p.Id == x.IdPadre)).ToList();
                Condiciones = (padres.Count > 0) ? padres.Concat(huerfanos).ToList() : condicionesRaw;

                // Sintomas
                var sintomasRaw = await (from su in _db.sintomasUsuario
                                         join s in _db.sintomas on su.idSintoma equals s.id
                                         where su.idUsuario == userIdGuid && !su.Eliminado
                                         select new SintomaDetalleView
                                         {
                                             Id = su.idSintoma.Value,
                                             Nombre = s.nombre,
                                             IdPadre = s.idPadre,
                                             FechaCreado = su.fechaCreado
                                         }).ToListAsync();
                var spadres = sintomasRaw.Where(x => x.IdPadre == null).ToList();
                foreach (var padre in spadres)
                    padre.Hijos = sintomasRaw.Where(x => x.IdPadre == padre.Id).ToList();
                var shuerfanos = sintomasRaw.Where(x => x.IdPadre != null && !spadres.Any(p => p.Id == x.IdPadre)).ToList();
                Sintomas = (spadres.Count > 0) ? spadres.Concat(shuerfanos).ToList() : sintomasRaw;

                // Tratamientos
                var tratsRaw = await (from tu in _db.tratamientoUsuario
                                      join t in _db.tratamientos on tu.idTratamiento equals t.id
                                      where tu.idUsuario == userIdGuid && !tu.Eliminado
                                      select new TratamientoDetalleView
                                      {
                                          Id = tu.idTratamiento.Value,
                                          Nombre = t.nombre,
                                          IdPadre = t.idPadre,
                                          FechaDeInicio = tu.fechaInicio
                                      }).ToListAsync();
                var tpadres = tratsRaw.Where(x => x.IdPadre == null).ToList();
                foreach (var padre in tpadres)
                    padre.Hijos = tratsRaw.Where(x => x.IdPadre == padre.Id).ToList();
                var thuerfanos = tratsRaw.Where(x => x.IdPadre != null && !tpadres.Any(p => p.Id == x.IdPadre)).ToList();
                Tratamientos = (tpadres.Count > 0) ? tpadres.Concat(thuerfanos).ToList() : tratsRaw;

                // Hash validación
                HashIsValid = Usuario != null &&
                              !string.IsNullOrEmpty(Usuario.PasswordHash) &&
                              Usuario.PasswordHash.Length >= 50 &&
                              Usuario.PasswordHash.StartsWith("AQAAAA");
                return Page();
            }

            catch (Exception ex)
            {
                ViewData["Error"] = "Error interno: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Guid.TryParse(Request.Form["UserId"], out Guid userIdGuid))
            {
                ViewData["Error"] = "No se recibió UserId o no es GUID.";
                return await OnGetAsync(Request.Form["UserId"]);
            }

            // USUARIO
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userIdGuid);
            if (user != null)
            {
                user.UserName = Request.Form["UserName"];
                user.Email = Request.Form["Email"];
                user.PhoneNumber = Request.Form["PhoneNumber"];
                user.EmailConfirmed = Request.Form["EmailConfirmed"] == "true";
                user.LockoutEnabled = Request.Form["LockoutEnabled"] == "true";
                user.LockoutEnd = DateTime.TryParse(Request.Form["LockoutEnd"], out var dtFe) ? dtFe : user.LockoutEnd;
                user.AccessFailedCount = int.TryParse(Request.Form["AccessFailedCount"], out var fa) ? fa : user.AccessFailedCount;
                user.RequiresPasswordReset = Request.Form["RequiresPasswordReset"] == "true";
            }

            // PERFIL
            var perfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == userIdGuid);
            if (perfil != null)
            {
                perfil.Avatar = Request.Form["Avatar"];
                if (int.TryParse(Request.Form["ImagenFondo"], out var imgfondo))
                    perfil.imagenFondo = imgfondo;
                else
                    perfil.imagenFondo = null;
                perfil.Titulo = Request.Form["Titulo"];
                perfil.Nombre = Request.Form["Nombre"];
                perfil.Apellidos = Request.Form["Apellidos"];
                
                perfil.FechaDeNacimiento = DateTime.TryParse(Request.Form["FechaDeNacimiento"], out var fnac) ? fnac : perfil.FechaDeNacimiento;
                perfil.Genero = Request.Form["Genero"];
                perfil.NombreCiudad = Request.Form["Ciudad"];
                perfil.NombrePais = Request.Form["Pais"];
                
                //perfil.notas = Request.Form["Notas"];
                //perfil.descripcion = Request.Form["Descripcion"];
                perfil.AcercaDe = Request.Form["AcercaDe"];
            }

            // Relación Condiciones
            var checkedCondIds = Request.Form["CondicionesSeleccionadas"].Select(int.Parse).ToHashSet();
            var userConds = await _db.condicionUsuario.Where(x => x.idUsuario == userIdGuid && !x.Eliminado).ToListAsync();
            foreach (var rel in userConds)
            {
                rel.Eliminado = !checkedCondIds.Contains(rel.idCondicion.Value);
                if (rel.Eliminado)
                {
                    rel.fechaEliminado = DateTime.Now.Date;
                    rel.fechaModificado = DateTime.Now;
                }
            }
            // Relación Síntomas
            var checkedSintIds = Request.Form["SintomasSeleccionados"].Select(int.Parse).ToHashSet();
            var userSints = await _db.sintomasUsuario.Where(x => x.idUsuario == userIdGuid && !x.Eliminado).ToListAsync();
            foreach (var rel in userSints)
            {
                rel.Eliminado = !checkedSintIds.Contains(rel.idSintoma.Value);
                if (rel.Eliminado)
                {
                    rel.fechaEliminado = DateTime.Now;
                    rel.fechaModificado = DateTime.Now;
                }
            }
            // Relación Tratamientos
            var checkedTratIds = Request.Form["TratamientosSeleccionados"].Select(int.Parse).ToHashSet();
            var userTrats = await _db.tratamientoUsuario.Where(x => x.idUsuario == userIdGuid && !x.Eliminado).ToListAsync();
            foreach (var rel in userTrats)
            {
                rel.Eliminado = !checkedTratIds.Contains(rel.idTratamiento.Value);
                if (rel.Eliminado)
                {
                    rel.fechaEliminado = DateTime.Now.Date;
                    rel.fechaModificado = DateTime.Now;
                }
            }

            await _db.SaveChangesAsync();
            ViewData["SuccessMessage"] = "Edición realizada correctamente";
            return await OnGetAsync(userIdGuid.ToString());
        }
    }
}