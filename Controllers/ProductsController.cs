using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Controllers
{   
    [Authorize(Roles = "Admin")]
    [Route("api/v{version:apiVersion}/[controller]")]
    // [ApiVersion("1.0")]
    // [ApiVersion("2.0")]
    [ApiVersionNeutral] //Al ser neutral sirve para todas las veriones
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository; //Interfaz para acceder a los datos desde el repositorio
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper; //Convertir del dominio al DTO


        //Inyectamos las dependencias
        public ProductsController(IProductRepository productRepository, ICategoryRepository categoryRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProduct()
        {
            var products = _productRepository.GetProducts();
            var productsDto = _mapper.Map<List<ProductDto>>(products);
            return Ok(productsDto);

        }

        // GETBYID
        [AllowAnonymous]
        [HttpGet("{productId:int}", Name = "GetProduct")] //El Name sirve para que sea invocado por otro método
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status200OK)]//Métadata para swagger
        public IActionResult GetProduct(int productId)
        {
            var product = _productRepository.GetProduct(productId);
            if (product == null)
            {
                return NotFound($"El producto con el {productId} no existe");
            }

            var productDto = _mapper.Map<ProductByIdDto>(product);
            return Ok(productDto);
        }


        // CREACIÓN
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status201Created)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]//Métadata para swagger
        public IActionResult CreateProduct([FromForm] CreateProductDto createProductDto) //Recibe el body del request
        {

            //Validar
            if (createProductDto == null)
            {
                return BadRequest(ModelState); //Devoldemos el estado en el que se encuentra el módelo
            }

            //Validar si ya existe el producto
            if (_productRepository.ProductExists(createProductDto.Name))
            {
                ModelState.AddModelError("CustomError", "El producto ya existe");//Error personalizado
                return BadRequest(ModelState); //Lanzamos la excepción
            }

            //Validar que la categoría sea válida
            if (!_categoryRepository.CategoryExists(createProductDto.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"La categoria {createProductDto.CategoryId} no existe");//Error personalizado
                return BadRequest(ModelState); //Lanzamos la excepción
            }


            var product = _mapper.Map<Product>(createProductDto);

            //Agregando imagen
            if(createProductDto.Image != null)
            {
                string fileName = product.ProductId + Guid.NewGuid().ToString() + Path.GetExtension(createProductDto.Image.FileName);
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductsImages");

                //Válida que exista el directorio
                if (!Directory.Exists(imagesFolder))
                {
                    //Crea el directorio
                    Directory.CreateDirectory(imagesFolder);
                }

                var filePath = Path.Combine(imagesFolder, fileName);

                FileInfo file = new FileInfo(filePath);

                //Comprobar que no exista ya esa imagen
                if (file.Exists)
                {
                    file.Delete();
                }
                
                //El using asegura que un recurso se libere automáticamente cuando deja de usarse.
                using var fileStream = new FileStream(filePath, FileMode.Create); //Creamos el archivo
                createProductDto.Image.CopyTo(fileStream);
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                product.ImgUrl = $"{baseUrl}/ProductsImages/{fileName}";
                product.ImgUrlLocal = filePath;
            } else
            {
                product.ImgUrl = "https://placehold.co/300x300";
            }


            if (!_productRepository.CreateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al guardar el registro {product.Name}");
                return StatusCode(500, ModelState);
            }
            var createdProduct = _productRepository.GetProduct(product.ProductId);
            var productDto = _mapper.Map<ProductDto>(createdProduct);
            return CreatedAtRoute("GetProduct", new { productId = product.ProductId }, productDto); //Llama al método getById
        }

        // Obtener producto x categoria
        [AllowAnonymous]
        [HttpGet("SearchProductByCategory/{categoryId:int}", Name = "GetProductForCategory")] //El Name sirve para que sea invocado por otro método
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status200OK)]//Métadata para swagger
        public IActionResult GetProductForCategory(int categoryId)
        {
            var products = _productRepository.GetProductsForCategory(categoryId);
            if (products.Count == 0)
            {
                return NotFound($"No se encontraron productos con esa categoría");
            }

            var productDto = _mapper.Map<List<ProductDto>>(products);
            return Ok(productDto);
        }


        [HttpGet("SearchProductByNameDescription/{searchTerm}", Name = "GetProductForCategoryName")] //El Name sirve para que sea invocado por otro método
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status200OK)]//Métadata para swagger
        public IActionResult GetProductForCategoryName(string searchTerm)
        {
            var products = _productRepository.SearchProducts(searchTerm);
            if (products.Count == 0)
            {
                return NotFound($"Los productos con el nombre o descripción {searchTerm}");
            }

            var productDto = _mapper.Map<List<ProductDto>>(products);
            return Ok(productDto);
        }


        [HttpPatch("buyProduct/{name}/{quantity:int}", Name = "BuyProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status200OK)]//Métadata para swagger
        public IActionResult BuyProduct(string name, int quantity)
        {
            // var products = _productRepository.BuyProduct(name, quantity);
            if (string.IsNullOrWhiteSpace(name) || quantity <= 0)
            {
                return BadRequest("El nombre del producto o la cantidad no son válidos");
            }
            var foundProduct = _productRepository.ProductExists(name);
            if (!foundProduct)
            {
                return NotFound($"El producto con el nombre {name} no existe");
            }

            if (!_productRepository.BuyProduct(name, quantity))
            {   //Error personalizado
                ModelState.AddModelError("CustomError", $"No hay suficiente stock del producto {name} para comprar {quantity} unidades");
                return BadRequest(ModelState); //Lanzamos la excepción
            }

            var unidad = quantity == 1 ? "unidad" : "unidades";
            return Ok($"Se han comprado {quantity} {unidad} del producto {name}");
        }




        // UPDATE
        [HttpPut("{productId:int}", Name = "UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status204NoContent)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]//Métadata para swagger
        public IActionResult UpdateProduct(int productId, [FromForm] UpdateProductDto updateProductDto)
        {
            if (updateProductDto == null)
            {
                return BadRequest(ModelState);
            }

            var product = _productRepository.GetProduct(productId);
            if (product == null)
            {
                return NotFound($"El producto con id {productId} no existe");
            }

            // Validar categoría
            if (!_categoryRepository.CategoryExists(updateProductDto.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"La categoria {updateProductDto.CategoryId} no existe");
                return BadRequest(ModelState);
            }



            // Mapear cambios del DTO
            _mapper.Map(updateProductDto, product);

            //Agregando imagen
            if (updateProductDto.Image != null)
            {
                string fileName = product.ProductId + Guid.NewGuid().ToString() + Path.GetExtension(updateProductDto.Image.FileName);
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductsImages");

                //Válida que exista el directorio
                if (!Directory.Exists(imagesFolder))
                {
                    //Crea el directorio
                    Directory.CreateDirectory(imagesFolder);
                }

                var filePath = Path.Combine(imagesFolder, fileName);

                FileInfo file = new FileInfo(filePath);

                //Comprobar que no exista ya esa imagen
                if (file.Exists)
                {
                    file.Delete();
                }

                //El using asegura que un recurso se libere automáticamente cuando deja de usarse.
                using var fileStream = new FileStream(filePath, FileMode.Create); //Creamos el archivo
                updateProductDto.Image.CopyTo(fileStream);
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                product.ImgUrl = $"{baseUrl}/ProductsImages/{fileName}";
                product.ImgUrlLocal = filePath;
            }
            else
            {
                product.ImgUrl = "https://placehold.co/300x300";
            }
            
            if (!_productRepository.UpdateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al actualizar el registro {product.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }


                // GETBYID
        [HttpDelete("{productId:int}", Name = "DeleteProduct")] //El Name sirve para que sea invocado por otro método
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status204NoContent)]//Métadata para swagger
        public IActionResult DeleteProduct(int productId)
        {
            if (productId == 0)
            {
                return BadRequest(ModelState);
            }

            var product = _productRepository.GetProduct(productId);
            if (product == null)
            {
                return NotFound($"El producto con el {productId} no existe");
            }

            if (!_productRepository.DeleteProduct(product))
            {
                return NotFound($"Algo salió mal eliminar el registro: {productId}");
            }

            
            return NoContent();
        }




    }


}