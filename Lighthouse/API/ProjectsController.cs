using Lighthouse.Models;
using Lighthouse.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IRepository<Project> repository;

        public ProjectsController(IRepository<Project> repository)
        {
            this.repository = repository;
        }

        [HttpGet]
        public async Task<IEnumerable<Project>> Get()
        {
            return await Task.FromResult(repository.GetAll());
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return NotFound();
        }
    }
}
