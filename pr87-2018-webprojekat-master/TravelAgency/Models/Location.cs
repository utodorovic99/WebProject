using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class Location
    {
        public string Address       { get; set; }
        public double GeoLongitude  { get; set; }
        public double GeoLatitude   { get; set; }

        public Location() { }
        public Location (string address, double geoLongitude, double geoLatitude)
        {
            Address = address;
            GeoLatitude = geoLatitude;
            GeoLongitude = GeoLongitude;
        }

        public override string ToString()
        {
            return String.Format("Address: {0}\nGeoLongitude: {1}\nGeoLatitude: {2}", Address, GeoLongitude, GeoLatitude);
        }
    }
}