using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class Accommodation
    {
        public ulong AcID { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Stars { get; set; }
        public bool Pool { get; set; }
        public bool Spa { get; set; }
        public bool DisabledCompatible { get; set; }
        public bool WiFi { get; set; }
        public List<AccommodationUnit> Units { get; set; }

        public Accommodation(ulong acID, string location, string type, string name, string stars, bool pool, bool spa, bool disabledCompatible, bool wiFi, List<AccommodationUnit> units)
        {
            AcID = acID;
            Location = location;
            Type = type;
            Name = name;
            Stars = stars;
            Pool = pool;
            Spa = spa;
            DisabledCompatible = disabledCompatible;
            WiFi = wiFi;
            this.Units = units;
        }

        public Accommodation() { Units = new List<AccommodationUnit>(); }

        public override string ToString()
        {
            string unitsStr = "";
            foreach (var unit in Units) unitsStr += unit.ToString() + "\n";

            return String.Format("ACID: {0}\nLocation: {1}\nType: {2}\n Name: {3}\nStars: {4}\nPool: {5}\nSpa: {6}\nDisabledCompatible: {7}\nWiFi: {8}\nUnits: \n{9}\n",
                                  AcID, Location, this.Type, Name, Stars, Pool, Spa, DisabledCompatible, WiFi, unitsStr);
        }

    }
}