using Microsoft.EntityFrameworkCore;
using WaterService.Data;
using WaterService.Models;

namespace WaterService.Services
{
    public interface ITierPriceService
    {
        Task<TierPrice> GetCurrentTierPriceAsync();
        Task<TierPrice> UpdateTierPriceAsync(TierPrice tierPrice);
        Task EnsureDefaultTierPriceExistsAsync();
    }

    public class TierPriceService : ITierPriceService
    {
        private readonly ApplicationDbContext _context;

        public TierPriceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TierPrice> GetCurrentTierPriceAsync()
        {
            var tierPrice = await _context.TierPrices
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefaultAsync();

            if (tierPrice == null)
            {
                // Tạo giá mặc định nếu chưa có
                await EnsureDefaultTierPriceExistsAsync();
                tierPrice = await _context.TierPrices
                    .OrderByDescending(t => t.UpdatedAt)
                    .FirstOrDefaultAsync() ?? new TierPrice();
            }

            return tierPrice;
        }

        public async Task<TierPrice> UpdateTierPriceAsync(TierPrice tierPrice)
        {
            tierPrice.UpdatedAt = DateTime.UtcNow;

            // Thêm bản ghi mới thay vì cập nhật (để giữ lịch sử)
            _context.TierPrices.Add(tierPrice);
            await _context.SaveChangesAsync();

            return tierPrice;
        }

        public async Task EnsureDefaultTierPriceExistsAsync()
        {
            var existing = await _context.TierPrices.AnyAsync();
            if (!existing)
            {
                var defaultTierPrice = new TierPrice
                {
                    Tier1Price = 8470,
                    Tier2Price = 11000,
                    Tier3Price = 13200,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TierPrices.Add(defaultTierPrice);
                await _context.SaveChangesAsync();
            }
        }
    }
}

