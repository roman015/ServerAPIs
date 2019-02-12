using IdentityAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityAPI.Repositories
{
    public class IdentityContext : DbContext
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
        { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountDetail> AccountDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Account>().ToTable("Account")
                .HasKey(acc => acc.UserID);
            builder.Entity<AccountDetail>().ToTable("AccountDetail")
                .HasKey(accDetail => accDetail.UserID);

            builder.Entity<Account>()
                .HasOne(acc => acc.Details)
                .WithOne();

            base.OnModelCreating(builder);
        }
    }
}
