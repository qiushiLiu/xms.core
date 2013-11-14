using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;

namespace XMS.Core
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue
       , Inherited = true, AllowMultiple = true)]
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class InvokeStatisticsAttribute : Attribute
    {

        public int AbnormalInvokeTimeLength
        {
            get;
            set;
        }
        public int[] InvokeThresholds
        {
            get;
            set;
        }
        
    }
    public enum EnumInvokeType
    {
        Interface, Method, Cache
    }
    /// <summary>
    /// 关于调用时长的统计类，nAbnormalInvokeTimeLength，异常调用的时长，可为空，将使用系统默认值，目前为10MS，超过该时长，将打印log，统计用的默认门限值为10，50，100，200,使用本类请使用Using,或者在使用结束后调用Dispose接口
    /// </summary>
    public sealed class InvokeStatistics:IDisposable
    {

        private int index = 0;
        public int Index
        {
            get
            {
                return index;
            }
        }
        private long[] rslt = new long[20];
        public long[] Rslt
        {
            get
            {
                return rslt;
            }
        }
        private string[] stepName = new string[20];
        public string[] StepName
        {
            get
            {
                return stepName;
            }
        }
       
        internal DateTime InvokeTime;
        private string _Key;
        public string Key
        {
            get
            {
                return _Key;
            }
        }
        private long Start;
        private int? _AbnormalInvokeTimeLength;
        private int AbnormalInvokeTimeLength
        {
            get
            {
                if (_AbnormalInvokeTimeLength.HasValue)
                    return _AbnormalInvokeTimeLength.Value;
                switch(InvokeType)
                {
                    case EnumInvokeType.Interface:
                        return StatisticsTool.Instance.AbnormalInvokeTimeLength_Interface;
                    case EnumInvokeType.Method:
                        return StatisticsTool.Instance.AbnormalInvokeTimeLength_Method;
                    case EnumInvokeType.Cache:
                        return StatisticsTool.Instance.AbnormalInvokeTimeLength_Cache;
                    default:
                        return 100;
                        
                }
              
            }
        }
        private string InvokeParas;
        public EnumInvokeType InvokeType
        {
            get
            {
                return _InvokeType;
            }
        }
        private EnumInvokeType _InvokeType;
        private int[] _Thresholds = null;
        public int[] Thresholds
        {
            get
            {
                if (_Thresholds != null)
                    return _Thresholds;
                switch (InvokeType)
                {
                    case EnumInvokeType.Method:
                        _Thresholds=Container.ConfigService.GetAppSetting<int[]>("StatisticTimeThresholds_InnerMethod", new int[] { 10, 50, 100, 200 });
                        break;
                    case EnumInvokeType.Interface:
                        _Thresholds= Container.ConfigService.GetAppSetting<int[]>("StatisticTimeThresholds_Interface", new int[] {10, 100, 500, 1000,2000 });
                        break;
                    case EnumInvokeType.Cache:
                        _Thresholds= Container.ConfigService.GetAppSetting<int[]>("StatisticTimeThresholds_Cache", new int[] { 5, 10, 20, 100});
                        break;
                           
                }
                return _Thresholds;
                
            }
        }
        private ParameterInfo[] ParaInfo;
        private object[] ParaValue;
      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sKey">统计的主键，一般取方法名</param>
        /// <param name="sInvokeParas">调用的参数</param>
        public InvokeStatistics(string sKey, string sInvokeParas)
        {
            Ini(sKey, sInvokeParas,null,null,EnumInvokeType.Method,null,null);
        }

       
        /// <summary>
        ///
        /// </summary>
        /// <param name="sKey">统计的主键，一般取方法名</param>
        /// <param name="sInvokeParas">调用的参数</param>
        /// <param name="nAbnormalInvokeTimeLength">异常调用的时长，超过该时长，将打印log</param>
        /// <param name="aThreshold">统计用的门限值，必须升序排列</param>

        public InvokeStatistics(string sKey, string sInvokeParas, int? nAbnormalInvokeTimeLength, int[] aThreshold)
        {
            Ini(sKey, sInvokeParas, nAbnormalInvokeTimeLength, aThreshold, EnumInvokeType.Method,null,null);          
        }

          /// <summary>
          /// XMS.Core cache专用
          /// </summary>
          /// <param name="sKey"></param>
          /// <param name="sInvokeParas"></param>
          /// <param name="NotNeedPara">没用的参数，主要为了跟外部的构造函数区别，写的elegant太麻烦了</param>
        internal InvokeStatistics(string sKey, string sInvokeParas,int NotNeedPara)
        {
            Ini(sKey, sInvokeParas, null, null, EnumInvokeType.Cache,null,null);
        }


        /// <summary>
        /// 接口专用
        /// </summary>
        /// <param name="sKey"></param>
        /// <param name="nAbnormalInvokeTimeLength"></param>
        /// <param name="aThreshold"></param>
        /// <param name="parameters"></param>
        /// <param name="inputs"></param>
        internal InvokeStatistics(string sKey,  int? nAbnormalInvokeTimeLength, int[] aThreshold, ParameterInfo[] parameters, object[] inputs)
        {

            Ini(sKey, null, nAbnormalInvokeTimeLength, aThreshold,EnumInvokeType.Interface, parameters, inputs);         

        }
        private void Ini(string sKey, string sInvokeParas, int? nAbnormalInvokeTimeLength, int[] aThreshold, EnumInvokeType eInVokeType, ParameterInfo[] parameters, object[] inputs)
        {
            if (String.IsNullOrWhiteSpace(sKey))
            {
                if (eInVokeType != EnumInvokeType.Interface)
                    sKey = "UnknownMethod";
                else
                    sKey = "UnknownInterface";
            }
            _Key = eInVokeType.ToString() + ":" + sKey;
            _AbnormalInvokeTimeLength = nAbnormalInvokeTimeLength;
            InvokeParas = sInvokeParas;
            _Thresholds = aThreshold;
            this.InvokeTime = DateTime.Now;
            _InvokeType = eInVokeType;
            ParaValue = inputs;
            ParaInfo = parameters;
            Start = StatisticsTool.Instance.objWatch.ElapsedTicks;
        }
        private void PrintInvokeInfo( )
        {
            int nElapsdMS=StatisticsTool.Instance.ConvertTick2MS(this.ElapsedTicks);
            if (nElapsdMS < AbnormalInvokeTimeLength)
            {
                return;
            }

            string sTmp = "Invoke_" + this.Key + "=" + nElapsdMS + "\r\n";
            
            if (InvokeType==EnumInvokeType.Interface)
            {
                this.InvokeParas = XMSTools.GetParaString(this.ParaInfo, this.ParaValue);
            }
          
            sTmp+="InvokeParas:"+this.InvokeParas+"\r\n";
            for(int i=0;i<index;i++)
            {
            
                sTmp += "\t" + stepName[i] + "=" + StatisticsTool.Instance.ConvertTick2MS(rslt[i]);
            
            }
            Logging.ILogger objLogger = Container.LogService.GetLogger("InvokeAbnormal");
            objLogger.Info(sTmp, "InvokeAbnormal");
          
        }

        public long ElapsedTicks;
       
        private void EndInvoke()
        {
            try
            {
                ElapsedTicks =StatisticsTool.Instance.objWatch.ElapsedTicks - this.Start;
                PrintInvokeInfo();
                //一定要释放掉其他资源
                this.InvokeParas = null;
                this.ParaValue = null;
                this.ParaInfo = null;
                StatisticsTool.Instance.AddInvokeInfo(this);
               
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
            }
            
        }

        /// <summary>
        /// 建立统计单步的对象
        /// </summary>
        /// <param name="sStepName">方法内每个单独的步骤名字</param>
        /// <returns></returns>
        public InvokeStep CreateInvokeStep(string sStepName)
        {
            if(String.IsNullOrWhiteSpace(sStepName))
            {
                sStepName = "UnknownStep";
            }
            
            InvokeStepInner obj=new InvokeStepInner(rslt,index);
            
            stepName[index] = sStepName;
            if (index < 18)
            {
                index++;
            }
            return obj;
        }

        private class InvokeStepInner : InvokeStep
        {

            public InvokeStepInner(long [] aRslt,int nIndex)
                : base(aRslt, nIndex)
            {

            }
        }

        #region IDisposable interface
		private bool disposed = false;

		/// <summary>
		/// 释放资源。
		/// </summary>
		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					EndInvoke();
				}
			}
			this.disposed = true;
		}

		/// <summary>
		/// 析构函数
		/// </summary>
		~InvokeStatistics()
		{
			Dispose(false);
		}
		#endregion
    }
    /// <summary>
    /// 使用本类，请使用using，或者在使用结束后调用Dispose接口，才能保证统计的准确性
    /// </summary>
    public abstract class InvokeStep:IDisposable
    {
        public InvokeStep(long [] aRslt,int nIndex)
        {
            index = nIndex;
            rslt = aRslt;
            rslt[nIndex] =StatisticsTool.Instance.objWatch.ElapsedTicks;
        }
     
        private long [] rslt;
        private int index;
        
         #region IDisposable interface
        
		private bool disposed = false;

        private void EndStep()
        {
            try
            {
                rslt[index] = StatisticsTool.Instance.objWatch.ElapsedTicks - rslt[index];
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
            }

        }

		/// <summary>
		/// 释放资源。
		/// </summary>
		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
                    EndStep();
				}
			}
			this.disposed = true;
		}

		    /// <summary>
		    /// 析构函数
		/// </summary>
        ~InvokeStep()
		{
			Dispose(false);
		}
		   #endregion
    }
    
    internal class StatisticsTool
    {

        public int ConvertTick2MS(long lTicks)
        {
            return (int)(lTicks*1000 /Stopwatch.Frequency);
        }
        public static StatisticsTool Instance = new StatisticsTool();
        private object objLock = new object();
        private StatisticsTool()
        {
            objWatch.Start();
        }
        private Dictionary<string, List<InvokeStatistics>> _dic = new Dictionary<string, List<InvokeStatistics>>(100);
        public Stopwatch objWatch = new Stopwatch();
        public void AddInvokeInfo(InvokeStatistics objInvoke)
        {
            if (objInvoke == null || String.IsNullOrWhiteSpace(objInvoke.Key))
                return;
            if (!IsWriteStatistic)
                return;
           
            lock (objLock)
            {
                if (!_dic.ContainsKey(objInvoke.Key))
                {
                    _dic[objInvoke.Key] = new List<InvokeStatistics>(StatisticIntervalCount);
                }
                _dic[objInvoke.Key].Add(objInvoke);
                if (_dic[objInvoke.Key].Count == StatisticIntervalCount)
                {
                    Task.Factory.StartNew(this.CalculateStatisticAndWriteLog, _dic[objInvoke.Key]);
                    _dic[objInvoke.Key] = new List<InvokeStatistics>(StatisticIntervalCount);
                }
            }

        }

        private class Statistic
        {
            private Dictionary<int, int> dicCount = new Dictionary<int, int>();
            private int Count = 0;
            private long Total = 0;
            private int[] Threshold;
            private string Name;
            public Statistic(int[] aThreshold,string sName)
            {
                Threshold = aThreshold;
                ///先把输出结果的key生成好，省得打印出来乱序           
               
                Name = sName;
            }
            
            public string GetStatisticString()
            {
                string sTmp = this.Name + "\r\n";
                if (this.Count <= 0)
                    return sTmp;
                sTmp += "Count=" + this.Count;
                sTmp += "\tAvrg=" +((double) StatisticsTool.Instance.ConvertTick2MS(this.Total) / this.Count).ToString("#.###");
                for (int z = 0; z <= Threshold.Length; z++)
                {
                    if (dicCount.ContainsKey(z))
                    {
                        sTmp += "\t" + GetStaKey(z) + "=" + dicCount[z];
                    }
                }
                sTmp += "\r\n";
                return sTmp;
            }
            public void CalculateStatic(long lElapseTicks)
            {
                this.Count++;
                this.Total += lElapseTicks;
                string s = String.Empty;
               
                int nIndex=-1;
                for (int z = 0; z < Threshold.Length; z++)
                {
                    if (StatisticsTool.Instance.ConvertTick2MS(lElapseTicks) <= Threshold[z])
                    {
                        nIndex=z;
                        break;
                    }
                }
                //没有找到
                if (nIndex<0)
                {
                    nIndex = Threshold.Length;
                }
                if (dicCount.ContainsKey(nIndex))
                    dicCount[nIndex]++;
                else
                {
                    dicCount[nIndex] = 1;
                }

            }
            private  string GetStaKey(int z)
            {
                string s = "";
                if (z == 0)
                {
                    s = "(0," + Threshold[z] + "]";

                }
                else
                {
                    if (z == Threshold.Length)
                        s = ">" + Threshold[z - 1];
                    else
                        s = "(" + Threshold[z - 1] + "," + Threshold[z] + "]";
                }

                return s;
            }
           
        }

   

      

     
        public void CalculateStatisticAndWriteLog(object objIn)
        {
            try
            {
                List<InvokeStatistics> lstInvoke = objIn as List<InvokeStatistics>;
                if (lstInvoke == null)
                    return;
                int[] aThreshold = lstInvoke[0].Thresholds;
                aThreshold = aThreshold.OrderBy(p => p).ToArray();
                Statistic objSta = new Statistic(aThreshold, lstInvoke[0].Key);
                Dictionary<string, Statistic> dicStep = new Dictionary<string, Statistic>();

                for (int i = 0; i < lstInvoke.Count; i++)
                {
                    InvokeStatistics objInvoke = lstInvoke[i];
                    objSta.CalculateStatic(objInvoke.ElapsedTicks);
                    for (int j = 0; j < objInvoke.Index; j++)
                    {
                       
                        if (!dicStep.ContainsKey(objInvoke.StepName[j]))
                        {
                            dicStep[objInvoke.StepName[j]] = new Statistic(aThreshold, "Step:" + objInvoke.StepName[j]);
                        }
                        dicStep[objInvoke.StepName[j]].CalculateStatic((int)objInvoke.Rslt[j]);
                       
                    }

                }
                string sTmp = "###############################################################################################\r\n";
                sTmp += objSta.GetStatisticString();
                foreach (string s in dicStep.Keys)
                {
                    sTmp += dicStep[s].GetStatisticString();
                }
                Container.LogService.GetLogger(lstInvoke[0].InvokeType.ToString() + "Statistics").Info(sTmp, lstInvoke[0].InvokeType.ToString() + "Statistics");
              //  Container.LogService.Info(sTmp, lstInvoke[0].InvokeType.ToString() + "Statistics");
            }
            catch (System.Exception e)
            {
                Container.LogService.Error(e);
            }
        }


        private int _StatisticIntervalCount = 1000;

        private DateTime tLastChangeTime_IsWriteStatistic = System.DateTime.MinValue;
        private DateTime tLastChangeTime_StatisticIntervalCount = System.DateTime.MinValue;
        private DateTime tLastChangeTime_AbnormalInvokeTimeLength_Interface = System.DateTime.MinValue;
        private DateTime tLastChangeTime_AbnormalInvokeTimeLength_Cache = System.DateTime.MinValue;
        private DateTime tLastChangeTime_AbnormalInvokeTimeLength_Method = System.DateTime.MinValue;
    
        private int StatisticIntervalCount
        {
            get
            {
                if (tLastChangeTime_StatisticIntervalCount.AddMinutes(1) < DateTime.Now)
                {
                    _StatisticIntervalCount = Container.ConfigService.GetAppSetting("StatisticIntervalCount", 5000);
                    tLastChangeTime_StatisticIntervalCount = DateTime.Now;
                    _StatisticIntervalCount = Math.Max(100, _StatisticIntervalCount);
                    _StatisticIntervalCount = Math.Min(10000, _StatisticIntervalCount);
                }
                return _StatisticIntervalCount;
              
            }
        }
        private bool _isWriteStatistic = true;
        private bool IsWriteStatistic
        {
            get
            {
                if (tLastChangeTime_IsWriteStatistic.AddMinutes(1) < DateTime.Now)
                {
                    _isWriteStatistic = Container.ConfigService.GetAppSetting("IsWriteStatistic", true);
                    tLastChangeTime_IsWriteStatistic = DateTime.Now;
                }
                return _isWriteStatistic;
            }
        }
        private int _AbnormalInvokeTimeLength_Interface = 1000;
        public int AbnormalInvokeTimeLength_Interface
        {
             get
            {
                if (tLastChangeTime_AbnormalInvokeTimeLength_Interface.AddMinutes(1) < DateTime.Now)
                {
                    _AbnormalInvokeTimeLength_Interface = Container.ConfigService.GetAppSetting<int>("AbnormalInvokeTimeLength_Interface", 1000);
                    tLastChangeTime_AbnormalInvokeTimeLength_Interface = DateTime.Now;
                }
                return _AbnormalInvokeTimeLength_Interface;
            }
          
        }
        private int _AbnormalInvokeTimeLength_Cache = 100;
        public int AbnormalInvokeTimeLength_Cache
        {
            get
            {
                if (tLastChangeTime_AbnormalInvokeTimeLength_Cache.AddMinutes(1) < DateTime.Now)
                {
                    _AbnormalInvokeTimeLength_Cache = Container.ConfigService.GetAppSetting<int>("AbnormalInvokeTimeLength_Cache", 30);
                    tLastChangeTime_AbnormalInvokeTimeLength_Cache = DateTime.Now;
                }
                return _AbnormalInvokeTimeLength_Cache;
            }

        }
        private int _AbnormalInvokeTimeLength_Method = 100;
        public int AbnormalInvokeTimeLength_Method
        {
            get
            {
                if (tLastChangeTime_AbnormalInvokeTimeLength_Method.AddMinutes(1) < DateTime.Now)
                {
                    _AbnormalInvokeTimeLength_Method = Container.ConfigService.GetAppSetting<int>("AbnormalInvokeTimeLength_Method", 100);
                    tLastChangeTime_AbnormalInvokeTimeLength_Method = DateTime.Now;
                }
                return _AbnormalInvokeTimeLength_Method;
            }

        }
        

       

    }
}
