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

        // ====== 測驗本列表：顯示所有學生的紀錄 ======
        public ActionResult Index()
        {
            // StdExamRec 連接 Std，依「學生 + 考卷」分組
            var examSummary = (
                from r in db.StdExamRec
                join s in db.Std on r.ExamStdPK equals s.StdPK
                group new { r, s } by new
                {
                    r.ExamID,
                    s.StdPK,
                    s.StdName
                }
                into g
                select new ExamBookSummaryVM
                {
                    ExamID = g.Key.ExamID,
                    // 🔸 新增：學生 PK、學生姓名（記得在 VM 裡加欄位）
                    ExamStdPK = g.Key.StdPK,
                    StdName = g.Key.StdName,

                    TotalQuestions = g.Count(),
                    CorrectCount = g.Count(x => x.r.ExamStdAnsRight == "E"),
                    Score = g.Sum(x => x.r.ExamDefaultScore),
                    StartTime = g.Min(x => x.r.ExamAnsST),
                    EndTime = g.Max(x => x.r.ExamAnsET)
                }
            )
            .OrderByDescending(x => x.EndTime)
            .ToList();

            return View(examSummary);
        }

        // ====== 單次測驗的作答明細（指定學生 + 考卷） ======
        public ActionResult Details(int examId, int stdPk)
        {
            // 用 StdExamRec + MoodQuestionBank JOIN 把題目撈出來
            var list = (
                from r in db.StdExamRec
                join q in db.MoodQuestionBank
                    on r.ExamMQBPK equals q.MQBPK
                join s in db.Std
                    on r.ExamStdPK equals s.StdPK
                where r.ExamStdPK == stdPk
                      && r.ExamID == examId
                orderby r.ExamDetPK
                select new
                {
                    Rec = r,    // 作答紀錄
                    Q = q,      // 題目
                    S = s       // 學生
                }
            ).ToList();

            if (!list.Any())
            {
                return HttpNotFound();
            }

            // 測驗主檔（名字）
            var exam = db.ExamMaster.FirstOrDefault(e => e.ExamID == examId);
            var stdName = list.First().S.StdName;

            int totalQ = list.Count;
            int correct = list.Count(x => x.Rec.ExamStdAnsRight == "E");

            // 這裡假設答對才計分，分數來自 ExamDefaultScore
            double? totalScore = list
                .Where(x => x.Rec.ExamStdAnsRight == "E")
                .Sum(x => (double?)x.Rec.ExamDefaultScore);

            // 每一題的明細列
            var rows = list
                .Select((x, index) => new ExamBookDetailRowVM
                {
                    No = index + 1,
                    QuestionText = x.Q.QContent,
                    StdAns = string.IsNullOrEmpty(x.Rec.ExamStdAns)
                                ? "（未作答）"
                                : x.Rec.ExamStdAns,
                    CorrectAns = x.Rec.ExamAns,   // 或 x.Q.QAns 皆可
                    IsCorrect = x.Rec.ExamStdAnsRight == "E"
                })
                .ToList();

            var vm = new ExamBookDetailVM
            {
                ExamId = examId,
                ExamName = exam != null ? exam.ExamName : ("測驗 " + examId),
                TotalQuestions = totalQ,
                CorrectCount = correct,
                TotalScore = totalScore,
                // 🔸 額外帶出學生資訊（記得在 VM 裡加欄位）
                ExamStdPK = stdPk,
                StdName = stdName,
                Rows = rows
            };

            return View(vm);
        }

        // ====== 刪除：某位學生的一次測驗紀錄 ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int examId, int stdPk)
        {
            var recs = db.StdExamRec
                         .Where(r => r.ExamID == examId && r.ExamStdPK == stdPk)
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
