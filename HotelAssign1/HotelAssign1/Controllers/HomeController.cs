using HotelAssign1.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace HotelAssign1.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        
        public ActionResult Index()
        {
            
             return View();
        }

        
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            //var userid = User.Identity.GetUserId();
            //ApplicationDbContext UsersContext = new ApplicationDbContext();
            //var Users = UsersContext.Users.ToList();
            //var UsersEmail = Users.Select(x => x.Email).ToList();

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}