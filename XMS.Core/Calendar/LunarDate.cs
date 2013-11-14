using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core.Calendar
{
    public class ResultDate
    {
        public ResultDate()
        {
            SolarFestival = new List<string>();
            LegalFestival = new List<string>();
            LunarDateObj = new LunarDate();
        }
        /// <summary>
        /// 公历日期
        /// </summary>
        public DateTime SolarDate { get; set; }
        /// <summary>
        /// 农历日期对象
        /// </summary>
        public LunarDate LunarDateObj { get; set; }
        /// <summary>
        /// 公历假日
        /// </summary>
        public List<string> SolarFestival { get; set; }
        /// <summary>
        /// 法定节日(包含农历和阳历假日)
        /// </summary>
        public List<string> LegalFestival { get; set; }
        /// <summary>
        /// 农历节日
        /// </summary>
        public string LunarFestival { get; set; }
    }

    public class LunarDate
    {
        /// <summary>
        /// 农历年份
        /// </summary>
        public string LunarYear { get; set; }
        /// <summary>
        /// 农历年份
        /// </summary>
        public int nLunarYear { get; set; }
        /// <summary>
        /// 农历月份
        /// </summary>
        public string LunarMonth { get; set; }
        /// <summary>
        /// 农历月份
        /// </summary>
        public int nLunarMonth { get; set; }
        /// <summary>
        /// 农历日
        /// </summary>
        public string LunarDay { get; set; }        
        /// <summary>
        /// 农历日
        /// </summary>
        public int nLunarDay { get; set; }
    }
    public class Festival
    {
        /// <summary>
        /// 节日名称
        /// </summary>
        public string FestivalName { get; set; }
        /// <summary>
        /// 是否法定
        /// </summary>
        public bool IsLegal { get; set; }
    }
}
