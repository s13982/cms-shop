using Shop.Models.Data;
using Shop.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Areas.Admin.Controllers
{
    public class ShopController : Controller
    {
        // GET: Admin/Shop/Categories
        public ActionResult Categories()
        {
            List<CategoryVM> categoryVMs;

            using (Db db = new Db())
            {
                categoryVMs = db.Categories.ToArray().OrderBy(a => a.Sorting).Select(b => new CategoryVM(b)).ToList();
            }

            return View(categoryVMs);
        }

        // Post: Admin/Shop/AddNewCategory
        [HttpPost]
        public string AddNewCategory(string catName)
        {
            string id;

            using (Db db = new Db())
            {
                if (db.Categories.Any(a => a.Name.Equals(catName)))
                {
                    return "titletaken";
                }

                CategoriesDTO dto = new CategoriesDTO();
                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = 100;

                db.Categories.Add(dto);
                db.SaveChanges();

                id = dto.Id.ToString();
            }

            return id;
        }

        // GET: Admin/Shop/ReorderCategories
        [HttpPost]
        public void ReorderCategories(int[] id)
        {
            using (Db db = new Db())
            {
                int count = 1;
                CategoriesDTO dto;
                for (int i = 0; i < id.Length; i++)
                {
                    dto = db.Categories.Find(id[i]);
                    dto.Sorting = count;

                    db.SaveChanges();
                    count++;
                }
            }
        }

        // GET: Admin/Shop/DeleteCategory/id
        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {
                CategoriesDTO dto = db.Categories.Find(id);

                db.Categories.Remove(dto);
                db.SaveChanges();
            }

            return RedirectToAction("Categories");
        }

        // Post: Admin/Shop/RenameCategory
        [HttpPost]
        public string RenameCategory(string newCatName, int id)
        {
            using (Db db = new Db())
            {
                if (db.Categories.Any(a => a.Name == newCatName))
                    return "titletaken";

                CategoriesDTO dto = db.Categories.Find(id);
                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", "-").ToLower();

                db.SaveChanges();
            }

            return "ok";
        }
    }
}