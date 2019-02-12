using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityAPI.Models
{
    public class Account
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }
        public string Email { get; set; }
        public string TOTPKey { get; set; }

        public AccountDetail Details { get; set; }
    }

    public class AccountDetail
    {
        public int UserID { get; set; }
        public string Nickname { get; set; }
    }
}
