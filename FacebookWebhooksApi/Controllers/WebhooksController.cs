using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Configuration;
using System.Web.Http;

namespace FacebookWebhooksApi.Controllers
{
    public class WebhooksController : ApiController
    {
        #region Fields

        private string _verificationToken = WebConfigurationManager.AppSettings["verificationToken"];
        private string _token = WebConfigurationManager.AppSettings["tokens"];


        #endregion

        WebRepo repo = new WebRepo();

        #region Get Request

        [HttpGet]
        public HttpResponseMessage Get()
        {
            try
            {
                var mode = HttpContext.Current.Request.QueryString["hub.mode"].ToString();
                var challenge = HttpContext.Current.Request.QueryString["hub.challenge"].ToString();
                var verifyToken = HttpContext.Current.Request.QueryString["hub.verify_token"].ToString();

                FacebookLogs log1 = new FacebookLogs { Name = "Verification", Mesaj1 = $"Mode->" + mode + "Challenge->" + challenge, Mesaj2 = $"Verification Token->" + verifyToken };
                repo.FacebookLogsInsert(log1);

                if (verifyToken == _verificationToken)
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(challenge) };
                else
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid verification token") };
            }
            catch (Exception ex)
            {
                FacebookLogs log2 = new FacebookLogs { Name = "Error", Mesaj1 = $"Get->" + ex.Message, Mesaj2 = $"StackTrace->" + ex.StackTrace };
                repo.FacebookLogsInsert(log2);
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(ex.Message) };
            }
        }

        #endregion Get Request

        #region Post Request

        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] JsonDataModel data)
        {
            FacebookLogs log3 = new FacebookLogs { Name = "LeadGen", Mesaj1 = $"Data ->" + JsonConvert.SerializeObject(data), Mesaj2 = "" };
            repo.FacebookLogsInsert(log3);
            try
            {
                var entry = data.Entry.FirstOrDefault();
                var change = entry?.Changes.FirstOrDefault();
                if (change == null) return new HttpResponseMessage(HttpStatusCode.BadRequest);



                //Generate user access token here https://developers.facebook.com/tools/accesstoken/

                var leadUrl = $"https://graph.facebook.com/v14.0/" + change.Value.LeadGenId + "?fields=platform,field_data,adset_name&access_token=" + _token;
                var formUrl = $"https://graph.facebook.com/v14.0/" + change.Value.FormId + "?access_token=" + _token;


                if (!string.IsNullOrEmpty(_token))
                {
                    if (change.Value.LeadGenId == "444444444444")
                    {
                        FacebookLogs LeadGenIdtestLog = new FacebookLogs { Name = "LeadGenId Test", Mesaj1 = $"Girdi", Mesaj2 = "444444444444" };
                        repo.FacebookLogsInsert(LeadGenIdtestLog);
                    }
                    else
                    {
                        using (var httpClientLead = new HttpClient())
                        {

                            var response = await httpClientLead.GetStringAsync(formUrl.ToString());
                            if (!string.IsNullOrEmpty(response))
                            {
                                var jsonObjLead = JsonConvert.DeserializeObject<LeadFormData>(response);


                                using (var httpClientFields = new HttpClient())
                                {
                                    var responseFields = await httpClientFields.GetStringAsync(leadUrl.ToString());
                                    if (!string.IsNullOrEmpty(responseFields))
                                    {

                                        string name = string.Empty;
                                        string tel = string.Empty;
                                        string mail = string.Empty;
                                        string platform = string.Empty;
                                        string adset_name = string.Empty;

                                        var jsonObjFields = JsonConvert.DeserializeObject<LeadData>(responseFields);


                                        platform = jsonObjFields.platform;
                                        adset_name = jsonObjFields.adset_name;
                                        foreach (var item in jsonObjFields.FieldData)
                                        {
                                            if (item.Name == "full_name")
                                            {
                                                name = item.Values[0].ToString();
                                            }

                                            if (item.Name == "phone_number")
                                            {
                                                tel = item.Values[0].ToString();
                                            }

                                            if (item.Name == "email")
                                            {
                                                mail = item.Values[0].ToString();
                                            }
                                        }

                                        Data fieldD = new Data { Name = name, CellPhone = tel, Mail = mail, Subject = adset_name + " platform: " + platform, LeadGenId = change.Value.LeadGenId, FormId = change.Value.FormId };
                                        repo.Data_Insert(fieldD);
                                    }
                                }
                            }
                        }
                    }

                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                FacebookLogs log5 = new FacebookLogs { Name = "Error", Mesaj1 = $"Post->" + ex.Message, Mesaj2 = $"StackTrace->" + ex.StackTrace };
                repo.FacebookLogsInsert(log5);
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
        }

        #endregion Post Request
    }
}