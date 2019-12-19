using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HotelAssign1.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace HotelAssign1.Controllers
{
   
    public class ChartController : Controller
    {
        private HotelAssign1Entities db = new HotelAssign1Entities(); //creating a dbcontext object
        
        //An action to show the dynamic charts. It will show a bar chart and a line chart.
        //The charts will show the various restaurants and their ratings. Once clicked on the bar chart
        //of a restaurant, the total number of bookings done at that restaurant will be shown.
        // I am using chart.js library to show the charts.
        public ActionResult Dashboard()
        {
            var list = db.Restaurants.Select(x => x);
            var ratings = db.Restaurants.Select(x => x.RestaurantRating);
             Dictionary<string, string> bookings = new Dictionary<string, string>(); //create a dictionary to store the restaurant names and the total number of bookings done at that restaurant.
            foreach (var item in list)
            {
                int spots = 0;
                var totalBookings = db.Bookings.Where(x => x.RestaurantId == item.RestaurantId).ToList();
                foreach (var spot in totalBookings)
                {
                    spots = spots + spot.Spots;  //calculate the total bookings done at that restaurant.
                }
                bookings.Add(item.RestaurantName, spots.ToString());
            }
            //pass data using viewbag and use chart.js to show the charts
            //code to show the charts taken from "chart.js" documentation.
            ViewBag.bookings = bookings;  //pass through viewbag to the view
            ViewBag.RestaurantNames = list.Select(x=>x.RestaurantName).ToList();
            ViewBag.Ratings = ratings;
            return View();
        }
       
    }
}
