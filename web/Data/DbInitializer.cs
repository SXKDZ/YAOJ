using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YAOJ.Models;

namespace YAOJ.Data
{
    public class DbInitializer
    {
        public static void Initialize(DataContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return;  // DB has been seeded
            }

            var defaultUser = new User
            {
                Name = "Admin",
                Password = "123456",
                AcceptanceCount = 0,
                IsAdmin = true
            };
            context.Users.Add(defaultUser);
            context.SaveChanges();
        }
    }
}
