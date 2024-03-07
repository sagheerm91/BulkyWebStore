using BulkyWeb.Data;
using BulkyWeb.Models;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics.Metrics;

namespace BulkyWeb.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer( 
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async void Initialize()
        {
            // migrate if any pending migrations
            try
            {
                if(_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch(Exception ex )
            {

            }

            // create roles if they are not created
            if (!_roleManager.RoleExistsAsync("Customer").GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole("Customer")).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole("Employee")).GetAwaiter().GetResult();


                // if roles are not created then create an admin role

                    ApplicationUser user = new ApplicationUser
                    {
                        UserName = "admin@gmail.com",
                        Email = "admin@gmail.com",
                        Name = "Sahir",
                        PhoneNumber = "03430360010",
                        StreetAddress = "Galli Qasim Jan",
                        City = "Delhi",
                        State = "Pata ni",
                        PostalCode = "1234567"
                    };

                    var result = await _userManager.CreateAsync(user, "Admin123*");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                
            }
            return;
            
        }
    }
}
