using System.Text;
using ApiEcommerce.Constants;
using ApiEcommerce.Mapping;
using ApiEcommerce.Models;
using ApiEcommerce.Repository;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var dbConectionString = builder.Configuration.GetConnectionString("ConexionSql"); //Toma la cádena de conexión del appsettings.json
var secretKey = builder.Configuration.GetValue<string>("ApiSettings:SecretKey") ?? "";

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(dbConectionString)); //Conecta con la DB

//Caching en middleware HTTP
builder.Services.AddResponseCaching(options =>
{
  options.MaximumBodySize = 1024 * 1024; //Espacio para guardar en cache, 1mb
  options.UseCaseSensitivePaths = true;
});

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>(); //Registrar la interfaz y el repositorio
builder.Services.AddScoped<IProductRepository, ProductRepository>(); //Registramos la interfaz y su implementación
builder.Services.AddScoped<IUserRepository, UserRepository>(); //Registramos la interfaz y su implementación
builder.Services.AddAutoMapper(cfg => { },
    typeof(CategoryProfile).Assembly,
    typeof(ProductProfile).Assembly,
    typeof(UserProfile).Assembly
);

/* AÑADIMOS IDENTITY: para autenticación/autorización */
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
  .AddEntityFrameworkStores<ApplicationDbContext>()
  .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
      // Especifica que el esquema predeterminado de autenticación y desafío será JWT (Bearer Token)
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }
).AddJwtBearer(options =>
{
  // Desactiva la obligación de usar HTTPS para validar el token.
  options.RequireHttpsMetadata = false;

  // Guarda el token JWT validado dentro del contexto de autenticación.
  // Esto permite acceder a la información del token (claims, usuario, etc.) desde HttpContext.
  options.SaveToken = true;

  //Configura las reglas de validación del token JWT
  options.TokenValidationParameters = new TokenValidationParameters
  {
    // ✅ Indica que se debe validar la firma del token (para confirmar que no fue alterado)
    ValidateIssuerSigningKey = true,

    //  Clave secreta usada para firmar y validar el token.
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

    // 🚫 Desactiva la validación del “Issuer” (emisor del token),
    // ya que en este caso no estamos definiendo un servidor emisor concreto.
    ValidateIssuer = false,

    // 🚫 Desactiva la validación del “Audience” (audiencia),
    // útil si no estás usando tokens destinados a múltiples clientes.
    // Si lo activas, deberías definir: ValidAudience = "nombre_cliente"
    ValidateAudience = false //
  };
});

//Agregamos Perfiles de cache
builder.Services.AddControllers(option =>
{
  option.CacheProfiles.Add(CacheProfiles.Default10, CacheProfiles.Profile10);
  option.CacheProfiles.Add(CacheProfiles.Default20, CacheProfiles.Profile20);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var apiVerioningBuilder = builder.Services.AddApiVersioning(option =>
{
  option.AssumeDefaultVersionWhenUnspecified = true; //Habilitar opción por defecto en caso de que no se mande
  option.DefaultApiVersion = new ApiVersion(1, 0); //Parametro: primero grupo: segundo opcional
  option.ReportApiVersions = true; //Ayuda para debugging, saber que versiones disponibles hay
  
  //Se agrega la version como queryParam ?api-version={version}
  // option.ApiVersionReader = ApiVersionReader.Combine(new QueryStringApiVersionReader("api-version")); 
});


apiVerioningBuilder.AddApiExplorer(option =>
{
  option.GroupNameFormat = "'v'VVV"; //v1,v2,v3 --FORMATO de la API
  option.SubstituteApiVersionInUrl = true; //api/v{version}/ -> Añade la versión de la api en la URL de la ruta

});

//Configurar las CORS
builder.Services.AddCors(options =>
    {   
        //Creamos la política
        options.AddPolicy("AllowSpecificOrigin",    
            builder =>
            {
                builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader(); 
            }
        );
    }
);
builder.Services.AddSwaggerGen(
  options =>
  {
      //Añadiendo un schema de seguridad que lo llamamos Bearer
      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
      {

          Description = "Nuestra API utiliza la Autenticación JWT usando el esquema Bearer. \n\r\n\r" +
                      "Ingresa la palabra a continuación el token generado en login.\n\r\n\r" +
                      "Ejemplo: \"12345abcdef\"",
          Name = "Authorization", //Especificamos que se enviá en el Authorizacion
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.Http,
          Scheme = "Bearer" //Schema para JWT
      });

    //Como swagger tiene que acceder a los endpoints de la API
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
      {
        new OpenApiSecurityScheme
        {
          Reference = new OpenApiReference
          {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer" //Definicion del SecurityDefinition
          },
          Scheme = "oauth2",
          Name = "Bearer",
          In = ParameterLocation.Header
        },
        new List<string>()
      }
    });
    //Añadir versionamiento a swagger
    options.SwaggerDoc("v1", new OpenApiInfo
    {
      Version = "v1",
      Title = "API Ecommerce",
      Description = "API para gestionar productos y usuarios",
      TermsOfService = new Uri("https://protfolioistdev.vercel.app/"),
      Contact = new OpenApiContact
      {
        Name = "DevTalles",
        Url = new Uri("https://devtalles.com")
      },
      License = new OpenApiLicense
      {
        Name = "Licencia de uso",
        Url = new Uri("https://protfolioistdev.vercel.app/"),
      }
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
      Version = "v2",
      Title = "API Ecommerce",
      Description = "API para gestionar productos y usuarios",
      TermsOfService = new Uri("https://protfolioistdev.vercel.app/"),
      Contact = new OpenApiContact
      {
        Name = "DevTalles",
        Url = new Uri("https://devtalles.com")
      },
      License = new OpenApiLicense
      {
        Name = "Licencia de uso",
        Url = new Uri("https://protfolioistdev.vercel.app/"),
      }
    });
  }
);

//Instancia de web aplication
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI(options =>
  {
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");//AGREGA EL SELECT EN SWAGGER 
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");//AGREGA EL SELECT EN SWAGGER 
  });

}

//Habilitar archivos estáticos
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors(PolicyNames.AllowSpecificOrigin); //Configuración del cors, middleware

app.UseResponseCaching();

app.UseAuthentication(); //Middleware de autenticación

app.UseAuthorization();

app.MapControllers();

app.Run(); //Iniciar la aplicación
