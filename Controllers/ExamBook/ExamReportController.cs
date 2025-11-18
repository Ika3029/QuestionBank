using System.Drawing.Printing;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;

namespace 專題MVC修正.Controllers
{
    public class ExamReportController : Controller
    {
        readonly MQBEntities db = new MQBEntities();

        public ActionResult Report(int examId, int page = 1, int pageSize = 10)
        {
            // 這個報告是給「學生本人」看的，所以要看 Session
            if (Session["StdID"] == null)
            {
                // 沒登入就丟回首頁或登入頁
                return RedirectToAction("Index", "Home");
            }

            // 1. 先從 Session 的學號找出 StdPK
            string stdId = Session["StdID"].ToString();

            var stdPk = db.Std
                          .Where(s => s.StdID == stdId)
                          .Select(s => s.StdPK)
                          .FirstOrDefault();

            if (stdPk == 0)
            {
                TempData["Msg"] = "找不到對應的學生資料，請重新登入。";
                return RedirectToAction("Index", "Home");
            }

            // 2. 用 ExamID + StdPK 去抓「這位學生」在這次測驗的紀錄
            var data = (from rec in db.StdExamRec
                        join q in db.MoodQuestionBank
                            on rec.ExamMQBPK equals q.MQBPK
                        join team in db.MQBTeam
                            on q.MQBTeamPK equals team.MQBTeamPK
                        join cls in db.MQBClassName
                            on q.QClass equals cls.MQBClassPK
                        where rec.ExamID == examId
                           && rec.ExamStdPK == stdPk      // ⭐ 關鍵：只算這個學生
                        select new
                        {
                            rec.ExamStdAnsRight,
                            rec.ExamMQBPK,
                            ClassName = cls.MQBClassName1,
                            TeamName = team.MQBTeamContent
                        }).ToList();

            if (!data.Any())
            {
                TempData["Msg"] = "找不到這次測驗的答題紀錄。";
                return RedirectToAction("Index", "Home");
            }

            int total = data.Count;
            int correct = data.Count(x => x.ExamStdAnsRight == "G");

            var classCorrect = data
                .GroupBy(x => x.ClassName)
                .ToDictionary(g => g.Key, g => g.Count(x => x.ExamStdAnsRight == "G"));

            var classTotal = data
                .GroupBy(x => x.ClassName)
                .ToDictionary(g => g.Key, g => g.Count());

            var teamCorrect = data
                .GroupBy(x => x.TeamName)
                .ToDictionary(g => g.Key, g => g.Count(x => x.ExamStdAnsRight == "G"));

            var teamTotal = data
                .GroupBy(x => x.TeamName)
                .ToDictionary(g => g.Key, g => g.Count());

            // 錯題列表（全部）
            var wrongListAll = data
                .Where(x => x.ExamStdAnsRight != "G")
                .Select(x => x.ExamMQBPK)
                .AsQueryable();

            // 分頁後的錯題
            var wrongListPaged = wrongListAll
            .OrderBy(x => x)     // 按題號排序
                .Paginate(page, pageSize)
                .ToList();

            // ViewModel 給 View 顯示
            var vm = new ExamReportVM
            {
                ExamID = examId,
                TotalQuestions = total,
                CorrectCount = correct,
                Accuracy = total == 0 ? 0 : correct * 100.0 / total,

                ClassCorrect = classCorrect,
                ClassTotal = classTotal,
                TeamCorrect = teamCorrect,
                TeamTotal = teamTotal,

                WrongQuestionList = wrongListPaged,   // 改這裡
            };

            // 送給 View 的分頁資訊
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = wrongListAll.Count();

            return View(vm);
        
        }

        public ActionResult Latest()
        {
            // 沒登入就回首頁
            if (Session["StdID"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Session 存的是學號（字串）
            string stdId = Session["StdID"].ToString();

            // 先找到這個學生對應的 StdPK
            var stdPk = db.Std
                          .Where(s => s.StdID == stdId)
                          .Select(s => s.StdPK)
                          .FirstOrDefault();

            if (stdPk == 0)
            {
                TempData["Msg"] = "找不到對應的學生資料，請重新登入。";
                return RedirectToAction("Index", "Home");
            }

            // 用 StdPK 去 StdExamRec 找這個學生最新一筆測驗
            var latestExamId = db.StdExamRec
                                 .Where(x => x.ExamStdPK == stdPk)
                                 .OrderByDescending(x => x.ExamAnsST)   // 以作答開始時間排序，越新越前面
                                 .Select(x => x.ExamID)
                                 .FirstOrDefault();

            if (latestExamId == 0)
            {
                TempData["Msg"] = "目前還沒有測驗紀錄，先去做一份測驗試試看吧！";
                return RedirectToAction("Index", "Home");
            }

            // 導到我們剛剛做好的成績報告
            return RedirectToAction("Report", new { examId = latestExamId });
        }

    }
}
