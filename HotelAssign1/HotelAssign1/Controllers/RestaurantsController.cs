using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;
using HotelAssign1.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace HotelAssign1.Controllers
{
    [Authorize]
    public class RestaurantsController : Controller
    {
        private HotelAssign1Entities db = new HotelAssign1Entities();   //db context object
        protected ApplicationDbContext ApplicationDbContext { get; set; }
        protected UserManager<ApplicationUser> UserManager { get; set; }  //created to get the email id of the current user

        public RestaurantsController()
        {
            this.ApplicationDbContext = new ApplicationDbContext();
            //created to get the email id of the current user, code taken from stackoverflow
            this.UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this.ApplicationDbContext));
        }
        


         /* GET Restaurants
         This action is called when the user clicks on the "Restaurant" tab. It is used to retreive 
         all the restaurants and the list is sent to the view.
         */
        public ActionResult Index()
        {
            //var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
            ViewBag.IsAdmin = false;
            var userDetails = UserManager.FindById(User.Identity.GetUserId());
            var ifMultipleRoles = userDetails.Roles.ToList();
            if (ifMultipleRoles.Count() > 0)
            {
                if (userDetails.Roles.ToList().FirstOrDefault().RoleId == "1")
                {
                    ViewBag.IsAdmin = true;
                }

            }
            return View(db.Restaurants.ToList());
        }


        /* GET: Restaurants/Details/5
         This action is called when the user clicks on the "Details" link. It shows the details 
         of that particular restaurant. It will further have the links to book a spot at that restaurant
         and rate the restaurant.
        */
        public ActionResult Details(int? id)
        {
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
            ViewBag.IsAdmin = false;
            ViewBag.BookingExists = false;
            var userDetails = UserManager.FindById(User.Identity.GetUserId());
            var ifMultipleRoles = userDetails.Roles.ToList();
            var bookings = db.Bookings.Where(x => x.RestaurantId == id && x.EmailId == EmailId).ToList();
            if (bookings.Count() > 0)
            {
                ViewBag.BookingExists = true;
            }
            if (ifMultipleRoles.Count() > 0)
            {
                if (userDetails.Roles.ToList().FirstOrDefault().RoleId == "1")
                {
                    ViewBag.IsAdmin = true;
                }
                
            }
            
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }
          
            return View(restaurant);
        }


        /* POST
         This action is called when the "Booking" button is clicked. It is a post action used to add 
         the booking entry in the "Booking" table. It is called via the ajax call.
        */
        [HttpPost]
        public ActionResult MakeBooking(String DateAndTime, String RestaurantId,String Spots)
        {
            var DateTime = Convert.ToDateTime(DateAndTime);
            var Restaurantid = Int32.Parse(RestaurantId);
            var RestaurantName = db.Restaurants.Where(x => x.RestaurantId == Restaurantid).Select(x => x.RestaurantName).FirstOrDefault();
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;   //code taken from stackoverflow when searched how to find emailid of currently logged in user.
            if (ModelState.IsValid)
            {
                var spotsSum = (Spots.Equals("") ? 1 : Int32.Parse(Spots));
                Booking booking = new Booking();
                booking.RestaurantId = Restaurantid;
                booking.EmailId = EmailId;
                booking.BookingDateTime = DateTime;
                booking.Spots = spotsSum;
                booking.RestaurantName = RestaurantName;
                var bookings = db.Bookings.ToList();
                ViewBag.OverBookings = false;
                var totalBookings = bookings.Where(x => x.BookingDateTime == DateTime && x.RestaurantId == Restaurantid).ToList();
                foreach (var item in totalBookings)
                {
                    spotsSum = spotsSum + item.Spots;  //adds the total bookings done for the particular time
                }
                if (spotsSum > 10)   //check if the number of bookings exceeds the number 10
                {
                    return Json(new  //code taken from stackoverflow
                    {
                        newUrl = Url.Action("Details", "Restaurants", new { id = Restaurantid }),
                        BookingStatus = "OverBooked"
                    });

                }
                else if (db.Bookings.Where(x=>(x.RestaurantId == Restaurantid) && (x.EmailId== EmailId) && (x.BookingDateTime == DateTime)).ToList().Count() > 0)
                {
                    return Json(new    //code taken from stackoverflow regarding how to send data back to the ajax request from the view
                    {
                        newUrl = Url.Action("Details", "Restaurants", new { id = Restaurantid }),
                        BookingStatus = "Already Booked"   //responds that a booking for the selected time already exists
                    });
                }

                else
                {
                    db.Bookings.Add(booking);  //does booking for that customer
                    db.SaveChanges();
                    SendEmail(EmailId, DateAndTime, RestaurantName);

                }
            }

            return Json(new   //sends notification that booking has been done
            {
                newUrl = Url.Action("Details", "Restaurants", new { id = Restaurantid }),
                BookingStatus = "Booked"
            });
        }

        /* 
         * It is called when the user selects to edit any booking. The view will show all the bookings
         * done by that user. And a delete button will be shown to delete a particular booking.
        */
        public ActionResult EditBooking()
        {
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
            var Bookings = db.Bookings.Where(x => x.EmailId == EmailId).ToList();
            return View(Bookings);
        }


        /* 
         The particular booking selected by the customer will be deleted from the system.
         */
        public ActionResult DeleteBooking(int? id)
        {
            
            Booking booking = db.Bookings.Find(id);
            db.Bookings.Remove(booking);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        /*
        This method is called from the "MakeBooking" ActionResult. Whenever the user does any booking at a restaurant
        an email is sent to the user confirming the booking.
        */
        public void SendEmail(String EmailId,String DateAndTime,String RestaurantName)
        {
            //The below code is using the sendgrid API to send an e-mail. Code is taken from the sendgrid website.
            String UNIQUE_KEY = "";
            var client = new SendGridClient(UNIQUE_KEY);
            var from = new EmailAddress("shridharproject@localhost.com", "Your Booking confirmation!");
            var to = new EmailAddress(EmailId, "");
            var plainTextContent = "Your booking has been confirmed for" + " " + DateAndTime + " " + "at" + " " + RestaurantName + ". Enjoy your meal!";
            var htmlContent = "<p>" + "Your booking has been confirmed for" + " " + DateAndTime + " " + "at" + " " + RestaurantName + ". Enjoy your meal!" + "</p>";
            var msg = MailHelper.CreateSingleEmail(from, to, "Your reservation!", plainTextContent, htmlContent);
            var response = client.SendEmailAsync(msg);

         }



        /* POST: RateRestaurant
         This action is called when the user selects to rate a restaurant. Action is called via a AJAX request.
         An entry is made in the "restaurantrating" table.
        */
        [HttpPost]
         public ActionResult RateRestaurant(String RestaurantRating,String RestaurantId)
         {
            
            int newRating = Convert.ToInt32(RestaurantRating);
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
            int Restaurantid = Convert.ToInt32(RestaurantId);
            bool AlreadyRatingGiven = false;
            if (ModelState.IsValid)
            {
                RestaurantRating rating = new RestaurantRating();
                rating.RestaurantId = Restaurantid;
                rating.CustomerEmail = EmailId;
                var isratinggiven = db.RestaurantRatings.Where(x => x.RestaurantId == Restaurantid && x.CustomerEmail == EmailId).Count();
                if (isratinggiven > 0)
                {
                    AlreadyRatingGiven = true;    //if rating is already given by the user, then prevent the user from giving the rating again.
                }
                else
                { 
                var ratings = db.RestaurantRatings.Where(x => x.RestaurantId == Restaurantid).Select(x => x.RestaurantRating1).ToList();
                if (ratings.Count() > 0) //this is executed when there is an entry in the "RestaurantRating" table for that restaurant.
                {
                    int sumRating = 0;
                    foreach (var Rating in ratings)
                    {
                        sumRating = sumRating + Rating;
                    }
                    int averageRating = (sumRating + newRating) / (ratings.Count() + 1);  //calculate the average rating
                    rating.RestaurantRating1 = newRating;
                    db.RestaurantRatings.Add(rating);
                    Restaurant res = db.Restaurants.Where(x => x.RestaurantId == Restaurantid).FirstOrDefault();
                    res.RestaurantRating = averageRating;
                    db.SaveChanges();
                }
                else   //this is executed if there is no entry in the "RestaurantRating" table for that restaurant.
                {
                    var defaultrating = db.Restaurants.Where(x => x.RestaurantId == Restaurantid).Select(x => x.RestaurantRating).FirstOrDefault();
                    var averageRating = (defaultrating + newRating) / 2;
                    rating.RestaurantRating1 = newRating;
                    db.RestaurantRatings.Add(rating);
                    Restaurant res = db.Restaurants.Where(x => x.RestaurantId == Restaurantid).FirstOrDefault();
                    res.RestaurantRating = averageRating;
                    db.SaveChanges();
                }
                }
            }

            return Json(new  //code taken from stackoverflow to send data back to the ajax request
            {
                newUrl = Url.Action("Details", "Restaurants", new { id = Restaurantid }),
                AlreadyRatingGiven
            });
        }



        // GET: Restaurants/Create
        //Only Admin has the authorizatin for this action.
        [Authorize(Roles ="Admin")]
        public ActionResult Create()
        {
            return View();
        }

        /*// POST: Restaurants/Create
         * This action creates a restaurant entry in the restaurant table. It is a post action.
         * Only Admin has the authorizatin for this action.
        */
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RestaurantId,RestaurantName,RestaurantCuisine,RestaurantRating,RestaurantLocation")] Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                restaurant.RestaurantRating = 4;   //default rating being 4
                db.Restaurants.Add(restaurant);
                db.SaveChanges();
                return RedirectToAction("Create", "Images", new { area = "" });
            }

            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        //Shows the details of the restaurant whose details is to be edited.
        //Only Admin has the authorizatin for this action.
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }
            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        //This is a post action which edits the details of a restaurant and posts it to the database.
        //Only Admin has the authorizatin for this action.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit([Bind(Include = "RestaurantId,RestaurantName,RestaurantCuisine,RestaurantRating,RestaurantLocation")] Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                db.Entry(restaurant).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(restaurant);
        }

        // GET: Restaurants/Delete/5
        //shows the details of the restaurant which is to be deleted. Only Admin has the authorization for this action.
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }
            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        //Confirms from the admin whether the restaurant is to be deleted. It is a post action which deletes
        //the restaurant from the database.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Restaurant restaurant = db.Restaurants.Find(id);
            var bookings = db.Bookings.Where(x => x.RestaurantId == restaurant.RestaurantId).ToList();
            foreach (var booking in bookings)
            {
                db.Bookings.Remove(booking);
            }
            var ratings = db.RestaurantRatings.Where(x => x.RestaurantId == restaurant.RestaurantId).ToList();
            foreach (var rating in ratings)
            {
                db.RestaurantRatings.Remove(rating);
            }
            var locations = db.Locations.Where(x=>x.RestaurantId == restaurant.RestaurantId).ToList();
            foreach (var location in locations)
            {
                db.Locations.Remove(location);
            }
            db.Restaurants.Remove(restaurant);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
