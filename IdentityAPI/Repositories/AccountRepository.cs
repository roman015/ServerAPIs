using IdentityAPI.Helpers;
using IdentityAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityAPI.Repositories
{
    public interface IAccountRepository
    {
        Account FindAccountByEmail(string email);
        Account Create(Account newAccount);
    }

    public class AccountRepository : IAccountRepository
    {
        private readonly IdentityContext Context;
        private readonly ITotpHelper TotpHelper;
        public AccountRepository(IdentityContext Context, ITotpHelper TotpHelper)
        {
            this.Context = Context;
            this.TotpHelper = TotpHelper;
        }

        public Account Create(Account newAccount)
        {
            Account temp = new Account()
            {
                TOTPKey = TotpHelper.GenerateRandomSecret(),
                Email = newAccount.Email
            };
            AccountDetail tempDetail = new AccountDetail()
            {
                Nickname = newAccount.Details.Nickname
            };

            Context.Accounts.Add(temp);
            Context.SaveChanges();

            tempDetail.UserID = temp.UserID;
            Context.AccountDetails.Add(tempDetail);
            Context.SaveChanges();

            return Context.Accounts
                .Include(acc => acc.Details)
                .Where(acc => acc.UserID.Equals(temp.UserID))
                .SingleOrDefault();
        }

        public Account FindAccountByEmail(string email)
        {
            return Context.Accounts
                    .Where(acc => acc.Email.Equals(email))
                    .SingleOrDefault();
        }
    }
}
