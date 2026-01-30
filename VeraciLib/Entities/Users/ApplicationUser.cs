using Microsoft.AspNetCore.Identity;

namespace VeraciBot.Entities.Users
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string AuthorId { get; set; } = string.Empty;
    }
}
