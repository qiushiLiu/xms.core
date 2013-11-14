using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace XMS.Core.Messaging.ServiceModel
{
	[Serializable]
	[DataContract(Name = "Message", Namespace = "http://schemas.datacontract.org/2004/07/XMS.Core.Messaging.ServiceModel")]
	public class Message : IMessage
	{
		[System.NonSerializedAttribute()]
		private System.Runtime.Serialization.ExtensionDataObject extensionDataField;

		[System.Runtime.Serialization.OptionalFieldAttribute()]
		private Guid IdField;

		[System.Runtime.Serialization.OptionalFieldAttribute()]
		private Guid TypeIdField;

		[System.Runtime.Serialization.OptionalFieldAttribute()]
		private string SourceAppNameField;

		[System.Runtime.Serialization.OptionalFieldAttribute()]
		private string SourceAppVersionField;

		[System.Runtime.Serialization.OptionalFieldAttribute()]
		private string BodyField;

		[System.Runtime.Serialization.OptionalFieldAttribute()]
		private DateTime CreateTimeField;

		[global::System.ComponentModel.BrowsableAttribute(false)]
		public System.Runtime.Serialization.ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionDataField;
			}
			set
			{
				this.extensionDataField = value;
			}
		}

        /// <summary>
        /// 主键
        /// </summary>
		[DataMember]
		public Guid Id
		{
			get
			{
				return this.IdField;
			}
			set
			{
				if ((this.IdField.Equals(value) != true))
				{
					this.IdField = value;
					this.RaisePropertyChanged("Id");
				}
			}
		}

        /// <summary>
        /// 消息类型编号
        /// </summary>
        [DataMember]
		public Guid TypeId
        {
			get
			{
				return this.TypeIdField;
			}
			set
			{
				if ((this.TypeIdField.Equals(value) != true))
				{
					this.TypeIdField = value;
					this.RaisePropertyChanged("TypeId");
				}
			}
		}

		/// <summary>
        /// 消息发送方 AppName
        /// </summary>
        [DataMember]
		public string SourceAppName
        {
			get
			{
				return this.SourceAppNameField;
			}
			set
			{
				if ((object.ReferenceEquals(this.SourceAppNameField, value) != true))
				{
					this.SourceAppNameField = value;
					this.RaisePropertyChanged("SourceAppName");
				}
			}
		}

        /// <summary>
        /// 消息发送方 AppVersion
        /// </summary>
        [DataMember]
		public string SourceAppVersion
        {
			get
			{
				return this.SourceAppVersionField;
			}
			set
			{
				if ((object.ReferenceEquals(this.SourceAppVersionField, value) != true))
				{
					this.SourceAppVersionField = value;
					this.RaisePropertyChanged("SourceAppVersion");
				}
			}
        }

        /// <summary>
        /// 消息体
        /// </summary>
        [DataMember]
		public string Body
        {
			get
			{
				return this.BodyField;
			}
			set
			{
				if ((object.ReferenceEquals(this.BodyField, value) != true))
				{
					this.BodyField = value;
					this.RaisePropertyChanged("Body");
				}
			}
		}

        /// <summary>
        /// 消息创建时间
        /// </summary>
        [DataMember]
        [Required]
		public DateTime CreateTime
        {
			get
			{
				return this.CreateTimeField;
			}
			set
			{
				if ((object.ReferenceEquals(this.CreateTimeField, value) != true))
				{
					this.CreateTimeField = value;
					this.RaisePropertyChanged("CreateTime");
				}
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string propertyName)
		{
			System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if ((propertyChanged != null))
			{
				propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
			}
		}
	}
}