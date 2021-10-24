using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class ReservedResp
    {
        public Dictionary<string, List<string>> Reservations { get; set; }

        public ReservedResp() { Reservations = new Dictionary<string, List<string>>(); }

        public void AddReservation(string accommodation, string unit)
        {
            if (!Reservations.ContainsKey(accommodation))
                    { Reservations[accommodation] = new List<string>(); Reservations[accommodation].Add(unit); }
            else    Reservations[accommodation].Add(unit);
        }
    }
}