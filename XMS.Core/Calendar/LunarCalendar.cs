using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Calendar
{
    public class LunarCalendar
    {
        private static LunarCalendar _instance;
        public static LunarCalendar Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LunarCalendar();
                }
                return _instance;
            }
        }

        public ResultDate GetDate(DateTime dtSolarDate)
        {
            int nYear = dtSolarDate.Year;
            if (nYear < START_YEAR || nYear > END_YEAR)
            {
                throw new ArgumentException("暂不支持这个时间的查询");
            }
            int nMonth = dtSolarDate.Month;
            int nDay = dtSolarDate.Day;
            ResultDate objResultDate = new ResultDate();
            //判断是否是本年份的清明节           
            if (nDay == QingMing(nYear))
            {
                objResultDate.LegalFestival.Add("清明节");
                objResultDate.SolarFestival.Add("清明节");
            }
            //根据公历返回农历日期对象
            LunarDate objLunarDate = GetLunarDate(dtSolarDate);
            objResultDate.LunarDateObj = objLunarDate;
            objResultDate.SolarDate = dtSolarDate;
            //取出这个日期公历节假日列表
            List<Festival> listSolarFestival = SolarFestival(nMonth, nDay);
            if (listSolarFestival != null)
            {
                foreach (Festival objItem in listSolarFestival)
                {
                    objResultDate.SolarFestival.Add(objItem.FestivalName);
                    if (objItem.IsLegal)
                    {
                        objResultDate.LegalFestival.Add(objItem.FestivalName);
                    }
                }
            }
            //取出这个日期农历节假日列表
            List<Festival> listLunarFestival = LunarFete(objLunarDate.nLunarMonth, objLunarDate.nLunarDay);
            if (listLunarFestival != null)
            {
                foreach (Festival objItem in listLunarFestival)
                {
                    objResultDate.SolarFestival.Add(objItem.FestivalName);
                    if (objItem.IsLegal)
                    {
                        objResultDate.LegalFestival.Add(objItem.FestivalName);
                    }
                }
            }
            //取出这个日期周期节假日列表
            List<Festival> listWeekFestival = WordFete(nMonth, returnweekNum(dtSolarDate), CaculateWeekDay(dtSolarDate));
            if (listWeekFestival != null)
            {
                foreach (Festival objItem in listWeekFestival)
                {
                    objResultDate.SolarFestival.Add(objItem.FestivalName);
                    if (objItem.IsLegal)
                    {
                        objResultDate.LegalFestival.Add(objItem.FestivalName);
                    }
                }
            }
            return objResultDate;
        }
        /// <summary>
        /// 查询某时间段内的节假日信息
        /// </summary>
        /// <param name="dtStartDate">开始时间</param>
        /// <param name="dtEndDate">结束时间</param>
        /// <returns></returns>
        public List<ResultDate> FindDate(DateTime dtStartDate, DateTime dtEndDate)
        {
            if (dtStartDate >= dtEndDate)
            {
                throw new ArgumentException("查询开始时间不能小于等于结束时间");
            }
            List<ResultDate> listResultDate = new List<ResultDate>();
            while (dtStartDate < dtEndDate)
            {
                listResultDate.Add(GetDate(dtStartDate));
                dtStartDate = dtStartDate.AddDays(1);
            }
            return listResultDate;
        }
        #region //静态数据
        private static int[] lunarInfo = { 0x04bd8,0x04ae0,0x0a570,0x054d5,0x0d260,0x0d950,0x16554,0x056a0,0x09ad0,0x055d2,
                                           0x04ae0,0x0a5b6,0x0a4d0,0x0d250,0x1d255,0x0b540,0x0d6a0,0x0ada2,0x095b0,0x14977,
                                           0x04970,0x0a4b0,0x0b4b5,0x06a50,0x06d40,0x1ab54,0x02b60,0x09570,0x052f2,0x04970,
                                           0x06566,0x0d4a0,0x0ea50,0x06e95,0x05ad0,0x02b60,0x186e3,0x092e0,0x1c8d7,0x0c950,
                                           0x0d4a0,0x1d8a6,0x0b550,0x056a0,0x1a5b4,0x025d0,0x092d0,0x0d2b2,0x0a950,0x0b557,
                                           0x06ca0,0x0b550,0x15355,0x04da0,0x0a5b0,0x14573,0x052b0,0x0a9a8,0x0e950,0x06aa0,
                                           0x0aea6,0x0ab50,0x04b60,0x0aae4,0x0a570,0x05260,0x0f263,0x0d950,0x05b57,0x056a0,
                                           0x096d0,0x04dd5,0x04ad0,0x0a4d0,0x0d4d4,0x0d250,0x0d558,0x0b540,0x0b6a0,0x195a6,
                                           0x095b0,0x049b0,0x0a974,0x0a4b0,0x0b27a,0x06a50,0x06d40,0x0af46,0x0ab60,0x09570,
                                           0x04af5,0x04970,0x064b0,0x074a3,0x0ea50,0x06b58,0x055c0,0x0ab60,0x096d5,0x092e0,
                                           0x0c960,0x0d954,0x0d4a0,0x0da50,0x07552,0x056a0,0x0abb7,0x025d0,0x092d0,0x0cab5,
                                           0x0a950,0x0b4a0,0x0baa4,0x0ad50,0x055d9,0x04ba0,0x0a5b0,0x15176,0x052b0,0x0a930,
                                           0x07954,0x06aa0,0x0ad50,0x05b52,0x04b60,0x0a6e6,0x0a4e0,0x0d260,0x0ea65,0x0d530,
                                           0x05aa0,0x076a3,0x096d0,0x04bd7,0x04ad0,0x0a4d0,0x1d0b6,0x0d250,0x0d520,0x0dd45,
                                           0x0b5a0,0x056d0,0x055b2,0x049b0,0x0a577,0x0a4b0,0x0aa50,0x1b255,0x06d20,0x0ada0,
                                           0x14b63
                                         };
        private static int[] solarMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private static string[] Gan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        private static string[] Zhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
        private static string[] Animals = { "鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪" };
        private static string[] solarTerm = { "小寒", "大寒", "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满", "芒种", "夏至", "小暑", "大暑", "立秋", "处暑", "白露", "秋分", "寒露", "霜降", "立冬", "小雪", "大雪", "冬至" };
        private static int[] sTermInfo = { 0, 21208, 42467, 63836, 85337, 107014, 128867, 150921, 173149, 195551, 218072, 240693, 263343, 285989, 308563, 331033, 353350, 375494, 397447, 419210, 440795, 462224, 483532, 504758 };
        private static string[] nStr1 = { "日", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
        private static string[] lunarYearName = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
        private static string[] nStr2 = { "初", "十", "廿", "卅", "□" };
        private static string[] monthName = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        private static string[] lunarMonthName = { "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一", "腊月" };
        //国历节日*表示放假日
        private static string[] sFtv = {    "0101*元旦节",
                                           "0202 世界湿地日",
                                           "0210 国际气象节",
                                           "0214 情人节",
                                           "0301 国际海豹日",
                                           "0303 全国爱耳日",
                                           "0305 学雷锋纪念日",
                                           "0308 妇女节",
                                           "0312 植树节 孙中山逝世纪念日",
                                           "0314 国际警察日",
                                           "0315 消费者权益日",
                                           "0317 中国国医节 国际航海日",
                                           "0321 世界森林日 消除种族歧视国际日 世界儿歌日",
                                           "0322 世界水日",
                                           "0323 世界气象日",
                                           "0324 世界防治结核病日",
                                           "0325 全国中小学生安全教育日",
                                           "0330 巴勒斯坦国土日",
                                           "0401 愚人节 全国爱国卫生运动月(四月) 税收宣传月(四月)",
                                           "0407 世界卫生日","0422 世界地球日","0423 世界图书和版权日",
                                           "0424 亚非新闻工作者日",
                                           "0501*劳动节",
                                           "0504 青年节",
                                           "0505 碘缺乏病防治日",
                                           "0508 世界红十字日",
                                           "0512 国际护士节",
                                           "0515 国际家庭日",
                                           "0517 国际电信日",
                                           "0518 国际博物馆日",
                                           "0520 全国学生营养日",
                                           "0523 国际牛奶日",
                                           "0531 世界无烟日",
                                           "0601 国际儿童节",
                                           "0605 世界环境保护日",
                                           "0606 全国爱眼日",
                                           "0617 防治荒漠化和干旱日",
                                           "0623 国际奥林匹克日",
                                           "0625 全国土地日",
                                           "0626 国际禁毒日",
                                           "0701 香港回归纪念日 中共诞辰 世界建筑日",
                                           "0702 国际体育记者日",
                                           "0707 抗日战争纪念日",
                                           "0711 世界人口日",
                                           "0730 非洲妇女日",
                                           "0801 建军节",
                                           "0808 中国男子节(爸爸节)",
                                           "0815 抗日战争胜利纪念",
                                           "0908 国际扫盲日 国际新闻工作者日",
                                           "0909 毛泽东逝世纪念",
                                           "0910 中国教师节",
                                           "0914 世界清洁地球日",
                                           "0916 国际臭氧层保护日",
                                           "0918 九·一八事变纪念日",
                                           "0920 国际爱牙日",
                                           "0927 世界旅游日",
                                           "0928 孔子诞辰",
                                           "1001*国庆节 世界音乐日 国际老人节",
                                           "1002*国庆节假日 国际和平与民主自由斗争日",
                                           "1003*国庆节假日",
                                           "1004*国庆节假日 世界动物日",
                                           "1005*国庆节假日",
                                           "1006*国庆节假日 老人节",
                                           "1007*国庆节假日",
                                           "1008 全国高血压日 世界视觉日",
                                           "1009 世界邮政日 万国邮联日",
                                           "1010 辛亥革命纪念日 世界精神卫生日",
                                           "1013 世界保健日 国际教师节",
                                           "1014 世界标准日",
                                           "1015 国际盲人节(白手杖节)",
                                           "1016 世界粮食日",
                                           "1017 世界消除贫困日",
                                           "1022 世界传统医药日",
                                           "1024 联合国日",
                                           "1031 世界勤俭日",
                                           "1107 十月社会主义革命纪念日",
                                           "1108 中国记者日",
                                           "1109 全国消防安全宣传教育日",
                                           "1110 世界青年节",
                                           "1111 国际科学与和平周(本日所属的一周)",
                                           "1112 孙中山诞辰纪念日",
                                           "1114 世界糖尿病日",
                                           "1117 国际大学生节 世界学生节",
                                           "1120*彝族年",
                                           "1121*彝族年 世界问候日 世界电视日",
                                           "1122*彝族年",
                                           "1129 国际声援巴勒斯坦人民国际日",
                                           "1201 世界艾滋病日",
                                           "1203 世界残疾人日",
                                           "1205 国际经济和社会发展志愿人员日",
                                           "1208 国际儿童电视日",
                                           "1209 世界足球日",
                                           "1210 世界人权日",
                                           "1212 西安事变纪念日",
                                           "1213 南京大屠杀(1937年)纪念日！谨记血泪史！",
                                           "1220 澳门回归纪念",
                                           "1221 国际篮球日",
                                           "1224 平安夜",
                                           "1225 圣诞节",
                                           "1226 毛泽东诞辰纪念"};
        //农历节日*表示放假日
        private static string[] lFtv = { "0101*春节", "0102*春节假日", "0103*春节假日", "0104*春节假日", "0105*春节假日", "0106*春节假日", "0115 元宵节", "0505*端午节", "0624*火把节", "0625*火把节", "0626*火把节", "0707 七夕情人节", "0715 中元节", "0815*中秋节", "0909 重阳节", "1208 腊八节", "1224 小年", "0100*除夕" };
        //某月的第几个星期几   //一月的最后一个星期日（月倒数第一个星期日）
        private static string[] twFtv = { "0150 世界麻风日", "0351 全国中小学生安全教育日", "0520 国际母亲节", "0530 全国助残日", "0630 父亲节", "0730 被奴役国家周", "0932 国际和平日", "0940 国际聋人节世界儿童日", "0950 世界海事日", "1011 国际住房日", "1013 国际减轻自然灾害日(减灾日)", "1144 感恩节" };
        #endregion
        private DateTime baseDate = new DateTime(1900, 1, 31);
        private const int START_YEAR = 1901;
        private const int END_YEAR = 2050;
        #region//日期计算方法

        ///<summary>
        ///传入农历年　返回农历y年的总天数
        ///</summary>
        ///<param name="year"></param>
        ///<returns></returns>
        public int LunarYearDays(int year) //====================================== 返回农历y年的总天数
        {
            int i, sum = 348;
            for (i = 0x8000; i > 0x8; i >>= 1)
            {
                sum += ((lunarInfo[year - 1900] & i) != 0) ? 1 : 0;
            }
            return (sum + leapDays(year));
        }
        ///<summary>
        ///返回农历y年闰月的天数
        ///</summary>
        ///<param name="year"></param>
        ///<returns></returns>
        public int leapDays(int year) //====================================== 返回农历y年闰月的天数
        {
            if (leapMonth(year) != 0)
            {
                return (((lunarInfo[year - 1900] & 0x10000) != 0) ? 30 : 29);
            }
            else
            {
                return (0);
            }
        }
        ///<summary>
        ///返回农历y年闰哪个月1-12 , 没闰返回0
        ///</summary>
        ///<param name="year"></param>
        ///<returns></returns>
        public int leapMonth(int year) //====================================== 返回农历y年闰哪个月1-12 , 没闰返回0
        {
            return (lunarInfo[year - 1900] & 0xf);
        }
        ///<summary>
        ///返回农历y年m月的总天数
        ///</summary>
        ///<param name="year"></param>
        ///<param name="month"></param>
        ///<returns></returns>
        public int monthDays(int year, int month)//====================================== 返回农历y年m月的总天数
        {
            return (((lunarInfo[year - 1900] & (0x10000 >> month)) != 0) ? 30 : 29);
        }
        ///<summary>
        ///返回公历y年某m+1月的天数
        ///</summary>
        ///<param name="y">公历年</param>
        ///<param name="m">公历月</param>
        ///<returns></returns>
        public int solarDays(int y, int m)
        {
            if (m == 1)
            {
                return (((y % 4 == 0) && (y % 100 != 0) || (y % 400 == 0)) ? 29 : 28);
            }
            else
            {
                return (solarMonth[m]);
            }
        }
        ///<summary>
        ///传入农历年返回干支, 0=甲子
        ///</summary>
        ///<param name="lunarYear"></param>
        ///<returns></returns>
        public string Cyclical(int lunarYear)
        {
            return (Gan[(lunarYear - 4) % 60 % 10] + Zhi[(lunarYear - 4) % 60 % 12]);
        }

        ///<summary>　
        ///传入农历年返回干支, 0=鼠
        ///</summary>
        ///<param name="lunarYear"></param>
        ///<returns></returns>
        private string Animal(int lunarYear)
        {
            return Animals[(lunarYear - 4) % 60 % 12];
        }
        private static string cDay(int d)
        {
            string s;
            switch (d)
            {
                case 10:
                    s = "初十";
                    break;

                case 20:
                    s = "二十";
                    break;
                case 30:
                    s = "三十";
                    break;
                default:
                    s = nStr2[d / 10];
                    s += nStr1[d % 10];
                    break;
            }
            return (s);
        }

        ///<summary>
        ///格式化中文年
        ///</summary>
        ///<param name="d"></param>
        ///<returns></returns>
        private static string FormatYear(int y)
        {
            int sValue = y / 1000;
            int hValue = (y - sValue * 1000) / 100;
            int tenValue = (y - (sValue * 1000 + hValue * 100)) / 10;
            int gValue = y - sValue * 1000 - hValue * 100 - tenValue * 10;
            return lunarYearName[sValue] + lunarYearName[hValue] + lunarYearName[tenValue] + lunarYearName[gValue];
        }
        #endregion
        #region 节假日计算
        ///<summary>
        ///根据公历日期返回农历日期对象
        ///</summary>
        ///<param name="dtSolarDate">公历日期</param>
        ///<returns></returns>
        public LunarDate GetLunarDate(DateTime dtSolarDate)
        {
            TimeSpan timeSpan = dtSolarDate - baseDate;
            int offset = Convert.ToInt32(timeSpan.TotalDays); //86400000=1000*24*60*60
            int temp = 0;
            int i, lunarYear = 0, leap = 0, lunarMonth, lunarDay;//农历年
            bool isLeap;
            for (i = 1900; i < 2050 && offset > 0; i++)
            {
                temp = LunarYearDays(i);
                offset -= temp;
            }
            if (offset < 0)
            {
                offset += temp;
                i--;
            }
            lunarYear = i;
            leap = leapMonth(i); //闰哪个月
            isLeap = false;
            for (i = 1; i < 13 && offset > 0; i++)
            {
                //闰月
                if (leap > 0 && i == (leap + 1) && isLeap == false)
                {
                    --i;
                    isLeap = true;
                    temp = leapDays(lunarYear);
                }
                else
                {
                    temp = monthDays(lunarYear, i);
                }
                //解除闰月
                if (isLeap == true && i == (leap + 1))
                {
                    isLeap = false;
                }
                offset -= temp;
            }
            if (offset == 0 && leap > 0 && i == leap + 1)
            {
                if (isLeap)
                {
                    isLeap = false;
                }
                else
                {
                    isLeap = true;
                    --i;
                }
            }
            if (offset < 0)
            {
                offset += temp;
                --i;
            }
            lunarMonth = i;
            lunarDay = offset + 1;
            LunarDate objLunarDate = new LunarDate();
            objLunarDate.nLunarYear = lunarYear;
            objLunarDate.nLunarMonth = lunarMonth;
            objLunarDate.nLunarDay = lunarDay;
            objLunarDate.LunarYear = Cyclical(lunarYear);
            objLunarDate.LunarMonth = (isLeap ? "闰" : "") + lunarMonthName[lunarMonth - 1];
            objLunarDate.LunarDay = cDay(lunarDay);
            return objLunarDate;
        }
        ///<summary>
        ///公历日期返回公历节假日
        ///</summary>
        ///<param name="month"></param>
        ///<param name="day"></param>
        ///<returns></returns>
        public List<Festival> SolarFestival(int nMonth, int nDay)
        {
            string str = @"(\d{2})(\d{2})([\s*])(.+)$"; //匹配的正则表达式
            System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(str);
            for (int i = 0; i < sFtv.Length; i++)
            {
                string[] s = re.Split(sFtv[i]);
                if (Convert.ToInt32(s[1]) == nMonth && Convert.ToInt32(s[2]) == nDay)
                {
                    string sFestivals = s[4];
                    if (!string.IsNullOrEmpty(sFestivals))
                    {
                        string[] sArrFestival = sFestivals.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        List<Festival> listFestival = new List<Festival>();
                        foreach (string sItem in sArrFestival)
                        {
                            Festival objFestival = new Festival();
                            objFestival.FestivalName = sItem;
                            objFestival.IsLegal = "*".Equals(s[3]);
                            listFestival.Add(objFestival);
                        }
                        return listFestival;
                    }
                }
            }
            return null;
        }
        ///<summary>
        ///农历日期返回农历节日
        ///</summary>
        ///<param name="lunarMonth"></param>
        ///<param name="Lunarday"></param>
        ///<returns></returns>
        public List<Festival> LunarFete(int nLunarMonth, int nLunarDay)
        {
            string str = @"(\d{2})(\d{2})([\s*])(.+)$"; //匹配的正则表达式
            System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(str);
            for (int i = 0; i < lFtv.Length; i++)
            {
                string[] s = re.Split(lFtv[i]);
                if (Convert.ToInt32(s[1]) == nLunarMonth && Convert.ToInt32(s[2]) == nLunarDay)
                {
                    string sFestivals = s[4];
                    if (!string.IsNullOrEmpty(sFestivals))
                    {
                        string[] sArrFestival = sFestivals.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        List<Festival> listFestival = new List<Festival>();
                        foreach (string sItem in sArrFestival)
                        {
                            Festival objFestival = new Festival();
                            objFestival.FestivalName = sItem;
                            objFestival.IsLegal = "*".Equals(s[3]);
                            listFestival.Add(objFestival);
                        }
                        return listFestival;
                    }
                }
            }
            return null;
        }
        ///<summary>
        ///取出是否是周几的节日
        ///</summary>
        ///<param name="month">月</param>
        ///<param name="num">该月第几周</param>
        /// <param name="week">周几</param>
        ///<returns></returns>
        public List<Festival> WordFete(int month, int num, int week)
        {
            string str = @"(\d{2})(\d{1})(\d{1})([\s*])(.+)$"; //匹配的正则表达式
            System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(str);
            for (int i = 0; i < twFtv.Length; i++)
            {
                string[] s = re.Split(twFtv[i]);
                if (Convert.ToInt32(s[1]) == month && Convert.ToInt32(s[2]) == num && Convert.ToInt32(s[3]) == week)
                {
                    string sFestivals = s[5];
                    if (!string.IsNullOrEmpty(sFestivals))
                    {
                        string[] sArrFestival = sFestivals.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        List<Festival> listFestival = new List<Festival>();
                        foreach (string sItem in sArrFestival)
                        {
                            Festival objFestival = new Festival();
                            objFestival.FestivalName = sItem;
                            objFestival.IsLegal = "*".Equals(s[4]);
                            listFestival.Add(objFestival);
                        }
                        return listFestival;
                    }
                }
            }
            return null;
        }
        ///<summary>
        ///期根据年月日计算星期几方法　返回int
        ///</summary>
        ///<param name="dtDate"></param>        
        ///<returns>周日为0</returns>
        public int CaculateWeekDay(DateTime dtDate)
        {
            int nWeekday = (int)dtDate.DayOfWeek;
            return nWeekday;
        }
        ///<summary>
        ///计算是该月第几周
        ///</summary>
        ///<param name="dtDate"></param>
        ///<returns></returns>
        public int returnweekNum(DateTime dtDate)
        {
            int weekNo = 0;
            int week = this.CaculateWeekDay(dtDate);
            if (dtDate.Day > week)
            {
                if ((dtDate.Day - week) % 7 == 0)
                {
                    weekNo = (dtDate.Day - week) / 7 + 1;
                }
                else
                {
                    weekNo = (dtDate.Day - week) / 7 + 2;
                }
            }
            else
            {
                weekNo = 1;
            }
            return weekNo;
        }
        /// <summary>
        ///[Y*D+C]-L
        //公式解读：Y=年数后2位，D=0.2422，L=闰年数，
        //清明日期爲4月4日～6日之間。
        //举例说明：2088年清明日期=[88×.0.2422+4.81]-[88/4]=26-22=4，4月4日是清明。
        /// </summary>
        /// <param name="nYeah"></param>
        /// <returns></returns>
        public int QingMing(int nYeah)
        {
            double dCenturyBase = 0;//世纪基数21世纪C=4.81，20世纪=5.59。
            int nQingMingDay = 0;
            int nCentury = nYeah / 100;
            if (nCentury == 20)
            {
                dCenturyBase = 4.81;
            }
            else if (nCentury == 19)
            {
                dCenturyBase = 5.59;
            }
            nYeah = nYeah % 100;
            nQingMingDay = (int)(nYeah * 0.2422 + dCenturyBase) - (int)(nYeah / 4);
            return nQingMingDay;
        }
        #endregion
    }
}
