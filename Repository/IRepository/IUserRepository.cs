using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;

namespace ApiEcommerce.Repository.IRepository;

public interface IUserRepository
{


    //Interface para auth manual
    // ICollection<User> GetUsers();

    //Interface para auth con Identity
    ICollection<ApplicationUser> GetUsers();

       //Interface para auth manual
    //    User? GetUser(int id);

    //Interface para auth con Identity
    ApplicationUser? GetUser(string id);

    bool IsUniqueUser(string username);

    Task<UserLoginResponseDto> Login(UserLoginDto user);

    //Interface para registar manual
    // Task<User> Register(CreateUserDto createUserDto);
    
    //Interface para register con Identity
    Task<UserDataDto> Register(CreateUserDto createUserDto);
}
