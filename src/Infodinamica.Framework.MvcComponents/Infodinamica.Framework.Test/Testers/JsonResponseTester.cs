using System;
using Infodinamica.Framework.MvcComponents.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Infodinamica.Framework.Test.Testers
{
    [TestClass]
    public class JsonResponseTester
    {
        [TestMethod]
        public void TwoGeneralErrors()
        {
            var jsonResult = new JsonResponse();
            jsonResult.AddError("Error 1");
            jsonResult.AddError("Error 2");
            jsonResult.AddError("Error 3");

            if (jsonResult.Errors.Count != 1)
                throw new Exception("La cantidad de errores entregadas es incorrecta");
        }
    }
}
