using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public CategoryMenuViewComponent(ApplicationDbContext db, IMemoryCache cache) { _db = db; _cache = cache; }

        public class Vm
        {
            public ContenidoCategoria Parent { get; set; }
            public List<ContenidoCategoria> Children { get; set; } = new List<ContenidoCategoria>();
        }

        public async Task<IViewComponentResult> InvokeAsync(int sequence = 11)
        {
            var cacheKey = $"CategoryMenu:{sequence}";

            if (!_cache.TryGetValue(cacheKey, out Vm vm))
            {
                vm = new Vm();

                var parent = await _db.ContenidosCategorias.AsNoTracking().FirstOrDefaultAsync(c => c.Sequence == sequence && !c.Borrado);
                if (parent != null)
                {
                    vm.Parent = parent;
                    vm.Children = await _db.ContenidosCategorias.AsNoTracking()
                        .Where(c => c.CategoriaPadre == sequence && !c.Borrado)
                        .OrderBy(c => c.Nombre)
                        .ToListAsync();
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(60));

                _cache.Set(cacheKey, vm, cacheEntryOptions);
            }

            return View(vm);
        }
    }
}
