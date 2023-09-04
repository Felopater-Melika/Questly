namespace QuestlyApi.Dtos;

public class LoginDto
{
    public LoginDto(string email, string password)
    {
        Email = email;
        Password = password;
    }

    public string Email { get; }
    public string Password { get; }
}