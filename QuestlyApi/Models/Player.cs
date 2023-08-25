using Microsoft.AspNetCore.Identity;

public class Player : IdentityUser
{
    public string? FirstName { get; set; } // First name of the player
    public string? LastName { get; set; } // Last name of the player
    public bool IsEmailConfirmed { get; set; } // Flag to indicate if the email is confirmed
    public string? ResetPasswordToken { get; set; } // Token for resetting the password
    public DateTime? ResetPasswordTokenExpiration { get; set; } // Expiration time for the reset password token
    // public string? DisplayName { get; set; } // The display name for the player
    // public int Level { get; set; } // The player's level in the gamification system
    // public int ExperiencePoints { get; set; } // Experience points for leveling up
    // public string? AvatarUrl { get; set; } // URL for the player's avatar image
}