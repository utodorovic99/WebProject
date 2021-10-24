using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class Tourist: User
    {
        public List<Reservation> Reservations { get; set; }
        public Tourist(User user)
        {
            base.Username = user.Username;
            base.Password = user.Password;
            base.Name = user.Name;
            base.Surname = user.Surname;
            base.Gender = user.Gender;
            base.Email = user.Email;
            base.BirthDate = user.BirthDate;
            base.Role = user.Role;

            Reservations = new List<Reservation>();
        }

        public void AddReservation(Reservation newReservation) { Reservations.Add(newReservation); }
        public void AddReservation(List<Reservation> newReservation) { Reservations.AddRange(newReservation); }
        public override string ToString()
        {
            string outStr = "\n\nReservations:\n";
            foreach (var reservation in Reservations) outStr += reservation.ToString() + "\n";
            return base.ToString() + outStr ;
        }
    }
}