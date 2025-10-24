using System.Linq;
using System.Web.Mvc;

// ✅ 建立別名：把你要用的 Code First 型別與 DbContext 指定清楚
using MyStd = 專題MVC修正.Models.Std;
using MyCtx = 專題MVC修正.Models.MQBContext;

namespace 專題MVC修正.Controllers
{
    public class StdController : Controller
    {
        // ✅ 這裡用別名的 DbContext
        private readonly MyCtx db = new MyCtx();

        // GET: Std
        public ActionResult Index()
        {
            var list = db.Stds.OrderByDescending(x => x.StdPK).ToList();
            return View(list);   // View 泛型也會一起改，見下方 Views
        }

        // GET: Std/Create
        public ActionResult Create()
        {
            return View(new MyStd());
        }

        // POST: Std/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MyStd std)   // ✅ 參數用別名型別
        {
            if (!ModelState.IsValid) return View(std);

            db.Stds.Add(std);     // ✅ Code First 的 DbSet 名稱通常是複數
            db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}
