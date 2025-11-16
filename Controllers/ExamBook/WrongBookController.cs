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

        // GET: /User/WrongBook
        public ActionResult Index()
        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            // 1. 先撈出這位學生所有作答紀錄 + 題目（對 + 錯 都要）
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
            ).ToList();   // 先拉到記憶體，下面用 .NET 功能

            // 2. 以題目分組，只保留「至少有一筆答錯紀錄」的題目
            var result = raw
                .GroupBy(x => x.Q.MQBPK)
                .Where(g => g.Any(x => x.Rec.ExamStdAnsRight == "E")) // ★ 曾經錯過就留下
                .Select(g =>
                {
                    // 最新的一次作答（可能是對也可能是錯）
                    var latest = g.OrderByDescending(x => x.Rec.ExamAnsET).FirstOrDefault();

                    // 最新的一次「錯誤」作答（顯示在錯題詳情時用得到）
                    var latestWrong = g
                        .Where(x => x.Rec.ExamStdAnsRight == "E")
                        .OrderByDescending(x => x.Rec.ExamAnsET)
                        .FirstOrDefault() ?? latest;

                    return new WrongBookItemVM
                    {
                        MQBPK = g.Key,
                        QuestionText = latest.Q.QContent,

                        // 列表上想看「最後一次作答的答案」
                        StdAns = string.IsNullOrEmpty(latest.Rec.ExamStdAns)
                                    ? "（未作答）"
                                    : latest.Rec.ExamStdAns,

                        // 正解：優先用紀錄裡的 ExamAns，沒有就用題庫 QAns
                        CorrectAns = string.IsNullOrEmpty(latest.Rec.ExamAns)
                                        ? latest.Q.QAns
                                        : latest.Rec.ExamAns,

                        // 這題總共錯幾次
                        WrongCount = g.Count(x => x.Rec.ExamStdAnsRight == "E"),

                        // 顯示最後作答時間（不論正確與否）
                        LastAnsTime = latest.Rec.ExamAnsET
                    };
                })
                .OrderByDescending(x => x.LastAnsTime)
                .ToList();

            return View(result);
        }

        // 單題詳情
        public ActionResult Detail(int mqbpk)
        {
            var notLogin = RedirectIfNotLogin();
            if (notLogin != null) return notLogin;

            int stdPk = CurrentStdPK.Value;

            // 撈出這位學生這一題「答錯」的紀錄中，最後一次作答
            var rec = db.StdExamRec
                .Where(r => r.ExamStdPK == stdPk
                            && r.ExamMQBPK == mqbpk
                            && r.ExamStdAnsRight == "E") // 只看錯題
                .OrderByDescending(r => r.ExamAnsET)
                .FirstOrDefault();

            if (rec == null)
            {
                // 理論上不會發生，因為 Index 已經確認「曾經錯過」
                return HttpNotFound();
            }

            // 題目內容
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

                // 正解：優先用 ExamAns，沒有就用題庫 QAns
                CorrectAns = string.IsNullOrEmpty(rec.ExamAns) ? q.QAns : rec.ExamAns
            };

            return View(vm);
        }
    }
}
