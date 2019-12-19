using SelectPdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelAssign1.Controllers
{
    public class PDFGeneratorController : Controller
    {
        // GET: PDFGenerator
        public ActionResult Index()
        {
            return View();
        }

        
        //This action is used to convert the webpage to PDF. The webpage will be downloaded in the form of a PDF
        //when this method is executed.
        public ActionResult SubmitAction(FormCollection collection)
        {
            //Code below is using the SendPDF component to convert the webpage to PDF.
            HtmlToPdf converter = new HtmlToPdf();
            string url = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, Url.Content("~"));
            PdfDocument doc = converter.ConvertUrl(url+"Chart/Dashboard");
            // save pdf document 
            byte[] pdf = doc.Save();
            
            // close pdf document 
            doc.Close();

            // return resulted pdf document 
            FileResult fileResult = new FileContentResult(pdf, "application/pdf");
            fileResult.FileDownloadName = "ShridharChart.pdf";
            return fileResult;
        }
    }
}