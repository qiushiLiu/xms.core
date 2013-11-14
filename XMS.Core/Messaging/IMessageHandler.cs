using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XMS.Core.Pipes;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 为消息处理程序定义一组统一的接口。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IMessageHandler<T>
	{
		/// <summary>
		/// 处理指定的消息并调用 context.Reply 通知消息代理服务器消息成功处理。
		///		注意：	1. context.Reply 的调用应该在事物提交前的最后一步执行，这样，可确保消息代理服务器能够同步确认消息成功处理。
		///				如果在提交事物之后调用 context.Reply，如果 context.Reply 过程中发生错误，消息代理服务器不一定能够同步确认消息成功，
		///				这样，消息代理服务器会任务此消息未成功处理，后面会再次发送消息给目标应用，从而造成消息的重复执行。
		///				2. 如果整个消息处理的过程中未抛出任何异常，那么消息总线系统会认为该消息已经成功处理，之后便会删除掉该消息；
		///				如果消息处理过程中发生错误，请抛出原始异常或者处理后的异常以通知消息代理服务器消息处理失败；
		/// </summary>
		/// <param name="context">消息上下文。</param>
		/// <param name="message">要处理的消息。</param>
		void Handle(IMessageContext context, T message);
	}

	
	//public class MyMessageHandler : IMessageHandler<MyMessage>
	//{
	//    public void Handle(IMessageContext context, MyMessage message)
	//    {
	//        IBusinessContext bussinessContext = null;

	//        IEntityContext entityContext = bussinessContext.CreateEntityContext();

	//        entityContext.BeginTransaction();

	//        try
	//        {
	//            // 业务处理



	//            // 在提交事物之前通知消息代理服务器消息已成功处理，并且只有当成功通知了消息代理服务器后，才提交事物，这可确保消息代理服务器能够同步确认消息成功处理。
	//            context.Reply();

	//            entityContext.Commit();
	//        }
	//        catch
	//        {
	//            entityContext.Rollback();

	//            throw;
	//        }
	//    }
	//}

}
