using System;
using System.Collections.Generic;
using System.Diagnostics;
using JsonToCsharp.Core;
using Microsoft.AspNetCore.Mvc;
using JsonToCsharp.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JsonToCsharp.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.SelectOptions = new SelectListItem[]
            {
                new SelectListItem {Value = nameof(ListType.IEnumerable), Text = "IEnumerable"},
                new SelectListItem {Value = nameof(ListType.IReadOnlyList), Text = "IReadOnlyList"},
            };
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public ActionResult<Result<IReadOnlyDictionary<string, string>>> Generate([FromBody] JsonSendModel input)
        {
            using (var reader = new MemoryReader(input.JsonText))
            {
                var options = new Options
                {
                    DeclareDataMember = false,
                    ListType = Enum.Parse<ListType>(input.ListType),
                    NameSpace = null
                };

                try
                {
                    var classData = new JsonToCsharpGenerator(options).Create(input.BaseClassName, reader);

                    return Result<IReadOnlyDictionary<string, string>>.CreateSuccess(classData);
                }
                catch(Exception e)
                {
                    return Result<IReadOnlyDictionary<string, string>>.CreateFail(e.Message);
                }
            }
        }

    }
    
}
