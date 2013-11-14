using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace XMS.Core.Calendar
{
    public interface ICalendarService
    {
        ResultDate GetDate(DateTime dtDate);

		List<ResultDate> FindDate(DateTime dtStartDate, DateTime dtEndDate);    
    }
}
