using System.Web;
using System.Web.Mvc;

namespace PearTI.NET.Contlib.Samples.FileUploader
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}