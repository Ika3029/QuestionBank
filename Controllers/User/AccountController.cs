using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;

namespace 專題MVC修正.Controllers.User
{
    public class AccountController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // GET/Login：不要顯示獨立登入頁 → 回首頁
        [HttpGet]
        public ActionResult Login()
        {
            return RedirectToAction("Index", "Home");
        }

        // POST：首頁右側登入
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string StdID, string Password)
        {
            // 帳密相同才行
            if (StdID != Password)
            {
                TempData["LoginError"] = "帳號與密碼必須相同（預設都是 StdID）。";
                return RedirectToAction("Index", "Home");
            }

            // 找學生 StdID（字串）
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

            // ===== 以下是「額外學生資料」示範寫法 =====
            // ⚠ 這裡的屬性名稱請依你的 Model 實際欄位改掉
            // 例如：std.StdDeptName、std.StdGrade、std.StdClass、std.SchoolEmail、std.PersonalEmail

            // 科系/年級/班級（例如：資管系 4A）
            /*
            Session["StdDeptGradeClass"] = string.Format("{0} {1}",
                std.StdDeptName,          // 科系名稱，例如「資管系」
                std.StdClassName);        // 年級班級，例如「4A」
            */

            // 學校 Email
            // Session["StdSchoolEmail"] = std.StdSchoolEmail;

            // 個人 Email
            // Session["StdPersonalEmail"] = std.StdPersonalEmail;
            // ===== 以上如果還沒對好欄位，就先保留註解狀態 =====

            return RedirectToAction("Index", "Home");

        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
