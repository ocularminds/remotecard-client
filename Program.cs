using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace remotecard_client
{
    class Program
    {
        static string RESOURCE_BASE = "https://localhost:8443";
        static string TERMINAL_ID = "1000000000123450";
        static void Main(string[] args){

            string status = doGet(RESOURCE_BASE);
            Console.WriteLine("Health Status -> " + status);

            Console.WriteLine("\nAuthenticating from server...");
            Dictionary<string,object> objects = new Dictionary<string, object>(){
                {"terminalId", TERMINAL_ID},
                {"operation", "handshake"},
            };
            string sessionResult = doPost(RESOURCE_BASE + "/api/handshake", objects);
            Console.WriteLine("\nServer response ..."+sessionResult);
            Dictionary<string, object> fault = JsonConvert.DeserializeObject<Dictionary<string,object>>(sessionResult);
            if(fault["error"].ToString() == "00")
            {
                var sessionKey = (string)fault["data"];
                Console.WriteLine("\nSending card read operation...");
                objects = new Dictionary<string, object>(){
                    {"terminalId", TERMINAL_ID},
                    {"operation", "read"},
                 };
                string responseJson = doPost(RESOURCE_BASE + "/api/read", objects);
                Console.WriteLine("\nServer response ..." + responseJson);
                fault = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson);
                if(fault["error"].ToString() == "00")
                {
                    string cipherJson = (string)fault["data"];
                    Console.WriteLine("cipherJson ..." + cipherJson);
                    string decrypted = Decrypt(cipherJson, sessionKey, TERMINAL_ID);
                    Console.WriteLine("decrypted ..." + decrypted);

                    fault = JsonConvert.DeserializeObject<Dictionary<string, object>>(decrypted);
                    if(fault["error"].ToString() == "00")
                    {
                        var pan = fault["pan"];
                        var application=fault["application"];
                        var expiry=fault["expiry"];
                        var cardholder = fault["cardholder"];
;
                    }
                    else
                    {
                        Console.WriteLine(fault["error"]);
                    }
                }
            }
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
        public static string Decrypt(string cipherJson, string sessionKey, string terminalID){
            byte[] key = Encoding.UTF8.GetBytes(sessionKey);
            byte[] iv = Encoding.UTF8.GetBytes(terminalID);

            try
            {
                using (var rijndaelManaged =
                       new RijndaelManaged { Key = key, IV = iv, Mode = CipherMode.CBC })
                using (var memoryStream =
                       new MemoryStream(Convert.FromBase64String(cipherJson)))
                using (var cryptoStream =
                       new CryptoStream(memoryStream,
                           rijndaelManaged.CreateDecryptor(key, iv),
                           CryptoStreamMode.Read))
                {
                    return new StreamReader(cryptoStream).ReadToEnd();
                }
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }
            // You may want to catch more exceptions here...
        }
    }
}
