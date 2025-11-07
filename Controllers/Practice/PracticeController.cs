using System;
using System.Linq;
using System.Web.Mvc;
using 專題MVC修正.Models;

namespace 專題MVC修正.Controllers.User
{
    public class PracticeController : Controller
    {
        private readonly MQBEntities db = new MQBEntities();

        // 條件選擇頁
        [HttpGet]
        public ActionResult Index()
        {
            // 直接從 lookup 表做下拉：value=PK(int), text=名稱
            var classes = db.MQBClassName
                            .OrderBy(c => c.MQBClassPK)
                            .Select(c => new { c.MQBClassPK, c.MQBClassName })
                            .ToList();

            ViewBag.ClassList = new SelectList(classes, "MQBClassPK", "MQBClassName");
            return View();
        }

        // 單題練習（GET）：classId=int
        [HttpGet]
        public ActionResult Single(int classId)
        {
            // 亂數抽題
            var q = db.MoodQuestionBank
                      .Where(x => x.QClass == classId)
                      .OrderBy(x => Guid.NewGuid())
                      .FirstOrDefault();

            if (q == null)
            {
                TempData["Msg"] = "此科別目前沒有題目。";
                return RedirectToAction(nameof(Index));
            }

            var vm = ToVM(q);
            return View(vm);
        }

        // 單題練習（POST）：送出作答並顯示解析
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Single(PracticeVM model)
        {
            var q = db.MoodQuestionBank.FirstOrDefault(x => x.MQBPK == model.MQBPK);
            if (q == null) return RedirectToAction(nameof(Index));

            var vm = ToVM(q);
            vm.SelectedAnswer = model.SelectedAnswer;
            vm.IsAnswered = true;
            vm.IsCorrect = string.Equals(vm.SelectedAnswer?.Trim(), vm.CorrectAnswer?.Trim(), StringComparison.OrdinalIgnoreCase);

            return View(vm);
        }

        // --- helper: table -> VM ---
        private PracticeVM ToVM(MoodQuestionBank q)
        {
            // 取出科別名稱
            var className = db.MQBClassName
                              .Where(c => c.MQBClassPK == q.QClass)
                              .Select(c => c.MQBClassName)
                              .FirstOrDefault();

            return new PracticeVM
            {
                MQBPK = q.MQBPK,
                ClassId = q.QClass ?? 0,
                ClassName = className,

                Content = q.QContent,
                ImgContentDataUrl = ToDataUrl(q.ImgQContent),

                OptionA = q.QOptionA,
                OptionB = q.QOptionB,
                OptionC = q.QOptionC,
                OptionD = q.QOptionD,
                ImgOptionADataUrl = ToDataUrl(q.ImgQOptionA),
                ImgOptionBDataUrl = ToDataUrl(q.ImgQOptionB),
                ImgOptionCDataUrl = ToDataUrl(q.ImgQOptionC),
                ImgOptionDDataUrl = ToDataUrl(q.ImgQOptionD),

                CorrectAnswer = (q.QAns ?? "").Trim(),
                Explanation = q.QExplain,
                ImgExplanationDataUrl = ToDataUrl(q.ImgQExplain)
            };
        }

        // --- helper: byte[] -> data URL（img 可直接 src 指向它） ---
        private static string ToDataUrl(byte[] bytes, string mime = "image/png")
        {
            if (bytes == null || bytes.Length == 0) return null;
            var b64 = Convert.ToBase64String(bytes);
            return $"data:{mime};base64,{b64}";
        }
    }

    // ------- ViewModel -------
    public class PracticeVM
    {
        public int MQBPK { get; set; }

        // 科別（int 外鍵 + 顯示名稱）
        public int ClassId { get; set; }
        public string ClassName { get; set; }

        // 題幹
        public string Content { get; set; }
        public string ImgContentDataUrl { get; set; }   // 由 byte[] 轉成 data URL

        // 選項
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string ImgOptionADataUrl { get; set; }
        public string ImgOptionBDataUrl { get; set; }
        public string ImgOptionCDataUrl { get; set; }
        public string ImgOptionDDataUrl { get; set; }

        // 答案與解析
        public string CorrectAnswer { get; set; }       // 例如 "A"
        public string Explanation { get; set; }
        public string ImgExplanationDataUrl { get; set; }

        // 作答狀態
        public string SelectedAnswer { get; set; }
        public bool IsAnswered { get; set; }
        public bool IsCorrect { get; set; }
    }
}
