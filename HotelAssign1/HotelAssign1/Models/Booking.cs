//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HotelAssign1.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Booking
    {
        public int BookingId { get; set; }
        public int RestaurantId { get; set; }
        public string EmailId { get; set; }
        public Nullable<System.DateTime> BookingDateTime { get; set; }
        public int Spots { get; set; }
        public string RestaurantName { get; set; }
    
        public virtual Restaurant Restaurant { get; set; }
    }
}
