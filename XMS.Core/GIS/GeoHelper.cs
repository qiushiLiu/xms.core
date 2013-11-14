using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.GIS
{
	/// <summary>
	/// 位置类
	/// </summary>
	public class LatLon
	{
		/// <summary>
		/// 赤道半径 earth radius
		/// </summary>
		public const double EARTH_RADIUS = 6378137;

		/// <summary>
		/// 极半径 polar radius
		/// </summary>
		public const double POLAR_RADIUS = 6356725;

		/// <summary>
		/// 
		/// </summary>
		public LatLon()
		{ }

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="lat">纬度</param>
		/// <param name="lon">经度</param>
		public LatLon(double lat, double lon)
		{
			this.Lat = lat;
			this.Lon = lon;
		}

		/// <summary>
		/// 纬度
		/// </summary>
		public double Lat { get; set; }

		/// <summary>
		/// 经度
		/// </summary>
		public double Lon { get; set; }

		/// <summary>
		/// 纬度的弧度
		/// </summary>
		public double RadLat { get { return Lat * Math.PI / 180; } }

		/// <summary>
		/// 经度的弧度
		/// </summary>
		public double RadLon { get { return Lon * Math.PI / 180; } }

		/// <summary>
		/// ?
		/// </summary>
		public double Ec { get { return POLAR_RADIUS + (EARTH_RADIUS - POLAR_RADIUS) * (90 - Lat) / 90; } }

		/// <summary>
		/// ?
		/// </summary>
		public double Ed { get { return Ec * Math.Cos(RadLat); } }
	}

	/// <summary>
	/// Geo辅助类
	/// </summary>
	public static class GeoHelper
	{
		public static string FormatDistance(double distance)
		{
			int nDistanct = (int)(distance * 1000) / 10 * 10;
			if (nDistanct <= 0)
				return String.Empty;
			else if (nDistanct < 100)
				return "<100米";
			else if (nDistanct > 50000)
				return ">50公里";
			else
				return (nDistanct > 1000) ? (nDistanct / 1000.0).ToString("f1") + "公里" : nDistanct + "米";
		}

		/// <summary>
		/// 根据两点的经纬度计算两点距离
		/// 可参考:通过经纬度计算距离的公式 http://www.storyday.com/html/y2009/2212_according-to-latitude-and-longitude-distance-calculation-formula.html
		/// </summary>
		/// <param name="src">A点维度</param>        
		/// <param name="dest">B点经度</param>
		/// <returns></returns>
		public static double GetDistance(LatLon src, LatLon dest)
		{
			if (Math.Abs(src.Lat) > 90 || Math.Abs(dest.Lat) > 90 || Math.Abs(src.Lon) > 180 || Math.Abs(dest.Lon) > 180)
				throw new ArgumentException("经纬度信息不正确！");

			double latDis = src.RadLat - dest.RadLat;
			double lonDis = src.RadLon - dest.RadLon;

			double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(latDis / 2), 2) + Math.Cos(src.RadLat) * Math.Cos(dest.RadLat) * Math.Pow(Math.Sin(lonDis / 2), 2)));

			s = s * LatLon.EARTH_RADIUS / 1000;
			s = Math.Round(s * 10000) / 10000;

			return s;
		}

		/// <summary>
		/// 根据两点的经纬度计算两点距离
		/// 可参考:通过经纬度计算距离的公式 http://www.storyday.com/html/y2009/2212_according-to-latitude-and-longitude-distance-calculation-formula.html
		/// </summary>
		/// <param name="lat1">A点维度</param>
		/// <param name="lon1">A点经度</param>
		/// <param name="lat2">B点维度</param>
		/// <param name="lon2">B点经度</param>
		/// <returns></returns>
		public static double GetDistance(double lat1, double lon1, double lat2, double lon2)
		{
			LatLon src = new LatLon(lat1, lon1);
			LatLon dest = new LatLon(lat2, lon2);
			return GetDistance(src, dest);
		}


		/// <summary>
		/// 已知点A经纬度，根据B点据A点的距离，和方位，求B点的经纬度
		/// </summary>
		/// <param name="a">已知点A</param>
		/// <param name="distance">B点到A点的距离 </param>
		/// <param name="angle">B点相对于A点的方位，12点钟方向为零度，角度顺时针增加</param>
		/// <returns>B点的经纬度坐标</returns>
		public static LatLon GetLatLon(LatLon a, double distance, double angle)
		{
			//if (distance > 0)
			//{
			//    distance *= Math.Sqrt(2);
			//}
			if (!IsLongLatValid(a))
				return null;
			double dx = distance * 1000 * Math.Sin(angle * Math.PI / 180);
			double dy = distance * 1000 * Math.Cos(angle * Math.PI / 180);

			double lon = (dx / a.Ed + a.RadLon) * 180 / Math.PI;
			double lat = (dy / a.Ec + a.RadLat) * 180 / Math.PI;

			LatLon b = new LatLon(lat, lon);
			return b;
		}

		public static LatLon GetLeftUpperCornerLatLon(double dLongitude, double dLatitude, double dDistanceInKilometer)
		{
			LatLon a = new LatLon(dLatitude, dLongitude);
			return GetLatLon(a, GetRealDistance(dDistanceInKilometer), 315);
		}
		public static LatLon GetRightLowerCornerLatLon(double dLongitude, double dLatitude, double dDistanceInKilometer)
		{
			LatLon a = new LatLon(dLatitude, dLongitude);
			return GetLatLon(a, GetRealDistance(dDistanceInKilometer), 135);
		}
		public static LatLon GetLeftUpperCornerLatLon(LatLon a, double dDistanceInKilometer)
		{
			return GetLatLon(a, GetRealDistance(dDistanceInKilometer), 315);
		}
		public static LatLon GetRightLowerCornerLatLon(LatLon a, double dDistanceInKilometer)
		{
			return GetLatLon(a, GetRealDistance(dDistanceInKilometer), 135);
		}
		private static double GetRealDistance(double dDistanceInKilometer)
		{
			return Math.Sqrt(2 * dDistanceInKilometer * dDistanceInKilometer);
		}

		/// <summary>
		/// 已知点A经纬度，根据B点据A点的距离，和方位，求B点的经纬度
		/// </summary>
		/// <param name="longitude">已知点A经度</param>
		/// <param name="latitude">已知点A纬度</param>
		/// <param name="distance">B点到A点的距离</param>
		/// <param name="angle">B点相对于A点的方位，12点钟方向为零度，角度顺时针增加</param>
		/// <returns>B点的经纬度坐标</returns>
		public static LatLon GetLatLon(double longitude, double latitude, double distance, double angle)
		{
			LatLon a = new LatLon(latitude, longitude);
			return GetLatLon(a, distance, angle);
		}


		private static bool IsLongLatValid(LatLon objLatLon)
		{
			if (objLatLon.Lat > 90 || objLatLon.Lat < -90 || objLatLon.Lon > 180 || objLatLon.Lon < -180)
				return false;
			return true;
		}
		/// <summary>
		///  format latitude,longtitude to 4 fractional ditigal double;
		/// </summary>
		/// <param name="objLatLon"></param>
		/// <returns></returns>
		public static LatLon GetFormatedLatLon(LatLon objLatLon)
		{
			///按照经纬度查询
			if (objLatLon.Lat > 90 || objLatLon.Lat < -90 || objLatLon.Lon > 180 || objLatLon.Lon < -180)
			{
				throw new ArgumentException("经纬度超限");
			}

			LatLon objRsltLatLon = new LatLon();


			objRsltLatLon.Lat = Math.Round(objLatLon.Lat, 6);
			objRsltLatLon.Lon = Math.Round(objLatLon.Lon, 6);

			return objRsltLatLon;
		}
	}

	/// <summary>
	/// GeoHash辅助类，方便周边查询
	/// </summary>
	public class GeoHashHelper
	{
		private static readonly char[] Base32 = {
		                                        	'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		                                        	'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'm',
		                                        	'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
		                                        	'y', 'z'
		                                        };

		private static readonly Dictionary<char, int> Decodemap = new Dictionary<char, int>();

		private const int Precision = 12;
		private static readonly int[] Bits = { 16, 8, 4, 2, 1 };


		static GeoHashHelper()
		{
			int sz = Base32.Length;
			for (int i = 0; i < sz; i++)
			{
				Decodemap[Base32[i]] = i;
			}
		}

		public static String Encode(double latitude, double longitude)
		{
			double[] latInterval = { -90.0, 90.0 };
			double[] lonInterval = { -180.0, 180.0 };

			var geohash = new StringBuilder();
			bool isEven = true;
			int bit = 0, ch = 0;

			while (geohash.Length < Precision)
			{
				double mid;
				if (isEven)
				{
					mid = (lonInterval[0] + lonInterval[1]) / 2;
					if (longitude > mid)
					{
						ch |= Bits[bit];
						lonInterval[0] = mid;
					}
					else
					{
						lonInterval[1] = mid;
					}

				}
				else
				{
					mid = (latInterval[0] + latInterval[1]) / 2;
					if (latitude > mid)
					{
						ch |= Bits[bit];
						latInterval[0] = mid;
					}
					else
					{
						latInterval[1] = mid;
					}
				}

				isEven = isEven ? false : true;

				if (bit < 4)
				{
					bit++;
				}
				else
				{
					geohash.Append(Base32[ch]);
					bit = 0;
					ch = 0;
				}
			}

			return geohash.ToString();
		}

		public static double[] Decode(String geohash)
		{
			double[] ge = DecodeExactly(geohash);
			double lat = ge[0];
			double lon = ge[1];
			double latErr = ge[2];
			double lonErr = ge[3];

			double latPrecision = Math.Max(1, Math.Round(-Math.Log10(latErr))) - 1;
			double lonPrecision = Math.Max(1, Math.Round(-Math.Log10(lonErr))) - 1;

			lat = GetPrecision(lat, latPrecision);
			lon = GetPrecision(lon, lonPrecision);

			return new[] { lat, lon };
		}

		public static double[] DecodeExactly(String geohash)
		{
			double[] latInterval = { -90.0, 90.0 };
			double[] lonInterval = { -180.0, 180.0 };

			double latErr = 90.0;
			double lonErr = 180.0;
			bool isEven = true;
			int sz = geohash.Length;
			int bsz = Bits.Length;
			for (int i = 0; i < sz; i++)
			{

				int cd = Decodemap[geohash[i]];

				for (int z = 0; z < bsz; z++)
				{
					int mask = Bits[z];
					if (isEven)
					{
						lonErr /= 2;
						if ((cd & mask) != 0)
						{
							lonInterval[0] = (lonInterval[0] + lonInterval[1]) / 2;
						}
						else
						{
							lonInterval[1] = (lonInterval[0] + lonInterval[1]) / 2;
						}

					}
					else
					{
						latErr /= 2;

						if ((cd & mask) != 0)
						{
							latInterval[0] = (latInterval[0] + latInterval[1]) / 2;
						}
						else
						{
							latInterval[1] = (latInterval[0] + latInterval[1]) / 2;
						}
					}
					isEven = isEven ? false : true;
				}

			}
			double latitude = (latInterval[0] + latInterval[1]) / 2;
			double longitude = (lonInterval[0] + lonInterval[1]) / 2;

			return new[] { latitude, longitude, latErr, lonErr };
		}

		public static double GetPrecision(double x, double precision)
		{
			double @base = Math.Pow(10, -precision);
			double diff = x % @base;
			return x - diff;
		}
	}
}
