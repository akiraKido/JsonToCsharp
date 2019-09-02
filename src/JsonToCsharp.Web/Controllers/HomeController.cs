using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JsonToCsharp.Core;
using Microsoft.AspNetCore.Mvc;
using JsonToCsharp.Web.Models;

namespace JsonToCsharp.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public ActionResult<string> Generate([FromBody] JsonSendModel input)
        {
            using (var reader = new MemoryReader(input.JsonText))
            {
                var options = new Options
                {
                    DeclareDataMember = false,
                    ListType = ListType.IReadOnlyList,
                    NameSpace = null
                };

                var classData = new JsonToCsharpGenerator(options).Create(input.BaseClassName, reader);

                var result = string.Empty;
                
                foreach (var kv in classData)
                {
                    var className = kv.Key;
                    var text = kv.Value;

                    result += $"// {className}.cs\n\n";
                    result += $"{text}\n\n";
                }
                
                return result;
            }
        }
    }
}
