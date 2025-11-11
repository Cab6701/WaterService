using Microsoft.AspNetCore.Mvc;
using WaterService.Models;
using WaterService.Services;

namespace WaterService.Controllers
{
    public class TierPriceController : Controller
    {
        private readonly ITierPriceService _tierPriceService;

        public TierPriceController(ITierPriceService tierPriceService)
        {
            _tierPriceService = tierPriceService;
        }

        // GET: TierPrice/Edit
        public IActionResult Edit()
        {
            TempData["InfoMessage"] = "Chức năng chỉnh sửa giá bậc thang hiện có trong trang Quản Lý Người Dùng.";
            return RedirectToAction("Index", "Customer");
        }

        // POST: TierPrice/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TierPrice tierPrice)
        {
            if (ModelState.IsValid)
            {
                await _tierPriceService.UpdateTierPriceAsync(tierPrice);
                TempData["SuccessMessage"] = "Đã cập nhật giá bậc thang thành công.";
                return Json(new { success = true, message = "Đã cập nhật giá bậc thang thành công." });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật giá." });
        }

        // GET: TierPrice/GetCurrent
        public async Task<IActionResult> GetCurrent()
        {
            var tierPrice = await _tierPriceService.GetCurrentTierPriceAsync();
            return Json(new 
            { 
                tier1Price = tierPrice.Tier1Price, 
                tier2Price = tierPrice.Tier2Price, 
                tier3Price = tierPrice.Tier3Price 
            });
        }
    }
}

