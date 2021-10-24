using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class ArrangementNSC
    {
        public string DateStart { get; set; }
        public string DateStop { get; set; }
        public ushort Days { get; set; }
        public string PosterURL { get; set; }
        public string MinPrice { get; set; }
        public string Name { get; set; }

        public string Transport { get; set; }
        public string Type { get; set; }

        public ArrangementNSC(){ }

        public ArrangementNSC(string dateStart, string dateStop, ushort days, string poseterURL, string minPrice, string name)
        {
            DateStart = dateStart;
            DateStop = dateStop;
            Days = days;
            PosterURL = poseterURL;
            MinPrice = minPrice;
            Name = Name;
        }
    }
}