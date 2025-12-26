using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;
using PagedList;   

namespace 專題MVC修正.Controllers.User
{
    public class StdExamHistoryController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // 測驗本
        [HttpGet]
        public ActionResult MyExams(int page = 1, int pageSize = 10)
        {
            if (Session["StdPK"] == null)
            {
                TempData["LoginError"] = "請先登入學生帳號。";
                return RedirectToAction("Index", "Home");
            }

            int stdPK = int.Parse(Session["StdPK"].ToString());

            
            var temp = db.StdExamRec
                         .Where(r => r.ExamStdPK == stdPK)
                         .GroupBy(r => new
                         {
                             r.ExamID,   
                             r.ExamAnsET 
                         })
                         .Select(g => new
                         {
                             ExamID = g.Key.ExamID,
                             FinishTime = g.Key.ExamAnsET,
                             TotalQuestions = g.Count(),
                             CorrectCount = g.Count(x => x.ExamStdAnsRight == "G"),
                             TotalScore = g
                                 .Where(x => x.ExamStdAnsRight == "G")                 
                                 .Sum(x => (double?)(x.ExamDefaultScore ?? 0)) ?? 0    
                         })
                         .ToList();   

            
            var query = temp
                .Select(x => new MyExamRecordVM
                {
                    ExamID = x.ExamID ?? 0,                                  
                    TotalQuestions = x.TotalQuestions,
                    CorrectCount = x.CorrectCount,
                    TotalScore = x.TotalScore,
                    FinishTime = x.FinishTime ?? DateTime.MinValue,
                    AttemptTicks = (x.FinishTime ?? DateTime.MinValue).Ticks 
                })
                .OrderByDescending(x => x.FinishTime);

            
            var pagedList = query.ToPagedList(page, pageSize);

            return View(pagedList);
        }

        
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

            
            var list = temp
                .Select((x, idx) => new MyExamDetailRowVM
                {
                    No = idx + 1,
                    ExamDetPK = x.ExamDetPK ?? 0,    
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
