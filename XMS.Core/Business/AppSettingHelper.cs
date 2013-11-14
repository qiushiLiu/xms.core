using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Business
{
    public class AppSettingHelper
    {
        #region All Prompt
        public static string sPromptForSupportedImageFormat
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForSupportedImageFormat", "上传图片文件格式不正确，目前我们只支持gif,bmp,jpg(jpeg),png四种图片文件格式");
            }
        }
        public static string sPromptForCheckCodeWrong
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForCheckCodeWrong", "验证码错误，请重新输入！");
            }
        }

        public static string sPromptForLoginPasswordWrong
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForLoginPasswordWrong", "用户名或密码错误，请重新输入，注意大小写！");
            }
        }
        public static string sPromptForTokenExpire
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForTokenExpire", "用户未登陆，或者登陆已过期！");
            }
        }
        public static string sPromptForUnknownExeption
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForUnknownExeption", "系统繁忙，请稍候再试！");
            }
        }
        public static string sPromptForPasswordCannotBeNull
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForPasswordCannotBeNull", "密码不允许为空");
            }
        }
        public static string sPromptForEmalIllegal
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForEmailIllegal", "Email格式有误");
            }
        }
        public static string sPromptForMobileIllegal
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForMobileIllegal", "不是合法的手机号");
            }
        }
        public static string sPromptForMemberNotExist
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("PromptForMemberNotExist", "会员不存在!");
            }
        }
        #endregion

        #region DES Encoder Key
        public static string sEncoderKey
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("EncoderKey", "850705t7e5l7e7");
            }
        }
        #endregion

        #region Web Root
        /// <summary>
        /// 图片站点Url
        /// </summary>
        internal static string sStaticUploadUrl
        {
            get
            {
                return Container.ConfigService.GetAppSetting<string>("StaticUploadUrl", "http://upload{0}.95171.cn/");
            }
        }
        #endregion
    }
}
