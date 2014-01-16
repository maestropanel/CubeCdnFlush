namespace CubeCdnFlush
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;

    class Program
    {
        enum ReturnCode
        {
            FILE_NOTFOUND = 0,
            CLEAR = 1,
            SERVER_ERROR = -1,
            MISSING_PARAMETER = 2,
            AUTHENTICATION_ERROR = 3,
            LOCAL_ERROR = 91,
            UNKNOWN = 92,
            ACCESS_ERROR = 93
        }
        
        static void Main(string[] args)
        {
            var prms = new Args(args);

            if (prms.isParameterMissing())
            {
                WriteHelp();
                return;
            }

            var files = Directory.GetFiles(prms.LocalPath);

            Console.Write("Are you ready? (Yes):");
            var isReady = Console.ReadLine();

            if (isReady.Equals("Yes"))
            {
                foreach (var item in files)
                {
                    var currentFile = String.Format("{0}{1}", prms.Prefix, Path.GetFileName(item));
                    var result = FlushRemoteObject(prms.Username, prms.Password, prms.Vhost, currentFile);
                    Console.WriteLine("Status: {1}\tFile: {0}", currentFile, result);
                }
            }
        }

        static string SendRequest(string username, string password, string vhosts, string filename)
        {
            var responseText = String.Empty;
            string requestData = String.Format("http://mapi.cubecdn.net/clearcache_api.php?username={0}&password={1}&vhost={2}&filename={3}", username, password, vhosts, filename);

            try
            {
                WebClient req = new WebClient();
                responseText = req.DownloadString(requestData);
            }
            catch (WebException ex)
            {
                Console.Write(ex.Message);
                responseText = @"<?xml version=""1.0"" encoding=""UTF-8""?><status>93</status>";
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                responseText = @"<?xml version=""1.0"" encoding=""UTF-8""?><status>92</status>";
            }

            return responseText;
        }

        static string readXml(string xmlText)
        {
            if (String.IsNullOrEmpty(xmlText))
                return String.Empty;

            XmlDocument document = new XmlDocument();
            document.LoadXml(xmlText);            

            return document["status"] != null? document["status"].InnerText : String.Empty; 
        }

        static ReturnCode FlushRemoteObject(string username, string password, string vhosts, string filename)
        {
            ReturnCode currentCode = ReturnCode.UNKNOWN;
            var defaultStatus = (int)ReturnCode.UNKNOWN;

            var response = SendRequest(username, password, vhosts, filename);

            if (int.TryParse(readXml(response), out defaultStatus))
                currentCode = (ReturnCode)defaultStatus;

            return currentCode;
        }

        static void WriteHelp()
        {
            Console.WriteLine("Source: http://www.github.com/maestropanel/flushcdn");
            Console.WriteLine("");
            Console.WriteLine("Usage:\tflushcdn [username=] [password=] [vhost=] [path=] [prefix=]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("\tusername\tcubeCDN Username");
            Console.WriteLine("\tpassword\tcubeCDN Password");
            Console.WriteLine("\tvhost\t\tcubeCDN Service Name or Origin Name");
            Console.WriteLine("\tpath\t\tLocal Directory Path");
            Console.WriteLine("\tprefix\t\tRemote Directory Name");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("flushcdn.exe username=asikome password=p@ssw0rd vhost=mydomain.maestropanel.com path=C:\\packages\\website prefix=/myfile/");

        }

        private class Args
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Vhost { get; set; }
            public string LocalPath { get; set; }
            public string Prefix { get; set; }

            private string[] _args;

            public Args(string[] args)
            {
                _args = args;

                Username = getArgument("username");
                Password = getArgument("password");
                Vhost = getArgument("vhost");
                LocalPath = getArgument("path");
                Prefix = getArgument("prefix");
            }


            public bool isParameterMissing()
            {
                return String.IsNullOrEmpty(Username) ||
                    String.IsNullOrEmpty(Password) ||
                    String.IsNullOrEmpty(Vhost) ||
                    String.IsNullOrEmpty(LocalPath);
            }

            private string getArgument(string name)
            {
                var arg = String.Empty;

                if (_args.Where(m => m.StartsWith(String.Format("{0}=", name))).Any())
                    arg = _args.FirstOrDefault(m => m.StartsWith(String.Format("{0}=", name))).Split('=').Last();

                return arg;                     
            }

        }
    }
}
