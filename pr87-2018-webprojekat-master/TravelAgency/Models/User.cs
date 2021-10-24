using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public enum EUserType { Tourist, Manager, None, Administrator};
    /*
        admin password is: 12345admin67890 via 224 hash size, 64 word size, SHA-3 function, MSB
    */
    public class User
    {

        public string Username  { get; set; }
        public string Password  { get; set; }   //Hash value
        public string Name      { get; set; }
        public string Surname   { get; set; }
        public string Gender    { get; set; }
        public string Email     { get; set; }
        public string BirthDate { get; set; }   // dd/MM/yyyy
        public string Role      { get; set; }

        public User() {; }

        public User(string username, string password, string name , string surname ,
        string gender , string email , string birthDate , string role)
        {
            this.Username=username;
            this.Password= password;
            this.Name= name;
            this.Surname= surname;
            this.Gender= gender;
            this.Email= email;
            this.BirthDate= birthDate;
            this.Role= role;
        }

        public bool IsEmpty()
        {
            return  this.Username == ""  && this.Password == "" && this.Name == "" &&
                    this.Surname == ""   && this.Gender == ""   && this.Email == "" &&
                    this.BirthDate == "" && this.Role == "";
        }

        public override string ToString()
        {
            return String.Format("Username: {0}\nPassword: {1}\nName: {2}\nSurname: {3}\nGender: {4}\nEmail: {5}\nBirthdate{6} Role: {7}",
                Username, Password, Name, Surname, Gender, Email, BirthDate, Role);
        }

    }
}