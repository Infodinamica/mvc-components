using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Infodinamica.Framework.Core.Containers;
using Infodinamica.Framework.Core.Exceptions;
using Infodinamica.Framework.MvcComponents.Resources;
using Newtonsoft.Json;

namespace Infodinamica.Framework.MvcComponents.Tools
{
    /// <summary>
    /// Respuesta JSON basada en Newtownsoft.Json
    /// </summary>
    public class JsonNetResult : JsonResult
    {
        /// <summary>
        /// Configuración
        /// </summary>
        public JsonSerializerSettings Settings { get; private set; }

        /// <summary>
        /// Error
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// Indica si existen errores
        /// </summary>
        public bool HaveErrors {get { return Error != null; }}

        /// <summary>
        /// Constructor
        /// </summary>
        public JsonNetResult()
        {
            Settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };
            Error = null;
            Data = null;
        }
        
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context", ErrorMessages.CurrentContextIsNull);
            if (this.JsonRequestBehavior == JsonRequestBehavior.DenyGet && string.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(ErrorMessages.GetIsNotAllowed);

            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = string.IsNullOrEmpty(this.ContentType) ? "application/json" : this.ContentType;

            if (this.ContentEncoding != null)
                response.ContentEncoding = this.ContentEncoding;
            
            //Se crea la respuesta
            dynamic responseItem = new
            {
                Data = this.Data ?? new object(),
                HaveErrors = this.HaveErrors,
                Errors = GetErrorList(this.Error)
            };
            
            var scriptSerializer = JsonSerializer.Create(this.Settings);

            using (var sw = new StringWriter())
            {
                scriptSerializer.Serialize(sw, responseItem);
                response.Write(sw.ToString());
            }
        }

        private IList<PlainItem> GetErrorList(Exception ex)
        {
            var errorsMessages = new List<PlainItem>();

            if (ex == null)
                return errorsMessages;

            if (ex is CustomException)
            {
                var customEx = (CustomException) ex;
                if (customEx.HaveDataErrors)
                {
                    foreach (KeyValuePair<string, string> entry in ex.Data)
                    {
                        errorsMessages.Add(new PlainItem()
                        {
                            Text = entry.Value,
                            Value = entry.Key
                        });
                    }
                    return errorsMessages;
                }
            }

            errorsMessages.Add(new PlainItem()
            {
                Text = ex.Message,
                Value = string.Empty
            });

            
            return errorsMessages;
        }
    }
}
