using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace PearTI.NET.Contlib.UI.Handlers
{
    /// <summary>
    /// Summary description for SimpleUploader
    /// </summary>
    public class SimpleUploader: DynamicPicturesUploaderHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            this.UploadsDirectoryPath = context.Server.MapPath("~/Content/uploads");
            this.UploadsDirectoryUrl = "~/Content/uploads";
            this.HttpHandlerApiUrl = context.Request.Path + "?id=" + this.post.PostIdentifier.ToString();
            this.GenerateUniqueName = true;
            this.MaximumNumberOfFiles = Post.MaximumLinkedImages;
            this.FileMaxSize = Post.MaximumLinkedImageSize * 1024 * 1024;

            base.ProcessRequest(context);
        }
    }
}