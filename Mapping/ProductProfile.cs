using System;
using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using AutoMapper;
using Microsoft.Data.SqlClient;

namespace ApiEcommerce.Mapping;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        // Producto con Category completo
        CreateMap<Product, ProductByIdDto>().ReverseMap();

        // Producto con solo el CategoryName
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ReverseMap();
        CreateMap<Category, CategoryDto>().ReverseMap();

        CreateMap<Product, CreateProductDto>().ReverseMap();
        CreateMap<Product, UpdateProductDto>().ReverseMap();
    }
}
