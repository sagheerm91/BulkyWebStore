using BulkyWeb.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CompanyController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            List<Company> companies = _db.Companies.ToList();
            return View(companies);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Company obj)
        {
            if (ModelState.IsValid)
            {
                _db.Companies.Add(obj);
                _db.SaveChanges();
                TempData["success"] = "Comapny created successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Company? companyFromDb = _db.Companies.Find(id);

            if (companyFromDb == null)
            {
                return NotFound();
            }
            return View(companyFromDb);
        }
        [HttpPost]
        public IActionResult Edit(Company obj)
        {

            if (ModelState.IsValid)
            {
                _db.Companies.Update(obj);
                _db.SaveChanges();
                TempData["success"] = "Company updated successfully";
                return RedirectToAction("Index");
            }
            return View();
        }


        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _db.Companies.ToList();

            return Json(new { data = objCompanyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var companyToBeDel = _db.Companies.Find(id);
            if (companyToBeDel == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while deleting"
                });
            }

            _db.Remove(companyToBeDel);
            _db.SaveChanges();

            return Json(new { success = true, message = "Deleted Successfully" });
        }

        #endregion
    }
}
