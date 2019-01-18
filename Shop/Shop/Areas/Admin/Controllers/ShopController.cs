using PagedList;
using Shop.Models.Data;
using Shop.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
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

        // GET: Admin/Shop/AddProduct
        [HttpGet]
        public ActionResult AddProduct()
        {
            ProductVM model = new ProductVM();

            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }

            return View(model);
        }

        // Post: Admin/Shop/AddProduct
        [HttpPost]
        public ActionResult AddProduct(ProductVM model, HttpPostedFileBase file)
        {
            // Check model state
            if (!ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    return View(model);
                }
            }

            // Make sure product name is unique
            using (Db db = new Db())
            {
                if (db.Products.Any(a => a.Name.Equals(model.Name)))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
            }

            // Make product id(declare)
            int id;
            // Initialize product Dto the save it
            using (Db db = new Db())
            {
                ProductDTO product = new ProductDTO();

                product.Name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;

                CategoriesDTO catDTO = db.Categories.FirstOrDefault(a => a.Id.Equals(model.CategoryId));
                product.CategoryName = catDTO.Name;

                db.Products.Add(product);
                db.SaveChanges();

                // Get inserted id
                id = product.Id;
            }

            // Dont forget about temp data message!
            TempData["SM"] = "You have added a product!";

            #region Upload Image

            // Create necessary directories
            DirectoryInfo originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

            string pathString = Path.Combine(originalDirectory.ToString(), "Products");
            string pathStringWithId = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
            string pathStringThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");
            string pathStringGallery = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
            string pathStringGalleryThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

            if (!Directory.Exists(pathString))
                Directory.CreateDirectory(pathString);

            if (!Directory.Exists(pathStringWithId))
                Directory.CreateDirectory(pathStringWithId);

            if (!Directory.Exists(pathStringThumbs))
                Directory.CreateDirectory(pathStringThumbs);

            if (!Directory.Exists(pathStringGallery))
                Directory.CreateDirectory(pathStringGallery);

            if (!Directory.Exists(pathStringGalleryThumbs))
                Directory.CreateDirectory(pathStringGalleryThumbs);

            // Check if file was uploaded
            if (file != null && file.ContentLength > 0)
            {
                // Get file extension
                string fileExtension = file.ContentType.ToLower();

                // Verify extension
                if (!fileExtension.Equals("image/jpg") &&
                    !fileExtension.Equals("image/jpeg") &&
                    !fileExtension.Equals("image/pjpeg") &&
                    !fileExtension.Equals("image/gif") &&
                    !fileExtension.Equals("image/x-png") &&
                    !fileExtension.Equals("image/png"))
                {
                    using (Db db = new Db())
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension!");
                        return View(model);
                    }
                }

                // Init image name
                string imageName = file.FileName;

                // Save image name to DTO
                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }

                // Set original and thumb image paths
                string pathId = string.Format("{0}\\{1}", pathStringWithId, imageName);
                string pathThumbs = string.Format("{0}\\{1}", pathStringThumbs, imageName);

                // Save original
                file.SaveAs(pathId);

                // Create and save thumb
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(pathThumbs);
            }

            #endregion

            return RedirectToAction("AddProduct");
        }

        // GET: Admin/Shop/Products
        public ActionResult Products(int? page, int? catId)
        {
            // Declare a list of ProductVM
            List<ProductVM> listOfProductVM;

            // Set page number
            int pageNumber = page ?? 1;

            using (Db db = new Db())
            {
                // Initialize the list
                listOfProductVM = db.Products.ToArray().
                                            Where(a => catId == null || catId == 0 || a.CategoryId == catId)
                                            .Select(b => new ProductVM(b)).ToList();

                // Populate categories select list
                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                // Set selected category
                ViewBag.SelectedCat = catId.ToString();
            }

            // Set pagination
            IPagedList<ProductVM> onePageOfProducts = listOfProductVM.ToPagedList(pageNumber, 3);
            ViewBag.OnePageOfProducts = onePageOfProducts;

            // Return view with the list
            return View(listOfProductVM);
        }

        // GET: Admin/Shop/EditProduct/id
        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            // Declare producVM
            ProductVM model;

            using (Db db = new Db())
            {
                // Get the product
                ProductDTO dto = db.Products.Find(id);

                // Make sure product exists
                if (dto == null)
                {
                    return Content("That product does not exist.");
                }

                // Initialize model
                model = new ProductVM(dto);

                // Make a select list
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

                // Get all galery images
                model.GalleryImages = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                                      .Select(a => Path.GetFileName(a));
            }

            // Return view with model
            return View(model);
        }

        // Post: Admin/Shop/EditProduct/id
        [HttpPost]
        public ActionResult EditProduct(ProductVM model, HttpPostedFileBase file)
        {
            // Get product id
            int id = model.Id;
            // Populate categories select list and gallery images
            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }
            model.GalleryImages = Directory.EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                                      .Select(a => Path.GetFileName(a));

            // Check model state
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Make sure product name is unique
            using (Db db = new Db())
            {
                if (db.Products.Where(a => a.Id != id).Any(b => b.Name.Equals(model.Name)))
                {
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
            }

            // Update product
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);

                dto.Name = model.Name;
                dto.Slug = model.Name.Replace(" ", "-").ToLower();
                dto.Description = model.Description;
                dto.Price = model.Price;
                dto.CategoryId = model.CategoryId;
                dto.ImageName = model.ImageName;

                CategoriesDTO catDTO = db.Categories.FirstOrDefault(a => a.Id == model.CategoryId);
                dto.CategoryName = catDTO.Name;

                db.SaveChanges();
            }

            // Dont forget about tempdata 
            TempData["SM"] = "You have edited the product!";

            #region Image Upload

            // Check for file upload
            if (file != null && file.ContentLength > 0)
            {
                // Get file extension
                string fileExtension = file.ContentType.ToLower();

                // Verify extension
                if (!fileExtension.Equals("image/jpg") &&
                    !fileExtension.Equals("image/jpeg") &&
                    !fileExtension.Equals("image/pjpeg") &&
                    !fileExtension.Equals("image/gif") &&
                    !fileExtension.Equals("image/x-png") &&
                    !fileExtension.Equals("image/png"))
                {
                    using (Db db = new Db())
                    {
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension!");
                        return View(model);
                    }
                }

                // Set upload directory paths
                DirectoryInfo originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

                string pathStringWithId = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
                string pathStringThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");

                // Delete filese from directories !!!

                DirectoryInfo productsDirectoryInfo = new DirectoryInfo(pathStringWithId);
                DirectoryInfo productsThumbsDirectoryInfo = new DirectoryInfo(pathStringThumbs);

                foreach (FileInfo item in productsDirectoryInfo.GetFiles())
                {
                    item.Delete();
                }

                foreach (FileInfo item in productsThumbsDirectoryInfo.GetFiles())
                {
                    item.Delete();
                }

                // Save image name

                string imageName = file.FileName;

                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }

                // Save original and thumb images
                string pathId = string.Format("{0}\\{1}", pathStringWithId, imageName);
                string pathThumbs = string.Format("{0}\\{1}", pathStringThumbs, imageName);

                // Save original
                file.SaveAs(pathId);

                // Create and save thumb
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(pathThumbs);
            }

            #endregion

            return RedirectToAction("EditProduct");
        }

        // GET: Admin/Shop/DeleteProduct/id
        public ActionResult DeleteProduct(int id)
        {
            // Delete product from DB
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);
                db.Products.Remove(dto);

                db.SaveChanges();
            }

            // Delete product folder
            DirectoryInfo originalDirectory = new DirectoryInfo(string.Format(string.Format("{0}Images\\Uploads", Server.MapPath(@"\"))));
            string pathString = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());

            if (Directory.Exists(pathString))
                Directory.Delete(pathString, true);

            // Redirect
            return RedirectToAction("Products");
        }

        // Post: Admin/Shop/SaveGalleryImages
        [HttpPost]
        public void SaveGalleryImages(int id)
        {
            // Loop through the files 
            foreach (string fileName in Request.Files)
            {
                // Initialize the file 
                HttpPostedFileBase file = Request.Files[fileName];

                // Check for null
                if (file != null && file.ContentLength > 0)
                {
                    DirectoryInfo originalDirectory = new DirectoryInfo(string.Format(string.Format("{0}Images\\Uploads", Server.MapPath(@"\"))));
                    string pathStringGallery = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
                    string pathStringGalleryThumbs = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

                    string finalPathGallery = Path.Combine("{0}\\{1}", pathStringGallery, file.FileName);
                    string finalPathGalleryThumbs = Path.Combine("{0}\\{1}", pathStringGalleryThumbs, file.FileName);

                    // Save original and thumb

                    file.SaveAs(finalPathGallery);
                    WebImage img = new WebImage(file.InputStream);
                    img.Resize(200, 200);
                    img.Save(finalPathGalleryThumbs);
                }
            }
        }

        // Post: Admin/Shop/DeleteImage
        [HttpPost]
        public void DeleteImage(int id, string imageName)
        {
            string pathGallery = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/" + imageName);
            string pathGalleryThumbs = Request.MapPath("~/Images/Uploads/Products/" + id.ToString() + "/Gallery/Thumbs/" + imageName);

            if (System.IO.File.Exists(pathGallery))
                System.IO.File.Delete(pathGallery);

            if (System.IO.File.Exists(pathGalleryThumbs))
                System.IO.File.Delete(pathGalleryThumbs);

            System.Diagnostics.Debug.WriteLine("GAVVNO");
        }
    }
}