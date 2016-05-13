using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using Infodinamica.Framework.Core.Extensions.IO;
using Infodinamica.Framework.Core.Extensions.Reflection;
using Infodinamica.Framework.MvcComponents.Resources;

namespace Infodinamica.Framework.MvcComponents.Controllers
{
    /// <summary>
    /// Clase base para implementar en los controladores
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Asigna los errores a la vista
        /// </summary>
        /// <param name="errors">Diccionario con el nombre del campo y el error que contiene.</param>
        protected void BindErrors(IDictionary<string, string> errors)
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
        /// Controla una excepción y la agrega a la vista
        /// </summary>
        /// <param name="ex">Error a ser controlada</param>
        protected virtual void ExceptionHandler(Exception ex)
        {
            IDictionary<string, string> businessErrorCollection = new Dictionary<string, string>();
            businessErrorCollection.Add(new KeyValuePair<string, string>(string.Empty, ex.Message));
            this.BindErrors(businessErrorCollection);

            if (ControllerContext.HttpContext.Request != null && ControllerContext.HttpContext.Request.Form != null)
            {
                var currentFormCollection = new FormCollection(ControllerContext.HttpContext.Request.Form);
                BindForm(currentFormCollection);    
            }
        }

        /// <summary>
        /// Asigna un formulario al formulario de una vista
        /// </summary>
        /// <param name="formCollection">Vector con los datos del formulario</param>
        protected void BindForm(FormCollection formCollection)
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
                this.BindForm(formCollection);
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
                        base.ModelState.AddModelError(propertyInfo.Name, string.Format(ErrorMessages.InvalidValue, rawValue, displayFieldName));
                    }
                }
            }

            if (!base.ModelState.IsValid)
            {
                throw new Exception(null);
            }
            return model;
        }
    }
}
