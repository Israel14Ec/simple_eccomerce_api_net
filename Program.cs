using ApiEcommerce.Mapping;
using ApiEcommerce.Repository;
using ApiEcommerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);
var dbConectionString = builder.Configuration.GetConnectionString("ConexionSql"); //Toma la cádena de conexión del appsettings.json

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(dbConectionString)); //Conecta con la DB
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>(); //Registrar la interfaz y el repositorio
builder.Services.AddScoped<IProductRepository, ProductRepository>(); //Registramos la interfaz y su implementación
builder.Services.AddScoped<IUserRepository, UserRepository>(); //Registramos la interfaz y su implementación
builder.Services.AddAutoMapper(cfg => { },
    typeof(CategoryProfile).Assembly,
    typeof(ProductProfile).Assembly,
    typeof(UserProfile).Assembly
);

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Instancia de web aplication
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run(); //Iniciar la aplicación
