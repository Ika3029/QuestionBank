using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;
using PagedList;   // ★ 分頁用

namespace 專題MVC修正.Controllers.User
{
    public class StdExamHistoryController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // 我的測驗本（每一列 = 一次完整測驗）＋ 分頁
        [HttpGet]
        public ActionResult MyExams(int page = 1, int pageSize = 10)
        {
            if (Session["StdPK"] == null)
            {
                TempData["LoginError"] = "請先登入學生帳號。";
                return RedirectToAction("Index", "Home");
            }

            int stdPK = int.Parse(Session["StdPK"].ToString());

            // 先在資料庫端做 GroupBy 和統計（這裡不要用 Ticks / DateTime.MinValue）
            var temp = db.StdExamRec
                         .Where(r => r.ExamStdPK == stdPK)
                         .GroupBy(r => new
                         {
                             r.ExamID,   // int?
                             r.ExamAnsET // DateTime?
                         })
                         .Select(g => new
                         {
                             ExamID = g.Key.ExamID,
                             FinishTime = g.Key.ExamAnsET,
                             TotalQuestions = g.Count(),
                             CorrectCount = g.Count(x => x.ExamStdAnsRight == "G"),
                             TotalScore = g
                                 .Where(x => x.ExamStdAnsRight == "G")                 // 只挑答對的題目
                                 .Sum(x => (double?)(x.ExamDefaultScore ?? 0)) ?? 0    // 把答對題目的配分加總
                         })
                         .ToList();   // ← 先撈回記憶體，後面就變成 LINQ to Objects

            // 再在記憶體裡組成 ViewModel，這裡就可以用 Ticks、DateTime.MinValue 等 .NET 功能
            var query = temp
                .Select(x => new MyExamRecordVM
                {
                    ExamID = x.ExamID ?? 0,                                  // int? -> int
                    TotalQuestions = x.TotalQuestions,
                    CorrectCount = x.CorrectCount,
                    TotalScore = x.TotalScore,
                    FinishTime = x.FinishTime ?? DateTime.MinValue,
                    AttemptTicks = (x.FinishTime ?? DateTime.MinValue).Ticks // 這裡才用 Ticks
                })
                .OrderByDescending(x => x.FinishTime);

            // ★ 這裡改成回傳分頁結果
            var pagedList = query.ToPagedList(page, pageSize);

            return View(pagedList);
        }

        // 單次測驗詳情：ExamID + AttemptTicks (完成時間)
        [HttpGet]
        public ActionResult MyExamDetail(int examId, long attemptTicks)
        {
            if (Session["StdPK"] == null)
            {
                TempData["LoginError"] = "請先登入學生帳號。";
                return RedirectToAction("Index", "Home");
            }

            int stdPK = int.Parse(Session["StdPK"].ToString());
            var finishTime = new DateTime(attemptTicks);

            // 先把資料從 DB 撈出來（Join 題目表）
            var temp = db.StdExamRec
                         .Where(r => r.ExamStdPK == stdPK
                                  && r.ExamID == examId
                                  && r.ExamAnsET == finishTime)
                         .Join(db.MoodQuestionBank,
                               r => r.ExamMQBPK,
                               q => q.MQBPK,
                               (r, q) => new
                               {
                                   r.ExamDetPK,
                                   r.ExamDefaultScore,
                                   r.ExamStdAnsRight,
                                   r.ExamStdAns,
                                   r.ExamAns,
                                   q.QContent,
                                   q.QOptionA,
                                   q.QOptionB,
                                   q.QOptionC,
                                   q.QOptionD
                               })
                         .OrderBy(x => x.ExamDetPK)
                         .ToList();

            // 再轉成 ViewModel（這裡用純 .NET，可以用 Trim、Equals 等）
            var list = temp
                .Select((x, idx) => new MyExamDetailRowVM
                {
                    No = idx + 1,
                    ExamDetPK = x.ExamDetPK ?? 0,     // int? → int
                    Score = x.ExamDefaultScore ?? 0,
                    QContent = x.QContent,
                    QOptionA = x.QOptionA,
                    QOptionB = x.QOptionB,
                    QOptionC = x.QOptionC,
                    QOptionD = x.QOptionD,
                    CorrectAns = x.ExamAns,
                    StdAns = x.ExamStdAns,
                    IsCorrect = !string.IsNullOrWhiteSpace(x.ExamStdAns)
                                && string.Equals(
                                       x.ExamStdAns.Trim(),
                                       (x.ExamAns ?? "").Trim(),
                                       StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            ViewBag.ExamId = examId;
            ViewBag.FinishTime = finishTime;

            return View(list);
        }
    }
}
