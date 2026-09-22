using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PasskeyAuthAspNet.DataAccess;

public class PasskeyAuthAppDbContext : IdentityDbContext<IdentityUser>
{
    public PasskeyAuthAppDbContext(DbContextOptions<PasskeyAuthAppDbContext> options) : base(options)
    {
    }
}
