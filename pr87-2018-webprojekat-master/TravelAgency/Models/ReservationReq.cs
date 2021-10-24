using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class ReservationReq
    {
        public ReservationReq(string arrangement, string accommodation, string unit, string user)
        {
            Arrangement = arrangement;
            Accommodation = accommodation;
            Unit = unit;
            User = user;
        }

        public ReservationReq()
        {

        }

        public string Arrangement { get; set; }
        public string Accommodation { get; set; }
        public string Unit { get; set; }
        public string User { get; set; }
    }
}