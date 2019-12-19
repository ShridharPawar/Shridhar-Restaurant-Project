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

namespace HotelAssign1.Controllers
{
    [Authorize]
    public class LocationsController : Controller
    {
        private HotelAssign1Entities db = new HotelAssign1Entities();  //create db context object
        protected ApplicationDbContext ApplicationDbContext { get; set; }
        protected UserManager<ApplicationUser> UserManager { get; set; }
        public LocationsController()
        {
            this.ApplicationDbContext = new ApplicationDbContext();
            //used to retreive the emailid of the logged in user. code taken from stackoverflow.
            this.UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this.ApplicationDbContext));
        }
        // GET: Locations
        //I am using the Mapbox API here to show the events on maps.
        public ActionResult Index()
        {
            ViewBag.IsAdmin = false;
            var userDetails = UserManager.FindById(User.Identity.GetUserId());
            var ifMultipleRoles = userDetails.Roles.ToList();
            if (ifMultipleRoles.Count() > 0)
            {
                if (userDetails.Roles.ToList().FirstOrDefault().RoleId == "1")
                {
                    ViewBag.IsAdmin = true;  //so as to show the 'Create' hyperlink only if the user is an admin.
                }

            }
            return View(db.Locations.ToList());
        }

        // GET: Locations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Location location = db.Locations.Find(id);
            if (location == null)
            {
                return HttpNotFound();
            }
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
            return View(location);
        }

        // GET: Locations/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Locations/Create
        //This is a post action to create an event. It adds the event details to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create([Bind(Include = "Id,Name,Address,Description,Latitude,Longitude")] Location location)
        {
            if (ModelState.IsValid)
            {
                location.RestaurantId = db.Restaurants.Where(x => x.RestaurantName.Equals(location.Name)).Select(x => x.RestaurantId).FirstOrDefault();
                if (location.RestaurantId == 0)
                {
                    location.RestaurantId = 7;
                }
                db.Locations.Add(location);
                db.SaveChanges();
                CustomersController cus = new CustomersController(); 
                cus.SendBulkEmailForEvent(location);  //send email to the subscribed customers stating that a new event is being organized.
                return RedirectToAction("Index");
            }

            return View(location);
        }

        // GET: Locations/Edit/5
        //Edit the details of an event. Only admin has the action to edit the details of the event.
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Location location = db.Locations.Find(id);
            if (location == null)
            {
                return HttpNotFound();
            }
            return View(location);
        }

        // POST: Locations/Edit/5
        //Edit the details of an event and save it in the database. Only admin has the action to edit the details of the event.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit([Bind(Include = "Id,Name,Address,Description,Latitude,Longitude")] Location location)
        {
            if (ModelState.IsValid)
            {
                db.Entry(location).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(location);
        }

        // GET: Locations/Delete/5
        //Delete an event. Only admin has the action to delete an event.
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Location location = db.Locations.Find(id);
            if (location == null)
            {
                return HttpNotFound();
            }
            return View(location);
        }

        // POST: Locations/Delete/5
        //Delete an event from the database. Only admin has the action to delete an event.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Location location = db.Locations.Find(id);
            db.Locations.Remove(location);
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
