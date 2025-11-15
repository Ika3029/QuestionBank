using System;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;

namespace 專題MVC修正.Controllers.User
{
    public class WrongBookController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // 目前登入學生的 StdPK
        private int? CurrentStdPK
        {
            get { return Session["StdPK"] as int?; }
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

        // GET: /User/WrongBook
        public ActionResult Index()
        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            // 1. 撈出這位學生所有「答錯」的作答紀錄 + 題目
            var raw = (
                from r in db.StdExamRec
                join q in db.MoodQuestionBank
                    on r.ExamMQBPK equals q.MQBPK          // FK：題目
                where r.ExamStdPK == stdPk                 // 這位學生
                      && r.ExamStdAnsRight != "E"          // 非 E = 答錯（依你自己的規則）
                select new
                {
                    Rec = r,
                    Q = q
                }
            ).ToList(); // 先拉到記憶體再做分組

            // 2. 以題目分組，每題只留「最後一次作答」當代表
            var result = raw
                .GroupBy(x => x.Q.MQBPK)
                .Select(g =>
                {
                    var latest = g
                        .OrderByDescending(x => x.Rec.ExamAnsET) // 最後答題時間
                        .FirstOrDefault();

                    return new WrongBookItemVM
                    {
                        MQBPK = g.Key,
                        QuestionText = latest.Q.QContent,
                        StdAns = string.IsNullOrEmpty(latest.Rec.ExamStdAns)
                                    ? "（未作答）"
                                    : latest.Rec.ExamStdAns,
                        // 正確答案你可以用作答紀錄裡的 ExamAns，也可以用題目表的 QAns
                        CorrectAns = latest.Rec.ExamAns, // 或 latest.Q.QAns
                        WrongCount = g.Count(),          // 這題總共錯幾次
                        LastAnsTime = latest.Rec.ExamAnsET
                    };
                })
                .OrderByDescending(x => x.LastAnsTime)
                .ToList();

            return View(result);
        }
        public ActionResult Detail(int mqbpk)
        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            // 撈出這位學生針對這一題「答錯」的紀錄中，最後一次作答
            var rec = db.StdExamRec
                .Where(r => r.ExamStdPK == stdPk      // ← 這裡用你紀錄表裡指向學生 PK 的欄位
                            && r.ExamMQBPK == mqbpk   // 這一題
                            && r.ExamStdAnsRight != "E")
                .OrderByDescending(r => r.ExamAnsET)
                .FirstOrDefault();

            if (rec == null)
            {
                return HttpNotFound();
            }

            // 題目內容（MoodQuestionBank）
            var q = db.MoodQuestionBank.FirstOrDefault(x => x.MQBPK == mqbpk);
            if (q == null)
            {
                return HttpNotFound();
            }

            var vm = new WrongBookDetailVM
            {
                MQBPK = q.MQBPK,
                QuestionText = q.QContent,     // 題目文字

                OptionA = q.QOptionA,
                OptionB = q.QOptionB,
                OptionC = q.QOptionC,
                OptionD = q.QOptionD,

                StdAns = string.IsNullOrEmpty(rec.ExamStdAns)
                            ? "（未作答）"
                            : rec.ExamStdAns,
                // 正解可以用作答紀錄 ExamAns，也可以直接用題目表的 QAns
                CorrectAns = string.IsNullOrEmpty(rec.ExamAns) ? q.QAns : rec.ExamAns
            };

            return View(vm);
        }

    }
}
