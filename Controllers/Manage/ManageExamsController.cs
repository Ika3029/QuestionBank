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
            double ScorePerQuestion = 1 // float 對應 C# 用 double
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

            // 必填：ExamSDate/ExamEDate
            var now = DateTime.Now;
            var em = new ExamMaster
            {
                ExamName = ExamName,
                ExamSDate = now,
                ExamEDate = now
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
                        ExamQMode = "0",
                        ExamMQBPK = qid,
                        ExamMQBTeamPK = MQBTeamPK,
                        ExamDefaultScore = ScorePerQuestion,
                        SortOrder = sort++
                    });
                }
                db.SaveChanges();

                TempData["ok"] = $"測驗卷建立成功（隨機 {qids.Count} 題）";
                return RedirectToAction("Exams_Index");
            }

            // 手動挑題（帶入 team 與 score）
            return RedirectToAction("SelectQuestions", new { id = em.ExamID, team = MQBTeamPK, score = ScorePerQuestion });
        }

        // 手動挑題（GET）— 加入下拉篩選 + 分頁
        [HttpGet]
        public ActionResult SelectQuestions(
            int id,
            int? team = null,
            int? qtype = null,
            int? chapter = null,
            int? session = null,
            string keyword = null,
            int page = 1,
            int pageSize = 10,
            double score = 1
        )
        {
            ViewBag.ExamMasterPK = id;
            ViewBag.ScorePerQuestion = score;

            // 先組 base query
            var baseQ = db.Set<MoodQuestionBank>().AsQueryable();

            // 用於下拉來源：可依 team 篩選後再取 distinct（體感較友善）
            var sourceForFilters = baseQ;
            if (team.HasValue) sourceForFilters = sourceForFilters.Where(x => x.MQBTeamPK == team.Value);

            // 下拉資料（隊別、題型、章、節）
            ViewBag.TeamList = new SelectList(
                db.Set<MQBTeam>().OrderBy(x => x.MQBTeamContent).ToList(),
                "MQBTeamPK", "MQBTeamContent", team
            );

            ViewBag.QtypeList = new SelectList(
                sourceForFilters.Select(x => x.QType).Distinct().OrderBy(x => x)
                                .Select(x => new { Value = x, Text = x.ToString() }).ToList(),
                "Value", "Text", qtype
            );

            ViewBag.ChapterList = new SelectList(
                sourceForFilters.Select(x => x.MQBChapter).Distinct().OrderBy(x => x)
                                .Select(x => new { Value = x, Text = x.ToString() }).ToList(),
                "Value", "Text", chapter
            );

            ViewBag.SessionList = new SelectList(
                sourceForFilters.Select(x => x.MQBSession).Distinct().OrderBy(x => x)
                                .Select(x => new { Value = x, Text = x.ToString() }).ToList(),
                "Value", "Text", session
            );

            // 讓 View 能保留目前篩選值
            ViewBag.Team = team;
            ViewBag.QType = qtype;
            ViewBag.Chapter = chapter;
            ViewBag.Session = session;
            ViewBag.Keyword = keyword;

            // 主查詢：join 隊別、依條件過濾
            var query = from q in db.Set<MoodQuestionBank>()
                        join t in db.Set<MQBTeam>() on q.MQBTeamPK equals t.MQBTeamPK
                        select new { q, t };

            if (team.HasValue) query = query.Where(x => x.q.MQBTeamPK == team.Value);
            if (qtype.HasValue) query = query.Where(x => x.q.QType == qtype.Value);
            if (chapter.HasValue) query = query.Where(x => x.q.MQBChapter == chapter.Value);
            if (session.HasValue) query = query.Where(x => x.q.MQBSession == session.Value);
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.q.QContent.Contains(keyword));

            var paged = query
                .OrderByDescending(x => x.q.MQBSort)
                .ThenByDescending(x => x.q.MQBPK)
                .Select(x => new ExamDselectlist
                {
                    MQBClassPK = 0,
                    MQBClassName1 = null,
                    MQBTeamContent = x.t.MQBTeamContent,
                    MQBTeamYN = x.t.MQBTeamYN,
                    MQBSort = x.q.MQBSort,
                    MQBChapter = x.q.MQBChapter.ToString(),
                    MQBSession = x.q.MQBSession.ToString(),
                    QType = x.q.QType.ToString(),
                    QContent = x.q.QContent,
                    QOptionA = x.q.QOptionA,
                    QOptionB = x.q.QOptionB,
                    QOptionC = x.q.QOptionC,
                    QOptionD = x.q.QOptionD,
                    ExamDefaultScore = score,
                    MQBPK = x.q.MQBPK,
                    MQBTeamPK = x.q.MQBTeamPK,
                    ExamID = id
                })
                .ToPagedList(page < 1 ? 1 : page, pageSize);   // ← 分頁

            return View(paged);
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
                    ExamDefaultScore = scorePerQuestion,
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
                                  d => d.ExamMQBPK,
                                  q => q.MQBPK,
                                  (d, q) => new ExamDetailRowVM
                                  {
                                      ExamDetPK = d.ExamDetPK,
                                      ExamDefaultScore = d.ExamDefaultScore,
                                      ExamMQBPK = d.ExamMQBPK,
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
