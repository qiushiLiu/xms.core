using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Calendar
{
    public class DefaultCalendarService : ICalendarService
    {
        public ResultDate GetDate(DateTime dtDate)
        {
            return LunarCalendar.Instance.GetDate(dtDate);
        }

        public List<ResultDate> FindDate(DateTime dtStartDate, DateTime dtEndDate)
        {
            return LunarCalendar.Instance.FindDate(dtStartDate, dtEndDate);
        }
    }
}
