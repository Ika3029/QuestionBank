using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using PagedList;
using 專題MVC修正.Models;

namespace 專題MVC修正.Controllers.User
{
    public class ExamRecordController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        [HttpPost]
        public ActionResult SaveRecord(int examId, int examDetPK, int examMQBPK, int stdPK, string studentAns, string correctAns)
        {
            // 建立新紀錄
            var rec = new StdExamRec
            {
                ExamID = examId,
                ExamDetPK = examDetPK,
                ExamMQBPK = examMQBPK,
                ExamStdPK = stdPK,
                ExamAns = correctAns,                // 標準答案
                ExamStdAns = studentAns,             // 學生作答內容
                ExamAnsST = DateTime.Now.AddSeconds(-20), // 假設開始時間是20秒前
                ExamAnsET = DateTime.Now,            // 結束時間
                ExamStdAnsRight = (studentAns == correctAns) ? "G" : "E", // 答對記 G，錯記 E
                ExamDefaultScore = (studentAns == correctAns) ? 1f : 0f,  // 配分
                ExamEmotion = null                   // 可留空或記錄情緒資料
            };

            db.StdExamRec.Add(rec);
            db.SaveChanges();

            return Json(new { success = true, msg = "紀錄已儲存" });
        }
    }
}