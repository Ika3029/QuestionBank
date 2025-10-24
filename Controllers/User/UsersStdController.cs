using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Collections.Generic;
using 專題MVC修正.Models;
using System.Net;

namespace 專題MVC修正.Controllers.User
{
    // 傳統路由：/UsersStd/{action}/{id}
    public class UsersStdController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();
        private static string S(object v) => v == null ? "" : v.ToString().Trim();
        private string NG(string g) { g = S(g); return (g == "男") ? "0" : (g == "女") ? "1" : (g == "0" || g == "1") ? g : ""; }

        // 下拉：沿用你原本 ViewBag 名稱
        private void DD(string dep = null, string grade = null, string cls = null, string gender = null)
        {
            ViewBag.StdDep = new SelectList(new[]
            {
                new {Text="國貿系",Value="國貿系"}, new {Text="法文系",Value="法文系"},
                new {Text="俄文系",Value="俄文系"}, new {Text="全商系",Value="全商系"},
                new {Text="資管系",Value="資管系"},
            }, "Value", "Text", S(dep));

            ViewBag.StaffGrade = new SelectList(new[]
            {
                new {Text="一年級",Value="1"}, new {Text="二年級",Value="2"},
                new {Text="三年級",Value="3"}, new {Text="四年級",Value="4"},
            }, "Value", "Text", S(grade));

            ViewBag.StaffClass = new SelectList(new[]
            {
                new {Text="A班",Value="A"}, new {Text="B班",Value="B"},
                new {Text="C班",Value="C"}, new {Text="甲班",Value="甲"},
            }, "Value", "Text", S(cls));

            ViewBag.StdGender = new SelectList(new[]
            { new {Text="男",Value="0"}, new {Text="女",Value="1"} }, "Value", "Text", S(gender));
        }

        // ============ Index（支援科系/年級/班級 + 學號/姓名模糊） ============
        public ActionResult Std_Index(string StdDep, string Grade, string Class, string q)
        {
            DD(StdDep, Grade, Class, null);

            var data = db.Std.AsQueryable();
            if (!string.IsNullOrWhiteSpace(StdDep)) data = data.Where(x => x.StdDepID.StartsWith(StdDep));
            if (!string.IsNullOrWhiteSpace(Grade)) data = data.Where(x => x.StdDepID.Contains(" " + Grade));
            if (!string.IsNullOrWhiteSpace(Class)) data = data.Where(x => x.StdDepID.EndsWith(Class));
            if (!string.IsNullOrWhiteSpace(q)) data = data.Where(x => x.StdID.Contains(q) || x.StdName.Contains(q));

            ViewBag.Query = q;
            return View("Std_Index", data.OrderBy(x => x.StdPK).ToList());
        }

        public ActionResult Std_Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var std = db.Std.Find(id);
            if (std == null) return HttpNotFound();
            return View("Std_Details", std);
        }




        // ============ Create ============
        [HttpGet]
        public ActionResult Std_Create()
        {
            DD(); return View("Std_Create", new Std());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Std_Create(string StdDep, string StaffGrade, string StaffClass,
                                       string StdID, string StdName, string WorkEmail,
                                       string PersonalGmail, string StdGender)
        {
            var depid = S(StdDep) + (string.IsNullOrWhiteSpace(StaffGrade) ? "" : " " + S(StaffGrade)) + S(StaffClass);
            var m = new Std
            {
                StdDepID = depid,
                StdID = S(StdID),
                StdName = S(StdName),
                WorkEmail = S(WorkEmail),
                PersonalGmail = S(PersonalGmail),
                StdGender = NG(StdGender)
            };

            if (string.IsNullOrWhiteSpace(m.StdDepID)) ModelState.AddModelError("StdDepID", "請選擇科系/年級/班級");
            if (string.IsNullOrWhiteSpace(m.StdID)) ModelState.AddModelError("StdID", "請輸入學號");
            if (string.IsNullOrWhiteSpace(m.StdName)) ModelState.AddModelError("StdName", "請輸入姓名");
            if (!ModelState.IsValid) { DD(StdDep, StaffGrade, StaffClass, m.StdGender); return View("Std_Create", m); }

            // 若 StdPK 不是 Identity → Max+1（是 Identity 的話可刪除這段）
            if (m.StdPK == 0) m.StdPK = (db.Std.Select(x => (int?)x.StdPK).Max() ?? 0) + 1;

            try { db.Std.Add(m); db.SaveChanges(); return RedirectToAction("Std_Index"); }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                ModelState.AddModelError("", "新增失敗：" + ex.Message);
                DD(StdDep, StaffGrade, StaffClass, m.StdGender); return View("Std_Create", m);
            }
        }

        // ============ Edit ============
        [HttpGet]
        public ActionResult Std_Edit(int? id)
        {
            if (id == null) return HttpNotFound();
            var m = db.Std.Find(id);
            if (m == null) return HttpNotFound();

            // 拆回三段供預選
            string dep = "", grade = "", cls = "";
            var parts = S(m.StdDepID).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1) dep = parts[0];
            if (parts.Length >= 2) { var g = parts[1]; if (g.Length >= 1) grade = g.Substring(0, 1); if (g.Length >= 2) cls = g.Substring(1); }

            DD(dep, grade, cls, S(m.StdGender));
            return View("Std_Edit", m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Std_Edit(int StdPK, string StdDep, string StaffGrade, string StaffClass,
                                     string StdID, string StdName, string WorkEmail,
                                     string PersonalGmail, string StdGender)
        {
            var m = db.Std.Find(StdPK);
            if (m == null) return HttpNotFound();

            m.StdDepID = S(StdDep) + (string.IsNullOrWhiteSpace(StaffGrade) ? "" : " " + S(StaffGrade)) + S(StaffClass);
            m.StdID = S(StdID); m.StdName = S(StdName);
            m.WorkEmail = S(WorkEmail); m.PersonalGmail = S(PersonalGmail);
            m.StdGender = NG(StdGender);

            if (string.IsNullOrWhiteSpace(m.StdDepID)) ModelState.AddModelError("StdDepID", "請選擇科系/年級/班級");
            if (string.IsNullOrWhiteSpace(m.StdID)) ModelState.AddModelError("StdID", "請輸入學號");
            if (string.IsNullOrWhiteSpace(m.StdName)) ModelState.AddModelError("StdName", "請輸入姓名");
            if (!ModelState.IsValid) { DD(StdDep, StaffGrade, StaffClass, m.StdGender); return View("Std_Edit", m); }

            try { db.Entry(m).State = EntityState.Modified; db.SaveChanges(); return RedirectToAction("Std_Index"); }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                ModelState.AddModelError("", "更新失敗：" + ex.Message);
                DD(StdDep, StaffGrade, StaffClass, m.StdGender); return View("Std_Edit", m);
            }
        }

        // ============ Delete ============
        [HttpGet]
        public ActionResult Std_Delete(int? id)
        {
            if (id == null) return HttpNotFound();
            var m = db.Std.Find(id);
            if (m == null) return HttpNotFound();
            return View("Std_Delete", m);
        }

        [HttpPost, ActionName("Std_Delete"), ValidateAntiForgeryToken]
        public ActionResult Std_Delete_Post(int id)
        {
            var m = db.Std.Find(id);
            if (m != null) { db.Std.Remove(m); db.SaveChanges(); }
            return RedirectToAction("Std_Index");
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
