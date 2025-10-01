using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using 專題MVC修正.Models;

namespace 專題MVC修正.Controllers
{
    public class HomeController : Controller
    {
        MQBEntities db = new MQBEntities();
        // GET: Home
        
        public ActionResult Index()
        {
            var MoodQuestionBank = db.MoodQuestionBank.OrderByDescending(x => x.MQBSort).ToList(); // 可加 .ToList()
            return View(MoodQuestionBank); // 若 View 有接收 Model
        }

        public ActionResult Create()
        {
            return View(); // 若 View 有接收 Model
        }
        [HttpPost]
        public ActionResult Create(MoodQuestionBank moodQuestionBank)
        {
            if (ModelState.IsValid)
            {
                ViewBag.Error = false;
                var temp = db.MoodQuestionBank
                    .Where(m => m.MQBPK == moodQuestionBank.MQBPK)
                    .FirstOrDefault();
                if (temp != null)
                {
                    ViewBag["error"] = true;
                    return View(moodQuestionBank);
                }

                db.MoodQuestionBank.Add(moodQuestionBank);
                db.SaveChanges();
                return RedirectToAction("index");
            }
            return View(moodQuestionBank);
        }
    }
}
