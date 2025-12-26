using System;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;

namespace 專題MVC修正.Controllers.Manage
{
    public class ManageLookupsController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // 題組
        [HttpPost]
        public ActionResult AddTeam(string name, int mqbClassPk, string yn = "N")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Json(new { ok = false, err = "請輸入題組名稱" });

                if (mqbClassPk <= 0)
                    return Json(new { ok = false, err = "請先選擇類別(QClass)" });

                name = name.Trim();

                
                var cls = db.MQBClassName.Find(mqbClassPk);
                if (cls == null)
                    return Json(new { ok = false, err = "找不到對應的分類(QClass)，請重新整理後再試。" });

                
                var exists = db.MQBTeam.Any(x =>
                    x.MQBTeamContent == name &&
                    x.MQBClassName.MQBClassPK == mqbClassPk);

                if (exists)
                    return Json(new { ok = false, err = "此分類底下已存在同名題組" });

                var t = new MQBTeam
                {
                    MQBTeamContent = name,
                    MQBTeamYN = string.IsNullOrWhiteSpace(yn) ? "N" : yn,
                    
                    MQBClassName = cls
                };

                db.MQBTeam.Add(t);
                db.SaveChanges();

                return Json(new
                {
                    ok = true,
                    id = t.MQBTeamPK,
                    text = t.MQBTeamContent
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg = ex.InnerException.Message;
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                    msg = ex.InnerException.InnerException.Message;

                System.Diagnostics.Debug.WriteLine("AddTeam error: " + msg);
                return Json(new { ok = false, err = "新增題組失敗：" + msg });
            }
        }

        // 類別
        [HttpPost]
        public ActionResult AddClass(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Json(new { ok = false, err = "請輸入分類名稱" });

                name = name.Trim();

                var exists = db.MQBClassName.Any(x => x.MQBClassName1 == name);
                if (exists)
                    return Json(new { ok = false, err = "此分類已存在" });

                var c = new MQBClassName
                {
                    MQBClassName1 = name
                };

                db.MQBClassName.Add(c);
                db.SaveChanges();

                return Json(new
                {
                    ok = true,
                    id = c.MQBClassPK,
                    text = c.MQBClassName1
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg = ex.InnerException.Message;
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                    msg = ex.InnerException.InnerException.Message;

                System.Diagnostics.Debug.WriteLine("AddClass error: " + msg);
                return Json(new { ok = false, err = "新增分類失敗：" + msg });
            }
        }
    }
}
