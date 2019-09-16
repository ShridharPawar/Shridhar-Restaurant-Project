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
    [Authorize]
    public class RestaurantsController : Controller
    {
        private HotelAssign1Entities db = new HotelAssign1Entities();
        protected ApplicationDbContext ApplicationDbContext { get; set; }
        protected UserManager<ApplicationUser> UserManager { get; set; }

        public RestaurantsController()
        {
            this.ApplicationDbContext = new ApplicationDbContext();
            this.UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this.ApplicationDbContext));
        }
        // GET: Restaurants
        public ActionResult Index()
        {
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
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

        // GET: Restaurants/Details/5
        public ActionResult Details(int? id)
        {
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
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


        [HttpPost]
        public ActionResult MakeBooking(String DateAndTime, String RestaurantId)
        {
            var DateTime = DateAndTime;
            var Restaurantid = Int32.Parse(RestaurantId);
            var RestaurantName = db.Restaurants.Where(x => x.RestaurantId == Restaurantid).Select(x => x.RestaurantName).FirstOrDefault();
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
            if (ModelState.IsValid)
            {
                Booking booking = new Booking();
                booking.RestaurantId = Restaurantid;
                booking.EmailId = EmailId;
                booking.BookingDateTime = Convert.ToDateTime(DateTime);
                var bookings = db.Bookings.ToList();
                ViewBag.OverBookings = false;

                var totalBookings = bookings.Where(x => x.BookingDateTime == Convert.ToDateTime(DateTime) && x.RestaurantId == Restaurantid).Count();
                if (totalBookings >= 1)
                {
                    return Json(new
                    {
                        newUrl = Url.Action("Details", "Restaurants", new { id = Restaurantid }),
                        BookingStatus = "OverBooked"
                    });

                }
                else
                {
                    db.Bookings.Add(booking);
                    db.SaveChanges();
                    String API_KEY = "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";
                    var client = new SendGridClient(API_KEY);
                    var from = new EmailAddress("shridharproject@localhost.com", "Your Booking confirmation!");
                    var to = new EmailAddress(EmailId, "");
                    var plainTextContent = "Your booking has been confirmed for" + " " + DateAndTime + " " + "at" + " " + RestaurantName + ". Enjoy your meal!";
                    var htmlContent = "<p>" + "Your booking has been confirmed for" + " " + DateAndTime + " " + "at" + " " + RestaurantName + ". Enjoy your meal!" + "</p>";
                    var msg = MailHelper.CreateSingleEmail(from, to,"Your reservation!", plainTextContent, htmlContent);
                    var response = client.SendEmailAsync(msg);
                }
            }

            return Json(new
            {
                newUrl = Url.Action("Details", "Restaurants", new { id = Restaurantid }),
                BookingStatus = "Booked"
            });
        }


         [HttpPost]
         public ActionResult RateRestaurant(String RestaurantRating,String RestaurantId)
         {
            
            int newRating = Convert.ToInt32(RestaurantRating);
            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
            int Restaurantid = Convert.ToInt32(RestaurantId);
            if (ModelState.IsValid)
            {
                RestaurantRating rating = new RestaurantRating();
                rating.RestaurantId = Restaurantid;
                rating.CustomerEmail = EmailId;
                
                var ratings = db.RestaurantRatings.Where(x => x.RestaurantId == Restaurantid).Select(x => x.RestaurantRating1).ToList();
                if (ratings.Count() > 0)
                {
                    int sumRating = 0;
                    foreach (var Rating in ratings)
                    {
                        sumRating = sumRating + Rating;
                    }
                    int averageRating = (sumRating + newRating) / (ratings.Count() + 1);
                    rating.RestaurantRating1 = newRating;
                    db.RestaurantRatings.Add(rating);
                    Restaurant res = db.Restaurants.Where(x => x.RestaurantId == Restaurantid).FirstOrDefault();
                    res.RestaurantRating = averageRating;
                    db.SaveChanges();
                }
                else
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

            return Json(new
            {
                newUrl = Url.Action("Details", "Restaurants", new { id = Restaurantid }),
                
            });
        }


        
       // GET: Restaurants/Create
        [Authorize(Roles ="Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Restaurants/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RestaurantId,RestaurantName,RestaurantCuisine,RestaurantRating,RestaurantLocation")] Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                db.Restaurants.Add(restaurant);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
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
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Restaurant restaurant = db.Restaurants.Find(id);
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
