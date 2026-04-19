using Microsoft.AspNetCore.Mvc;
using FE.services;
using FE.viewmodels;

namespace FE.controllers
{
    public class CarOfAutoOwnerController : Controller
    {
        private readonly CarOfAutoOwnerService _carService;

        public CarOfAutoOwnerController(CarOfAutoOwnerService carService)
        {
            _carService = carService;
        }

        // ✅ Danh sách xe theo Owner
        public async Task<IActionResult> Index(int ownerId)
        {
            var cars = await _carService.GetCarsByOwnerIdAsync(ownerId);
            ViewBag.OwnerId = ownerId;
            return View(cars);
        }

        // ✅ Chi tiết xe
        public async Task<IActionResult> Details(int id)
        {
            var car = await _carService.GetCarByIdAsync(id);
            if (car == null) return NotFound();
            return View(car);
        }

        // ✅ Thêm xe
        [HttpGet]
        public IActionResult Create(int ownerId)
        {
            var model = new CarOfAutoOwnerViewModel { UserId = ownerId };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CarOfAutoOwnerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var created = await _carService.CreateCarAsync(model);
            if (created != null)
            {
                // Quay lại danh sách xe
                return RedirectToAction("Index", new { ownerId = model.UserId });
            }

            ModelState.AddModelError("", "Thêm xe thất bại. Vui lòng thử lại!");
            return View(model);
        }


        // ✅ Sửa xe
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var car = await _carService.GetCarByIdAsync(id);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CarOfAutoOwnerViewModel car)
        {
            if (!ModelState.IsValid)
                return View(car);

            await _carService.UpdateCarAsync(id, car);
            return RedirectToAction("Index", new { ownerId = car.UserId });
        }

        // ✅ Xoá xe
        [HttpPost]
        public async Task<IActionResult> Delete(int id, int ownerId)
        {
            var isDeleted = await _carService.DeleteCarAsync(id);

            if (!isDeleted)
            {
                TempData["ErrorMessage"] = "Xóa xe thất bại, vui lòng thử lại!";
            }
            else
            {
                TempData["SuccessMessage"] = "Xóa xe thành công!";
            }

            return RedirectToAction("Index", new { ownerId });
        }

    }
}
