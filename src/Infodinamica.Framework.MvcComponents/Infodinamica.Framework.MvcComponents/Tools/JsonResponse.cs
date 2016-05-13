using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infodinamica.Framework.MvcComponents.Tools
{
    /// <summary>
    /// Contiene una respuesta generica en formato JSON
    /// </summary>
    public class JsonResponse
    {
        /// <summary>
        /// Indica si la variable Errors contiene valores y que deben ser procesados por la capa de presentación
        /// </summary>
        public bool HaveErrors { get; set; }

        /// <summary>
        /// Contiene los errores que deben ser mostrados en la capa de presentación
        /// </summary>
        public IDictionary<string, string> Errors { get; set; }

        /// <summary>
        /// Contiene el resultado de la operación
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Constructor de la clase
        /// </summary>
        public JsonResponse()
        {
            HaveErrors = true;
            Errors = new Dictionary<string, string>();
        }

        /// <summary>
        /// Agrega un error general, no asociado a un campo especifico del formulario
        /// </summary>
        /// <param name="message">Mensaje del error</param>
        public void AddError(string message)
        {
            var generalError = Errors.FirstOrDefault(e => e.Key == string.Empty);
            if (!generalError.Equals(new KeyValuePair<string, string>()))
            {
                Errors[string.Empty] = string.Format("{0}\n{1}", generalError.Value, message);
            }
            else
            {
                Errors.Add(string.Empty, message);    
            }
        }
    }
}
