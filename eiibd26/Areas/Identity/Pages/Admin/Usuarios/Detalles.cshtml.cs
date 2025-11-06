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
            public string Nombre { get; set; }
            public DateTime FechaInicio { get; set; }
        }

        public class SintomaDetalleView
        {
            public string Nombre { get; set; }
            public DateTime FechaCreado { get; set; }
        }

        public class TratamientoDetalleView
        {
            public string Nombre { get; set; }
            public DateTime FechaDeInicio { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("El parámetro id está vacío.");

            if (!Guid.TryParse(id, out Guid userIdGuid))
                return BadRequest("El parámetro id no es un GUID válido.");

            Usuario = await _db.Users.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (Usuario == null)
            {
                Perfil = null;
                return Page();
            }

            Perfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == userIdGuid);

            // Condiciones del usuario + su nombre
            Condiciones = await (from cu in _db.condicionUsuario
                                 join c in _db.condiciones on cu.idCondicion equals c.id
                                 where cu.idUsuario == userIdGuid && !cu.Eliminado
                                 select new CondicionDetalleView
                                 {
                                     Nombre = c.nombre,
                                     FechaInicio = cu.fechaInicio
                                 }).ToListAsync();

            // Síntomas del usuario + su nombre
            Sintomas = await (from su in _db.sintomasUsuario
                              join s in _db.sintomas on su.idSintoma equals s.id
                              where su.idUsuario == userIdGuid && !su.Eliminado
                              select new SintomaDetalleView
                              {
                                  Nombre = s.nombre,
                                  FechaCreado = su.fechaCreado
                              }).ToListAsync();

            // Tratamientos del usuario + nombre
            Tratamientos = await (from tu in _db.tratamientoUsuario
                                  join t in _db.tratamientos on tu.idTratamiento equals t.id
                                  where tu.idUsuario == userIdGuid && !tu.Eliminado
                                  select new TratamientoDetalleView
                                  {
                                      Nombre = t.nombre,
                                      FechaDeInicio = tu.fechaDeInicio
                                  }).ToListAsync();

            // Valida hash
            HashIsValid = Usuario != null &&
                          !string.IsNullOrEmpty(Usuario.PasswordHash) &&
                          Usuario.PasswordHash.Length >= 50 &&
                          Usuario.PasswordHash.StartsWith("AQAAAA");

            return Page();
        }
    }
}