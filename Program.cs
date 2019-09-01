using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace remotecard_client
{
    class Program
    {
        static string RESOURCE_BASE = "https://localhost:8443";
        static void Main(string[] args){
            string status = doGet(RESOURCE_BASE);
            Console.WriteLine("Health Status -> " + status);

            Console.WriteLine("\nAuthenticating from server...");
            Dictionary<string,object> objects = new Dictionary<string, object>(){
                {"terminalId", "1000000000123450"},
                {"operation", "handshake"},
            };
            string sessionResult = doPost(RESOURCE_BASE + "/authenticate", objects);
            Console.WriteLine("\nServer response ..."+sessionResult);
        }

        static string doGet(string endpoint){
            // Enable TLS 1.2
            ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
            WebRequest request = WebRequest.Create(endpoint);
            request.Credentials = CredentialCache.DefaultCredentials;
            try
            {
                WebResponse response = request.GetResponse();
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                Console.WriteLine(responseFromServer);
                reader.Close();
                response.Close();
                return responseFromServer;
            } catch (WebException e){
                Console.WriteLine("error reading from remotecard");
                Console.WriteLine(String.Concat(e.Message, e.StackTrace));
                return "error";
            }
        }
        static string doPost(string endpoint, Dictionary<string, object> objects){
            // Enable TLS 1.2
            ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
            string json = JsonConvert.SerializeObject(objects);
            Console.WriteLine("req -> \n"+json);

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(endpoint);
            req.Method = "POST";
            byte[] data = System.Text.Encoding.ASCII.GetBytes(json);
            req.ContentType = "application/json";
            req.ContentLength = data.Length;

            Stream requestStream = req.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream responseStream = res.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
            string received = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            responseStream.Close();

            res.Close();
            Console.WriteLine("res -> \n" + received);
            return received;
        }
    }
}
