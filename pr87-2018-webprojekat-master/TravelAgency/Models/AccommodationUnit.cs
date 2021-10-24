using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class AccommodationUnit
    {
        public AccommodationUnit(string uID, int maxGuessts, bool petAllowed, int price, bool available)
        {
            UID = uID;
            MaxGuessts = maxGuessts;
            PetAllowed = petAllowed;
            Price = price;
            Available = available;
        }

        public AccommodationUnit()
        {

        }

        public string UID { get; set; }
        public int MaxGuessts { get; set; }
        public bool PetAllowed { get; set; }
        public int Price { get; set; }
        public bool Available { get; set; }

        public override string ToString()
        {
            return String.Format("UID: {0}\nMaxGuessts: {1}\nPetAllowed: {2}\nPrice: {3}", UID, MaxGuessts, PetAllowed, Price);
        }
    }
}