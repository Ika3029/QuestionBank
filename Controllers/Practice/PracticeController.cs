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

        
        private class ClassItem
        {
            public int Id { get; set; }       
            public string Name { get; set; }   
        }

        
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
            return View(); 
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

        
        private static string ToImageSrc(object imgData, string mime = "image/png")
        {
            if (imgData == null) return null;

            
            var bytes = imgData as byte[];
            if (bytes != null && bytes.Length > 0)
            {
                var b64 = Convert.ToBase64String(bytes);
                return $"data:{mime};base64,{b64}";
            }

            
            var s = imgData as string;
            if (!string.IsNullOrWhiteSpace(s)) return s;

            return null;
        }
    }
}
