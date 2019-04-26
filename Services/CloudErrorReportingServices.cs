using CloudErrorReporting.Helpers;
using Google.Api.Gax.ResourceNames;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.ErrorReporting.V1Beta1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Auth;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CloudErrorReporting.Services
{
    public class CloudErrorReportingServices
    {
        private readonly IConfiguration configuration;
        private readonly GoogleCredential googleCredential;
        private readonly Channel channel;
        private readonly ReportErrorsServiceClient client;
        private readonly string projectId;

        public CloudErrorReportingServices(IConfiguration configuration)
        {
            this.configuration = configuration;
            var helper = new CloudErrorReportingHelper(configuration);

            googleCredential = helper.GetGoogleCredential();
            channel = new Channel(
                ReportErrorsServiceClient.DefaultEndpoint.Host,
                ReportErrorsServiceClient.DefaultEndpoint.Port,
                googleCredential.ToChannelCredentials()
                );

            client = ReportErrorsServiceClient.Create(channel);
            projectId = helper.GetProjectId();
        }

        public void CreateReportErrorEventAsync(Exception ex, string userid, string service, string version, HttpContext httpContext = null)
        {
            ProjectName projectName = new ProjectName(projectId);

            StackTrace stackTrace = new StackTrace(ex, true);
            StackFrame stackFrame = stackTrace.GetFrame(stackTrace.FrameCount - 1);

            ReportedErrorEvent error = new ReportedErrorEvent
            {
                Context = new ErrorContext
                {
                    ReportLocation = new SourceLocation
                    {
                        FilePath = stackFrame.GetFileName(),
                        FunctionName = stackFrame.GetMethod().Name,
                        LineNumber = stackFrame.GetFileLineNumber(),
                    },
                    User = userid
                },
                // If this is a stack trace, the service will parse it.
                Message = ex.Message,

                EventTime = Timestamp.FromDateTime(DateTime.UtcNow),
                ServiceContext = new ServiceContext
                {
                    Service = service,
                    Version = version
                }
            };

            if (httpContext != null)
            {
                HttpRequestContext httpRequest = new HttpRequestContext
                {
                    Method = httpContext.Request.Method,
                    Referrer = httpContext.Request.Path,
                    RemoteIp = httpContext.Connection.RemoteIpAddress.ToString(),
                    Url = httpContext.Request.Path
                };

                error.Context.HttpRequest = httpRequest;
            }

            client.ReportErrorEventAsync(projectName, error);
        }

        ~CloudErrorReportingServices()
        {
            channel.ShutdownAsync().Wait();
        }
    }
}
