using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace kadroff.Components.Data
{
    public class DatabaseSeeder
    {
        private readonly AppDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(AppDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await _context.Database.MigrateAsync();

            if (!await _context.Attributes.AnyAsync())
            {
                var attrs = new List<AppAttribute>
                {
                    new() { Name = "C# / .NET", Category = "Hard Skills", DataType = "Boolean", Description = "Владение стеком .NET" },
                    new() { Name = "PostgreSQL", Category = "Hard Skills", DataType = "Boolean", Description = "Опыт работы с PostgreSQL" },
                    new() { Name = "English Level", Category = "Soft Skills", DataType = "String", Description = "Уровень английского языка" }
                };

                await _context.Attributes.AddRangeAsync(attrs);
                await _context.SaveChangesAsync();
            }

            if (!await _context.Positions.AnyAsync())
            {
                var csharpAttr = await _context.Attributes.FirstAsync(a => a.Name == "C# / .NET");

                var position = new Position
                {
                    Title = "Backend C# Developer",
                    Description = "Разработка высоконагруженных сервисов на .NET 8 и EF Core.",
                    MaxProjects = 5,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Positions.Add(position);
                await _context.SaveChangesAsync();

                _context.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = position.Id,
                    AttributeId = csharpAttr.Id
                });

                await _context.SaveChangesAsync();
            }
        }
    }
}
