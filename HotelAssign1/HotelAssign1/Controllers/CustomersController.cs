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
    public class CustomersController : Controller
    {
        private HotelAssign1Entities db = new HotelAssign1Entities();  //db context object

        protected ApplicationDbContext ApplicationDbContext { get; set; }
        protected UserManager<ApplicationUser> UserManager { get; set; }
        public CustomersController()
        {
            this.ApplicationDbContext = new ApplicationDbContext();
            //code taken from stackoverflow to retreive the email id of the currently logged in user.
            this.UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this.ApplicationDbContext));
        }

        // GET: Customers
        //Shows the details of the currently logged in user if they have subscribed to the newsletter.
        [Authorize]
        public ActionResult Index()
        {

            var EmailId = UserManager.FindById(User.Identity.GetUserId()).Email;
            ViewBag.PersonalDetailsAdded = false;
            if (db.Customers.ToList().Select(x=>x.EmailAddress).Contains(EmailId)) //check if the customer has already subscribed to the newsletter
            {
                ViewBag.PersonalDetailsAdded = true;
            }

            return View(db.Customers.Where(x=>x.EmailAddress==EmailId));
        }

        

        // GET: Customers/Create
        //shows the view which gives the customer an opportunity to subscribe to the newsletter.
        public ActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        //When the customer agrees to subscribe to the newsletter, then this action adds an entry of that customer in the database.
        //It is a post action.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,FirstName,LastName,EmailAddress,ContactNumber")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Customers.Add(customer);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(customer);
        }


        //This method is called from the "LocationsController". The reason behind is that whenever a new event is 
        //created by the admin, an email is to be sent to all the subscribed customers that a new event is being organized.
        public void SendBulkEmailForEvent(Location location)
        {
            //The following code is using the sendgrid API to send an email. code taken from the sendgrid website.
            var subscribedCustomers = db.Customers.Select(x => x).ToList();
            String UNIQUE_KEY = "";
            var client = new SendGridClient(UNIQUE_KEY);
            var from = new EmailAddress("shridharproject@localhost.com", "New event being organized near you!");
            
            foreach (var customer in subscribedCustomers) //to send email to every customer, use the foreach loop
            {
                var to = new EmailAddress(customer.EmailAddress, "");
                var plainTextContent = "Hey " + customer.FirstName + ",a new event called " + "'" + location.Name + "'"+ " is being organized near you.You can visit our web application and know the further exciting details!";
                var htmlContent = "<p>" + "Hey " + customer.FirstName + ",a new event called " + "'" + location.Name + "'" + " is being organized near you.You can visit our web application and know the further exciting details!" + "</p>";
                var msg = MailHelper.CreateSingleEmail(from, to, "New event being organized near you!", plainTextContent, htmlContent);
                var response = client.SendEmailAsync(msg);
            }
        }

        // GET: Customers/Delete/5
        //to unsubscribe to the newsletter.
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Customer customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View(customer);
        }

        // POST: Customers/Delete/5
        //This action deletes the entry of the user from the 'Customer' table, meaning that the customer has
        //unsubscribed to the newsletter.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Customer customer = db.Customers.Find(id);
            db.Customers.Remove(customer);
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
