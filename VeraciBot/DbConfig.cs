using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using VeraciBot.Data;

namespace VeraciBot
{
    public class DbConfig
    {
        private readonly VeraciDbContext _dbContext;

        public DbConfig(VeraciDbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            _dbContext = dbContext;
        }

        public async Task<DateTime> GetLastDateTimeForTwitterCheck()
        {
            Config lastCheck = await _dbContext.Configs.FirstOrDefaultAsync(e =>
                e.Id == "TWIT_last_check"
            );
            if (lastCheck is null)
            {
                lastCheck = new Config()
                {
                    Id = "TWIT_last_check",
                    Value = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                };
                _dbContext.Configs.Add(lastCheck);
                _dbContext.SaveChanges();
            }

            return DateTime.Parse(lastCheck.Value);
        }

        public async Task SetLastDateTimeForTwitterCheck(DateTime last)
        {
            Config lastCheck = await _dbContext.Configs.FirstOrDefaultAsync(e =>
                e.Id == "TWIT_last_check"
            );
            if (lastCheck is null)
            {
                lastCheck = new Config()
                {
                    Id = "TWIT_last_check",
                    Value = last.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                };
                _dbContext.Configs.Add(lastCheck);
                _dbContext.SaveChanges();
                return;
            }

            lastCheck.Value = last.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _dbContext.Configs.Update(lastCheck);
            _dbContext.SaveChanges();
        }
    }
}
