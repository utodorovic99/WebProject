using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class ArrangementAccomodationNameTraslationReq
    {
        public ArrangementAccomodationNameTraslationReq(string arrangementName, string accommodationName)
        {
            ArrangementName = arrangementName;
            AccommodationName = accommodationName;
        }

        public string ArrangementName { get; set; }
        public string AccommodationName { get; set; }
    }
}