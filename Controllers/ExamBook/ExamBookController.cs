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

        private int? CurrentStdPK
        {
            get { return Session["StdPK"] as int?; }
        }

        private ActionResult RedirectIfNotLogin()
        {
            if (CurrentStdPK == null)
            {
                TempData["LoginError"] = "請先登入學生帳號再查看測驗本。";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        // 測驗本列表
        public ActionResult Index()
        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            var examSummary = db.StdExamRec
                .Where(r => r.ExamStdPK == stdPk)           // ← 這裡用你 StdExamRec 裡指向 Std 的 FK
                .GroupBy(r => r.ExamID)
                .Select(g => new ExamBookSummaryVM
                {
                    ExamID = g.Key,
                    TotalQuestions = g.Count(),
                    CorrectCount = g.Count(x => x.ExamStdAnsRight == "E"),
                    Score = g.Sum(x => x.ExamDefaultScore),
                    StartTime = g.Min(x => x.ExamAnsST),
                    EndTime = g.Max(x => x.ExamAnsET)
                })
                .OrderByDescending(x => x.EndTime)
                .ToList();

            return View(examSummary);
        }

        // ⭐ 新增：單次測驗的作答明細
        public ActionResult Details(int examId)
        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            // 用 StdExamRec + MoodQuestionBank JOIN 把題目撈出來
            var list = (
                from r in db.StdExamRec
                join q in db.MoodQuestionBank
                    on r.ExamMQBPK equals q.MQBPK      // ← FK：StdExamRec.ExamMQBPK → MoodQuestionBank.MQBPK
                where r.ExamStdPK == stdPk             // 這裡用你紀錄表裡指向學生 PK 的欄位
                   && r.ExamID == examId
                orderby r.ExamDetPK
                select new
                {
                    Rec = r,   // 作答紀錄
                    Q = q    // 題目
                }
            ).ToList();

            if (!list.Any())
            {
                return HttpNotFound();
            }

            // 測驗主檔（名字）
            var exam = db.ExamMaster.FirstOrDefault(e => e.ExamID == examId);

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
                    // ⭐ 題目文字：用 QContent
                    QuestionText = x.Q.QContent,

                    // 學生答案，沒作答顯示「(未作答)」
                    StdAns = string.IsNullOrEmpty(x.Rec.ExamStdAns)
                                ? "（未作答）"
                                : x.Rec.ExamStdAns,

                    // 正確答案：用 StdExamRec.ExamAns（或直接用 x.Q.QAns 也可以）
                    CorrectAns = x.Rec.ExamAns,
                    //CorrectAns = x.Q.QAns,

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
                Rows = rows
            };

            return View(vm);
        }

    }
}
