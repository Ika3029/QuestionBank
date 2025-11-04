using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;

namespace 專題MVC修正.Controllers.Manage
{
    public class ExamHistoryController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // GET: /Manage/ExamHistory
        // 加入搜尋 + 分頁
        public ActionResult Exam_Index(string keyword, int page = 1, int pageSize = 10)
        {
            var query = db.ExamMaster.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(e => e.ExamName.Contains(keyword));
            }

            var totalCount = query.Count();
            var list = query
                        .OrderByDescending(e => e.ExamSDate)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            ViewBag.Keyword = keyword;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(list);
        }

        // GET: /Manage/ExamHistory/Details/5
        public ActionResult Exam_Details(int id)
        {
            var exam = db.ExamMaster.FirstOrDefault(e => e.ExamID == id);
            if (exam == null) return HttpNotFound();

            var rows =
                (from d in db.ExamDetail
                 where d.ExamID == id
                 join q in db.MoodQuestionBank on d.ExamMQBPK equals q.MQBPK into qg
                 from q in qg.DefaultIfEmpty()
                 orderby d.SortOrder, d.ExamDetPK
                 select new ExamDetailRowVM
                 {
                     ExamDetPK = d.ExamDetPK,
                     ExamDefaultScore = d.ExamDefaultScore,
                     ExamMQBPK = d.ExamMQBPK,
                     QContent = d.ExamQMode == "0" ? q.QContent : "(題組)",
                     QAns = d.ExamQMode == "0" ? q.QAns : null
                 }).ToList();

            var vm = new ExamDetailsVM
            {
                Exam = exam,
                Details = rows
            };

            return View(vm);
        }

        // POST: /Manage/ExamHistory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Exam_Delete(int id)
        {
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var details = db.ExamDetail.Where(d => d.ExamID == id);
                    if (details.Any()) db.ExamDetail.RemoveRange(details);

                    var exam = db.ExamMaster.FirstOrDefault(e => e.ExamID == id);
                    if (exam == null)
                    {
                        TempData["Msg"] = "找不到要刪除的考卷。";
                        return RedirectToAction("Exam_Index");
                    }

                    db.ExamMaster.Remove(exam);
                    db.SaveChanges();
                    tx.Commit();

                    TempData["Msg"] = $"已刪除考卷（ID={id}）。";
                    return RedirectToAction("Exam_Index");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["Msg"] = "刪除失敗：" + ex.Message;
                    return RedirectToAction("Exam_Index");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
