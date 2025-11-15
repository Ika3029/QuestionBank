using System;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs;

namespace 專題MVC修正.Controllers.User
{
    public class PracticeController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // 小型 DTO 只給 Raw SQL 投影用
        private class ClassItem
        {
            public int Id { get; set; }        // MQBClassPK
            public string Name { get; set; }   // MQBClassName
        }

        // 進入條件頁：下拉顯示中文科別名稱（Raw SQL JOIN，避免 EDMX 屬性名不一致）
        [HttpGet]
        public ActionResult Index()
        {
            var sql = @"
SELECT DISTINCT c.MQBClassPK AS Id, c.MQBClassName AS Name
FROM dbo.MoodQuestionBank q
JOIN dbo.MQBClassName     c ON q.QClass = c.MQBClassPK
ORDER BY c.MQBClassPK";

            var classItems = db.Database.SqlQuery<ClassItem>(sql).ToList();

            ViewBag.ClassList = new SelectList(classItems, "Id", "Name");
            return View(); // Views/Practice/Index.cshtml
        }

        // 抽一題
        [HttpGet]
        public ActionResult Single(int classId)
        {
            var q = db.MoodQuestionBank
                      .Where(x => x.QClass == classId)
                      .OrderBy(x => Guid.NewGuid())
                      .FirstOrDefault();

            if (q == null)
            {
                TempData["Msg"] = "此科別目前沒有題目。";
                return RedirectToAction(nameof(Index));
            }

            // 取中文名稱（Raw SQL，避免 EDMX 命名差異）
            var className = db.Database
                              .SqlQuery<string>(
                                  "SELECT TOP 1 MQBClassName FROM dbo.MQBClassName WHERE MQBClassPK = @p0",
                                  classId)
                              .FirstOrDefault() ?? classId.ToString();

            var vm = ToVM(q, classId, className);
            return View("Single", vm);
        }

        // 送出答案
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Single(PracticeVM model)
        {
            var q = db.MoodQuestionBank.FirstOrDefault(x => x.MQBPK == model.MQBPK);
            if (q == null) return RedirectToAction(nameof(Index));

            var className = db.Database
                              .SqlQuery<string>(
                                  "SELECT TOP 1 MQBClassName FROM dbo.MQBClassName WHERE MQBClassPK = @p0",
                                  model.ClassId)
                              .FirstOrDefault() ?? model.ClassId.ToString();

            var vm = ToVM(q, model.ClassId, className);
            vm.SelectedAnswer = model.SelectedAnswer;
            vm.IsAnswered = true;
            vm.IsCorrect = string.Equals(vm.SelectedAnswer?.Trim(),
                                         vm.CorrectAnswer?.Trim(),
                                         StringComparison.OrdinalIgnoreCase);

            return View("Single", vm);
        }

        // Table → VM（所有圖片欄位統一丟 ToImageSrc(object)）
        private static PracticeVM ToVM(MoodQuestionBank q, int classId, string className)
        {
            return new PracticeVM
            {
                MQBPK = q.MQBPK,
                ClassId = classId,
                ClassName = className,

                Content = q.QContent,
                ImgContentDataUrl = ToImageSrc(q.ImgQContent),

                OptionA = q.QOptionA,
                OptionB = q.QOptionB,
                OptionC = q.QOptionC,
                OptionD = q.QOptionD,
                ImgOptionADataUrl = ToImageSrc(q.ImgQOptionA),
                ImgOptionBDataUrl = ToImageSrc(q.ImgQOptionB),
                ImgOptionCDataUrl = ToImageSrc(q.ImgQOptionC),
                ImgOptionDDataUrl = ToImageSrc(q.ImgQOptionD),

                CorrectAnswer = (q.QAns ?? "").Trim(),
                
                ImgExplanationDataUrl = ToImageSrc(q.ImgQExplain)
            };
        }

        // 單一 helper：同一招吃 byte[] 或 string（確保只有這個版本，刪掉其它多載）
        private static string ToImageSrc(object imgData, string mime = "image/png")
        {
            if (imgData == null) return null;

            // 若為 byte[] → 轉 data URL
            var bytes = imgData as byte[];
            if (bytes != null && bytes.Length > 0)
            {
                var b64 = Convert.ToBase64String(bytes);
                return $"data:{mime};base64,{b64}";
            }

            // 若為 string（實體路徑或 URL）→ 原樣回傳
            var s = imgData as string;
            if (!string.IsNullOrWhiteSpace(s)) return s;

            return null;
        }
    }
}
