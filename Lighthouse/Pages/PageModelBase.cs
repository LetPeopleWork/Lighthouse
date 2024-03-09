using Lighthouse.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lighthouse.Pages
{
    public abstract class PageModelBase<T> : PageModel where T : class
    {
        protected PageModelBase(IRepository<T> repository)
        {
            Repository = repository;
        }

        [BindProperty]
        public T Entity { get; set; } = default!;

        protected IRepository<T> Repository { get; }

        public IActionResult OnGet(int? id)
        {
            var entity = GetById(id);

            if (!id.HasValue || entity == null)
            {
                return NotFound();
            }

            Entity = entity;

            OnGet(id.Value);

            return Page();
        }

        protected virtual void OnGet(int id)
        {
            // To be overriden
        }

        protected T? GetById(int? id)
        {
            return id.HasValue ? Repository.GetById(id.Value) : null;
        }
    }
}
