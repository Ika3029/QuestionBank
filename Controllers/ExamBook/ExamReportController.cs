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
            
            if (Session["StdID"] == null)
            {
                
                return RedirectToAction("Index", "Home");
            }

            
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

            
            var data = (from rec in db.StdExamRec
                        join q in db.MoodQuestionBank
                            on rec.ExamMQBPK equals q.MQBPK
                        join team in db.MQBTeam
                            on q.MQBTeamPK equals team.MQBTeamPK
                        join cls in db.MQBClassName
                            on q.QClass equals cls.MQBClassPK
                        where rec.ExamID == examId
                           && rec.ExamStdPK == stdPk      
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

            // 錯題列表
            var wrongListAll = data
                .Where(x => x.ExamStdAnsRight != "G")
                .Select(x => x.ExamMQBPK)
                .AsQueryable();

            // 分頁後的錯題
            var wrongListPaged = wrongListAll
            .OrderBy(x => x)     
                .Paginate(page, pageSize)
                .ToList();

            
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

                WrongQuestionList = wrongListPaged,   
            };

            
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = wrongListAll.Count();

            return View(vm);
        
        }

        public ActionResult Latest()
        {
            
            if (Session["StdID"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            
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

            
            var latestExamId = db.StdExamRec
                                 .Where(x => x.ExamStdPK == stdPk)
                                 .OrderByDescending(x => x.ExamAnsST)   
                                 .Select(x => x.ExamID)
                                 .FirstOrDefault();

            if (latestExamId == 0)
            {
                TempData["Msg"] = "目前還沒有測驗紀錄，先去做一份測驗試試看吧！";
                return RedirectToAction("Index", "Home");
            }

            
            return RedirectToAction("Report", new { examId = latestExamId });
        }

    }
}
