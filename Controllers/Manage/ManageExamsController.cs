using System;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;
using PagedList;

namespace 專題MVC修正.Controllers.Manage
{
    public class ManageExamsController : Controller
    {
        readonly MQBEntities db = new MQBEntities();

        // 列表
        public ActionResult Exams_Index(int page = 1, int pageSize = 10)
        {
            var data = db.Set<ExamMaster>()
                         .OrderByDescending(x => x.ExamID)
                         .ToPagedList(page < 1 ? 1 : page, pageSize);
            return View(data);
        }

        // 建卷
        [HttpGet]
        public ActionResult Exams_Create()
        {
            ViewBag.MQBTeamPK = new SelectList(db.Set<MQBTeam>().OrderBy(x => x.MQBTeamContent).ToList(),
                                               "MQBTeamPK", "MQBTeamContent");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Exams_Create(string ExamName, int MQBTeamPK, bool IsRandom, int QuestionCount = 10, int ScorePerQuestion = 1)
        {
            if (string.IsNullOrWhiteSpace(ExamName))
                ModelState.AddModelError("", "請輸入測驗卷名稱");
            if (!ModelState.IsValid)
            {
                ViewBag.MQBTeamPK = new SelectList(db.Set<MQBTeam>().OrderBy(x => x.MQBTeamContent).ToList(),
                                                   "MQBTeamPK", "MQBTeamContent", MQBTeamPK);
                return View();
            }

            // 🔸先只寫必要欄位（避免 ExamDurationTime 等不存在造成紅線）
            var em = new ExamMaster { ExamName = ExamName };
            db.Set<ExamMaster>().Add(em);
            db.SaveChanges(); // 取得 em.ExamID

            if (IsRandom)
            {
                var qids = db.Set<MoodQuestionBank>()
                             .Where(q => q.MQBTeamPK == MQBTeamPK)
                             .OrderBy(q => Guid.NewGuid())
                             .Take(QuestionCount)
                             .Select(q => q.MQBPK)
                             .ToList();

                foreach (var qid in qids)
                {
                    db.Set<ExamDetail>().Add(new ExamDetail
                    {
                        ExamID = em.ExamID,
                        ExamQMode = "0",                // 0=題目、1=題組
                        ExamMQBPK = qid,                // 🔴 正確欄位名
                        ExamDefaultScore = ScorePerQuestion
                    });
                }
                db.SaveChanges();

                TempData["ok"] = $"測驗卷建立成功（隨機 {qids.Count} 題）";
                return RedirectToAction("Index");
            }

            // 手動挑題
            return RedirectToAction("SelectQuestions", new { id = em.ExamID, team = MQBTeamPK, score = ScorePerQuestion });
        }

        // 手動挑題（先不 join 類別表，避免 MQBClassPK 紅線）
        // 手動挑題（參數型別改成 int?，避免 int 和 string 比較）
        [HttpGet]
        public ActionResult SelectQuestions(int id, int team, int score = 1, int? qtype = null, int? chapter = null, int? session = null)
        {
            ViewBag.ExamMasterPK = id;
            ViewBag.ScorePerQuestion = score;

            var data = (from q in db.Set<MoodQuestionBank>()
                        join t in db.Set<MQBTeam>() on q.MQBTeamPK equals t.MQBTeamPK
                        where q.MQBTeamPK == team
                           && (!qtype.HasValue || q.QType == qtype.Value)
                           && (!chapter.HasValue || q.MQBChapter == chapter.Value)
                           && (!session.HasValue || q.MQBSession == session.Value)
                        orderby q.MQBSort descending, q.MQBPK descending
                        select new ExamDselectlist
                        {
                            MQBClassPK = 0,                 // 暫不帶類別，避免紅線
                            MQBClassName1 = null,
                            MQBTeamContent = t.MQBTeamContent,
                            MQBTeamYN = t.MQBTeamYN,
                            MQBSort = q.MQBSort,
                            MQBChapter = q.MQBChapter.ToString(),  // DTO 是 string 就 ToString()
                            MQBSession = q.MQBSession.ToString(),
                            QType = q.QType.ToString(),
                            QContent = q.QContent,
                            QOptionA = q.QOptionA,
                            QOptionB = q.QOptionB,
                            QOptionC = q.QOptionC,
                            QOptionD = q.QOptionD,
                            ExamDefaultScore = score,
                            MQBPK = q.MQBPK,
                            MQBTeamPK = q.MQBTeamPK,
                            ExamID = id
                        }).ToList();

            return View(data);
        }


        // 手動挑題提交
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SelectQuestions(int examMasterPK, int scorePerQuestion, int[] selectedQIds)
        {
            if (selectedQIds == null || selectedQIds.Length == 0)
            {
                TempData["err"] = "請至少選一題";
                return RedirectToAction("Details", new { id = examMasterPK });
            }

            foreach (var qid in selectedQIds.Distinct())
            {
                db.Set<ExamDetail>().Add(new ExamDetail
                {
                    ExamID = examMasterPK,
                    ExamQMode = "0",
                    ExamMQBPK = qid,               // 🔴 正確欄位名
                    ExamDefaultScore = scorePerQuestion
                });
            }
            db.SaveChanges();
            return RedirectToAction("Details", new { id = examMasterPK });
        }

        // 明細（用 ExamDetail.ExamMQBPK 去 join 題庫）
        public ActionResult Details(int id)
        {
            var exam = db.Set<ExamMaster>().Find(id);
            if (exam == null) return HttpNotFound();

            var dets = db.Set<ExamDetail>()
                         .Where(d => d.ExamID == id)
                         .Join(db.Set<MoodQuestionBank>(),
                               d => d.ExamMQBPK,         // 🔴 正確欄位名
                               q => q.MQBPK,
                              (d, q) => new
                              {
                                  d.ExamDetPK,
                                  d.ExamDefaultScore,
                                  d.ExamMQBPK,
                                  QContent = q.QContent,
                                  QAns = q.QAns
                              })
                         .ToList();

            ViewBag.Details = dets;
            return View(exam);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RemoveDetail(int id)
        {
            var det = db.Set<ExamDetail>().Find(id);
            if (det == null) return HttpNotFound();
            var examId = det.ExamID;

            db.Set<ExamDetail>().Remove(det);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = examId });
        }
    }
}
