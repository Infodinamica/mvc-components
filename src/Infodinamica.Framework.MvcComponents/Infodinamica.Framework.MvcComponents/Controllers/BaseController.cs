using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Mvc;
using Infodinamica.Framework.Core.Exceptions;
using Infodinamica.Framework.Core.Extensions.IO;
using Infodinamica.Framework.Core.Extensions.Reflection;
using Infodinamica.Framework.MvcComponents.Resources;
using Infodinamica.Framework.MvcComponents.Tools;

namespace Infodinamica.Framework.MvcComponents.Controllers
{
    /// <summary>
    /// Clase base para implementar en los controladores
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Asigna los errores a la vista. Solo sirve cuando existe un postback de la página
        /// </summary>
        /// <param name="errors">Diccionario con el nombre del campo y el error que contiene.</param>
        protected void BindErrorsOnPostback(IDictionary<string, string> errors)
        {
            if (errors != null)
            {
                foreach (KeyValuePair<string, string> error in errors)
                {
                    if (base.ModelState.IsValidField(error.Key))
                    {
                        base.ModelState.AddModelError(error.Key, error.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Controla una excepción y la agrega a la vista. Solo sirve cuando existe un postback de la página
        /// </summary>
        /// <param name="ex">Error a ser controlada</param>
        protected virtual void ExceptionHandlerOnPostback(Exception ex)
        {
            IDictionary<string, string> businessErrorCollection = new Dictionary<string, string>();
            businessErrorCollection.Add(new KeyValuePair<string, string>(string.Empty, ex.Message));

            //Por cada detalle en el diccionario, se agrega a la colección de errores
            foreach (DictionaryEntry entry in ex.Data)
            {
                var current = businessErrorCollection.FirstOrDefault(x => x.Key == entry.Key.ToString());
                
                //Se agrega en caso que no exista o se modifica en caso que si exista
                if (current.Equals(default(KeyValuePair<string, string>)))
                    businessErrorCollection.Add(new KeyValuePair<string, string>(entry.Key.ToString(),
                        entry.Value.ToString()));
                else
                {
                    var newMessage = current.Value + Environment.NewLine + entry.Value.ToString();
                    businessErrorCollection.Remove(current.Key);
                    businessErrorCollection.Add(new KeyValuePair<string, string>(entry.Key.ToString(), newMessage));
                }
            }
            this.BindErrorsOnPostback(businessErrorCollection);

            if (ControllerContext.HttpContext.Request != null && ControllerContext.HttpContext.Request.Form != null)
            {
                var currentFormCollection = new FormCollection(ControllerContext.HttpContext.Request.Form);
                BindFormOnPostback(currentFormCollection);    
            }
        }
        
        /// <summary>
        /// Limpia los names del formCollection. Util cuando la vista ha sido bindeada a través de un Tuple
        /// </summary>
        /// <param name="formCollection">FormCollectiona limpiar</param>
        /// <param name="keyToClean">Caracter a buscar que separa el Name con el agregado en la vista</param>
        /// <returns></returns>
        protected FormCollection CleanKeys(FormCollection formCollection, string keyToClean)
        {
            var newFormCollection = new FormCollection();

            foreach (string key in formCollection)
            {
                var value = formCollection[key];
                var pos = key.IndexOf(keyToClean);
                var newKey = string.Empty;
                if (pos >= 0)
                    newKey = key.Substring(pos + 1);
                else
                    newKey = key;

                newFormCollection.Add(newKey, value);
            }

            return newFormCollection;
        }

        /// <summary>
        /// Asigna un formulario al formulario de una vista. Solo sirve cuando existe un postback de la página
        /// </summary>
        /// <param name="formCollection">Vector con los datos del formulario</param>
        protected void BindFormOnPostback(FormCollection formCollection)
        {
            foreach (var key in formCollection.AllKeys)
            {
                var value = formCollection.GetValue(key);
                base.ModelState.SetModelValue(key, value);
            }
        }
        
        /// <summary>
        /// Realiza el bind del FormCollection a un tipo de dato fuertemente tipeado (POCO)
        /// </summary>
        /// <typeparam name="T">Tipo de dato del POCO</typeparam>
        /// <param name="formCollection">Elementos enviados en el formulario</param>
        /// <returns>Instancia del POCO con los datos del FormCollection</returns>
        protected T BindToModel<T>(FormCollection formCollection = null) where T : class
        {
            bool isFormCollection = false;
            if (formCollection != null)
            {
                isFormCollection = true;
                this.BindFormOnPostback(formCollection);
                formCollection = this.CleanKeys(formCollection, ".");
            }
            Type modelType = typeof(T);
            T model = Activator.CreateInstance<T>();
            var propertyInfos = modelType.GetProperties();

            for (int i = 0; i < propertyInfos.Length; i++)
            {
                var propertyInfo = propertyInfos[i];
                if (propertyInfo.CanWrite)
                {
                    string rawValue = null;
                    try
                    {
                        if (isFormCollection && formCollection.AllKeys.Contains(propertyInfo.Name))
                        {
                            var valueProvider = formCollection.GetValue(propertyInfo.Name);
                            rawValue = valueProvider.AttemptedValue;
                            object val = valueProvider.RawValue.CastToType(propertyInfo.PropertyType);
                            if (val != null)
                                propertyInfo.SetValue(model, val, null);

                        }
                        else if (!string.IsNullOrEmpty(base.Request[propertyInfo.Name]))
                        {
                            rawValue = base.Request[propertyInfo.Name];
                            object val = base.Request[propertyInfo.Name].CastToType(propertyInfo.PropertyType);
                            if (val != null)
                                propertyInfo.SetValue(model, val, null);

                        }
                        else if (base.Request.Files[propertyInfo.Name] != null)
                        {
                            rawValue = base.Request.Files[propertyInfo.Name].ToString();
                            object val = base.Request.Files[propertyInfo.Name].InputStream.CastToByteArray();
                            if (val != null)
                                propertyInfo.SetValue(model, val, null);

                        }
                        else if (base.ValueProvider.GetValue(propertyInfo.Name) != null)
                        {
                            ValueProviderResult valueProvider2 = base.ValueProvider.GetValue(propertyInfo.Name);
                            rawValue = valueProvider2.AttemptedValue;
                            object val = valueProvider2.RawValue.CastToType(propertyInfo.PropertyType);
                            if (val != null)
                                propertyInfo.SetValue(model, val, null);

                        }
                    }
                    catch (Exception)
                    {
                        DisplayAttribute[] arrDisplayAtt = (DisplayAttribute[])propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), false);
                        string displayFieldName;
                        if (arrDisplayAtt.Length > 0)
                        {
                            displayFieldName = arrDisplayAtt.First<DisplayAttribute>().GetName();
                        }
                        else
                        {
                            DescriptionAttribute[] arrDescAtt = (DescriptionAttribute[])propertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                            displayFieldName = ((arrDescAtt.Length > 0) ? arrDescAtt.First<DescriptionAttribute>().Description : propertyInfo.Name);
                        }
                        //base.ModelState.AddModelError(propertyInfo.Name, string.Format(ErrorMessages.InvalidValue, rawValue, displayFieldName));
                        base.ModelState.AddModelError(propertyInfo.Name, string.Format("Valor inválido", rawValue, displayFieldName));
                    }
                }
            }

            if (!base.ModelState.IsValid)
            {
                throw new Exception(null);
            }
            return model;
        }
        
        /// <summary>
        /// Convierte un dictionary en un dropdown generico
        /// </summary>
        /// <param name="data">Diccionario a retornar a la interfaz</param>
        /// <returns></returns>
        protected IList<SelectListItem> CreateDropDownList(IDictionary<int, string> data)
        {
            var dataList = new List<SelectListItem>();
            foreach (KeyValuePair<int, string> item in data)
            {
                dataList.Add(new SelectListItem()
                {
                    Selected = false,
                    Text = item.Value,
                    Value = item.Key.ToString()
                });
            }
            return dataList;
        }

        /// <summary>
        /// Convierte un dictionary en un dropdown generico
        /// </summary>
        /// <param name="data">Diccionario a retornar a la interfaz</param>
        /// <returns></returns>
        protected IList<SelectListItem> CreateDropDownList(IDictionary<string, string> data)
        {
            var dataList = new List<SelectListItem>();
            foreach (KeyValuePair<string, string> item in data)
            {
                dataList.Add(new SelectListItem()
                {
                    Selected = false,
                    Text = item.Value,
                    Value = item.Key
                });
            }
            return dataList;
        }
        
        /// <summary>
        /// Obtiene la IP del cliente conectado
        /// </summary>
        /// <returns>IP del cliente conectado</returns>
        protected string GetClientAddress()
        {
            string clientAddress = string.Empty;
            if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                clientAddress = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
            }
            else if (!string.IsNullOrWhiteSpace(System.Web.HttpContext.Current.Request.UserHostAddress) && System.Web.HttpContext.Current.Request.UserHostAddress.Length != 0)
            {
                clientAddress = System.Web.HttpContext.Current.Request.UserHostAddress;
            }
            return clientAddress;
        }

        /// <summary>
        /// Calcula el número de página solicitado
        /// </summary>
        /// <param name="formCollection">Colección de items solicitados</param>
        /// <returns>Numero de página solicitado o nulo si no han solicitado ningúna página en particular</returns>
        protected short? GetPageNumberRequested(FormCollection formCollection)
        {
            var formItem = formCollection["PageNumberRequested"];
            short returnValue;
            if (formItem == null)
                return 1;

            if (short.TryParse(formItem, out returnValue))
                return returnValue;
            else
                return 1;
        }

        /// <summary>
        /// Retorna un objeto JSON usando Newtonsoft.JSON
        /// </summary>
        /// <param name="data">Elemento a transformar</param>
        /// <returns>Objeto JSON</returns>
        protected JsonResult CustomJson(object data)
        {
            return new JsonNetResult
            {
                Data = data,
                JsonRequestBehavior = JsonRequestBehavior.DenyGet
            };
        }

        /// <summary>
        /// Retorna un objeto JSON usando Newtonsoft.JSON
        /// </summary>
        /// <param name="data">Elemento a transformar</param>
        /// <param name="behavior">Indica si se admiten llamadas GET</param>
        /// <returns>Objeto JSON</returns>
        protected JsonResult CustomJson(object data, JsonRequestBehavior behavior)
        {
            return new JsonNetResult
            {
                Data = data,
                JsonRequestBehavior = behavior
            };
        }

        /// <summary>
        /// Retorna un objeto JSON usando Newtonsoft.JSON
        /// </summary>
        /// <param name="ex">Error a mostrar en la interfaz, el cual es presentado en formato JSON</param>
        /// <returns>Objeto JSON</returns>
        protected JsonResult CustomJson(Exception ex)
        {
            return new JsonNetResult
            {
                Error = ex,
                JsonRequestBehavior = JsonRequestBehavior.DenyGet
            };
        }

        /// <summary>
        /// Retorna un objeto JSON usando Newtonsoft.JSON
        /// </summary>
        /// <param name="ex">Error a mostrar en la interfaz, el cual es presentado en formato JSON</param>
        /// <param name="behavior">Indica si se admiten llamadas GET</param>
        /// <returns>Objeto JSON</returns>
        protected JsonResult CustomJson(Exception ex, JsonRequestBehavior behavior)
        {
            return new JsonNetResult
            {
                Error = ex,
                JsonRequestBehavior = behavior
            };
        }

        /// <summary>
        /// Retorna un objeto JSON usando Newtonsoft.JSON
        /// </summary>
        /// <param name="data">Elemento a transformar</param>
        /// <param name="ex">Error a mostrar en la interfaz, el cual es presentado en formato JSON</param>
        /// <returns>Objeto JSON</returns>
        protected JsonResult CustomJson(object data, Exception ex)
        {
            return new JsonNetResult
            {
                Data = data,
                Error = ex,
                JsonRequestBehavior = JsonRequestBehavior.DenyGet
            };
        }

        /// <summary>
        /// Retorna un objeto JSON usando Newtonsoft.JSON
        /// </summary>
        /// <param name="data">Elemento a transformar</param>
        /// <param name="ex">Error a mostrar en la interfaz, el cual es presentado en formato JSON</param>
        /// <param name="behavior">Indica si se admiten llamadas GET</param>
        /// <returns>Objeto JSON</returns>
        protected JsonResult CustomJson(object data, Exception ex, JsonRequestBehavior behavior)
        {
            return new JsonNetResult
            {
                Data = data,
                Error = ex,
                JsonRequestBehavior = behavior
            };
        }
        
        /// <summary>
        /// Obtiene el HTML del partialview
        /// </summary>
        /// <returns>string con el HTML del partialview</returns>
        protected string RenderPartialToString()
        {
            InvalidateControllerContext();
            string partialViewName = this.ControllerContext.RouteData.Values["action"].ToString(); 
            IView view = ViewEngines.Engines.FindPartialView(ControllerContext, partialViewName).View;
            string result = RenderViewToString(view);
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del partialview
        /// </summary>
        /// <param name="partialViewName">Nombre del partialview</param>
        /// <returns>string con el HTML del partialview</returns>
        protected string RenderPartialToString(string partialViewName)
        {
            InvalidateControllerContext();
            IView view = ViewEngines.Engines.FindPartialView(ControllerContext, partialViewName).View;
            string result = RenderViewToString(view);
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del partialview
        /// </summary>
        /// <param name="model">Modelo para bindear la vista parcial</param>
        /// <returns>string con el HTML del partialview</returns>
        protected string RenderPartialToString(object model)
        {
            InvalidateControllerContext();
            string partialViewName = this.ControllerContext.RouteData.Values["action"].ToString();    
            IView view = ViewEngines.Engines.FindPartialView(ControllerContext, partialViewName).View;
            string result = RenderViewToString(view, model);
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del partialview
        /// </summary>
        /// <param name="partialViewName">Nombre del partialview</param>
        /// <param name="model">Modelo para bindear la vista parcial</param>
        /// <returns>string con el HTML del partialview</returns>
        protected string RenderPartialToString(string partialViewName, object model)
        {
            InvalidateControllerContext();
            IView view = ViewEngines.Engines.FindPartialView(ControllerContext, partialViewName).View;
            string result = RenderViewToString(view, model);
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del view
        /// </summary>
        /// <returns>string con el HTML del view</returns>
        protected string RenderViewToString()
        {
            InvalidateControllerContext();
            string viewName = this.ControllerContext.RouteData.Values["action"].ToString(); 
            IView view = ViewEngines.Engines.FindView(ControllerContext, viewName, null).View;
            string result = RenderViewToString(view);
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del view
        /// </summary>
        /// <param name="viewName">Nombre del view</param>
        /// <returns>string con el HTML del view</returns>
        protected string RenderViewToString(string viewName)
        {
            InvalidateControllerContext();
            IView view = ViewEngines.Engines.FindView(ControllerContext, viewName, null).View;
            string result = RenderViewToString(view);
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del view
        /// </summary>
        /// <param name="model">Modelo para bindear la vista</param>
        /// <returns>string con el HTML del view</returns>
        protected string RenderViewToString(object model)
        {
            InvalidateControllerContext();
            string viewName = this.ControllerContext.RouteData.Values["action"].ToString();
            IView view = ViewEngines.Engines.FindView(ControllerContext, viewName, null).View;
            string result = RenderViewToString(view, model);
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del view
        /// </summary>
        /// <param name="viewName">Nombre del view</param>
        /// <param name="model">Modelo para bindear la vista</param>
        /// <returns>string con el HTML del view</returns>
        protected string RenderViewToString(string viewName, object model)
        {
            InvalidateControllerContext();
            IView view = ViewEngines.Engines.FindView(ControllerContext, viewName, null).View;
            string result = RenderViewToString(view, model);
            return result;
        }
        
        /// <summary>
        /// Obtiene el HTML del view
        /// </summary>
        /// <param name="view">Interfaz de la vista</param>
        /// <returns>string con el HTML del view</returns>
        protected string RenderViewToString(IView view)
        {
            InvalidateControllerContext();
            string result = null;
            if (view != null)
            {
                StringBuilder sb = new StringBuilder();
                using (StringWriter writer = new StringWriter(sb))
                {
                    ViewContext viewContext = new ViewContext(ControllerContext, view, new ViewDataDictionary(), new TempDataDictionary(), writer);
                    view.Render(viewContext, writer);
                    writer.Flush();
                }
                result = sb.ToString();
            }
            return result;
        }

        /// <summary>
        /// Obtiene el HTML del view
        /// </summary>
        /// <param name="view">Interfaz de la vista</param>
        /// <param name="model">Modelo para bindear la vista</param>
        /// <returns>string con el HTML del view</returns>
        protected string RenderViewToString(IView view, object model)
        {
            InvalidateControllerContext();
            string result = null;
            if (view != null)
            {
                StringBuilder sb = new StringBuilder();
                using (StringWriter writer = new StringWriter(sb))
                {
                    ViewContext viewContext = new ViewContext(ControllerContext, view, new ViewDataDictionary(model), new TempDataDictionary(), writer);
                    view.Render(viewContext, writer);
                    writer.Flush();
                }
                result = sb.ToString();
            }
            return result;
        }

        /// <summary>
        /// Transforma un string que representa un JSON en un objeto fuertemente tipado
        /// </summary>
        /// <typeparam name="T">Tipo de dato a retornar</typeparam>
        /// <param name="json">string que representa al objeto en formato JSON</param>
        /// <seealso cref="http://haacked.com/archive/2010/04/15/sending-json-to-an-asp-net-mvc-action-method-argument.aspx/"/>
        /// <returns>Objeto fuertemente tipado del tipo T</returns>
        protected T BindToModelFromJson<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serialiser = new DataContractJsonSerializer(typeof(T));
                return (T)serialiser.ReadObject(ms);
            }
        }

        private void InvalidateControllerContext()
        {
            if (ControllerContext == null)
            {
                ControllerContext context = new ControllerContext(System.Web.HttpContext.Current.Request.RequestContext, this);
                ControllerContext = context;
            }
        }
    }
}
