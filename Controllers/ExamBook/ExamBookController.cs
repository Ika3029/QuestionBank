using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;

namespace 專題MVC修正.Controllers.User
{
    public class ExamBookController : Controller
    {
        MQBEntities db = new MQBEntities();

        // ====== 測驗本列表：帶搜尋 + 分頁 ======
        public ActionResult Index(string keyword, int page = 1, int pageSize = 10)
        {
            // 先做 grouping：一位學生對同一張考卷的一次作答 = 一筆紀錄
            var query = from r in db.StdExamRec
                        join s in db.Std on r.ExamStdPK equals s.StdPK
                        group new { r, s } by new
                        {
                            r.ExamID,
                            r.ExamStdPK,
                            s.StdName,
                            r.ExamAnsST      // 用開始作答時間當作「這次作答」的識別
                        }
                into g
                        select new ExamBookSummaryVM
                        {
                            ExamID = g.Key.ExamID,
                            ExamStdPK = g.Key.ExamStdPK,
                            StdName = g.Key.StdName,

                            TotalQuestions = g.Count(),
                            // G = 答對
                            CorrectCount = g.Count(x => x.r.ExamStdAnsRight == "G"),

                            // 只算答對題目的分數
                            Score = g.Where(x => x.r.ExamStdAnsRight == "G")
                                     .Sum(x => (double?)x.r.ExamDefaultScore) ?? 0,

                            StartTime = g.Key.ExamAnsST,
                            EndTime = g.Max(x => x.r.ExamAnsET),

                            // 這裡先不算 Ticks，避免 EF 爆炸；等拉到記憶體再算
                            AttemptTicks = 0
                        };

            // 關鍵字搜尋（這裡先用學生姓名 + 測驗編號）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.StdName.Contains(keyword) ||
                    (x.ExamID.HasValue && x.ExamID.Value.ToString().Contains(keyword))
                );
            }

            // 總筆數
            int totalItems = query.Count();

            // 排序 + 分頁（仍然在資料庫做）
            var pageQuery = query
                .OrderByDescending(x => x.EndTime)
                .ThenByDescending(x => x.ExamID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            // 先把這一頁拉到記憶體，再補上 AttemptTicks
            var list = pageQuery.ToList();

            foreach (var x in list)
            {
                x.AttemptTicks = x.StartTime.HasValue
                    ? x.StartTime.Value.Ticks
                    : 0L;
            }

            var model = new PagedResult<ExamBookSummaryVM>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                Query = list.AsQueryable()
            };

            ViewBag.Keyword = keyword;
            return View(model);
        }

        // ====== 某一次作答的明細（一定要帶 attemptTicks） ======
        [HttpGet]
        public ActionResult Details(int examId, int stdPk, long attemptTicks)
        {
            // 用 ticks 還原出 DateTime，拿來比對 ExamAnsST
            DateTime attemptTime = new DateTime(attemptTicks);

            var list = (
                from r in db.StdExamRec
                join q in db.MoodQuestionBank
                    on r.ExamMQBPK equals q.MQBPK
                join s in db.Std
                    on r.ExamStdPK equals s.StdPK
                where r.ExamID == examId
                      && r.ExamStdPK == stdPk
                      && r.ExamAnsST.HasValue
                      && r.ExamAnsST.Value == attemptTime   // 這裡不再用 Ticks
                orderby r.ExamDetPK
                select new
                {
                    Rec = r,
                    Q = q,
                    S = s
                }
            ).ToList();

            if (!list.Any())
            {
                return HttpNotFound();   // 沒有這次作答紀錄
            }

            // 測驗主檔（名稱）
            var exam = db.ExamMaster.FirstOrDefault(e => e.ExamID == examId);
            string examName = exam != null ? exam.ExamName : ("測驗 " + examId);
            string stdName = list.First().S.StdName;

            int totalQ = list.Count;
            int correct = list.Count(x => x.Rec.ExamStdAnsRight == "G");

            double? totalScore = list
                .Where(x => x.Rec.ExamStdAnsRight == "G")
                .Sum(x => (double?)x.Rec.ExamDefaultScore);

            var rows = list
                .Select((x, index) => new ExamBookDetailRowVM
                {
                    No = index + 1,
                    QuestionText = x.Q.QContent,
                    StdAns = string.IsNullOrEmpty(x.Rec.ExamStdAns)
                                ? "（未作答）"
                                : x.Rec.ExamStdAns,
                    CorrectAns = string.IsNullOrEmpty(x.Rec.ExamAns)
                                ? x.Q.QAns
                                : x.Rec.ExamAns,
                    IsCorrect = x.Rec.ExamStdAnsRight == "G"
                })
                .ToList();

            var vm = new ExamBookDetailVM
            {
                ExamId = examId,
                ExamName = examName,
                TotalQuestions = totalQ,
                CorrectCount = correct,
                TotalScore = totalScore,

                ExamStdPK = stdPk,
                StdName = stdName,
                Rows = rows
            };

            return View(vm);
        }

        // ====== 刪除：某位學生的一次測驗紀錄 ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int examId, int stdPk, long attemptTicks)
        {
            DateTime attemptTime = new DateTime(attemptTicks);

            var recs = db.StdExamRec
                         .Where(r => r.ExamID == examId
                                  && r.ExamStdPK == stdPk
                                  && r.ExamAnsST.HasValue
                                  && r.ExamAnsST.Value == attemptTime)
                         .ToList();

            if (!recs.Any())
            {
                return HttpNotFound();
            }

            db.StdExamRec.RemoveRange(recs);
            db.SaveChanges();

            TempData["Msg"] = "已刪除該學生此次測驗紀錄。";
            return RedirectToAction("Index");
        }
    }
}
