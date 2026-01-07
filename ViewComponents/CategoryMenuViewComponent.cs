using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        public CategoryMenuViewComponent(ApplicationDbContext db) { _db = db; }

        public class Vm
        {
            public ContenidoCategoria Parent { get; set; }
            public List<ContenidoCategoria> Children { get; set; } = new List<ContenidoCategoria>();
        }

        public async Task<IViewComponentResult> InvokeAsync(int sequence = 11)
        {
            var vm = new Vm();
            var parent = await _db.ContenidosCategorias.AsNoTracking().FirstOrDefaultAsync(c => c.Sequence == sequence && !c.Borrado);
            if (parent != null)
            {
                vm.Parent = parent;
                vm.Children = await _db.ContenidosCategorias.AsNoTracking()
                    .Where(c => c.CategoriaPadre == sequence && !c.Borrado)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
            }
            return View(vm);
        }
    }
}
