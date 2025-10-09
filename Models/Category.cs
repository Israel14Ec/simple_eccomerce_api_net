using System.ComponentModel.DataAnnotations;
using ApiEcommerce.Models;

public class Category
{
    [Key] //Llave primera, son decoradores
    public int Id { get; set; }
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "La descripcion es requerid")]
    public DateTime CreationDate { get; set; }

    //Relación inversa con la tabla Product
    public required ICollection<Product> Products { get; set; } 
}