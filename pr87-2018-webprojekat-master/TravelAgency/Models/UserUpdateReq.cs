using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class UserUpdateReq
    {
        public UserUpdateReq(string username, string password, string passwordNew1, string passwordNew2, string name, string surname, string gender, string email, string birthDate, string role, string usernameNew)
        {
            Username = username;
            Password = password;
            PasswordNew1 = passwordNew1;
            PasswordNew2 = passwordNew2;
            Name = name;
            Surname = surname;
            Gender = gender;
            Email = email;
            BirthDate = birthDate;
            Role = role;
            this.UsernameNew = usernameNew;
        }

        public UserUpdateReq() { } 
        

        public string Username { get; set; }
        public string UsernameNew { get; set; }
        public string Password { get; set; }   //Hash value
        public string PasswordNew1 { get; set; }
        public string PasswordNew2 { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string BirthDate { get; set; }   // dd/MM/yyyy
        public string Role { get; set; }

        public bool IsEmpty()
        {
            return this.Username == "" && this.Password == "" && this.Name == "" &&
                    this.Surname == "" && this.Gender == "" && this.Email == "" &&
                    this.BirthDate == "" && this.Role == "" && this.PasswordNew1=="" &&
                    this.PasswordNew2=="" && this.UsernameNew=="";
        }
    }
}