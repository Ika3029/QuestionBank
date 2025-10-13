using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;
using PagedList;

namespace 專題MVC修正.Controllers.Manage
{
    public class ManageQuestionsController : Controller
    {
        MQBEntities db = new MQBEntities();

        // 題庫列表 + 搜尋 + 分頁
        public ActionResult Questions_Index(string MQBTeamPK, string searchString, int page = 1)
        {
            int pageSize = 10;
            int pageCurrent = page < 1 ? 1 : page;

            var query = db.MoodQuestionBank.AsQueryable();

            if (!string.IsNullOrEmpty(MQBTeamPK))
                query = query.Where(x => x.MQBTeamPK.ToString() == MQBTeamPK);
            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(x => x.QContent.Contains(searchString));

            var list = query.OrderByDescending(x => x.MQBPK).ToPagedList(pageCurrent, pageSize);

            ViewBag.MQBTeamPK = new SelectList(db.MQBTeam.ToList(), "MQBTeamPK", "MQBTeamContent", MQBTeamPK);
            return View(list);
        }

        // 新增題目
        [HttpGet]
        public ActionResult Questions_Create()
        {
            ViewBag.MQBTeamPK = new SelectList(
                db.MQBTeam.OrderBy(x => x.MQBTeamContent),
                "MQBTeamPK", "MQBTeamContent");

            // ⬇️ 用 QClass (連到 MQBClassName.MQBClassPK)
            ViewBag.QClass = new SelectList(
                db.MQBClassName.OrderBy(x => x.MQBClassName1),
                "MQBClassPK", "MQBClassName1");

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Questions_Create(MoodQuestionBank m)
        {
            // 題組驗證
            if (m.MQBTeamPK <= 0 || !db.MQBTeam.Any(t => t.MQBTeamPK == m.MQBTeamPK))
                ModelState.AddModelError("MQBTeamPK", "請選擇有效的題組");

            // ⬇️ 類別驗證 (QClass)
            if (m.QClass <= 0 || !db.MQBClassName.Any(c => c.MQBClassPK == m.QClass))
                ModelState.AddModelError("QClass", "請選擇有效的分類");

            if (string.IsNullOrWhiteSpace(m.QContent))
                ModelState.AddModelError("QContent", "請輸入題目內容");

            if (!ModelState.IsValid)
            {
                ViewBag.MQBTeamPK = new SelectList(
                    db.MQBTeam.OrderBy(x => x.MQBTeamContent),
                    "MQBTeamPK", "MQBTeamContent", m.MQBTeamPK);

                ViewBag.QClass = new SelectList(
                    db.MQBClassName.OrderBy(x => x.MQBClassName1),
                    "MQBClassPK", "MQBClassName1", m.QClass);

                return View(m);
            }

            // 排序（同題組最大 + 1）
            m.MQBSort = (db.MoodQuestionBank
                            .Where(x => x.MQBTeamPK == m.MQBTeamPK)
                            .Select(x => (int?)x.MQBSort).Max() ?? 0) + 1;

            try
            {
                db.MoodQuestionBank.Add(m);
                db.SaveChanges();
                TempData["ok"] = "題目新增成功";
                return RedirectToAction("Questions_Index");
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                ModelState.AddModelError("", "新增失敗（資料庫）： " + ex.GetBaseException().Message);

                ViewBag.MQBTeamPK = new SelectList(
                    db.MQBTeam.OrderBy(x => x.MQBTeamContent),
                    "MQBTeamPK", "MQBTeamContent", m.MQBTeamPK);

                ViewBag.QClass = new SelectList(
                    db.MQBClassName.OrderBy(x => x.MQBClassName1),
                    "MQBClassPK", "MQBClassName1", m.QClass);

                return View(m);
            }
        }



        // 編輯
        // ========== 共同方法：重綁下拉 ==========
        private void BuildDropDownsForEdit(專題MVC修正.Models.MoodQuestionBank m)
        {
            ViewBag.MQBTeamPK = new SelectList(
                db.MQBTeam.OrderBy(x => x.MQBTeamContent),
                "MQBTeamPK", "MQBTeamContent", m.MQBTeamPK);

            ViewBag.QClass = new SelectList(
                db.MQBClassName.OrderBy(x => x.MQBClassName1),
                "MQBClassPK", "MQBClassName1", m.QClass);
        }

        // ========== GET: 編輯 ==========
        [HttpGet]
        public ActionResult Questions_Edit(int? id)
        {
            if (id == null) return RedirectToAction("Questions_Index");

            var e = db.MoodQuestionBank.Find(id.Value);
            if (e == null) return HttpNotFound();

            BuildDropDownsForEdit(e);
            return View(e);
        }

        // ========== POST: 編輯 ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Questions_Edit(專題MVC修正.Models.MoodQuestionBank m)
        {
            if (!ModelState.IsValid)
            {
                BuildDropDownsForEdit(m);
                return View(m);
            }

            var e = db.MoodQuestionBank.Find(m.MQBPK);
            if (e == null) return HttpNotFound();

            // ✅ 僅更新允許的欄位（避免把其他欄位清空）
            e.MQBTeamPK = m.MQBTeamPK;
            e.QClass = m.QClass;
            e.QType = m.QType;
            e.QContent = m.QContent;
            e.QOptionA = m.QOptionA;
            e.QOptionB = m.QOptionB;
            e.QOptionC = m.QOptionC;
            e.QOptionD = m.QOptionD;
            e.QAns = m.QAns;
            // e.MQBSort   視情況是否允許修改

            db.SaveChanges();

            TempData["ok"] = "題目已更新";
            return RedirectToAction("Questions_Index");
        }


        // 刪除
        public ActionResult Questions_Delete(int id)
        {
            var e = db.MoodQuestionBank.Find(id);
            if (e == null) return HttpNotFound();
            db.MoodQuestionBank.Remove(e);
            db.SaveChanges();
            TempData["ok"] = "題目已刪除";
            return RedirectToAction("Questions_Index");
        }
        [HttpPost]
        public ActionResult AddClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return Json(new { success = false, message = "分類名稱不能為空" });

            if (db.MQBClassName.Any(c => c.MQBClassName1 == className))
                return Json(new { success = false, message = "分類名稱已存在" });

            var newClass = new MQBClassName { MQBClassName1 = className };
            db.MQBClassName.Add(newClass);
            db.SaveChanges();

            return Json(new { success = true, id = newClass.MQBClassPK, name = newClass.MQBClassName1 });
        }

    }
}
