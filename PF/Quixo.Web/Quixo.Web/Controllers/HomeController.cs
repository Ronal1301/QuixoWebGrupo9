using System.Web.Mvc;

namespace Quixo.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Aplicación Quixo.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Información de contacto.";
            return View();
        }
    }
}
