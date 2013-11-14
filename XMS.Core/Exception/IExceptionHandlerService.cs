using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
    public interface IExceptionHandlerService
    {
        void HandlerException(Exception err);
    }
}
