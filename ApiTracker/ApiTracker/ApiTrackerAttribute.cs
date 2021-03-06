﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ApiTracker
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ApiTrackerAttribute : ActionFilterAttribute
    {
        private ElasticClient elastic;

        private readonly string Key = "_ApiTracker_";

        /// <summary>
        /// ApiTracker
        /// 写入使用异步Task，无论成功失败，1秒后自动取消操作
        /// </summary>
        /// <param name="IndexName">索引名称，默认为apitracker</param>
        /// <param name="timeout">写入动作超时阈值，单位为毫秒</param>
        public ApiTrackerAttribute(string IndexName = "apitracker",int timeout = 500)
        {
            elastic = new ElasticClient(IndexName.Trim().ToLower(), timeout);
        }

        #region Action执行前
        public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var log = new ApiTrackerEntity()
            {
                StartTime = DateTime.UtcNow,
                Action = actionContext.ActionDescriptor.ActionName,
                Controller = actionContext.ActionDescriptor.ControllerDescriptor.ControllerName,
                Method = actionContext.Request.Method.Method,
                Params = string.Empty,
            };

            var options = new ApiTrackerOptions();

            var OptionsFromController = actionContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<ApiTrackerOptions>();

            if (OptionsFromController != null && OptionsFromController.Count > 0)
            {
                options = OptionsFromController[0];
            }

            var OptionsFromAction = actionContext.ActionDescriptor.GetCustomAttributes<ApiTrackerOptions>();

            if (OptionsFromAction != null && OptionsFromAction.Count > 0)
            {
                options = OptionsFromAction[0];
            }

            if (options.EnableHeaders) {
                log.Headers = actionContext.Request.Headers.ToString();
            }

            if (options.EnableClientIp){
                log.ClientIp = ClientIP();
            }

            if (options.EnableParams)
            {
                #region Request Url Params 
                if (actionContext.ActionArguments != null &&
                    actionContext.ActionArguments.Keys.Count > 0)
                {
                    log.Params = JsonConvert.SerializeObject(actionContext.ActionArguments);
                }
                #endregion
                #region Request Body Params 
                var stream = actionContext.Request.Content.ReadAsStreamAsync().Result;

                if (stream.Length > 0 && stream.CanSeek)
                {
                    var requestData = string.Empty;

                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);

                        requestData = Encoding.UTF8.GetString(ms.ToArray());
                    }

                    if (!string.IsNullOrEmpty(log.Params))
                    {
                        log.Params += "&";
                    }

                    log.Params += "StreamEntity=" + requestData;

                    stream.Seek(0, SeekOrigin.Begin);
                }
                #endregion
            }

            //var api = actionContext.ControllerContext.Controller as BasicAPI;

            //if (api.Caller != null)
            //{
            //    MonLog.CallerName = api.Caller.UserName;
            //    MonLog.CallerID = api.Caller.ID;
            //}

            actionContext.Request.Properties[Key] = log;

            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }
        #endregion

        #region Action执行完成后
        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            var log = actionExecutedContext.Request.Properties[Key] as ApiTrackerEntity;

            log.EndTime = DateTime.UtcNow;

            log.UsedSeconds = (log.EndTime - log.StartTime).TotalSeconds;

            if (actionExecutedContext.Exception != null)
            {
                log.Error = actionExecutedContext.Exception.Message + actionExecutedContext.Exception.StackTrace;
            }

            elastic.Index(JsonConvert.SerializeObject(log));

            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
        #endregion

        #region 获取ClientIP
        private string ClientIP()
        {
            var variables = HttpContext.Current.Request.ServerVariables;

            var result = string.Empty;

            if (!string.IsNullOrEmpty(variables["HTTP_VIA"]))
            {
                result = Convert.ToString(variables["HTTP_X_FORWARDED_FOR"]);
            }

            if (string.IsNullOrEmpty(result))
            {
                result = Convert.ToString(variables["REMOTE_ADDR"]);
            }

            if (string.IsNullOrEmpty(result))
            {
                result = HttpContext.Current.Request.UserHostAddress;
            }

            return result;
        }
        #endregion
    }

    /// <summary>
    /// API日志记录配置项
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ApiTrackerOptions : Attribute
    {
        public bool EnableHeaders { get; set; }

        public bool EnableClientIp { get; set; }

        public bool EnableParams { get; set; }

        public ApiTrackerOptions():this(true,true,true) {

        }

        public ApiTrackerOptions(bool EnableHeaders, bool EnableClientIp, bool EnableParams) {
            this.EnableHeaders = EnableHeaders;
            this.EnableClientIp = EnableClientIp;
            this.EnableParams = EnableParams;
        }
    }
}
