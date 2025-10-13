using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;

namespace 專題MVC修正.Controllers.Manage
{
    [RoutePrefix("Manage/Lookups")]
    public class ManageLookupsController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // 題組：新增（AJAX）
        [HttpPost, Route("AddTeam")]
        public ActionResult AddTeam(string name, string yn = "N")
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { ok = false, err = "請輸入題組名稱" });

            // 防重複（以名稱唯一為例）
            var exists = db.MQBTeam.Any(x => x.MQBTeamContent == name);
            if (exists) return Json(new { ok = false, err = "此題組已存在" });

            var t = new MQBTeam
            {
                MQBTeamContent = name,
                MQBTeamYN = string.IsNullOrWhiteSpace(yn) ? "N" : yn
            };
            db.MQBTeam.Add(t);
            db.SaveChanges();
            return Json(new { ok = true, id = t.MQBTeamPK, text = t.MQBTeamContent });
        }

        // 類別：新增（AJAX）
        [HttpPost, Route("AddClass")]
        public ActionResult AddClass(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { ok = false, err = "請輸入分類名稱" });

            var exists = db.MQBClassName.Any(x => x.MQBClassName1 == name);
            if (exists) return Json(new { ok = false, err = "此分類已存在" });

            var c = new MQBClassName
            {
                MQBClassName1 = name
            };
            db.MQBClassName.Add(c);
            db.SaveChanges();
            return Json(new { ok = true, id = c.MQBClassPK, text = c.MQBClassName1 });
        }
    }
}
