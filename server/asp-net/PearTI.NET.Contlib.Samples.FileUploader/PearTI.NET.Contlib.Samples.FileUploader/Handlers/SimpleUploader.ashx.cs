using PearTI.NET.Contlib.UI.Handlers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace PearTI.NET.Contlib.Samples.FileUploader.Handlers
{
    /// <summary>
    /// Summary description for SimpleUploader
    /// </summary>
    public class SimpleUploader: DynamicPicturesUploaderHandler
    {
        private readonly string UploadDirectory = "uploads/";

        public override void ProcessRequest(HttpContext context)
        {
            var uploadPath = Path.Combine(context.Server.MapPath("~/Content/"), UploadDirectory);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            this.UploadsDirectoryPath = uploadPath;
            this.UploadsDirectoryUrl = "~/Content/" + UploadDirectory.Trim('/');
            this.GenerateUniqueName = true;

            /* NOTE:
             * Most likely you'd like to use this uploader in order to upload pictures to a specific row in your data source,
             * for example, a images described an Article or Post.
             * 
             * You can define the API call by using HttpHandlerApiUrl, so you can do, for example:
             * <code>
             * try
                {
                    // Or just load somehow the post by GUID or any other identifier from the QueryString.
                    Post post = ServiceLocator.SharedInstance.GetInstance<IPostService>().GetPost(Guid.Parse(context.Request["id"]));

                    if (post == null)
                    {
                        throw new Exception("Could not find the requested post.");
                    }
                }
                catch (Exception)
                {
                    context.Response.ClearHeaders();
                    context.Response.StatusCode = 404;
                    return;
                }
                this.HttpHandlerApiUrl = context.Request.Path + "?id=" + post.PostIdentifier.ToString();
             * </code>
             * 
             * This will navigate the calls the API resolve. You most likely would like also to set the uploads directory to a unique directory
             * for this post. When doing that, the parent class will know automaticly to load only that specific file(s) and in that way you can distinguish between posts.
             * 
             * TODO: Create a demo for that concept.
             */

            base.ProcessRequest(context);
        }
    }
}