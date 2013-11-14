using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core.Calendar.Model
{
    [DataContract]
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
        [DataMember]
        public DateTime SolarDate { get; set; }
        /// <summary>
        /// 农历日期对象
        /// </summary>
        [DataMember]
        public LunarDate LunarDateObj { get; set; }
        /// <summary>
        /// 公历假日
        /// </summary>
        [DataMember]
        public List<string> SolarFestival { get; set; }
        /// <summary>
        /// 法定节日(包含农历和阳历假日)
        /// </summary>
        [DataMember]
        public List<string> LegalFestival { get; set; }
        /// <summary>
        /// 农历节日
        /// </summary>
        [DataMember]
        public string LunarFestival { get; set; }
    }

    [DataContract]
    public class LunarDate
    {
        /// <summary>
        /// 农历年份
        /// </summary>
        [DataMember]
        public string LunarYear { get; set; }
        /// <summary>
        /// 农历年份
        /// </summary>
        [DataMember]
        public int nLunarYear { get; set; }
        /// <summary>
        /// 农历月份
        /// </summary>
        [DataMember]
        public string LunarMonth { get; set; }
        /// <summary>
        /// 农历月份
        /// </summary>
        [DataMember]
        public int nLunarMonth { get; set; }
        /// <summary>
        /// 农历日
        /// </summary>
        [DataMember]
        public string LunarDay { get; set; }        
        /// <summary>
        /// 农历日
        /// </summary>
        [DataMember]
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
