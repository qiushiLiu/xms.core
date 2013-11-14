using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XMS.Core.Members
{
	public partial class Member
	{
		/// <summary>
		/// 支持属性变化事件
		/// </summary>
		/// <param name="propertyName"></param>
		protected void RaisePropertyChanged(string propertyName)
		{
			switch (propertyName)
			{
				case "NickName":
				case "Email":
				case "Name":
				case "Sex":
				case "MobilePhone":
					this.displayName = null;
					this.anonymousName = null;
					this.honourName = null;
					break;
				default:
					break;
			}
			System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if ((propertyChanged != null))
			{
				propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
			}
		}

		private string displayName = null;
		private string anonymousName = null;
		/// <summary>
		/// 获取当前会员的显示名称。
		/// </summary>
		public string DisplayName
		{
			get
			{
				if(this.displayName == null)
				{
					this.displayName = GetDisplayNameInternal(false, this.NickName, this.Email, this.Name, this.Sex, this.MobilePhone);
				}
				return this.displayName;
			}
		}

		/// <summary>
		/// 获取会员的匿名显示名称。
		/// </summary>
		public string AnonymousName
		{
			get
			{
				if (this.anonymousName == null)
				{
					this.anonymousName = GetDisplayNameInternal(true, this.NickName, this.Email, this.Name, this.Sex, this.MobilePhone);
				}
				return this.anonymousName;
			}
		}

		private string honourName = null;
		/// <summary>
		/// 获取用户的尊称。
		/// </summary>
		public string HonourName
		{
			get
			{
				if (this.honourName == null)
				{
					this.honourName = GetHonourName(this.Name, this.Sex, this.NickName, this.MobilePhone, this.Email);
				}
				return this.honourName;
			}
		}

		#region 显示名、匿名、尊称、复姓公用方法
		/// <summary>
		/// 获取显示名，结合提供的昵称、邮箱、姓名、性别、手机号获取一个可用于在站点中向已登录用户显示的名称，如：X先生、X女士、admin@57.cn、13800138000等等。
		/// </summary>
		/// <param name="nickName"></param>
		/// <param name="email"></param>
		/// <param name="name"></param>
		/// <param name="sex"></param>
		/// <param name="mobilePhone"></param>
		/// <returns></returns>
		public static string GetDisplayName(string nickName, string email, string name, Sex sex, string mobilePhone)
		{
			return GetDisplayNameInternal(false, nickName, email, name, sex, mobilePhone);
		}

		/// <summary>
		/// 获取匿名名称，结合提供的昵称、邮箱、姓名、性别、手机号获取一个可用于在站点中向所有用户显示的匿名名称，如：X先生、X女士、admin@57.cn、138****5678等等。
		/// </summary>
		/// <param name="nickName"></param>
		/// <param name="email"></param>
		/// <param name="name"></param>
		/// <param name="sex"></param>
		/// <param name="mobilePhone"></param>
		public static string GetAnonymousName(string nickName, string email, string name, Sex sex, string mobilePhone)
		{
			return GetDisplayNameInternal(true, nickName, email, name, sex, mobilePhone);
		}

		private static string GetDisplayNameInternal(bool anonymous, string nickName, string email, string name, Sex sex, string mobilePhone)
		{
			if (!String.IsNullOrEmpty(nickName))
			{
				return nickName;
			}

			if (!String.IsNullOrEmpty(email))
			{
				int index = email.IndexOf("@");
				if (index > 0)
				{
					return email.Substring(0, index);
				}
			}

			if (!String.IsNullOrEmpty(name))
			{
				return GetLastName(name) + (sex == Members.Sex.Female ? "女士" : "先生");
			}

			if (!String.IsNullOrEmpty(mobilePhone) && mobilePhone.Length == 11)
			{
				if (anonymous)
				{
					return mobilePhone.Substring(0, 3) + "****" + mobilePhone.Substring(7);
				}
				else
				{
					return mobilePhone;
				}
			}
			return "游客";
		}

		/// <summary>
		/// 获取尊称，结合提供的姓名、性别、昵称、手机号、邮箱获取用户的尊称，适用于向用户发送短信、邮件等场景。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sex"></param>
		/// <param name="nickName"></param>
		/// <param name="mobilePhone"></param>
		/// <param name="email"></param>
		/// <returns></returns>
		public static string GetHonourName(string name, Sex sex, string nickName, string mobilePhone, string email)
		{
			if (!String.IsNullOrEmpty(name))
			{
				return name + (sex == Sex.Female ? "女士" : "先生");
			}
			if (!String.IsNullOrEmpty(nickName))
			{
				return nickName;
			}
			if (!String.IsNullOrEmpty(email))
			{
				int index = email.IndexOf("@");
				if (index > 0)
				{
					return email.Substring(0, index);
				}
			}
			//if (!String.IsNullOrEmpty(mobilePhone) && mobilePhone.Length == 11)
			//{
			//    return mobilePhone;
			//}
			return "亲";
		}

		private static Regex regexName = new Regex("^[\u4E00-\u9FA5]+$", RegexOptions.Compiled);

		private static readonly HashSet<string> defaultHyphenatedNames = new HashSet<string>(
			new string[]{
			"司马","欧阳","司徒","上官","诸葛","慕容","皇甫","公孙","重光","德宫","纳兰","夏侯","令狐","尉迟","长孙","宇文"
		});
		
		/// <summary>
		/// 获取姓名中的姓。
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string GetLastName(string name)
		{
			if (String.IsNullOrEmpty(name))
			{
				return name;
			}

			HashSet<string> hyphenatedNames = XMS.Core.Container.ConfigService.GetAppSetting<string>("Member_HyphenatedNames", defaultHyphenatedNames);

			string lastName = null;

			if (name.Length > 2)
			{
				lastName = name.Substring(0, 2);
				if (hyphenatedNames.Contains(lastName))
				{
					return lastName;
				}
			}

			if (name.Length > 3)
			{
				lastName = name.Substring(0, 3);
				if (hyphenatedNames.Contains(lastName))
				{
					return lastName;
				}
			}

			if (regexName.IsMatch(name))
			{
				return name[0].ToString();
			}

			return name;
		}
		#endregion
	}
}