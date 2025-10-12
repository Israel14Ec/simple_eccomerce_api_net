using ApiEcommerce.Constants;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Controllers.V1
{
    [Authorize(Roles = "Admin")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")] //El controlador acepta las dos versiones
    // [ApiVersion("2.0")] //El controlador acepta las dos versiones
    [ApiController]
    // [EnableCors(PolicyNames.AllowSpecificOrigin)] //Política cors aplicado a nivel de controlador
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoriesController(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        //GET ALL
        [AllowAnonymous] //Permite hacer la petición sin estar autenticados
        [HttpGet] //Método HTTP
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status200OK)]//Métadata para swagger
        // [EnableCors(PolicyNames.AllowSpecificOrigin)] //Política cors aplicado a nivel de método
        // [MapToApiVersion("1.0")] //Especificamos que es para la versión 1.0
        [Obsolete("Método obsoleto, use la v2")] //Marca como obseleto este endpoint
        public IActionResult GetCategories()
        {
            var categories = _categoryRepository.GetCategories(); //Instancia del repositorio
            var categoriesDto = new List<CategoryDto>(); //Lista de categories DTO
            //Pasa por automapper, para formatear la respuesta como lo definido en el DTO
            foreach (var category in categories)
            {
                categoriesDto.Add(_mapper.Map<CategoryDto>(category));
            }

            return Ok(categoriesDto);
        }

        // GETBYID
         [AllowAnonymous]
        [HttpGet("{id:int}", Name = "GetCategoryById")] //El Name sirve para que sea invocado por otro método
        //Peticion guardada en cache
        [ResponseCache(CacheProfileName = CacheProfiles.Default10)]
        // [ResponseCache(
        //     Duration = 20 //10 segundos de duración
        // )] 
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status200OK)]//Métadata para swagger
        public IActionResult GetCategoryById(int id)
        {
            Console.WriteLine($"Categoría con el ID: {id} a las {DateTime.Now}");
            var category = _categoryRepository.GetCategory(id);
            Console.WriteLine($"Respuesta con el ID: {id}");
            if (category == null)
            {
                return NotFound($"La categoria {id} no fue encontrada"); //Mensaje personalizado
            }
            var categoriesDto = _mapper.Map<CategoryDto>(category); //Convierte en el formato del DTO
            return Ok(categoriesDto);
        }


        // CREACIÓN
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status201Created)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]//Métadata para swagger
        public IActionResult CreateCategory([FromBody] CreateCategoryDto createCategoryDto) //Recibe el body del request
        {

            //Validar
            if (createCategoryDto == null)
            {
                return BadRequest(ModelState); //Devoldemos el estado en el que se encuentra el módelo
            }

            //Validar si ya existe la categoria
            if (_categoryRepository.CategoryExists(createCategoryDto.Name))
            {
                ModelState.AddModelError("CustomError", "La categoria ya existe");//Error personalizado
                return BadRequest(ModelState); //Lanzamos la excepción
            }

            var category = _mapper.Map<Category>(createCategoryDto);

            if (!_categoryRepository.CreateCategory(category))
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al guardar el registro {category.Name}");
                return StatusCode(500, ModelState);
            }

            return CreatedAtRoute("GetCategoryById", new { id = category.Id }, category); //Llama al método getById
        }

        [HttpPut("{id:int}", Name = "UpdateCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status400BadRequest)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]//Métadata para swagger
        public IActionResult UpdateCategory(int id, [FromBody] CreateCategoryDto updateCategoryDto) //Recibe el body del request
        {

            //Validar si existe
            if (!_categoryRepository.CategoryExists(id))
            {
                return NotFound($"La categoria {id} no fue encontrada"); //Me
            }

            //Validar datos no vacíos
            if (updateCategoryDto == null)
            {
                return BadRequest(ModelState); //Devoldemos el estado en el que se encuentra el módelo
            }

            //Validar si ya existe la categoria
            if (_categoryRepository.CategoryExists(updateCategoryDto.Name))
            {
                ModelState.AddModelError("CustomError", "La categoria ya existe");//Error personalizado
                return BadRequest(ModelState); //Lanzamos la excepción
            }

            var category = _mapper.Map<Category>(updateCategoryDto);
            category.Id = id;

            if (!_categoryRepository.UpdateCategory(category))
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al actualizar el registro {category.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        [HttpDelete("{id:int}", Name = "DeleteCategory")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]//Métadata para swagger
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]//Métadata para swagger
        public IActionResult DeleteCategory(int id)
        {

            //Valida que existe el id
            if (!_categoryRepository.CategoryExists(id))
            {
                return NotFound($"La categoria {id} no fue encontrada");
            }

            //Busca por el id
            var category = _categoryRepository.GetCategory(id);

            if (category == null)
            {
                return NotFound($"La categoria {id} no fue encontrada");
            }

            if (!_categoryRepository.DeleteCategory(category)) //eliminamos la categoría
            {
                ModelState.AddModelError("CustomError", $"Algo salió mal al eliminar el registro {category.Name}");
                return StatusCode(500, ModelState);
            }
            return NoContent();
        }
    }
}

