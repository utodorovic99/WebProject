using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class Manager : User
    {
        public List<Arrangement> Arrangements { get; set;}
        public Manager(User user)
        {
            base.Username = user.Username;
            base.Password = user.Password;
            base.Name = user.Name;
            base.Surname = user.Surname;
            base.Gender = user.Gender;
            base.Email = user.Email;
            base.BirthDate = user.BirthDate;
            base.Role = user.Role;

            Arrangements = new List<Arrangement>();
        }

        public void AddArrangement(Arrangement newArrangement) { Arrangements.Add(newArrangement); }
        public void AddArrangement(List<Arrangement> newArrangement) { Arrangements.AddRange(newArrangement); }
        public override string ToString()
        {
            string outStr = "\n\nArrangements:\n";
            foreach (var arrangement in Arrangements) outStr += arrangement.ToString() + "\n";
            return base.ToString() + outStr;
        }

    }
}