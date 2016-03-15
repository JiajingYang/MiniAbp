﻿using System;
using MiniAbp.Auditing;
using MiniAbp.Dependency;
using MiniAbp.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Yooya.Bpm.Framework.Route;

namespace MiniAbp.Route
{
    public class YRequestHandler 
    {
        private static readonly ILogger Logger = IocManager.Instance.Resolve<ILogger>();

        public static string ApiService(string service, string method, object param)
        {
            AuditingManager auditing = new AuditingManager();
            AjaxResult result = null;
                auditing.Start(service, method, param.ToString());
            try
            {
                var dataResult = ServiceController.Instance.Execute(service, method, param, RequestType.ServiceFile);
                result = new AjaxResult()
                {
                    IsSuccess = true,
                    Result = dataResult
                };
            }
            catch (Exception ex)
            {
                var except = ex.InnerException ?? ex;
                result = new AjaxResult()
                {
                    IsSuccess = false,
                    Result = null,
                    Errors = new Errors()
                    {
                        Message = except.Message,
                        CallStack = except.StackTrace
                    }
                };
                if (except.GetType() == typeof (UserFriendlyException))
                {
                    result.Errors.IsFriendlyError = true;
                }
                auditing.Exception(except.Message + except.StackTrace);
                Logger.Error(ex.Message, except);
            }
            var responseStr = JsonConvert.SerializeObject(result, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            auditing.Stop(responseStr);
            return responseStr;
        }

    }
}