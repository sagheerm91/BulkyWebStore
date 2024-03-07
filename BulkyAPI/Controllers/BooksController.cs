using BulkyWeb.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Product = BulkyWeb.Models.Product;

namespace BulkyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        public BooksController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetBooks()
        {
            return Ok(_applicationDbContext.Products.ToList());

        }

        [HttpGet("{id:int}", Name = "GetBook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Product> GetBook(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }

            var villa = _applicationDbContext.Products.FirstOrDefault(u => u.Id == id);

            if (villa == null)
            {
                return NotFound();
            }

            return Ok(villa);
        }

        [HttpPost]
        public ActionResult<Product> AddBook([FromBody] Product product) 
        {
            if (product == null)
            {
                return BadRequest(product);
            }
            if (_applicationDbContext.Products.FirstOrDefault(u => u.Title.ToLower() == product.Title.ToLower()) != null)
            {
                ModelState.AddModelError("Name", "Already Exists");
                return BadRequest(ModelState);
            }


            Product model = new()
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                Price50 = product.Price50,
                Price100 = product.Price100,
                ListPrice = product.ListPrice,
                ISBN = product.ISBN,
                Author = product.Author,
                Category = product.Category,
                ImageURL = product.ImageURL,
                CategoryId = product.CategoryId,
            };

            _applicationDbContext.Products.Add(model);
            _applicationDbContext.SaveChanges();
            return CreatedAtRoute("GetBook", new { id = product.Id }, product);
        }
    }
}
