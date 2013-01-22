/*
 * jQuery File Upload Plugin PHP Class 6.1
 * https://github.com/blueimp/jQuery-File-Upload
 *
 * Copyright 2010, Sebastian Tschan
 * https://blueimp.net
 * 
 * Licensed under the MIT license:
 * http://www.opensource.org/licenses/MIT
 * 
 * ASP.NET jQuery File Upload handler
 * 
 * Author: Yahav Gindi Bar.
 * http://yahavgindibar.com
 * Copyrights: 2013, Pear Technology Investments, Ltd.
 * 
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Dynamic;
using System.Drawing.Imaging;
using PearTI.NET;
using PearTI.NET.ThirdParty;
using PearTI.NET.Contlib.Mvc;
using PearTI.NET.Contlib.Samples.FileUploader.Resources;

namespace PearTI.NET.Contlib.UI.Handlers
{
    /// <summary>
    /// Abstraction class for implementing dynamic pictures uploading.
    /// This class using HTTP methods in order to retrive specific file, file listing, uploading and removing files.
    /// The class results format is JSON, but the SendResponse() can be override in order to provide any desired result format type.
    /// </summary>
    public abstract class DynamicPicturesUploaderHandler : IHttpHandler
    {
        #region Class members

        protected readonly UrlHelper urlHelper;
        protected readonly int PartialFileReadBytesLength = 1024;

        #endregion

        #region Properties

        /// <summary>
        /// The current HttpHandler API url
        /// </summary>
        public string HttpHandlerApiUrl { get; set; }

        /// <summary>
        /// The physical path to the destination uploads directory
        /// </summary>
        public string UploadsDirectoryPath { get; set; }

        /// <summary>
        /// The url to the destination uploads directory
        /// </summary>
        public string UploadsDirectoryUrl { get; set; }

        /// <summary>
        /// Maximum file size
        /// </summary>
        public int FileMaxSize { get; set; }

        /// <summary>
        /// Minimum file size
        /// </summary>
        public int FileMinSize { get; set; }

        /// <summary>
        /// Maximum numbers of files available to upload at once
        /// </summary>
        public int MaximumNumberOfFiles { get; set; }

        /// <summary>
        /// The method type to use with DELETE requests
        /// </summary>
        public DeleteMethodType DeleteType { get; set; }

        /// <summary>
        /// Discrad abroted uploads
        /// </summary>
        public bool DiscardAbortedUploads { get; set; }

        /// <summary>
        /// Collection contains the allowed methods to use in the HTTP request
        /// </summary>
        public ICollection<string> AllowedHttpMethods { get; set; }

        /// <summary>
        /// The allowed access control headers
        /// </summary>
        public ICollection<string> AllowedAccessControlHeaders { get; set; }

        /// <summary>
        /// The access control origin
        /// </summary>
        public string AllowedAccessControlOrigin { get; set; }

        /// <summary>
        /// Value that indicating if we're allowing access control credentials
        /// </summary>
        public bool AccessControlAllowCredentials { get; set; }

        /// <summary>
        /// Defines which files based on a pattern can be uploaded
        /// </summary>
        public Regex AcceptedFilesPattern { get; set; }

        /// <summary>
        /// Defines which files can be displayed inline when downloaded
        /// </summary>
        public Regex InlineFileTypesRegExp { get; set; }

        /// <summary>
        /// The minimum width required in the uploaded image
        /// </summary>
        public int ImageMinAllowedWidth { get; set; }

        /// <summary>
        /// The image height required in the uploaded image
        /// </summary>
        public int ImageMinAllowedHeight { get; set; }

        /// <summary>
        /// The image maximum allowed width in the uploaded image
        /// </summary>
        public int ImageMaxAllowedWidth { get; set; }

        /// <summary>
        /// The image maximum allowed height in the uploaded image
        /// </summary>
        public int ImageMaxAllowedHeight { get; set; }

        /// <summary>
        /// The maximum width allowed to return to the client
        /// </summary>
        public int ClientReturnedMaximumWidth { get; set; }

        /// <summary>
        /// The maximum height allowed to return to the client
        /// </summary>
        public int ClientReturnedMaximumHeight { get; set; }

        /// <summary>
        /// The maximum width allowed when resizing the uploaded image
        /// </summary>
        public int ResizeImageActionMaximumWidth { get; set; }

        /// <summary>
        /// The maximum height allowed when resizing the uploaded image
        /// </summary>
        public int ResizeImageActionMaximumHeight { get; set; }

        /// <summary>
        /// Gets or sets the high quality image level (jpeg image noly, value between 0 and 100)
        /// </summary>
        public int HighQualityImageLevel { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Event occured before resolving an action to execute
        /// </summary>
        public event EventHandler<HttpContextEventArgs> BeforeResolveAction;

        /// <summary>
        /// Event occured after the uploader resolved the action and wrote the output into the context Response stream
        /// </summary>
        public event EventHandler<HttpContextEventArgs> AfterSendResponse;

        /// <summary>
        /// Event occured when the resolving method could not resolve the given HTTP method
        /// </summary>
        public event EventHandler<HttpContextEventArgs> UnsupportedHttpMethodTriggered;

        /// <summary>
        /// Event occured if a whole file has been uploaded
        /// </summary>
        public event EventHandler<FileUploadEventArgs> FileUploaded;

        /// <summary>
        /// Event occured after partial file has been uploaded
        /// </summary>
        public event EventHandler<PartialFileUploadEventArgs> PartialFileUploaded;

        /// <summary>
        /// Event occured after the the whole file exists on the file-system
        /// </summary>
        public event EventHandler<FileInfoEventArgs> UploadWholeFileCompleted;

        /// <summary>
        /// Event occured before deleting an file from the file system
        /// </summary>
        public event EventHandler<FileInfoEventArgs> BeforeFileDeleted;

        /// <summary>
        /// Event occured before listing the available files to the user
        /// </summary>
        public event EventHandler<FileInfoListingEventArgs> ListingFiles;

        /// <summary>
        /// Event occured before browsing a single file
        /// </summary>
        public event EventHandler<FileInfoEventArgs> BrowsingFile;

        #endregion

        #region Boolean options

        /// <summary>
        /// Allow to download the files if the client requested to do so
        /// </summary>
        public bool AllowFilesDownloading { get; set; }

        /// <summary>
        /// Generate file unique name
        /// </summary>
        public bool GenerateUniqueName { get; set; }

        /// <summary>
        /// Allow to convert the given image into high-quality JPEG image
        /// </summary>
        public bool AllowHighQualityImagesConvertion { get; set; }

        #endregion

        #region ctor

        public DynamicPicturesUploaderHandler()
        {
            //-------------------------------------------
            //  Setup
            //-------------------------------------------

            /* -1 = unlimited */
            this.FileMaxSize = -1;
            this.MaximumNumberOfFiles = -1;

            this.FileMinSize = 1;
            this.DiscardAbortedUploads = true;
            this.DeleteType = DeleteMethodType.Post;
            this.AllowedHttpMethods = new List<string>() { "DELETE", "GET", "HEAD", "POST", "PUT", "OPTIONS", "PATCH" };
            this.AllowedAccessControlHeaders = new List<string>() { "Content-Type", "Content-Range", "Content-Disposition", "Content-Description" };
            this.AllowedAccessControlOrigin = "*";
            this.AccessControlAllowCredentials = false;
            this.InlineFileTypesRegExp = new Regex(@"\.(gif|jpe?g|png)$", RegexOptions.IgnoreCase);
            this.AcceptedFilesPattern = new Regex(@".+$", RegexOptions.IgnoreCase);
            this.HttpHandlerApiUrl = HttpContext.Current.Request.Path;

            /* Dims */
            this.ImageMinAllowedWidth = -1;
            this.ImageMinAllowedHeight = -1;
            this.ImageMaxAllowedWidth = -1;
            this.ImageMaxAllowedHeight = -1;

            /* Client dims */
            this.ClientReturnedMaximumWidth = 100;
            this.ClientReturnedMaximumHeight = 100;

            /* Resizing maximum width & height */
            this.ResizeImageActionMaximumWidth = 1000;
            this.ResizeImageActionMaximumHeight = 1000;

            /* Yes/No configs */
            this.AllowFilesDownloading = false;

            /* High-quality images */
            this.HighQualityImageLevel = 100;
            this.AllowHighQualityImagesConvertion = true;

            /* Setup */
            this.urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
        }

        #endregion

        #region IHttpHandler

        public bool IsReusable
        {
            get { return true; }
        }

        public virtual void ProcessRequest(HttpContext context)
        {
            /* Check that we got required parameters */
            if (string.IsNullOrEmpty(this.UploadsDirectoryPath))
            {
                throw new ArgumentNullException("The uploads directory path cannot be null.", "UploadsDirectoryPath");
            }

            if (string.IsNullOrEmpty(this.UploadsDirectoryUrl))
            {
                throw new ArgumentNullException("The uploads directory url cannot be null.", "UploadsDirectoryUrl");
            }

            /* Setup headers */
            context.Response.AddHeader("Pragma", "no-cache");
            context.Response.AddHeader("Cache-Control", "private, no-cache");
            context.Response.ContentType = "application/json";

            /* GOOOO! */
            this.ResolveRequest(context);
        }

        protected virtual void ResolveRequest(HttpContext context)
        {
            //-------------------------------------------
            //  Resolve the action based on the given HTTP method
            //-------------------------------------------

            this.OnBeforeResolveAction(context);

            switch (context.Request.HttpMethod)
            {
                case "HEAD":
                case "OPTIONS":
                    this.ResolveHeadRequest(context);
                    break;
                case "GET":
                    this.ResolveGetRequest(context);
                    break;
                case "POST":
                case "PUT":
                case "PATCH":
                    this.ResolvePostRequest(context);
                    break;
                case "DELETE":
                    this.ResolveDeleteRequest(context);
                    break;
                default:
                    this.OnUnsupportedHttpMethodTriggered(context);
                    context.Response.ClearHeaders();
                    context.Response.StatusCode = 405;
                    break;
            }
        }

        #endregion

        #region HTTP Method resolvers methods

        /// <summary>
        /// Resolves HEAD or OPTION request
        /// </summary>
        /// <param name="context">The associated HTTP Context</param>
        protected virtual void ResolveHeadRequest(HttpContext context)
        {
            //-------------------------------------------
            //  Just print out headers
            //-------------------------------------------

            context.Response.ClearHeaders();
            context.Response.AddHeader("Pragma", "no-cache");
            context.Response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate");
            context.Response.AddHeader("Content-Disposition", "inline; filename=\"files.json\"");

            /* Prevent Internet Explorer from MIME-sniffing the content-type */
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");

            /* Extended options in case we've declared the control origin */
            if (!string.IsNullOrEmpty(this.AllowedAccessControlOrigin))
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", this.AllowedAccessControlOrigin);
                context.Response.AddHeader("Access-Control-Allow-Credentials", this.AccessControlAllowCredentials.ToString().ToLower());
                context.Response.AddHeader("Access-Control-Allow-Methods", string.Join(", ", this.AllowedHttpMethods));
                context.Response.AddHeader("Access-Control-Allow-Headers", string.Join(", ", this.AllowedAccessControlHeaders));
            }

            context.Response.AddHeader("Vary", "Accept");

            try
            {
                if (context.Request["HTTP_ACCEPT"].Contains("application/json"))
                {
                    context.Response.AddHeader("Content-type", "application/json");
                }
                else
                {
                    context.Response.AddHeader("Content-type", "application/plain");
                }
            }
            catch (Exception)
            {
                context.Response.AddHeader("Content-type", "application/plain");
            }
        }

        /// <summary>
        /// Resolves GET request
        /// </summary>
        /// <param name="context">The associated HTTP Context</param>
        /// <param name="printResponse">Allows to download the file if it was been requested to download</param>
        protected virtual void ResolveGetRequest(HttpContext context)
        {
            //-------------------------------------------
            //  Got something?
            //-------------------------------------------
            if (context.Request["download"] != null)
            {
                this.DownloadFile(context);
            }

            /* If we got a requested file, we shall show it
             * otherwise, we'll list the available files */
            string fileName = this.GetRequestedFileName(context);
            List<TransportableListedFile> listedFiles = new List<TransportableListedFile>();
            if (fileName != null)
            {
                listedFiles.Add(this.TransportableListSingleFile(context, fileName));
            }
            else
            {
                listedFiles.AddRange(this.TransportableListAvailableFiles(context));
            }

            this.SendResponse(context, listedFiles);
        }

        /// <summary>
        /// Resolves POST / PUT / PATCH request
        /// </summary>
        /// <param name="context">The associated HTTP Context</param>
        protected virtual void ResolvePostRequest(HttpContext context)
        {
            //-------------------------------------------
            //  Did we requested to use the DELETE method
            //-------------------------------------------
            if (this.DeleteType != DeleteMethodType.Delete)
            {
                if (context.Request["_method"] != null && context.Request["_method"] == "DELETE")
                {
                    this.ResolveDeleteRequest(context);
                    return;
                }
            }

            //-------------------------------------------
            //  We got any file?
            //-------------------------------------------
            if (context.Request.Files.Count < 1)
            {
#if DEBUG
                throw new ArgumentException("The Request.Files array is empty.", "context");
#else
                context.Response.StatusCode = 405;
                return;
#endif
            }

            //-------------------------------------------
            //  Init
            //-------------------------------------------
            string fileName = null;
            string headersFileName = null;
            string fileType = null;
            string headersFileType = null;
            string[] contentRange = null;
            long fileSize = -1;
            long headersFileSize = -1;

            bool isIE = context.Request.Browser.Browser.ToUpper() == "IE";
            List<TransportableUploadedFile> files = new List<TransportableUploadedFile>();

            /* Setup data from headers? */
            if (context.Request["HTTP_CONTENT_DISPOSITION"] != null)
            {
                headersFileName = Regex.Replace(context.Request["HTTP_CONTENT_DISPOSITION"].ToString(), "(^[^\"]+\")|(\"$)", "");
            }

            if (context.Request["HTTP_CONTENT_DESCRIPTION"] != null)
            {
                headersFileType = context.Request["HTTP_CONTENT_DESCRIPTION"].ToString();
            }

            /*
             * Parse the Content-Range header, which has the following form:
             * Content-Range: bytes 0-524287/2000000
             */
            if (context.Request["HTTP_CONTENT_RANGE"] != null)
            {
                contentRange = Regex.Split(context.Request["HTTP_CONTENT_RANGE"].ToString(), "[^0-9]+");
                if (contentRange.Length < 4)
                {
                    throw new ArgumentException("The given content range could not be splitted correctly.", "HTTP_CONTENT_RANGE");
                }

                if (!long.TryParse(contentRange[3], out fileSize))
                {
                    headersFileSize = -1;
                }
            }

            //-------------------------------------------
            //  Iterate on the posted files
            //-------------------------------------------
            for (int i = 0; i < context.Request.Files.Count; i++)
            {
                HttpPostedFile file = context.Request.Files[i];

                /* File name */
                fileName = headersFileType ?? file.FileName;
                fileName = isIE ? fileName.Split(new char[] { '\\' }).Last() : fileName;

                /* File type */
                if (headersFileType == null)
                {
                    if (context.Request.Files.Count == 1)
                    {
                        fileType = !string.IsNullOrEmpty(file.ContentType) ? file.ContentType : context.Request.ContentType;
                    }
                    else
                    {
                        fileType = file.ContentType;
                    }
                }
                else
                {
                    fileType = headersFileType;
                }

                /* File size */
                if (headersFileSize == -1)
                {
                    if (context.Request.Files.Count == 1)
                    {
                        fileSize = file.ContentLength > 0 ? file.ContentLength : context.Request.ContentLength;
                    }
                    else
                    {
                        fileSize = file.ContentLength;
                    }
                }
                else
                {
                    fileSize = headersFileSize;
                }

                //-------------------------------------------
                //  Upload the file and add it to the JSON response list
                //-------------------------------------------
                files.Add(this.UploadFile(context, file,
                    fileName, fileType,
                    fileSize, contentRange));
            }

            this.SendResponse(context, files);
        }

        /// <summary>
        /// Resolves DELETE request
        /// </summary>
        /// <param name="context">The associated HTTP Context</param>
        protected virtual void ResolveDeleteRequest(HttpContext context)
        {
            string fileName = this.GetRequestedFileName(context);
            if (string.IsNullOrWhiteSpace(fileName) || fileName == ".")
            {
                throw new ArgumentException("The given file name is invalid.", "fileName");
            }

            string filePath = this.CombineUploadsPathAndFileName(context, fileName);

            this.OnBeforeFileDeleted(context, new FileInfo(filePath));

            bool result = this.DeleteFile(context, filePath);

            this.SendResponse(context, result);
        }

        #endregion

        #region Action methods

        protected virtual void DownloadFile(HttpContext context)
        {
            if (!this.AllowFilesDownloading)
            {
                context.Response.StatusCode = 403;
                return;
            }

            string fileName = this.GetRequestedFileName(context);
            if (fileName == null || !this.IsValidFile(context, fileName))
            {
                context.Response.StatusCode = 403;
                return;
            }

            FileInfo fileInfo = new FileInfo(this.CombineUploadsPathAndFileName(context, fileName));
            if (this.InlineFileTypesRegExp.IsMatch(fileName))
            {
                /*  Prevent Internet Explorer from MIME-sniffing the content-type */
                context.Response.AddHeader("X-Content-Type-Options", "nosniff");
                context.Response.AddHeader("Content-Type", this.ResolveMimeType(Path.GetExtension(fileName)));
                context.Response.AddHeader("Content-Disposition", "inline; filename=\"" + fileName + "\"");
            }
            else
            {
                context.Response.AddHeader("ontent-Description", "File Transfer");
                context.Response.AddHeader("Content-Type", "application/octet-stream");
                context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
                context.Response.AddHeader("Content-Transfer-Encoding", "binary");
            }

            context.Response.AddHeader("Content-Length", this.GetFileSize(context, fileInfo).ToString());
            context.Response.Cache.SetLastModified(fileInfo.LastWriteTime);
            this.WriteFile(context, fileInfo);
        }

        protected virtual void SendResponse(HttpContext context, object responseContent)
        {
            string serializedResponse = new JavaScriptSerializer().Serialize(responseContent);
            string redirectionUrl = context.Request["redirect"] != null ? Regex.Replace(context.Request["redirect"], @"(\\)([\000\010\011\012\015\032\042\047\134\140])", "$2") : null;

            if (redirectionUrl != null)
            {
                context.Response.Redirect(string.Format(context.Request["redirect"].Replace("%s", "{0}"), context.Server.UrlEncode(serializedResponse)));
            }

            if (context.Request["HTTP_CONTENT_RANGE"] != null)
            {
                if (responseContent is ICollection<TransportableUploadedFile>)
                {
                    if (((ICollection<TransportableUploadedFile>)responseContent).Count > 0
                        && ((ICollection<TransportableUploadedFile>)responseContent).ElementAt(0).size > 0)
                    {
                        context.Response.AddHeader("Range", "0-" + (((ICollection<TransportableUploadedFile>)responseContent).ElementAt(0).size - 1).ToString());
                    }
                }
            }

            context.Response.Write(serializedResponse);

            this.OnAfterSendResponse(context);
        }

        #endregion

        #region Data receiving

        protected virtual string GetRequestedFileName(HttpContext context)
        {
            if (context.Request["file"] == null)
            {
                return null;
            }

            return Path.GetFileName(context.Request["file"]);
        }

        #endregion

        #region Overridable methods

        /// <summary>
        /// Checks whether the requested file is valid and acceessable
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file name (basename)</param>
        /// <returns>True if one can access this file, false otherwise</returns>
        /// <remarks>This method can be used to make sure that the file exists & to check that the
        /// current user got access to this file, if this method returns false, the user is rejected.
        /// The default implementation checks that the file exists in the uploads directory</remarks>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "context", Justification = "In most cases, in sub-classes there's a need for the context in order to implement user-specific access layer.")]
        protected virtual bool IsValidFile(HttpContext context, string fileName)
        {
            return File.Exists(this.CombineUploadsPathAndFileName(context, fileName));
        }

        /// <summary>
        /// Resovle the given file extension into its mime type
        /// </summary>
        /// <param name="fileExtension">The file extension</param>
        /// <returns>The mapped mime type, or null if the mapping could not be done because unmapped extension</returns>
        protected virtual string ResolveMimeType(string fileExtension)
        {
            switch (fileExtension.Trim('.').ToLower())
            {
                case "jpg":
                case "jpeg":
                    return "image/jpeg";
                case "png":
                    return "image/png";
                case "gif":
                    return "image/gif";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the file full path
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file name (basename)</param>
        /// <returns>The file full path</returns>
        /// <remarks>This method can be used to modify the original way that the pathes is constructed.
        /// The default implementation is simple usage of Path.Combine with the uploads directory and the file name</remarks>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "context", Justification = "In most cases, in sub-classes there's a need for the context in order to implement user-specific access layer.")]
        protected virtual string CombineUploadsPathAndFileName(HttpContext context, string fileName)
        {
            return Path.Combine(this.UploadsDirectoryPath, fileName);
        }

        /// <summary>
        /// Gets the file url
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file name (basename)</param>
        /// <returns>The file full url</returns>
        /// <remarks>This method can be used to modify the original way that the pathes is constructed.
        /// The default implementation is simple usage of Path.Combine with the uploads directory and the file name</remarks>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "context", Justification = "In most cases, in sub-classes there's a need for the context in order to implement user-specific access layer.")]
        protected virtual string CombineUploadsUrlAndFileName(HttpContext context, string fileName)
        {
            return urlHelper.Content(Path.Combine(this.UploadsDirectoryUrl, fileName));
        }

        /// <summary>
        /// Gets the file size in bytes
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file full path</param>
        /// <returns>The number of bytes the file contains</returns>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "context", Justification = "In most cases, in sub-classes there's a need for the context in order to implement user-specific access layer.")]
        protected virtual long GetFileSize(HttpContext context, string filePath)
        {
            return this.GetFileSize(context, new FileInfo(filePath));
        }

        /// <summary>
        /// Gets the file size in bytes
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileInfo">The related file info data</param>
        /// <returns>The number of bytes the file contains</returns>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "context", Justification = "In most cases, in sub-classes there's a need for the context in order to implement user-specific access layer.")]
        protected virtual long GetFileSize(HttpContext context, FileInfo fileInfo)
        {
            return fileInfo.Length;
        }

        /// <summary>
        /// Writes the given file into the context response output stream
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileInfo">The related file info data</param>
        protected virtual void WriteFile(HttpContext context, FileInfo fileInfo)
        {
            context.Response.WriteFile(fileInfo.FullName);
        }

        /// <summary>
        /// Construct the download url for a given file
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file name to download</param>
        /// <returns>The url used to download the file</returns>
        protected virtual string ConstructDownloadUrlForFile(HttpContext context, string fileName)
        {
            if (this.AllowFilesDownloading)
            {
                return this.BuildUrl(this.HttpHandlerApiUrl, "file=" + context.Server.UrlEncode(fileName) + "&download=1");
            }

            return this.CombineUploadsUrlAndFileName(context, fileName);
        }

        /// <summary>
        /// Builds a url from a base url (which can contains protocol and uri or be a relative url and optionaly query string) and additional query string
        /// </summary>
        /// <param name="baseUrl">The base url</param>
        /// <param name="queryString">The additional query string values</param>
        /// <returns>The URL</returns>
        protected virtual string BuildUrl(string baseUrl, string queryString)
        {
            return this.BuildUrl(baseUrl, HttpUtility.ParseQueryString(queryString));
        }

        /// <summary>
        /// Builds a url from a base url (which can contains protocol and uri or be a relative url and optionaly query string) and additional query string
        /// </summary>
        /// <param name="baseUrl">The base url</param>
        /// <param name="queryStringParams">The additional query string values</param>
        /// <returns>The URL</returns>
        protected virtual string BuildUrl(string baseUrl, NameValueCollection queryStringParams)
        {
            if (queryStringParams.Count < 1)
            {
                return baseUrl;
            }

            if (baseUrl.IndexOf("?") > -1)
            {
                queryStringParams.Add(HttpUtility.ParseQueryString(baseUrl.Substring(baseUrl.IndexOf("?"))));
                baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("?"));
            }

            return baseUrl + "?" + queryStringParams.ToString();
        }

        /// <summary>
        /// Format the given file name
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file name</param>
        /// <param name="fileType">The file MIME type</param>
        /// <returns>The formatted file name</returns>
        protected virtual string FormatFileName(HttpContext context, string fileName, string fileType)
        {
            //  Remove path information and dots around the filename, to prevent uploading
            //  into different directories or replacing hidden system files.
            //  Also remove control characters and spaces (\x00..\x20) around the filename:
            fileName = Regex.Replace(fileName.Trim('.', '\x00', '\x20'), @"(\\)([\000\010\011\012\015\032\042\047\134\140])", "$2");

            //  Add missing file extension for known image types:
            if (fileName.IndexOf(".") < 0
                && Regex.IsMatch(fileType, @"^image\/(gif|jpe?g|png)"))
            {
                fileName += "." + Regex.Replace(fileType, @"^image\/(gif|jpe?g|png)", "$1");
            }

            //-------------------------------------------
            //  If we requested to generate unique file name, stop here
            //-------------------------------------------
            if (this.GenerateUniqueName)
            {
                return Guid.NewGuid() + Path.GetExtension(fileName);
            }

            //-------------------------------------------
            //  Check if the file exists, if so, we shall rename it to "FileName-(N)"
            //  where N is an number
            //-------------------------------------------
            if (File.Exists(this.CombineUploadsPathAndFileName(context, fileName)))
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string fileExtension = Path.GetExtension(fileName);
                int i = 2;

                while (File.Exists(this.CombineUploadsPathAndFileName(context, fileName)))
                {
                    fileName = string.Format("{0}-({1}){2}", fileNameWithoutExtension, i++, fileExtension);
                }
            }

            return fileName;
        }

        /// <summary>
        /// Valiate the upload file is allowed to be save
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="postedFile">The uploaded file</param>
        /// <param name="fileName">The destination file name</param>
        /// <param name="fileType">The destination file MIME type</param>
        /// <param name="fileSize">The file full size (in multipart upload it can be that the postedFile is not in the same size)</param>
        /// <param name="errorMessage">An error message related to the vertification process</param>
        /// <returns>Boolean value that indicates if we can save the file</returns>
        public virtual bool ValidateUploadedFile(HttpContext context, HttpPostedFile postedFile, string fileName, string fileType, long fileSize, out string errorMessage)
        {
            errorMessage = string.Empty;

            //-------------------------------------------
            //  Do we exceed the IIS post size?
            //-------------------------------------------
            if (fileSize > this.GetIISMaxAllowedPostSize() * 1024)
            {
                errorMessage = GetResourceString(context, "PostMaxSizeExceeded");
                return false;
            }

            //-------------------------------------------
            //  Is our file matching the allowed files pattern?
            //-------------------------------------------
            if (!this.AcceptedFilesPattern.IsMatch(fileName))
            {
                errorMessage = GetResourceString(context, "InvalidFileType");
                return false;
            }

            //-------------------------------------------
            //  Check file size
            //-------------------------------------------
            if (this.FileMaxSize > 0
                && fileSize > this.FileMaxSize)
            {
                errorMessage = GetResourceString(context, "FileMaxSizeExceeded", ((long)this.FileMaxSize).ToFileSize());
                return false;
            }

            if (this.FileMinSize > 0
                && fileSize < this.FileMinSize)
            {
                errorMessage = GetResourceString(context, "FileMinSizeNotReached", ((long)this.FileMinSize).ToFileSize());
                return false;
            }

            //-------------------------------------------
            //  We're exceeding the maximum number of files?
            //-------------------------------------------
            if (this.MaximumNumberOfFiles > -1
                && this.CountAvailableFiles(context) > this.MaximumNumberOfFiles)
            {
                errorMessage = GetResourceString(context, "MaximumNumberOfFilesExcceded", this.MaximumNumberOfFiles.ToString());
                return false;
            }

            //-------------------------------------------
            //  We DO got an image, don't we?
            //-------------------------------------------
            if (!postedFile.IsImage())
            {
                errorMessage = "NotValidImage";
                return false;
            }

            //-------------------------------------------
            //  Image dims vertification
            //-------------------------------------------
            if (this.ImageMinAllowedWidth > -1
                || this.ImageMinAllowedHeight > -1
                || this.ImageMaxAllowedWidth > -1
                || this.ImageMaxAllowedHeight > -1)
            {
                /* Note: we're not using try-catch block
                 * because we do know that we can instantiate Bitmap from the input stream
                 * since we've already done it in IsImage() extension method. */

                using (Bitmap image = new Bitmap(postedFile.InputStream))
                {
                    if (this.ImageMinAllowedWidth > image.Width)
                    {
                        errorMessage = GetResourceString(context, "ImageMinAllowedWidthNotMet", this.ImageMinAllowedWidth.ToString());
                        return false;
                    }

                    if (this.ImageMinAllowedHeight > image.Height)
                    {
                        errorMessage = GetResourceString(context, "ImageMinAllowedHeightNotMet", this.ImageMinAllowedHeight.ToString());
                        return false;
                    }

                    if (this.ImageMaxAllowedWidth < image.Width)
                    {
                        errorMessage = GetResourceString(context, "ImageMaxAllowedWidthExceeded", this.ImageMaxAllowedWidth.ToString());
                        return false;
                    }

                    if (this.ImageMaxAllowedHeight < image.Height)
                    {
                        errorMessage = GetResourceString(context, "ImageMaxAllowedHeightExceeded", this.ImageMaxAllowedHeight.ToString());
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the maximum IIS POST size
        /// </summary>
        /// <returns>The maximum allowed post size or -1 in case of failure</returns>
        protected virtual int GetIISMaxAllowedPostSize()
        {
            try
            {
                System.Web.Configuration.HttpRuntimeSection runtimeConfigurationSection = (System.Web.Configuration.HttpRuntimeSection)System.Configuration.ConfigurationManager.GetSection("system.web/httpRuntime");
                if (runtimeConfigurationSection != null)
                {
                    return runtimeConfigurationSection.MaxRequestLength;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Get scaled dimmensions for the given image file path
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="filePath">The image file path</param>
        /// <param name="maxWidth">The maximum allowed width</param>
        /// <param name="maxHeight">The maximum allowed height</param>
        /// <returns>Annonymous object contains the scaled image (width and height)</returns>
        protected virtual ExpandoObject ScaleImage(HttpContext context, string filePath, int maxWidth, int maxHeight)
        {
            ExpandoObject result;

            using (Bitmap b = new Bitmap(filePath))
            {
                result = this.ScaleImage(context, b, maxWidth, maxHeight);
            }

            return result;
        }

        /// <summary>
        /// Get scaled dimmensions for the given image file path
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="bitmap">The image</param>
        /// <param name="maxWidth">The maximum allowed width</param>
        /// <param name="maxHeight">The maximum allowed height</param>
        /// <returns>Annonymous object contains the scaled image (width and height)</returns>
        protected virtual ExpandoObject ScaleImage(HttpContext context, Bitmap bitmap, int maxWidth, int maxHeight)
        {
            return this.ScaleImage(context, bitmap, maxWidth, maxHeight, bitmap.Width, bitmap.Height);
        }

        /// <summary>
        /// Get scaled dimmensions for the given image file path
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="bitmap">The image</param>
        /// <param name="maxWidth">The maximum allowed width</param>
        /// <param name="maxHeight">The maximum allowed height</param>
        /// <param name="currentWidth">The current image width</param>
        /// <param name="currentHeight">The current image height</param>
        /// <returns>Annonymous object contains the scaled image (width and height)</returns>
        protected virtual ExpandoObject ScaleImage(HttpContext context, Bitmap bitmap, int maxWidth, int maxHeight, int currentWidth, int currentHeight)
        {
            dynamic result = new ExpandoObject();

            double maxAspect = (double)maxWidth / (double)maxHeight;
            double aspect = (double)currentWidth / (double)currentHeight;

            if (maxAspect > aspect && currentWidth > maxWidth)
            {
                //Width is the bigger dimension relative to max bounds
                currentWidth = maxWidth;
                currentHeight = (int)((double)maxWidth / aspect);
            }
            else if (maxAspect <= aspect && currentHeight > maxHeight)
            {
                //Height is the bigger dimension
                currentHeight = maxHeight;
                currentWidth = (int)((double)maxHeight * aspect);
            }

            result.Width = currentWidth;
            result.Height = currentHeight;


            return result;
        }

        /// <summary>
        ///     If the user specified a ResourceClassKey try to load the resource they specified.
        ///     If the class key is invalid, an exception will be thrown.
        ///     If the class key is valid but the resource is not found, it returns null, in which
        ///     case it will fall back to the MVC default error message. 
        /// </summary>
        /// <param name="context">The http context</param>
        /// <param name="resourceName">The resource name</param>
        /// <returns>The resource string</returns>
        private static string GetResourceString(HttpContext context, string resourceName, params string[] args)
        {
            string localizedValue = DynamicPicturesUploaderHandlerStrings.ResourceManager.GetString(resourceName);
            return string.IsNullOrEmpty(localizedValue) ? string.Format(resourceName, args) : localizedValue;
            /*
            string result = null;

            if (!String.IsNullOrEmpty(ResourceClassKey) && (controllerContext != null) && (controllerContext.HttpContext != null))
            {
                result = controllerContext.HttpContext.GetGlobalResourceObject(ResourceClassKey, resourceName, CultureInfo.CurrentUICulture) as string;
            }

            return result;
             */
        }

        #endregion

        #region Abstraction methods

        /// <summary>
        /// Get information about a given file
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file name</param>
        /// <returns>The given file information</returns>
        protected virtual FileInfo ListSingleFile(HttpContext context, string fileName)
        {
            return new FileInfo(this.CombineUploadsPathAndFileName(context, fileName));
        }

        /// <summary>
        /// Get information about all of the available uploaded files
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <returns>Array contains the available listed files</returns>
        /// <remarks>The default implementation just iterate through the uploads directory, however you can override this method and
        /// use any logic you'd like, such as get files related to a specific entity in the DB etc.</remarks>
        protected virtual IEnumerable<FileInfo> ListAvailableFiles(HttpContext context)
        {
            return (from file in new DirectoryInfo(this.UploadsDirectoryPath).GetFiles("*", SearchOption.TopDirectoryOnly)
                    where !file.Attributes.HasFlag(FileAttributes.Hidden)
                    select file);
        }

        /// <summary>
        /// Get information about all of the available uploaded files
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <returns>Array contains the available listed files</returns>
        protected virtual int CountAvailableFiles(HttpContext context)
        {
            return this.ListAvailableFiles(context).Count();
        }

        /// <summary>
        /// Uploads the given file
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="postedFile">The posted file</param>
        /// <param name="destinationFilePath">The destination file path</param>
        protected virtual void SaveUploadedFile(HttpContext context, HttpPostedFile postedFile, string destinationFilePath)
        {
            postedFile.SaveAs(destinationFilePath);
        }

        /// <summary>
        /// Save the given part of the file, stored in the input stream, in the destination file path.
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="inputStream">The input stream that contains the part of the file to save.</param>
        /// <param name="destinationFilePath">The destination file name to save the chunk in.</param>
        protected virtual void SavePartialUploadedFile(HttpContext context, Stream inputStream, string destinationFilePath)
        {
            using (FileStream fileStream = new FileStream(destinationFilePath, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = new byte[PartialFileReadBytesLength];
                int length = inputStream.Read(buffer, 0, PartialFileReadBytesLength);

                while ((length = inputStream.Read(buffer, 0, PartialFileReadBytesLength)) > 0)
                {
                    fileStream.Write(buffer, 0, length);
                }

                fileStream.Flush();
            }
        }

        /// <summary>
        /// Get the size of an uploaded file (or part of the file in case of multipart form)
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <param name="filePath">The file full path</param>
        /// <returns>The file size</returns>
        protected virtual long GetUploadedFileSize(HttpContext context, string filePath)
        {
            return new FileInfo(filePath).Length;
        }

        /// <summary>
        /// Deletes an uploaded file
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="filePath">The file full path</param>
        /// <returns>Boolean value indicating if the removal process succeded</returns>
        protected virtual bool DeleteFile(HttpContext context, string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }

            return false;
        }

        #endregion

        #region Transportation overridable methods

        /// <summary>
        /// Get transportable data about a given file
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="fileName">The file name</param>
        /// <returns>The given file transportable data</returns>
        protected virtual TransportableListedFile TransportableListSingleFile(HttpContext context, string fileName)
        {
            var fileInfo = this.ListSingleFile(context, fileName);
            this.OnBrowsingFile(context, fileInfo);

            return new TransportableListedFile(context, this, fileInfo);
        }

        /// <summary>
        /// Get transportable data about all of the available uploaded files
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <returns>Array contains the available listed files</returns>
        protected virtual IEnumerable<TransportableListedFile> TransportableListAvailableFiles(HttpContext context)
        {
            List<TransportableListedFile> files = new List<TransportableListedFile>();
            IEnumerable<FileInfo> availableFiles = this.ListAvailableFiles(context);

            this.OnListingFiles(context, availableFiles);

            foreach (var fInfo in availableFiles)
            {
                files.Add(new TransportableListedFile(context, this, fInfo));
            }

            return files;
        }

        /// <summary>
        /// Uploads a given file using whole-file upload OR partial file upload, base on the need
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <param name="postedFile">The posted file</param>
        /// <param name="fileName">The destination file name</param>
        /// <param name="fileType">The file type</param>
        /// <param name="fileSize">The file size</param>
        /// <param name="contentRange">The total content range</param>
        /// <returns>Transportable data about the uploaded file</returns>
        protected virtual TransportableUploadedFile UploadFile(HttpContext context, HttpPostedFile postedFile, string fileName, string fileType, long fileSize, string[] contentRange)
        {
            //-------------------------------------------
            //  Init
            //-------------------------------------------
            fileName = this.FormatFileName(context, fileName, fileType);
            string errorMessage;
            Bitmap bitmap = null;

            //-------------------------------------------
            //  The image is valid?
            //-------------------------------------------
            if (this.ValidateUploadedFile(context, postedFile, fileName, fileType, fileSize, out errorMessage))
            {
                //-------------------------------------------
                //  Create the image uploads directory in case it's not exists
                //-------------------------------------------
                if (!Directory.Exists(this.UploadsDirectoryPath))
                {
                    Directory.CreateDirectory(this.UploadsDirectoryPath);
                }

                //-------------------------------------------
                //  Deletermine the file full path and if we're using multipart upload
                //-------------------------------------------
                string filePath = this.CombineUploadsPathAndFileName(context, fileName);
                bool isPartialUpload = (contentRange != null && File.Exists(filePath)
                    && fileSize > new FileInfo(filePath).Length);

                if (isPartialUpload)
                {
                    //  We're executing a multipart uploading
                    this.SavePartialUploadedFile(context, postedFile.InputStream, filePath);
                }
                else
                {
                    //  We're uploading the whole file
                    this.SaveUploadedFile(context, postedFile, filePath);

                    //  Run uploaded file event
                    this.OnFileUploaded(context, postedFile, filePath);
                }

                if (fileSize == this.GetFileSize(context, filePath))
                {
                    //-------------------------------------------
                    //  If we got JPG, we can rotate the image based on the
                    //  image EXIF data
                    //-------------------------------------------
                    if ((fileType == "image/jpg" || fileType == "image/jpeg"))
                    {
                        ExifReader reader = null;
                        try
                        {
                            using (bitmap = new Bitmap(filePath))
                            {
                                if (fileType == "image/jpg" || fileType == "image/jpeg")
                                {
                                    using (reader = new ExifReader(filePath))
                                    {
                                        int orientation = 0;
                                        if (reader.GetTagValue(ExifTags.Orientation, out orientation))
                                        {
                                            switch (orientation)
                                            {
                                                case 3:
                                                    bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                                    break;
                                                case 6:
                                                    bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                                    break;
                                                case 8:
                                                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                                    break;
                                                default:
                                                    throw new ArgumentException("The given orientation is not valid (" + orientation + ")", "orientation");
                                            }
                                        }
                                    }
                                }
                            }

                            /* Move it back */
                            File.Delete(filePath);
                            File.Move(filePath + ".tmp", filePath);
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            if (bitmap != null)
                            {
                                bitmap.Dispose();
                                bitmap = null;
                            }

                            if (reader != null)
                            {
                                reader.Dispose();
                                reader = null;
                            }
                        }
                    }

                    //-------------------------------------------
                    //  Resize the image?
                    //-------------------------------------------

                    try
                    {
                        bool shouldResizeImage = false;
                        if (this.AllowHighQualityImagesConvertion)
                        {
                            using (bitmap = new Bitmap(filePath))
                            {
                                shouldResizeImage = this.ResizeImageActionMaximumWidth > -1 && this.ResizeImageActionMaximumHeight > -1
                                        && (this.ResizeImageActionMaximumWidth < bitmap.Width || this.ResizeImageActionMaximumHeight < bitmap.Height);

                                /* Should we resize this image? */
                                if (shouldResizeImage)
                                {
                                    /* Resize the image and save it as high-quality image */
                                    dynamic result = this.ScaleImage(context, bitmap, this.ResizeImageActionMaximumWidth, this.ResizeImageActionMaximumHeight);
                                    using (Bitmap resizedBitmap = ResizeImage(bitmap, result.Width, result.Height))
                                    {
                                        SaveJpeg(filePath + ".tmp", resizedBitmap, this.HighQualityImageLevel);
                                    }
                                }
                                else
                                {
                                    /* Since we've requested to save the image as high-quality
                                     * we'll just do so */
                                    SaveJpeg(filePath + ".tmp", bitmap, this.HighQualityImageLevel);
                                }
                            }

                            File.Delete(filePath);
                            File.Move(filePath + ".tmp", filePath);
                        }
                        else
                        {
                            using (bitmap = new Bitmap(filePath))
                            {
                                /* Just resize the image if we need to */
                                shouldResizeImage = this.ResizeImageActionMaximumWidth > -1 && this.ResizeImageActionMaximumHeight > -1
                                        && (this.ResizeImageActionMaximumWidth < bitmap.Width || this.ResizeImageActionMaximumHeight < bitmap.Height);

                                if (shouldResizeImage)
                                {
                                    dynamic result = this.ScaleImage(context, bitmap, this.ResizeImageActionMaximumWidth, this.ResizeImageActionMaximumHeight);
                                    using (Graphics graphics = Graphics.FromImage(result))
                                    {
                                        //draw the image into the target bitmap
                                        graphics.DrawImage(bitmap, 0, 0, result.Width, result.Height);
                                    }

                                    bitmap.Save(filePath + ".tmp");
                                }
                            }

                            if (shouldResizeImage)
                            {
                                File.Delete(filePath);
                                File.Move(filePath + ".tmp", filePath);
                            }
                        }
                    }
                    finally
                    {
                        if (bitmap != null)
                        {
                            bitmap.Dispose();
                            bitmap = null;
                        }
                    }

                    //-------------------------------------------
                    //  Call event
                    //-------------------------------------------
                    this.OnUploadWholeFileCompleted(context, new FileInfo(filePath));
                }
                else if (this.DiscardAbortedUploads && contentRange == null)
                {
                    //  We got abroted file, remove it
                    File.Delete(filePath);
                    errorMessage = "FileUploadAbroted";
                }
            }

            return new TransportableUploadedFile(context, this, fileName, fileSize, errorMessage);
        }

        #endregion

        #region DeleteMethodType

        /// <summary>
        /// Enum representing the HTTP request method type to use when user wish to
        /// remove files.
        /// </summary>
        public enum DeleteMethodType
        {
            Delete,
            Post
        }

        #endregion

        #region TransportableListedFile

        /// <summary>
        /// JSON response transportation class representing listed file
        /// </summary>
        protected class TransportableListedFile
        {
            public string name { get; set; }
            public long size { get; set; }
            public string url { get; set; }
            public string delete_url { get; set; }
            public string delete_type { get; set; }
            public bool delete_with_credentials { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string error { get; set; }

            public TransportableListedFile()
            {

            }

            public TransportableListedFile(HttpContext context, DynamicPicturesUploaderHandler parent, FileInfo fileInfo)
            {
                this.SetProperties(context, parent, fileInfo);
            }

            #region Methods

            protected void SetProperties(HttpContext context, DynamicPicturesUploaderHandler parent, FileInfo fileInfo)
            {
                this.SetProperties(context, parent, fileInfo.Name, fileInfo.Length);
            }

            protected void SetProperties(HttpContext context, DynamicPicturesUploaderHandler parent, string fileName, long fileSize)
            {
                this.name = fileName;
                this.size = fileSize;
                this.url = parent.CombineUploadsUrlAndFileName(context, fileName);
                this.delete_type = parent.DeleteType.ToString().ToUpper();

                if (parent.DeleteType != DeleteMethodType.Delete)
                {
                    this.delete_url = parent.BuildUrl(parent.HttpHandlerApiUrl, "file=" + context.Server.UrlEncode(fileName) + "&_method=DELETE");
                }
                else
                {
                    this.delete_url = parent.BuildUrl(parent.HttpHandlerApiUrl, "file=" + context.Server.UrlEncode(fileName));
                }

                if (parent.AccessControlAllowCredentials)
                {
                    this.delete_with_credentials = true;
                }

                if ((parent.ClientReturnedMaximumHeight > -1
                    || parent.ClientReturnedMaximumWidth > -1)
                    && string.IsNullOrEmpty(this.error))
                {
                    dynamic newSize = parent.ScaleImage(context, parent.CombineUploadsPathAndFileName(context, fileName), parent.ClientReturnedMaximumWidth, parent.ClientReturnedMaximumHeight);
                    this.width = newSize.Width;
                    this.height = newSize.Height;
                }
            }

            #endregion
        }

        #endregion

        #region TransportableUploadedFile

        /// <summary>
        /// JSON response transportation class representing uploaded file
        /// </summary>
        protected class TransportableUploadedFile : TransportableListedFile
        {
            public TransportableUploadedFile(HttpContext context, DynamicPicturesUploaderHandler parent, FileInfo fileInfo, string errorMessage)
                : base(context, parent, fileInfo)
            {
                this.error = errorMessage;
            }

            public TransportableUploadedFile(HttpContext context, DynamicPicturesUploaderHandler parent, string fileName, long fileSize)
            {
                this.SetProperties(context, parent, fileName, fileSize);
            }

            public TransportableUploadedFile(HttpContext context, DynamicPicturesUploaderHandler parent, string fileName, long fileSize, string errorMessage)
            {
                this.error = errorMessage;
                this.SetProperties(context, parent, fileName, fileSize);
            }
        }

        #endregion

        #region High-quality image scaling (http://stackoverflow.com/questions/249587/high-quality-image-scaling-c-sharp)

        /// <summary>
        /// A quick lookup for getting image encoders
        /// </summary>
        private static Dictionary<string, ImageCodecInfo> encoders = null;

        /// <summary>
        /// A quick lookup for getting image encoders
        /// </summary>
        public static Dictionary<string, ImageCodecInfo> Encoders
        {
            //get accessor that creates the dictionary on demand
            get
            {
                //if the quick lookup isn't initialised, initialise it
                if (encoders == null)
                {
                    encoders = new Dictionary<string, ImageCodecInfo>();
                }

                //if there are no codecs, try loading them
                if (encoders.Count == 0)
                {
                    //get all the codecs
                    foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
                    {
                        //add each codec to the quick lookup
                        encoders.Add(codec.MimeType.ToLower(), codec);
                    }
                }

                //return the lookup
                return encoders;
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            //a holder for the result
            Bitmap result = new Bitmap(width, height);
            // set the resolutions the same to avoid cropping due to resolution differences
            result.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            //use a graphics object to draw the resized image into the bitmap
            using (Graphics graphics = Graphics.FromImage(result))
            {
                //set the resize quality modes to high quality
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;


                //draw the image into the target bitmap
                graphics.DrawImage(image, 0, 0, result.Width, result.Height);
            }

            //return the resulting bitmap
            return result;
        }

        /// <summary> 
        /// Saves an image as a jpeg image, with the given quality 
        /// </summary> 
        /// <param name="path">Path to which the image would be saved.</param> 
        /// <param name="quality">An integer from 0 to 100, with 100 being the 
        /// highest quality</param> 
        /// <exception cref="ArgumentOutOfRangeException">
        /// An invalid value was entered for image quality.
        /// </exception>
        public static void SaveJpeg(string path, Image image, int quality)
        {
            //ensure the quality is within the correct range
            if ((quality < 0) || (quality > 100))
            {
                //create the error message
                string error = string.Format("Jpeg image quality must be between 0 and 100, with 100 being the highest quality.  A value of {0} was specified.", quality);
                //throw a helpful exception
                throw new ArgumentOutOfRangeException(error);
            }

            //create an encoder parameter for the image quality
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            //get the jpeg codec
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

            //create a collection of all parameters that we will pass to the encoder
            EncoderParameters encoderParams = new EncoderParameters(1);
            //set the quality parameter for the codec
            encoderParams.Param[0] = qualityParam;
            //save the image using the codec and the parameters
            image.Save(path, jpegCodec, encoderParams);
        }

        /// <summary> 
        /// Returns the image codec with the given mime type 
        /// </summary> 
        public static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            //do a case insensitive search for the mime type
            string lookupKey = mimeType.ToLower();

            //the codec to return, default to null
            ImageCodecInfo foundCodec = null;

            //if we have the encoder, get it to return
            if (Encoders.ContainsKey(lookupKey))
            {
                //pull the codec from the lookup
                foundCodec = Encoders[lookupKey];
            }

            return foundCodec;
        }


        #endregion

        #region Events implementation

        protected virtual void OnBeforeResolveAction(HttpContext context)
        {
            if (this.BeforeResolveAction != null)
                this.BeforeResolveAction(this, new HttpContextEventArgs(context));
        }

        protected virtual void OnUnsupportedHttpMethodTriggered(HttpContext context)
        {
            if (this.UnsupportedHttpMethodTriggered != null)
                this.UnsupportedHttpMethodTriggered(this, new HttpContextEventArgs(context));
        }

        protected virtual void OnFileUploaded(HttpContext context, HttpPostedFile postedFile, string destinationFilePath)
        {
            if (this.FileUploaded != null)
                this.FileUploaded(this, new FileUploadEventArgs(context, postedFile, destinationFilePath));
        }

        protected virtual void OnPartialFileUploaded(HttpContext context, Stream fileStream, string destinationFilePath)
        {
            if (this.PartialFileUploaded != null)
                this.PartialFileUploaded(this, new PartialFileUploadEventArgs(context, fileStream, destinationFilePath));
        }

        protected virtual void OnUploadWholeFileCompleted(HttpContext context, FileInfo info)
        {
            if (this.UploadWholeFileCompleted != null)
                this.UploadWholeFileCompleted(this, new FileInfoEventArgs(context, info));
        }

        protected virtual void OnAfterSendResponse(HttpContext context)
        {
            if (this.AfterSendResponse != null)
                this.AfterSendResponse(this, new HttpContextEventArgs(context));
        }

        protected virtual void OnBeforeFileDeleted(HttpContext context, FileInfo fileInfo)
        {
            if (this.BeforeFileDeleted != null)
                this.BeforeFileDeleted(this, new FileInfoEventArgs(context, fileInfo));
        }

        protected virtual void OnListingFiles(HttpContext context, IEnumerable<FileInfo> filesList)
        {
            if (this.ListingFiles != null)
                this.ListingFiles(this, new FileInfoListingEventArgs(context, filesList));
        }

        protected virtual void OnBrowsingFile(HttpContext context, FileInfo fileInfo)
        {
            if (this.BrowsingFile != null)
                this.BrowsingFile(this, new FileInfoEventArgs(context, fileInfo));
        }

        #endregion
    }

    /// <summary>
    /// Event args that contains the used HttpContext instance
    /// </summary>
    public class HttpContextEventArgs : EventArgs
    {
        public HttpContext Context { get; set; }

        public HttpContextEventArgs(HttpContext context)
            : base()
        {
            this.Context = context;
        }
    }

    /// <summary>
    /// Event args contains the posted file to upload and its destination file path
    /// </summary>
    public class FileUploadEventArgs : HttpContextEventArgs
    {
        public string DestinationFilePath { get; set; }
        public HttpPostedFile InputFile { get; set; }

        public FileUploadEventArgs(HttpContext context, HttpPostedFile inputFile, string destinationFilePath)
            : base(context)
        {
            this.InputFile = inputFile;
            this.DestinationFilePath = destinationFilePath;
        }
    }

    /// <summary>
    /// Event args contains the partial posted file input stream and the destination file path
    /// </summary>
    public class PartialFileUploadEventArgs : HttpContextEventArgs
    {
        public Stream InputStream { get; set; }
        public string DestinationFilePath { get; set; }

        public PartialFileUploadEventArgs(HttpContext context, Stream inputStream, string destinationFilePath)
            : base(context)
        {
            this.InputStream = inputStream;
            this.DestinationFilePath = destinationFilePath;
        }
    }

    /// <summary>
    /// Event args contains uploaded file (existsting file)
    /// </summary>
    public class FileInfoEventArgs : HttpContextEventArgs
    {
        public FileInfo UploadedFile { get; set; }

        public FileInfoEventArgs(HttpContext context, FileInfo uploadedFile)
            : base(context)
        {
            this.UploadedFile = uploadedFile;
        }
    }

    /// <summary>
    /// Event args contains files info array that're going to be listed to the client as TransportableListedFile
    /// </summary>
    public class FileInfoListingEventArgs : HttpContextEventArgs
    {
        public IEnumerable<FileInfo> UploadedFilesList { get; set; }

        public FileInfoListingEventArgs(HttpContext context, IEnumerable<FileInfo> uploadedFilesList)
            : base(context)
        {
            this.UploadedFilesList = uploadedFilesList;
        }
    }
}