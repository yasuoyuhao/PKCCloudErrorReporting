using Base.Helpers;
using CloudKeyFileProvider;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CloudErrorReporting.Helpers
{
    class CloudErrorReportingHelper
    {
        private readonly IConfiguration configuration;

        private readonly string jsonKey = "";
        private GoogleCredential googleCredential;

        public CloudErrorReportingHelper(IConfiguration configuration)
        {
            this.configuration = configuration;
            jsonKey = KeyProvider.GetCloudKey(configuration["KeyFilesSetting:KeyFileName:StackdriverErrorReporting"], KeyType.GoogleCloudErrorReporting);
                
        }

        public GoogleCredential GetGoogleCredential()
        {
            try
            {
                googleCredential = GoogleCredential.FromJson(jsonKey);
                return googleCredential;
            }
            catch (Exception ex)
            {
                #region Error Catch
                Logger.PringDebug($"-----Error{ System.Reflection.MethodBase.GetCurrentMethod().Name }-----");
                Logger.PringDebug(ex.ToString());
                return null;
                #endregion
            }
        }

        public string GetProjectId()
        {
            string result = configuration["GCPSetting:PROJECTNAME"];
            return result ?? "";
        }
    }
}
