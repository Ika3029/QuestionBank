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

        //  測驗本列表
        public ActionResult Index(string keyword, int page = 1, int pageSize = 10)
        {
            
            var query = from r in db.StdExamRec
                        join s in db.Std on r.ExamStdPK equals s.StdPK
                        group new { r, s } by new
                        {
                            r.ExamID,
                            r.ExamStdPK,
                            s.StdName,
                            r.ExamAnsST      
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

                            // 只算答對題目
                            Score = g.Where(x => x.r.ExamStdAnsRight == "G")
                                     .Sum(x => (double?)x.r.ExamDefaultScore) ?? 0,

                            StartTime = g.Key.ExamAnsST,
                            EndTime = g.Max(x => x.r.ExamAnsET),

                            
                            AttemptTicks = 0
                        };

            // 關鍵字搜尋
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.StdName.Contains(keyword) ||
                    (x.ExamID.HasValue && x.ExamID.Value.ToString().Contains(keyword))
                );
            }

            
            int totalItems = query.Count();

            
            var pageQuery = query
                .OrderByDescending(x => x.EndTime)
                .ThenByDescending(x => x.ExamID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            
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

       
        [HttpGet]
        public ActionResult Details(int examId, int stdPk, long attemptTicks)
        {
            
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
                      && r.ExamAnsST.Value == attemptTime   
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
                return HttpNotFound();   
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

        //  刪除
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
