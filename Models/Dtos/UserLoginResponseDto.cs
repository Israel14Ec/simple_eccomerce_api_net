using System;

namespace ApiEcommerce.Models.Dtos;

public class UserLoginResponseDto
{

    //Propiedad usada en registro manual
    // public UserRegisterDto? User { get; set; }

    //Con identity
    public UserDataDto? User { get; set; }

    public string? Token { get; set; }
    public string? Message { get; set; }
}
