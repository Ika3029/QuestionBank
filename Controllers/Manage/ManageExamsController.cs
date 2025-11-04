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
        public ActionResult Exams_Create(
    string ExamName,
    int MQBTeamPK,
    bool IsRandom,
    int QuestionCount = 10,
    double ScorePerQuestion = 1 // ← float 對應 C# 用 double
)
        {
            if (string.IsNullOrWhiteSpace(ExamName))
                ModelState.AddModelError("", "請輸入測驗卷名稱");
            if (!ModelState.IsValid)
            {
                ViewBag.MQBTeamPK = new SelectList(db.Set<MQBTeam>().OrderBy(x => x.MQBTeamContent).ToList(),
                                                   "MQBTeamPK", "MQBTeamContent", MQBTeamPK);
                return View();
            }

            // 依你的規格：ExamSDate/ExamEDate/ExamDurationTime 必填
            var now = DateTime.Now;
            var em = new ExamMaster
            {
                ExamName = ExamName,
                ExamSDate = now,
                ExamEDate = now,           // 先同一天；之後在編輯頁修改
                     // 規格預設 60 分鐘
            };
            db.Set<ExamMaster>().Add(em);
            db.SaveChanges(); // 取得 ExamID

            if (IsRandom)
            {
                var qids = db.Set<MoodQuestionBank>()
                             .Where(q => q.MQBTeamPK == MQBTeamPK)
                             .OrderBy(q => Guid.NewGuid())
                             .Take(QuestionCount)
                             .Select(q => q.MQBPK)
                             .ToList();

                int sort = 1;
                foreach (var qid in qids)
                {
                    db.Set<ExamDetail>().Add(new ExamDetail
                    {
                        ExamID = em.ExamID,
                        ExamQMode = "0",            // 題目
                        ExamMQBPK = qid,            // 可為 null；這裡有值
                        ExamMQBTeamPK = MQBTeamPK,  // 可為 null；提供參考
                        ExamDefaultScore = ScorePerQuestion, // double? 對齊 float
                        SortOrder = sort++
                    });
                }
                db.SaveChanges();

                TempData["ok"] = $"測驗卷建立成功（隨機 {qids.Count} 題）";
                return RedirectToAction("Exams_Index");
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
        public ActionResult SelectQuestions(int examMasterPK, double scorePerQuestion, int[] selectedQIds)
        {
            if (selectedQIds == null || selectedQIds.Length == 0)
            {
                TempData["err"] = "請至少選一題";
                return RedirectToAction("Details", new { id = examMasterPK });
            }

            // 取得目前排序最大值，接續排
            int sort = (db.Set<ExamDetail>()
                          .Where(d => d.ExamID == examMasterPK)
                          .Select(d => (int?)d.SortOrder)
                          .Max()) ?? 0;

            foreach (var qid in selectedQIds.Distinct())
            {
                db.Set<ExamDetail>().Add(new ExamDetail
                {
                    ExamID = examMasterPK,
                    ExamQMode = "0",
                    ExamMQBPK = qid,
                    ExamDefaultScore = scorePerQuestion, // double? 對應 float
                    SortOrder = ++sort
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

            var details = db.Set<ExamDetail>()
                            .Where(d => d.ExamID == id)
                            .Join(db.Set<MoodQuestionBank>(),
                                  d => d.ExamMQBPK,     // join 題庫 PK
                                  q => q.MQBPK,
                                  (d, q) => new ExamDetailRowVM
                                  {
                                      ExamDetPK = d.ExamDetPK,               // ← PK_ExamDetail
                                      ExamDefaultScore = d.ExamDefaultScore,  // double?
                                      ExamMQBPK = d.ExamMQBPK,               // int?
                                      QContent = q.QContent,
                                      QAns = q.QAns
                                  })
                            .OrderBy(x => x.ExamDetPK) // 或 SortOrder
                            .ToList();

            var vm = new ExamDetailsVM { Exam = exam, Details = details };
            return View(vm);
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
