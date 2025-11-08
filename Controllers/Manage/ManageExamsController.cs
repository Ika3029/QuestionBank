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
            // ▼ 改：用科別（JOIN 顯示中文名在 View）
            ViewBag.MQBClassPK = new SelectList(
                db.Set<MQBClassName>().OrderBy(x => x.MQBClassName1).ToList(),
                "MQBClassPK", "MQBClassName1"
            );
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Exams_Create(
            string ExamName,
            int MQBClassPK,          // ▼ 改：用科別 PK
            bool IsRandom,
            int QuestionCount = 10,
            double ScorePerQuestion = 1
        )
        {
            if (string.IsNullOrWhiteSpace(ExamName))
                ModelState.AddModelError("", "請輸入測驗卷名稱");
            if (!ModelState.IsValid)
            {
                ViewBag.MQBClassPK = new SelectList(
                    db.Set<MQBClassName>().OrderBy(x => x.MQBClassName1).ToList(),
                    "MQBClassPK", "MQBClassName1", MQBClassPK
                );
                return View();
            }

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
                // ▼ 改：依科別抽題
                var qids = db.Set<MoodQuestionBank>()
                             .Where(q => q.QClass == MQBClassPK)
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
                        // ExamMQBTeamPK 可不填；若你要保留可加欄位 ExamMQBClassPK
                        ExamDefaultScore = ScorePerQuestion,
                        SortOrder = sort++
                    });
                }
                db.SaveChanges();

                TempData["ok"] = $"測驗卷建立成功（隨機 {qids.Count} 題）";
                return RedirectToAction("Exams_Index");
            }

            // 手動挑題：帶入科別
            return RedirectToAction("SelectQuestions", new { id = em.ExamID, qclass = MQBClassPK, score = ScorePerQuestion });
        }

        // 手動挑題（GET）— 用「科別(QClass)」篩選 + 分頁 + 顯示中文科別名稱（修正編譯錯誤）
        [HttpGet]
        public ActionResult SelectQuestions(
            int id,
            int? qclass = null,
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

            // 下拉資料來源（科別、題型、章、節）
            var baseQ = db.Set<MoodQuestionBank>().AsQueryable();
            var sourceForFilters = baseQ;
            if (qclass.HasValue) sourceForFilters = sourceForFilters.Where(x => x.QClass == qclass.Value);

            ViewBag.ClassList = new SelectList(
                db.Set<MQBClassName>().OrderBy(x => x.MQBClassName1).ToList(),
                "MQBClassPK", "MQBClassName1", qclass
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

            // 保留目前篩選值
            ViewBag.QClass = qclass;
            ViewBag.QType = qtype;
            ViewBag.Chapter = chapter;
            ViewBag.Session = session;
            ViewBag.Keyword = keyword;

            // 主查詢：JOIN 科別表，顯示中文名稱
            var query = from q in db.Set<MoodQuestionBank>()
                        join c in db.Set<MQBClassName>() on q.QClass equals c.MQBClassPK
                        select new { q, c };

            if (qclass.HasValue) query = query.Where(x => x.q.QClass == qclass.Value);
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
                    MQBClassPK = x.q.QClass,          // ← 去掉 ?? 0，因為是非 nullable int
                    MQBClassName1 = x.c.MQBClassName1,    // 中文科別名
                    MQBTeamContent = null,                // 你如不需要，可留空
                    MQBTeamYN = null,
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
                    MQBTeamPK = 0,                        // ← 不給 null，若 DTO 是 int
                    ExamID = id
                })
                .ToPagedList(page < 1 ? 1 : page, pageSize);

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
                            .OrderBy(x => x.ExamDetPK)
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
