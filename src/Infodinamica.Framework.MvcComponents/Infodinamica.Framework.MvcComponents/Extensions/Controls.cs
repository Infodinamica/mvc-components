using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Mvc;
using Infodinamica.Framework.MvcComponents.Resources;

namespace Infodinamica.Framework.MvcComponents.Extensions
{
    /// <summary>
    /// Permite crear nuevos tipos de elementos en HTML
    /// </summary>
    public static class Controls
    {
        /// <summary>
        /// Crea un nuevo multiple drop down list
        /// </summary>
        /// <typeparam name="TModel">Modelo a utilizar</typeparam>
        /// <typeparam name="TProperty">Propiedad a utilizar</typeparam>
        /// <param name="htmlHelper">HtmlHelper de MVC</param>
        /// <param name="expression">Expresión que contiene la propiedad a utilizar</param>
        /// <param name="selectList">Listado de elementos a colocar en la lista</param>
        /// <returns>DropDownList del cual se pueden escoger varios elementos de forma simultánea</returns>
        public static MvcHtmlString MultipleDropDownList<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList)
        {
            var selectBuilder = new TagBuilder("select");
            var options = string.Empty;

            if (expression == null)
                throw new ArgumentNullException("expression");

            if (expression.Body == null)
                throw new ArgumentException(ErrorMessages.NoBodyInExpression);

            //Se obtiene el member de la expresion
            var member = (expression.Body as MemberExpression);

            if (member == null || member.Member == null)
                throw new NullReferenceException();

            //Se establece el id
            selectBuilder.GenerateId(member.Member.Name);

            //Se establece el name
            selectBuilder.Attributes.Add(new KeyValuePair<string, string>("name", member.Member.Name));

            //Se establece el css
            selectBuilder.AddCssClass("form-control");
            selectBuilder.AddCssClass("select2");

            //Por cada elemento en el listado, se agrega a los options
            foreach (var item in selectList)
            {
                //Se establece el constructor para cada option
                var optionBuilder = new TagBuilder("option");

                //Se establece el ID y el valor del listado
                optionBuilder.SetInnerText(item.Text);
                optionBuilder.Attributes.Add(new KeyValuePair<string, string>("value", item.Value));

                //Se agrega el option
                options += optionBuilder.ToString();
            }

            //Se asigna el contenido de los elementos hijos
            selectBuilder.InnerHtml = options;

            //Se establece la propiedad multiple
            selectBuilder.Attributes.Add(new KeyValuePair<string, string>("multiple", "multiple"));

            //Se establece el ancho
            selectBuilder.Attributes.Add(new KeyValuePair<string, string>("style", "width: 100%;"));

            //Se establece el mensaje seleccione elementos
            selectBuilder.Attributes.Add(new KeyValuePair<string, string>("data-placeholder", @Presentation.SelectSomeElementFromListMessage));

            //Se retorna el elemento
            return MvcHtmlString.Create(selectBuilder.ToString());
        }

        /// <summary>
        /// Crea un mensaje que puede ser cliqueado y eliminado de la interfaz
        /// </summary>
        /// <param name="helper">Html helper de MVC</param>
        /// <returns>Mensaje de validación que puede ser cliqueado y eliminado de la interfaz</returns>
        public static MvcHtmlString ValidationMessageDismissable(this HtmlHelper helper)
        {
            if (helper.ViewData.ModelState[""] == null || helper.ViewData.ModelState[""].Errors == null)
            {
                return MvcHtmlString.Empty;
            }

            //Se crea el div principal
            TagBuilder tagMainDiv = new TagBuilder("div");
            tagMainDiv.AddCssClass("box-body");

            //Se crea el div hijo
            TagBuilder tagChildDiv = new TagBuilder("div");
            tagChildDiv.AddCssClass("alert");
            tagChildDiv.AddCssClass("alert-danger");
            tagChildDiv.AddCssClass("alert-dismissable");

            //Se crea el botón que permite cerrar la alerta
            TagBuilder tagCloseButton = new TagBuilder("button");
            tagCloseButton.AddCssClass("close");
            tagCloseButton.Attributes.Add(new KeyValuePair<string, string>("data-dismiss", "alert"));
            tagCloseButton.Attributes.Add(new KeyValuePair<string, string>("aria-hidden", "true"));
            tagCloseButton.SetInnerText("x");

            //Se crea el titulo de la ventana de alerta
            TagBuilder tagTitle = new TagBuilder("h4");

            //Se crea el icono de warning
            TagBuilder tagIcon = new TagBuilder("i");
            tagIcon.AddCssClass("icon");
            tagIcon.AddCssClass("fa");
            tagIcon.AddCssClass("fa-warning");

            //Se añade el icono al h4 y el titulo
            tagTitle.InnerHtml += tagIcon.ToString(TagRenderMode.EndTag);
            tagTitle.SetInnerText(Presentation.LabelErrorTitle);

            //Se añade el boton, el h4 y el mensaje al div hijo
            tagChildDiv.InnerHtml += tagCloseButton.ToString(TagRenderMode.Normal);
            tagChildDiv.InnerHtml += tagTitle.ToString(TagRenderMode.Normal);
            tagChildDiv.InnerHtml += helper.ViewData.ModelState[""].Errors[0].ErrorMessage;

            //Se añade el div hijo al div padre 
            tagMainDiv.InnerHtml += tagChildDiv.ToString(TagRenderMode.Normal);

            //Se retorna el mensaje
            return MvcHtmlString.Create(tagMainDiv.ToString(TagRenderMode.Normal));
        }
    }
}
