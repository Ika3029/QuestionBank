using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic; 
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;
using PagedList;
using System.Data.Entity; 


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

        //建卷（GET）
        [HttpGet]
        public ActionResult Exams_Create()
        {
            ViewBag.MQBClassPK = new SelectList(
                db.Set<MQBClassName>().OrderBy(x => x.MQBClassName1).ToList(),
                "MQBClassPK", "MQBClassName1"
            );
            ViewBag.MQBTeamPK = new SelectList(
                db.Set<MQBTeam>().OrderBy(x => x.MQBTeamContent).ToList(),
                "MQBTeamPK", "MQBTeamContent"
            );
            return View();
        }

        // 建卷（POST）
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Exams_Create(
            string ExamName,
            int MQBClassPK,          
            int? MQBTeamPK,          
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
                ViewBag.MQBTeamPK = new SelectList(
                    db.Set<MQBTeam>().OrderBy(x => x.MQBTeamContent).ToList(),
                    "MQBTeamPK", "MQBTeamContent", MQBTeamPK
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
            db.SaveChanges(); 

            if (IsRandom)
            {
                
                var qset = db.Set<MoodQuestionBank>().Where(q => q.QClass == MQBClassPK);
                if (MQBTeamPK.HasValue) qset = qset.Where(q => q.MQBTeamPK == MQBTeamPK.Value);

                var qids = qset.OrderBy(q => Guid.NewGuid())
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
                        ExamDefaultScore = ScorePerQuestion,
                        SortOrder = sort++
                    });
                }
                db.SaveChanges();

                TempData["ok"] = $"測驗卷建立成功（隨機 {qids.Count} 題）";
                return RedirectToAction("Exams_Index");
            }

            // 手動挑題
            return RedirectToAction("SelectQuestions", new
            {
                id = em.ExamID,
                qclass = MQBClassPK,
                team = MQBTeamPK,     
                score = ScorePerQuestion
            });
        }

        // 手動挑題（GET）
        [HttpGet]
        public ActionResult SelectQuestions(
            int id,
            int? qclass = null,
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

            
            var baseQ = db.Set<MoodQuestionBank>().AsQueryable();
            if (qclass.HasValue) baseQ = baseQ.Where(x => x.QClass == qclass.Value);
            if (team.HasValue) baseQ = baseQ.Where(x => x.MQBTeamPK == team.Value);

            // 科別
            ViewBag.ClassList = new SelectList(
                db.Set<MQBClassName>().OrderBy(x => x.MQBClassName1).ToList(),
                "MQBClassPK", "MQBClassName1", qclass
            );
            // 題組
            ViewBag.TeamList = new SelectList(
                db.Set<MQBTeam>().OrderBy(x => x.MQBTeamContent).ToList(),
                "MQBTeamPK", "MQBTeamContent", team
            );
            //其他
            ViewBag.QtypeList = new SelectList(
                baseQ.Select(x => x.QType).Distinct().OrderBy(x => x)
                     .Select(x => new { Value = x, Text = x.ToString() }).ToList(),
                "Value", "Text", qtype
            );
            ViewBag.ChapterList = new SelectList(
                baseQ.Select(x => x.MQBChapter).Distinct().OrderBy(x => x)
                     .Select(x => new { Value = x, Text = x.ToString() }).ToList(),
                "Value", "Text", chapter
            );
            ViewBag.SessionList = new SelectList(
                baseQ.Select(x => x.MQBSession).Distinct().OrderBy(x => x)
                     .Select(x => new { Value = x, Text = x.ToString() }).ToList(),
                "Value", "Text", session
            );

            
            ViewBag.QClass = qclass;
            ViewBag.Team = team;
            ViewBag.QType = qtype;
            ViewBag.Chapter = chapter;
            ViewBag.Session = session;
            ViewBag.Keyword = keyword;

            
            var query = from q in db.Set<MoodQuestionBank>()
                        join c in db.Set<MQBClassName>() on q.QClass equals c.MQBClassPK
                        join t0 in db.Set<MQBTeam>() on q.MQBTeamPK equals t0.MQBTeamPK into tj
                        from t in tj.DefaultIfEmpty()
                        select new { q, c, t };

            if (qclass.HasValue) query = query.Where(x => x.q.QClass == qclass.Value);
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
                    // 科別
                    MQBClassPK = x.q.QClass,
                    MQBClassName1 = x.c.MQBClassName1,

                    // 題組
                    MQBTeamPK = x.q.MQBTeamPK,
                    MQBTeamContent = x.t != null ? x.t.MQBTeamContent : null,
                    MQBTeamYN = x.t != null ? x.t.MQBTeamYN : null,

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
                    ExamID = id
                })
                .ToPagedList(page < 1 ? 1 : page, pageSize);

            return View(paged);
        }


        //手動挑題（POST）
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

        //明細
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

        // 考試流程
        public class ExamRunQuestionVM
        {
            public int ExamID { get; set; }
            public int Index { get; set; }      
            public int Total { get; set; }      
            public int ExamDetPK { get; set; }
            public int MQBPK { get; set; }
            public string QContent { get; set; }
            public string QOptionA { get; set; }
            public string QOptionB { get; set; }
            public string QOptionC { get; set; }
            public string QOptionD { get; set; }
            public string CorrectAns { get; set; } 
            public string Selected { get; set; }   
        }

        public class ExamRunResultVM
        {
            public ExamMaster Exam { get; set; }
            public int Total { get; set; }
            public int CorrectCount { get; set; }
            public double ScorePerQuestion { get; set; }
            public double TotalScore { get { return CorrectCount * ScorePerQuestion; } }
            public List<(int No, string UserAns, string CorrectAns, string QContent)> Detail { get; set; }
        }

        const string ExamSessionPrefix = "ExamRun_";

        // 開始考試 
        [HttpGet]
        public ActionResult StartExam(int id, int index = 1)
        {
            
            if (Session["StdPK"] == null)
            {
                TempData["LoginError"] = "請先登入學生帳號再作答。";
                return RedirectToAction("Index", "Home");
            }

            int stdPK = 0;
            int.TryParse(Session["StdPK"].ToString(), out stdPK);

            if (stdPK <= 0)
            {
                TempData["LoginError"] = "學生登入資訊有誤，請重新登入。";
                return RedirectToAction("Index", "Home");
            }

            var qList = db.Set<ExamDetail>()
                          .Where(d => d.ExamID == id)
                          .OrderBy(d => d.SortOrder)
                          .ThenBy(d => d.ExamDetPK)
                          .Join(db.Set<MoodQuestionBank>(),
                                d => d.ExamMQBPK,
                                q => q.MQBPK,
                                (d, q) => new
                                {
                                    d.ExamDetPK,
                                    d.ExamID,
                                    d.ExamDefaultScore,
                                    q.MQBPK,
                                    q.QContent,
                                    q.QOptionA,
                                    q.QOptionB,
                                    q.QOptionC,
                                    q.QOptionD,
                                    q.QAns
                                })
                          .ToList();

            if (!qList.Any())
            {
                TempData["err"] = "此測驗卷尚未加入題目。";
                return RedirectToAction("Details", new { id });
            }

            if (index < 1) index = 1;
            if (index > qList.Count) index = qList.Count;

            var row = qList[index - 1];

            
            var key = ExamSessionPrefix + id;
            var ansDict = Session[key] as Dictionary<int, string>;
            if (ansDict == null) { ansDict = new Dictionary<int, string>(); Session[key] = ansDict; }
            ansDict.TryGetValue(row.MQBPK, out var selected);

            var vm = new ExamRunQuestionVM
            {
                ExamID = id,
                Index = index,
                Total = qList.Count,
                ExamDetPK = row.ExamDetPK,
                MQBPK = row.MQBPK,
                QContent = row.QContent,
                QOptionA = row.QOptionA,
                QOptionB = row.QOptionB,
                QOptionC = row.QOptionC,
                QOptionD = row.QOptionD,
                CorrectAns = row.QAns,
                Selected = selected
            };

            ViewBag.ScorePerQuestion = row.ExamDefaultScore ?? 1;
            return View(vm);
        }


        // 單題作答（POST）
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult StartExam(int examId, int mqbpk, int index, string choice, string nav)
        {
            var key = ExamSessionPrefix + examId;
            var ansDict = Session[key] as Dictionary<int, string>;
            if (ansDict == null) { ansDict = new Dictionary<int, string>(); Session[key] = ansDict; }
            ansDict[mqbpk] = choice ?? "";

            if (nav == "prev") return RedirectToAction("StartExam", new { id = examId, index = index - 1 });
            if (nav == "next") return RedirectToAction("StartExam", new { id = examId, index = index + 1 });
            return RedirectToAction("SubmitExam", new { id = examId }); // finish
        }

        // 交卷並對答案（GET）
        [HttpGet]
        public ActionResult SubmitExam(int id)
        {
            var key = ExamSessionPrefix + id;
            var ansDict = Session[key] as Dictionary<int, string> ?? new Dictionary<int, string>();

            var exam = db.Set<ExamMaster>().Find(id);
            if (exam == null) return HttpNotFound();

            var list = db.Set<ExamDetail>()
                         .Where(d => d.ExamID == id)
                         .OrderBy(d => d.SortOrder)
                         .ThenBy(d => d.ExamDetPK)
                         .Join(db.Set<MoodQuestionBank>(),
                               d => d.ExamMQBPK,
                               q => q.MQBPK,
                               (d, q) => new { d.ExamDetPK, d.ExamID, d.ExamDefaultScore, q.MQBPK, q.QContent, q.QAns })
                         .ToList();

            int correct = 0, no = 0;
            double scorePer = list.FirstOrDefault()?.ExamDefaultScore ?? 1;
            var detail = new List<(int No, string UserAns, string CorrectAns, string QContent)>();

            foreach (var it in list)
            {
                ++no;
                ansDict.TryGetValue(it.MQBPK, out var userAns);
                var isCorrect = !string.IsNullOrWhiteSpace(userAns)
                                && string.Equals(userAns.Trim(), (it.QAns ?? "").Trim(), System.StringComparison.OrdinalIgnoreCase);
                if (isCorrect) correct++;

                detail.Add((no, userAns ?? "", it.QAns ?? "", it.QContent));
            }

            
            Session.Remove(key);

            

            
            if (Session["StdPK"] == null)
            {
                TempData["err"] = "找不到學生登入資訊，無法寫入作答紀錄。請重新登入後再試。";
                return RedirectToAction("Index", "Home");
            }

            int stdPK = 0;
            int.TryParse(Session["StdPK"].ToString(), out stdPK);

            
            bool stdExists = stdPK > 0 && db.Std.Any(s => s.StdPK == stdPK);
            if (!stdExists)
            {
                TempData["err"] = "學生資料不存在，無法寫入作答紀錄。";
                return RedirectToAction("Index", "Home");
            }

            
            
            var now = System.DateTime.Now;
            foreach (var it in list)
            {
                ansDict.TryGetValue(it.MQBPK, out var userAns);
                var correctAns = it.QAns ?? "";
                var isRight = string.Equals((userAns ?? "").Trim(), correctAns.Trim(), System.StringComparison.OrdinalIgnoreCase);

                var rec = new StdExamRec
                {
                    ExamID = id,
                    ExamDetPK = it.ExamDetPK,
                    ExamMQBPK = it.MQBPK,
                    ExamDefaultScore = it.ExamDefaultScore ?? 1,
                    ExamAns = correctAns,         
                    ExamStdPK = stdPK,            
                    ExamStdAns = userAns,         
                    ExamAnsST = now,              
                    ExamAnsET = now,
                    ExamStdAnsRight = isRight ? "G" : "E",
                    ExamEmotion = null
                };

                db.StdExamRec.Add(rec);
            }
            db.SaveChanges();

            

            var vm = new ExamRunResultVM
            {
                Exam = exam,
                Total = list.Count,
                CorrectCount = correct,
                ScorePerQuestion = scorePer,
                Detail = detail
            };
            return View(vm);
        }

    }
}
