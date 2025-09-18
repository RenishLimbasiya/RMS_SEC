using Microsoft.EntityFrameworkCore;
using RMS.Data;
using RMS.Models.DTOs.Tables;
using RMS.Models.Entities;
using RMS.Repositories.Interfaces;

namespace RMS.Services
{
    public class TableService
    {
        private readonly IGenericRepository<RestaurantTable> _repo;
        private readonly RmsDbContext _ctx;

        public TableService(IGenericRepository<RestaurantTable> repo, RmsDbContext ctx)
        {
            _repo = repo;
            _ctx = ctx;
        }

        
        public async Task<IEnumerable<TableDto>> GetAllAsync()
        {
            var tables = await _ctx.Tables.ToListAsync();

            var result = tables.Select(t => new TableDto
            {
                Id = t.Id,
                Name = t.Name,
                Capacity = t.Capacity,
                Status = t.Status   
            }).ToList();

            return result;
        }

        
        public Task<RestaurantTable?> GetAsync(int id) => _repo.GetByIdAsync(id);

        
        public async Task<RestaurantTable> AddAsync(RestaurantTable t)
        {
            await _repo.AddAsync(t);
            await _repo.SaveAsync();
            return t;
        }

        
        public async Task<bool> UpdateAsync(RestaurantTable t)
        {
            _repo.Update(t);
            await _repo.SaveAsync();
            return true;
        }

        
        public async Task<bool> DeleteAsync(int id)
        {
            var t = await _repo.GetByIdAsync(id);
            if (t == null) return false;
            _repo.Delete(t);
            await _repo.SaveAsync();
            return true;
        }
    }
}
