using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class Comment
    {
        public string UID    { get; set; }
        public string AID     { get; set; }
        public string Txt    { get; set; }
        public string Rating { get; set; }
        public string Status { get; set; }

        public Comment() { }
        public Comment(string uID, string aID, string txt, string rating, string status)
        {
            UID = uID;
            AID = aID;
            Txt=txt;
            Rating = rating;
            Status = status;
        }

        public override string ToString()
        {
            return String.Format("UID: {0}\nAID: {1}\nTxt: \n{2}\n\nRating: {3}", UID, AID, Txt, Rating);
        }
    }
}