using Microsoft.AspNetCore.Identity;

namespace MikadoNet.Models;

public class AppUser : IdentityUser
{
    public short? TantoCode { get; set; }
}
