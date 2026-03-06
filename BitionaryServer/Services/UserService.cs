using BitionaryServer.Data;
using BitionaryServer.Models;

namespace BitionaryServer.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string id)
    {
        return await GetUserByIdAsync(new Guid(id));
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(Guid id)
    {
        return await _db.Users.FindAsync(id);
    }
}