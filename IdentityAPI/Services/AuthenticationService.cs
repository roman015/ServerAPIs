using IdentityAPI.Helpers;
using IdentityAPI.Models;
using IdentityAPI.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityAPI.Services
{
    public interface IAuthenticationService
    {
        Account ConfirmUserOtp(string email, string otp);
        bool CreateNewAccount(Account newAccount, string adminOtp);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAccountRepository AccountRepository;
        private readonly ITotpHelper TotpHelper;
        private readonly IConfiguration Configuration;

        public AuthenticationService(
            ITotpHelper TotpHelper,
            IAccountRepository AccountRepository,
            IConfiguration Configuration)
        {
            this.TotpHelper = TotpHelper;
            this.AccountRepository = AccountRepository;
            this.Configuration = Configuration;
        }

        public Account ConfirmUserOtp(string email, string otp)
        {
            Account result = null;

            Account account = AccountRepository.FindAccountByEmail(email);

            if (account != null && TotpHelper.ConfirmSecret(otp, account.TOTPKey))
            {
                result = account;
            }

            return result;
        }
        
        public bool CreateNewAccount(Account newAccount, string adminOtp)
        {
            bool result = false;

            // Approved by admin only 
            if (TotpHelper.ConfirmSecret(adminOtp, Configuration["AdminTOTPKey"]))
            {
                Account account = AccountRepository.Create(newAccount);               
                result = (account != null);
            }

            return result;
        }
    }
}
