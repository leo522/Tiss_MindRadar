using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tiss_MindRadar.Controllers
{
    public class RadarErrorController : Controller
    {
        // GET: RadarError
        public ActionResult AccessDenied()
        {
            return View();
        }
    }
}