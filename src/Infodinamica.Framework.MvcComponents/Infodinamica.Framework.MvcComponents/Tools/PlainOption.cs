using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Infodinamica.Framework.Core.Containers;

namespace Infodinamica.Framework.MvcComponents.Tools
{
    /// <summary>
    /// Represente un option en un select o dropdownlist
    /// </summary>
    public class PlainOption: PlainItem
    {
        /// <summary>
        /// Indica si el valor está seleccionado. Por defecto es falso
        /// </summary>
        public bool IsSelected { get; set; }
    }
}
