using System;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;
using PagedList;   

namespace 專題MVC修正.Controllers.User
{
    public class WrongBookController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        
        private int? CurrentStdPK
        {
            get
            {
                if (Session["StdPK"] == null) return null;
                return (int)Session["StdPK"];
            }
        }

        private ActionResult RedirectIfNotLogin()
        {
            if (CurrentStdPK == null)
            {
                TempData["LoginError"] = "請先登入學生帳號再查看錯題本。";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        
        public ActionResult Index(int page = 1, int pageSize = 10)
        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            
            var raw = (
                from r in db.StdExamRec
                join q in db.MoodQuestionBank
                    on r.ExamMQBPK equals q.MQBPK
                where r.ExamStdPK == stdPk
                select new
                {
                    Rec = r,
                    Q = q
                }
            ).ToList();   

            
            var query = raw
                .GroupBy(x => x.Q.MQBPK)
                .Where(g => g.Any(x => x.Rec.ExamStdAnsRight == "E")) 
                .Select(g =>
                {
                    
                    var latest = g.OrderByDescending(x => x.Rec.ExamAnsET).FirstOrDefault();

                    
                    var latestWrong = g
                        .Where(x => x.Rec.ExamStdAnsRight == "E")
                        .OrderByDescending(x => x.Rec.ExamAnsET)
                        .FirstOrDefault() ?? latest;

                    return new WrongBookItemVM
                    {
                        MQBPK = g.Key,
                        QuestionText = latest.Q.QContent,

                        
                        StdAns = string.IsNullOrEmpty(latest.Rec.ExamStdAns)
                                    ? "（未作答）"
                                    : latest.Rec.ExamStdAns,

                        
                        CorrectAns = string.IsNullOrEmpty(latest.Rec.ExamAns)
                                        ? latest.Q.QAns
                                        : latest.Rec.ExamAns,

                        
                        WrongCount = g.Count(x => x.Rec.ExamStdAnsRight == "E"),

                        
                        LastAnsTime = latest.Rec.ExamAnsET
                    };
                })
                .OrderByDescending(x => x.LastAnsTime);

            
            var model = query.ToPagedList(page, pageSize);

            return View(model);
        }

        
        public ActionResult Detail(int mqbpk, int examId)

        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            
            var rec = db.StdExamRec
                .Where(r => r.ExamStdPK == stdPk
                            && r.ExamMQBPK == mqbpk
                            && r.ExamStdAnsRight == "E") 
                .OrderByDescending(r => r.ExamAnsET)
                .FirstOrDefault();

            if (rec == null)
            {
                
                return HttpNotFound();
            }

            
            var q = db.MoodQuestionBank.FirstOrDefault(x => x.MQBPK == mqbpk);
            if (q == null)
            {
                return HttpNotFound();
            }

            var vm = new WrongBookDetailVM
            {
                MQBPK = q.MQBPK,
                QuestionText = q.QContent,

                OptionA = q.QOptionA,
                OptionB = q.QOptionB,
                OptionC = q.QOptionC,
                OptionD = q.QOptionD,

                StdAns = string.IsNullOrEmpty(rec.ExamStdAns)
                            ? "（未作答）"
                            : rec.ExamStdAns,

                
                CorrectAns = string.IsNullOrEmpty(rec.ExamAns) ? q.QAns : rec.ExamAns
            };
            ViewBag.ExamID = examId;
            return View(vm);
        }
    }
}
