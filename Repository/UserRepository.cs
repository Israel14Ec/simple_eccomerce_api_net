using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ApiEcommerce.Repository;

public class UserRepository : IUserRepository
{
    public readonly ApplicationDbContext _db;
    private string? secretKey;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMapper _mapper;

    public UserRepository(
        ApplicationDbContext db,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMapper mapper
    )
    {
        _db = db;
        secretKey = configuration.GetValue<string>("ApiSettings:SecretKey");
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
    }
    public ApplicationUser? GetUser(string id)
    {
        return _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
    }

    public ICollection<ApplicationUser> GetUsers()
    {
        return _db.ApplicationUsers.OrderBy(u => u.UserName).ToList();
    }

    public bool IsUniqueUser(string username)
    {
        return !_db.Users.Any(u => u.Username.ToLower().Trim() == username.ToLower().Trim());

    }


    //Login Personalizado ---------------
    // public async Task<UserLoginResponseDto> Login(UserLoginDto userLoginDto)
    // {
    //     if (string.IsNullOrEmpty(userLoginDto.Username))
    //     {
    //         return new UserLoginResponseDto()
    //         {
    //             Token = "",
    //             User = null,
    //             Message = "El usuario no existe"
    //         };
    //     }

    //     var user = await _db.Users.FirstOrDefaultAsync<User>(u => u.Username.ToLower().Trim() == userLoginDto.Username.ToLower().Trim());
    //     if (user == null)
    //     {
    //         return new UserLoginResponseDto()
    //         {
    //             Token = "",
    //             User = null,
    //             Message = "Username no encontrado"
    //         };
    //     }

    //     if (!BCrypt.Net.BCrypt.Verify(userLoginDto.Password, user.Password))
    //     {
    //         return new UserLoginResponseDto()
    //         {
    //             Token = "",
    //             User = null,
    //             Message = "Credenciales incorrectas"
    //         };
    //     }

    //     //JWT
    //     var handlerToken = new JwtSecurityTokenHandler();

    //     if (string.IsNullOrWhiteSpace(secretKey))
    //     {
    //         throw new InvalidOperationException("Secret key no está configurada");
    //     }

    //     var key = Encoding.UTF8.GetBytes(secretKey); //Soporte para caracteres especiales, codificación (UTF8)
    //     var tokenDescriptor = new SecurityTokenDescriptor
    //     {
    //         Subject = new ClaimsIdentity(new[]
    //         {
    //             new Claim("id", user.Id.ToString()),
    //             new Claim("username", user.Username.ToString()),
    //             new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
    //         }),
    //         Expires = DateTime.UtcNow.AddDays(2), //Expira en 2 horas
    //         //Firmar credenciales
    //         SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    //     };

    //     var token = handlerToken.CreateToken(tokenDescriptor);

    //     return new UserLoginResponseDto()
    //     {
    //         Token = handlerToken.WriteToken(token),
    //         User = new UserRegisterDto()
    //         {
    //             Username = user.Username,
    //             Name = user.Name,
    //             Role = user.Role,
    //             Password = user.Password ?? ""
    //         },
    //         Message= "USUARIO LOGUREADO CORRECTAMENTE"
    //     };
    // }

    //Login usando Identity
    public async Task<UserLoginResponseDto> Login(UserLoginDto userLoginDto)
    {
        if (string.IsNullOrEmpty(userLoginDto.Username))
        {
            return new UserLoginResponseDto()
            {
                Token = "",
                User = null,
                Message = "El usuario no existe"
            };
        }

        var user = await _db.ApplicationUsers.FirstOrDefaultAsync<ApplicationUser>(
            u => u.UserName != null && u.UserName.ToLower().Trim() ==
            userLoginDto.Username.ToLower().Trim()
        );

        if (user == null)
        {
            return new UserLoginResponseDto()
            {
                Token = "",
                User = null,
                Message = "Username no encontrado"
            };
        }

        if (userLoginDto.Password == null)
        {
            return new UserLoginResponseDto()
            {
                Token = "",
                User = null,
                Message = "Password requerido"
            };
        }

        //Valida las credenciales
        bool isValid = await _userManager.CheckPasswordAsync(user, userLoginDto.Password);

        if (!isValid)
        {
            return new UserLoginResponseDto()
            {
                Token = "",
                User = null,
                Message = "Credenciales incorrectas"
            };
        }

        //JWT
        var handlerToken = new JwtSecurityTokenHandler();

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Secret key no está configurada");
        }
        var roles = await _userManager.GetRolesAsync(user);
        var key = Encoding.UTF8.GetBytes(secretKey); //Soporte para caracteres especiales, codificación (UTF8)
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("username", user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? string.Empty),
            }),
            Expires = DateTime.UtcNow.AddDays(2), //Expira en 2 horas
            //Firmar credenciales
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handlerToken.CreateToken(tokenDescriptor);

        return new UserLoginResponseDto()
        {
            Token = handlerToken.WriteToken(token),
            User = _mapper.Map<UserDataDto>(user),
            Message = "USUARIO LOGUREADO CORRECTAMENTE"
        };
    }

    //Register para validación manual
    // public async Task<User> Register(CreateUserDto createUserDto)
    // {
    //     var encriptedPassword = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
    //     var user = new User()
    //     {
    //         Username = createUserDto.Username ?? "N/A",
    //         Name = createUserDto.Name,
    //         Role = createUserDto.Role,
    //         Password = encriptedPassword
    //     };

    //     _db.Users.Add(user);
    //     await _db.SaveChangesAsync();
    //     return user;

    // }

    //Register con identity
    public async Task<UserDataDto> Register(CreateUserDto createUserDto)
    {
        if (string.IsNullOrEmpty(createUserDto.Username))
        {
            throw new ArgumentNullException("El username es requerido");
        }

        if (string.IsNullOrEmpty(createUserDto.Password))
        {
            throw new ArgumentNullException("El password es requerido");
        }

        var user = new ApplicationUser()
        {
            UserName = createUserDto.Username,
            Email = createUserDto.Username,
            NormalizedEmail = createUserDto.Username.ToUpper(),
            Name = createUserDto.Name
        };


        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (result.Succeeded)
        {
            var userRole = createUserDto.Role ?? "User";
            var roleExist = await _roleManager.RoleExistsAsync(userRole); //Valida que el rol exsita
            if (!roleExist)
            {
                var identityRole = new IdentityRole(userRole);
                await _roleManager.CreateAsync(identityRole);
            }

            await _userManager.AddToRoleAsync(user, userRole);

            //Busca por el registro recién creado
            var createdUser = _db.ApplicationUsers.FirstOrDefault(u => u.UserName == createUserDto.Username);
            return _mapper.Map<UserDataDto>(createdUser);

        }
        //Mostrar los errores
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new ApplicationException($"No se pudo realizar el registro: {errors}");

    }
}
