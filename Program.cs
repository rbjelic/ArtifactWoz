using Glove.Model;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Glove
{
    class Program
    {

        static void Main(string[] args)
        {
            GlovoUpdateProducts();
        }

        private static void GlovoUpdateProducts()
        {
            //string[] SifraObjekta = { "259","278" };
            
            object getShops;
            object getProducts;
            object saveInsGlovoTransakcije;
            object saveGlovoLog;

            getShops = ProductList.GetShopID(); 
            if (getShops.GetType().Equals(typeof(string)))
            {
                WriteResutlToFile("Error at GetShopId's. ", getShops);
            }
            else
            {
                foreach (ShopId shop in (IList)getShops)
                {
                    while (ProductList.GetProductsCount(shop.shopid, 1) > 0) // 
                    {
                        getProducts = ProductList.GetProductsAndPromotions(shop.shopid, 1);
                        if (getProducts.GetType().Equals(typeof(string)))
                        {
                            WriteResutlToFile("Error at get Articles", getProducts);
                        }
                        else
                        {
                            (string, string) ReturnStatusAndResponse = UpdProducts(getProducts, shop.shopid);
                            if (ReturnStatusAndResponse.Item1 == "0")
                            {
                                Transaction transaction = JsonConvert.DeserializeObject<Transaction>(ReturnStatusAndResponse.Item2);
                                saveInsGlovoTransakcije = ProductList.InsGlovoTransakcije(shop.shopid, transaction.transaction_id, "0", DateTime.Now);
                                if (saveInsGlovoTransakcije.GetType().Equals(typeof(string)))
                                {
                                    WriteResutlToFile(shop.shopid, saveInsGlovoTransakcije);
                                    //SlanjeMaila(true, shop.shopid);
                                }
                            }
                            else if (ReturnStatusAndResponse.Item1 == "-1")
                            {
                                //SlanjeMaila(false, shop.shopid, ReturnStatusAndResponse.Item2, ProductList.InsGlovo_Log(shop.shopid, ReturnStatusAndResponse.Item2, ReturnStatusAndResponse.Item1));
                                saveGlovoLog = ProductList.InsGlovo_Log(shop.shopid, ReturnStatusAndResponse.Item2, ReturnStatusAndResponse.Item1);
                                if (saveGlovoLog.GetType().Equals(typeof(string)))
                                {
                                    WriteResutlToFile(shop.shopid, saveGlovoLog);
                                }
                            }
                            else
                            {
                                //SlanjeMaila(false, shop.shopid, ReturnStatusAndResponse.Item2, ProductList.InsGlovo_Log(shop.shopid, ReturnStatusAndResponse.Item2, ReturnStatusAndResponse.Item1));
                                saveGlovoLog = ProductList.InsGlovo_Log(shop.shopid, ReturnStatusAndResponse.Item2, ReturnStatusAndResponse.Item1);
                                if (saveGlovoLog.GetType().Equals(typeof(string))) WriteResutlToFile(shop.shopid, saveGlovoLog);
                            }
                        }
                    }
                }
                getShops = ProductList.UpdGlovo_EvidencijaServisa();
                if (getShops.GetType().Equals(typeof(string))) WriteResutlToFile("Error at update vreme_azuriranja", getShops);
            }
        }

        private async static void JsonWriteToFile(string SifraObjekta, string request)
        {
            try
            {
                string fileName = SifraObjekta + "-JsonBodyRequest-" +  (DateTime.Now.ToString().Replace(".","-")).Replace(":","-")+".txt";
                FileStream fs = null;
                string n = "\r\n";

                if (File.Exists(SifraObjekta + "-JsonBodyRequest-" + (DateTime.Now.ToString().Replace(".", "-")).Replace(":","-") + ".txt"))
                    fs = new FileStream(fileName, FileMode.Append);
                else
                    fs = new FileStream(fileName, FileMode.Create);


                StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("utf-8"));
                await sw.WriteAsync(request);
                sw.AutoFlush = true;
                sw.Close();
            }
            catch (Exception e)
            {
                WriteResutlToFile(SifraObjekta, e);
            }
        }

        private async static void WriteResutlToFile(string SifraObjekta, object result)
        {
            try
            {
                string fileName = "Glovo.txt";
                FileStream fs = null;
                string n = "\r\n";

                if (File.Exists("Glovo.txt"))
                    fs = new FileStream(fileName, FileMode.Append);
                else
                    fs = new FileStream(fileName, FileMode.Create);

                StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("utf-8"));
                await sw.WriteAsync(DateTime.Now.ToString() + ">>>" + SifraObjekta + ":" + result + n);
                sw.AutoFlush = true;
                sw.Close();
            }
            catch (Exception e)
            {
                // do nothing at this point
            }
        }

        private static (string, string) UpdProducts(object data, string shop)
        {
            string ReturnStatus = "";
            string ReturnResponse = "";

            // serijalizacija objekta
            //string json = @"{""products"": "+ JsonConvert.SerializeObject(data) + "}";
            var json = JsonConvert.SerializeObject(data);
            //JsonWriteToFile(shop, json);

            // uri
            //string url = "https://stageapi.glovoapp.com/webhook/stores/store-01/menu/updates"; //CG test
            //string url = "https://stageapi.glovoapp.com/webhook/stores/mercator-01/menu/updates"; //RS test
            string url = "https://api.glovoapp.com/webhook/stores/"+shop+"/menu/updates"; // RS i CG production
            Uri uri = new Uri(url);

            string authHeaderValue = "68ae6094-f7a8-48f1-bbba-910e21e6735e";
                                     

            // make http POST request throw async task
            HttpContent content = new StringContent(json, Encoding.ASCII, "application/json");
            var t = Task.Run(() => PostUri(authHeaderValue, uri, content));
            t.Wait();

            // Display request result
            ReturnStatus = t.Result.Item2;
            ReturnResponse = t.Result.Item1;

            return (ReturnStatus, ReturnResponse);
        }

        static async Task<(string, string)> PostUri(string AuthValue, Uri u, HttpContent c)
        {
            HttpClientHandler handler = new HttpClientHandler();

            IWebProxy proxy = WebRequest.GetSystemWebProxy();
            proxy.Credentials = CredentialCache.DefaultCredentials;
            handler.Proxy = proxy;

            string response = string.Empty;
            string status = "";

            HttpClient client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(300);

            client.DefaultRequestHeaders.Add("Authorization", AuthValue);
            try
            {
                HttpResponseMessage result = await client.PostAsync(u, c);

                if ((int)result.StatusCode >= 200 && ((int)result.StatusCode) < 300)
                {
                    status = "0";
                    response = await result.Content.ReadAsStringAsync();
                }
                else if ((int)result.StatusCode >= 400 && ((int)result.StatusCode) < 500)
                {
                    status = "-1";
                    response = await result.Content.ReadAsStringAsync();
                }
                else
                {
                    status = "-2";
                    response = "Network error!";
                }
                return (response, status);
            }
            catch (Exception e)
            {
                status = "-3";
                //response = "Internet error";
                response = e.Message;
                return (response, status);
            }
        }

        static void SlanjeMaila(bool greska = false, string shop = "", string poruka = "", int id_log = 0)
        {
            StringBuilder html = new StringBuilder();

            string datum = DateTime.Now.Date.ToString("dd.MM.yyyy");

            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("mail.idea.rs", 25);
            mail.IsBodyHtml = true;
            mail.From = new MailAddress("glovo.interface@mercator.rs");

           // mail.To.Add("nenad.bobic@mercator.rs");
            mail.To.Add("ratko.bjelic@mercator.rs");
            //mail.To.Add("it.centralne.aplikacije@mercator.rs");

            mail.Body += @"<style>
                            .multiline {
                                white - space: pre - wrap;
                            }
                            </style>";

            if (greska == false)
            {
                mail.Subject = "GRESKA - Dnevno slanje podataka Glovo " + datum;
                mail.Body += @"<p>Greska prilikom slanja podataka na Glovo API, za objekat:" + shop + "</p>";
                mail.Body += @"</br>";
                mail.Body += @"<p>Log: select * from idea_bde.dbo.Glovo_Log where id = " + id_log.ToString() + "</p>";
                mail.Body += @"</br>";
                mail.Body += @"<p>" + poruka + "</p>";
                //SmtpServer.Send(mail);
            }
            else
            {
                mail.Subject = "Dnevno slanje podataka Glovo " + datum;
                mail.Body = @"<p>Uspesno poslati podaci za " + datum + ", objekat: " + shop + "</p>";
                //SmtpServer.Send(mail);
            }
        }

    }
}

