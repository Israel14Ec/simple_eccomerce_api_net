using System;

namespace ApiEcommerce.Models.Dtos;

public class UpdateProductDto
{

    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImgUrl { get; set; }

    public string? ImgUrlLocal { get; set; }

    public IFormFile? Image { get; set; } //propiedad para subir archivos

    public string SKU { get; set; } = string.Empty; //
    public int Stock { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public DateTime? UpdateDate { get; set; } = null;
    public int CategoryId { get; set; }
}
