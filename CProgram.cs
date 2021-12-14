//*****************************************************************************
//
//                                                                     RPEngine
//
//*****************************************************************************


//*****************************************************************************
//                                               Application Configuration File
//*****************************************************************************
/*
 *   RPEngine.exe.config must be located in the same folder as the executable.
 *   In the Visual Studio IDE, this file is awkwardly named "app.config" which
 *   generates the exe.config file at build time.
 */


//*****************************************************************************
//                                                    Windows Defender Firewall
//*****************************************************************************
/*
 *   HttpListener is built on top of http.sys which will listen on the port 
 *   you specified on behalf of your program. You must limit your inbound rule 
 *   to system components only by:
 *
 *   1.   Open Windows Defender Firewall with Advanced Security.
 *        Select "Inbound Rules".
 *
 *   2.   Add a new rule for a "Program", next
 *        This program path is simply "system", next
 *        Allow the connection, next
 *        Apply rule to "Private and Public", next
 *        Give the rule a name "HTTP Visibility", 
 *        The description "Allows remote access to applications using the system's .NET HTTP client.",
 *        and click finish.
 *
 *   3.   Double-click the rule to see the rule's properties dialog.
 *   4.   Select Protocol and Ports tab.
 *   5.   Protocol Type: TCP,
 *        Local Port: Specific Ports,
 *        enter underneath: "80, 443". Click OK.
 */


using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;


//*****************************************************************************
//                                                                  NSEngineApp
//*****************************************************************************
namespace NSEngineApp
{


//=============================================================================
//                                                                     CProgram
//-----------------------------------------------------------------------------
class CProgram
{

     public static bool gbfRunServer ;

     public static string gstrProxyIp ;
     public static string gstrServerIp ;

     public static string gstrHttp ;
     public static string gstrHttps ;
     public static string gstrServer ;

     public static HttpClient   g_server ;        // Class support for localized server.

     public static HttpListener g_internet ;      // Reverse proxy server for Internet clients.


//-----------------------------------------------------------------------------
//                                                                         Main
//-----------------------------------------------------------------------------
static void Main (string [] args)
{
     Console.Title = "RPEngine - Reverse Proxy Engine" ;
     Console.ForegroundColor = ConsoleColor.White ;
     Console.WriteLine ("RPEngine") ;
     Console.WriteLine ("Reverse Proxy Engine. (c) 2021, Programify.") ;
     Console.WriteLine ("") ;

// Load configuration
     if (! GetConfiguration (args))
          return ;
// Start listening for incoming http connections
     if (! StartReverseProxy ())
          return ;
// Connect to localized server which actually handles the Internet client's requests
     if (! ConnectLocalizedServer ())
          return ;

// Invoke main server handler thread
     Task listenTask = HandleIncomingConnections () ;
// Wait for server thread to terminate is main control loop [thread blocking]
     listenTask.GetAwaiter ().GetResult () ;

// Close the listenere
     g_internet.Close () ;
}


//-----------------------------------------------------------------------------
//                                                             GetConfiguration
//-----------------------------------------------------------------------------
public static bool GetConfiguration (string [] args)
{
// Get network settings
     gstrProxyIp  = ConfigurationManager.AppSettings ["ReverseProxyIP"] ;
     gstrServerIp = ConfigurationManager.AppSettings ["LocalizedServerIP"] ;

// Construct reverse proxy address (this code runs at this location)
     gstrHttp  = "http://"  + gstrProxyIp + ":80/" ;
     gstrHttps = "https://" + gstrProxyIp + ":443/" ;
// Construct base address of localized server
     gstrServer = "http://"  + gstrServerIp + ":80/" ;
     return true ;
}


//-----------------------------------------------------------------------------
//                                                            StartReverseProxy
//-----------------------------------------------------------------------------
public static bool StartReverseProxy ()
{
// Create a Http server and start listening for incoming connections
     try
     {
          g_internet = new HttpListener () ;
          g_internet.Prefixes.Add (gstrHttp) ;
          g_internet.Prefixes.Add (gstrHttps) ;
          g_internet.Start () ;
     }
     catch (Exception error)
     {
          Console.WriteLine ("*** " + error.Message + " ***") ;
          return false ;
     }

     Console.ForegroundColor = ConsoleColor.Cyan ;
     Console.WriteLine ("Hosting Reverse Proxy on {0}", gstrProxyIp) ;
     Console.WriteLine ("Fronting True Server at  {0}", gstrServerIp) ;
     Console.WriteLine ("") ;
     Console.ForegroundColor = ConsoleColor.White ;
     return true ;
}


//-----------------------------------------------------------------------------
//                                                       ConnectLocalizedServer
//-----------------------------------------------------------------------------
public static bool ConnectLocalizedServer ()
{
     g_server = new HttpClient () ;
     g_server.BaseAddress = new Uri (gstrServer, UriKind.Absolute) ;
     g_server.MaxResponseContentBufferSize = 1000000 ;
     g_server.Timeout = TimeSpan.FromSeconds (3) ;
     return true ;
}


//-----------------------------------------------------------------------------
//                                                    HandleIncomingConnections
//-----------------------------------------------------------------------------
public static async Task HandleIncomingConnections ()
{
     int iStatus ;

     HttpListenerContext      f_context ;
     HttpResponseMessage      f_message ;
     HttpListenerRequest      f_request ;
     HttpListenerResponse     f_response ;

// Init
     gbfRunServer = true ;
// While a user hasn't visited the `shutdown` url, keep on handling requests
     while (gbfRunServer)
     {
          // Wait to receive a request [thread blocking]
               f_context = g_internet.GetContext () ;
          // Isolate request and response objects
               f_request  = f_context.Request ;
               f_response = f_context.Response ;

          // Display debug info about the request
               if (f_request.Url.LocalPath.Equals ("/"))
                    Console.ForegroundColor = ConsoleColor.Yellow ;
               if (f_request.Url.LocalPath.Contains (".html"))
                    Console.ForegroundColor = ConsoleColor.Yellow ;
               if (f_request.Url.LocalPath.Contains (".js"))
                    Console.ForegroundColor = ConsoleColor.DarkYellow ;

          // Report remote user accessing this proxy server
               Console.Write (f_request.RemoteEndPoint.Address.ToString () + " ") ;
               Console.Write (f_request.RemoteEndPoint.Port.ToString () + " ") ;
               Console.Write (f_request.HttpMethod       + " ") ;
               Console.Write (f_request.Url.ToString ()  + " ") ;
               Console.ForegroundColor = ConsoleColor.White ;

          /// Add code to catch timeout exceptions (true server offline)
          // Create connection to localized server to process redirected requests
               f_message = await g_server.GetAsync (f_request.Url.PathAndQuery) ;

               iStatus = ((int) f_message.StatusCode) ;

               Console.WriteLine ("[" + iStatus.ToString() + " - " + f_message.StatusCode + "]") ;

          // Fetch the localized server's response (file content)
               byte[] abResponse = await f_message.Content.ReadAsByteArrayAsync () ;
          // Transfer http status code
               f_response.StatusCode = (int) f_message.StatusCode ;
          // Check if the asset was found on the localized server
               if (iStatus == (int) HttpStatusCode.OK)
               {
               // Prepare the response info
                    if (f_message.Content.Headers.ContentType == null)
                         f_response.ContentType = null ;
                    else
                         f_response.ContentType = f_message.Content.Headers.ContentType.ToString () ;
                    f_response.ContentEncoding = Encoding.UTF8 ;
                    f_response.ContentLength64 = abResponse.LongLength ;
                    f_response.KeepAlive       = true ;
               }
          // Return data over the Internet to the client
               try
               {
                    f_response.OutputStream.Write (abResponse, 0, abResponse.Length) ;
               }
               catch (Exception e)
               {
               // Report failure to write back contents
                    Console.ForegroundColor = ConsoleColor.Red ;
                    Console.WriteLine ("***" + e.Message) ;
                    Console.ForegroundColor = ConsoleColor.White ;
               }
          // Close channel to client
               f_response.Close () ;
     }
}


//*****************************************************************************
}
//*****************************************************************************
}
