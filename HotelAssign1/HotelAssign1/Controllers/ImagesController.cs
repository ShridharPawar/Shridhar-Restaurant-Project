using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HotelAssign1.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace HotelAssign1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImagesController : Controller
    {
        private HotelAssign1Entities db = new HotelAssign1Entities(); //dbcontext object

        // GET: Images
        public ActionResult Index()
        {
            return View(db.Images.ToList());
        }

        

        // GET: Images/Create
        public ActionResult Create()
        {
           ViewBag.subscribedcustomers = db.Customers.Select(x => x).ToList();
            return View();
        }

        // POST: Images/Create
        //This is a post action which is called when a bulk email is to be sent to the customers, selected by
        //the admin along with an attachment. Admin will have the provision of selecting customers using a
        //checkbox and then an option to upload an attachment which is to be sent.
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create([Bind(Include = "Id,Email")] Image image, HttpPostedFileBase
        postedFile, FormCollection formcollection)
        {
            ModelState.Clear();
            var EmailTemplate = formcollection["EmailDescription"]; //gets the content from the richtext editor
            string[] emailaddresses=new string[1000];
            if (formcollection["customers"] != null)
            {
                emailaddresses = formcollection["customers"].Split(',');  //gets the emailaddresses of the customers which were checked by the admin
            }
            else if (formcollection["customers"] == null)
            {
                emailaddresses = new string[1];
                emailaddresses[0] = "";
            }
            if (emailaddresses.Contains("SelectAll"))
            {
               emailaddresses = db.Customers.Select(x => x.EmailAddress).ToArray();
            }
            var myUniqueFileName = string.Format(@"{0}", Guid.NewGuid());
            image.Path = myUniqueFileName;
            TryValidateModel(image);
             if (ModelState.IsValid)
                {
                if (postedFile != null) //This is executed if a file is uploaded by the admin.
                {
                    string serverPath = Server.MapPath("~/Uploads/");
                    string fileExtension = Path.GetExtension(postedFile.FileName);
                    string filePath = image.Path + fileExtension;
                    image.Path = filePath;
                    postedFile.SaveAs(serverPath + image.Path);
                    db.Images.Add(image);  //adds the image to the database.
                    db.SaveChanges();
                    String UNIQUE_KEY = "";
                    var client = new SendGridClient(UNIQUE_KEY);
                    var from = new EmailAddress("shridharproject@localhost.com", "New Restaurant launched near you!");
                    foreach (var customer in emailaddresses)  //send emal via sendgrid to each customer
                    {
                        var DefaultTemplate = "A New Restaurant has launched near you! Visit the look & book web application to reserve a spot!";
                        var to = new EmailAddress(customer, "");
                        var plainTextContent = ((EmailTemplate.Equals("")) ? DefaultTemplate : EmailTemplate);
                        var htmlContent = "<p>" + ((EmailTemplate.Equals("")) ? DefaultTemplate : EmailTemplate) + "</p>";
                        var msg = MailHelper.CreateSingleEmail(from, to, "New Restaurant launched near you!!", plainTextContent, htmlContent);
                        var bytes = System.IO.File.ReadAllBytes(serverPath + image.Path);
                        var file = Convert.ToBase64String(bytes);
                        msg.AddAttachment("ShridharBulkEmail.jpg", file); //add an attachment along with the email.
                        var response = client.SendEmailAsync(msg);

                    }
                }
                else  //This is executed if no file is uploaded by the admin.
                {
                    String UNIQUE_KEY = "";
                    var client = new SendGridClient(UNIQUE_KEY);
                    var from = new EmailAddress("shridharproject@localhost.com", "New Restaurant launched near you!");
                    foreach (var customer in emailaddresses)
                    {
                        var DefaultTemplate = "A New Restaurant has launched near you! Visit the look & book web application to reserve a spot!";
                        var to = new EmailAddress(customer, "");
                        var plainTextContent =  ((EmailTemplate.Equals("")) ? DefaultTemplate : EmailTemplate);
                        var htmlContent = "<p>"  + ((EmailTemplate.Equals("")) ? DefaultTemplate : EmailTemplate) + "</p>";
                        var msg = MailHelper.CreateSingleEmail(from, to, "New Restaurant launched near you!!", plainTextContent, htmlContent);
                        var response = client.SendEmailAsync(msg);

                    }
                }
                return RedirectToAction("Index", "Restaurants", new { area = "" });
            }

            return View(image);
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
