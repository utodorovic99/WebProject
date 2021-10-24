using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravelAgency.Models
{
    public class ReservationsResp
    {
        public Dictionary<string, List<Accommodation>> Incoming { get; set; }
        public Dictionary<string, List<Accommodation>> Canceled { get; set; }
        public Dictionary<string, List<Accommodation>> Ongoing { get; set; }
        public Dictionary<string, List<Accommodation>> Passed { get; set; }

        public ReservationsResp()
        {
            Incoming = new Dictionary<string, List<Accommodation>>();
            Canceled = new Dictionary<string, List<Accommodation>>();
            Ongoing = new Dictionary<string, List<Accommodation>>();
            Passed = new Dictionary<string, List<Accommodation>>();
        }

    }
}