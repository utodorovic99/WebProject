using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class LoginReq
    {
        public LoginReq(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public LoginReq() {}

        public string Username { get; set; }
        public string Password { get; set; }
    }
}