using System;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;
using 專題MVC修正.Models.DTOs; // 讓 Controller 能用 PracticeVM


namespace 專題MVC修正.Controllers.User
{
    public class PracticeController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        [HttpGet]
        public ActionResult Index()
        {
            var classItems = db.MoodQuestionBank
                               .GroupBy(x => x.QClass)             // QClass = int
                               .Select(g => new { Id = g.Key, Name = g.Key.ToString() })
                               .OrderBy(x => x.Id)
                               .ToList();

            ViewBag.ClassList = new SelectList(classItems, "Id", "Name");
            return View();
        }

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

            var vm = ToVM(q, classId, classId.ToString());
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Single(PracticeVM model)
        {
            var q = db.MoodQuestionBank.FirstOrDefault(x => x.MQBPK == model.MQBPK);
            if (q == null) return RedirectToAction(nameof(Index));

            var vm = ToVM(q, model.ClassId, model.ClassName);
            vm.SelectedAnswer = model.SelectedAnswer;
            vm.IsAnswered = true;
            vm.IsCorrect = string.Equals(vm.SelectedAnswer?.Trim(), vm.CorrectAnswer?.Trim(), StringComparison.OrdinalIgnoreCase);

            return View(vm);
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

        // ------- 同名多載：同一招吃不同型別 -------

        // byte[] → data URL
        private static string ToImageSrc(byte[] bytes, string mime = "image/png")
        {
            if (bytes == null || bytes.Length == 0) return null;
            var b64 = Convert.ToBase64String(bytes);
            return $"data:{mime};base64,{b64}";
        }

        // string（檔案路徑或 URL）→ 原樣回傳（給 <img src> 用）
        private static string ToImageSrc(string pathOrUrl)
        {
            return string.IsNullOrWhiteSpace(pathOrUrl) ? null : pathOrUrl;
        }

        // object → data URL 或路徑
        private static string ToImageSrc(object imgData, string mime = "image/png")
        {
            if (imgData == null) return null;

            // 若為 byte[]
            if (imgData is byte[] bytes && bytes.Length > 0)
            {
                var b64 = Convert.ToBase64String(bytes);
                return $"data:{mime};base64,{b64}";
            }

            // 若為 string
            if (imgData is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            return null;
        }

        // ViewModel：沿用上一版名稱（*DataUrl）
        
    }
}
