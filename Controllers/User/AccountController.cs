using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;

namespace 專題MVC修正.Controllers.User
{
    public class AccountController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        
        [HttpGet]
        public ActionResult Login()
        {
            return RedirectToAction("Index", "Home");
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string StdID, string Password)
        {
            
            if (StdID != Password)
            {
                TempData["LoginError"] = "帳號與密碼必須相同（預設都是 StdID）。";
                return RedirectToAction("Index", "Home");
            }

            
            var std = db.Std.SingleOrDefault(s => s.StdID == StdID);

            if (std == null)
            {
                TempData["LoginError"] = "找不到此學生，請確認 StdID 是否正確。";
                return RedirectToAction("Index", "Home");
            }

            // 登入成功
            Session["StdPK"] = std.StdPK;
            Session["StdID"] = std.StdID;
            Session["StdName"] = std.StdName;

            
            return RedirectToAction("Index", "Home");

        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
