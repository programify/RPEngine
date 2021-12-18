//*****************************************************************************
//
//                                                                     RPEngine
//
//*****************************************************************************
/*
 *   Reverse Proxy Engine.
 *
 *   (c) Copyright 2021, Programify Ltd.
 *   All rights reserved.
 */


//*****************************************************************************
//                                                    Application Configuration
//*****************************************************************************
/*
 *   "RPEngine.exe.config" must be located in the same folder as the executable.
 *   In the Visual Studio IDE, this file is awkwardly named "app.config" which
 *   generates the RPEngine.exe.config file at build time and places it in the
 *   Debug or Release folder depending on your selected Configuration.
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


//*****************************************************************************
//                                                                  Development
//*****************************************************************************
/*
 *   14-12-21  Started development.
 *   18-12-21  v1.0; Save restore point before next release.
 */


//*****************************************************************************
//                                                          Planned Development
//*****************************************************************************
/*
 *   Manage spin-out of multiple threads to service incoming requests. This
 *   should provide a higher throughput since some string processing is
 *   required when verifying incoming client requests.
 */


using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Security.Principal;
using System.Reflection;


//*****************************************************************************
//                                                                  NSEngineApp
//*****************************************************************************
namespace NSEngineApp
{


//=============================================================================
//                                                                     CProgram
//-----------------------------------------------------------------------------
static class CProgram
{
     public static byte[] gaPage400 ;        // Preloaded HTTP error [400] page.

     public static bool gbfRunServer ;       // Engine continues running while this flag is set true.

     public static string gstrDomains ;      // Config: Whitelist of valid domain names to serve.
     public static string gstrError400 ;     // Config: Fully qualified URI to Error 400 page on localized server.
     public static string gstrProxyIp ;      // Config: "ReverseProxyIP" RPEngine's host IP address.
     public static string gstrServerIp ;     // Config: "LocalizedServerIP" IP address of true localized server.

     public static string[] gastrDomains ;   // Array of valid domain names to serve.
     public static string gstrHttp ;         // Fully qualified HTTP address and port to listen on.
     public static string gstrHttps ;        // Fully qualified HTTPS address and port to listen on.
     public static string gstrServer ;       // Fully qualified base address and port of localized server.
     public static string gstrVersion ;      // Full version number for application including build code.

     public static HttpClient   g_server ;        // Class support for localized server.

     public static HttpListener g_internet ;      // Reverse proxy server for Internet clients.


//-----------------------------------------------------------------------------
//                                                                         Main
//-----------------------------------------------------------------------------
static void Main (string [] args)
{
// Get the unique version ID for this build
     gstrVersion = GetBuildVersion () ;
// Display application's title
     Console.Title = "RPEngine - Reverse Proxy Engine" ;
     Console.ForegroundColor = ConsoleColor.White ;
     Console.WriteLine ("RPEngine v{0}", gstrVersion) ;
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
     gstrDomains  = ConfigurationManager.AppSettings ["DomainNames"] ;

// Construct reverse proxy address (this code runs at this location)
     gstrHttp  = "http://"  + gstrProxyIp + ":80/" ;
     gstrHttps = "https://" + gstrProxyIp + ":443/" ;
// Construct base address of localized server
     gstrServer = "http://"  + gstrServerIp + ":80/" ;

// Convert DomainNames into an array of strings
     gastrDomains = gstrDomains.Split (',') ;
     for (var iIndex = 0 ; iIndex < gastrDomains.Length ; iIndex ++ )
          gastrDomains [iIndex] = gastrDomains [iIndex].Trim () ;
          
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
     Console.WriteLine ("Monitoring the Domain Name {0}", gstrDomains) ;
     Console.WriteLine ("Hosting Reverse Proxy on   {0}", gstrProxyIp) ;
     Console.WriteLine ("Fronting True Server at    {0}", gstrServerIp) ;
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
// Preload common error pages
     Console.ForegroundColor = ConsoleColor.Cyan ;
     gstrError400 = GetWebPage ("/errors/400.html") ;
     Console.WriteLine ("") ;
     Console.ForegroundColor = ConsoleColor.White ;
// Convert page strings into byte arrays
     gaPage400 = Encoding.ASCII.GetBytes (gstrError400) ;
     return true ;
}


//-----------------------------------------------------------------------------
//                                                    HandleIncomingConnections
//-----------------------------------------------------------------------------
public static async Task HandleIncomingConnections ()
{
     int       iStatus ;
     string    strStatus ;

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
               f_message  = null ;
          // Send info to console window without terminating the console line
               DisplayEvent (f_request) ;
          // Check if any known Domain Name is provided in client request
               if (! CheckDomains (f_request))
               {
                    ErrorReflex (f_response, HttpStatusCode.BadRequest) ;
                    goto close_channel ;
               }
          // Default to bad request
               iStatus   = (int) HttpStatusCode.InternalServerError ;
               strStatus = "Internal Server Error" ;
          // Attempt to create connection to localized server to process redirected requests
               try
               {
                    f_message = await g_server.GetAsync (f_request.Url.PathAndQuery) ;
               // Report the HTTP Statuc Code
                    iStatus = ((int) f_message.StatusCode) ;
               }
               catch (Exception e)
               {
                    ReportException (e) ;
               }
          // Transfer http status code
               f_response.StatusCode = iStatus ;
               if (f_message != null)
                    strStatus = f_message.StatusCode.ToString () ;
          // Report the HTTP status code
               DisplayStatus ('[', iStatus, strStatus, ']') ;
          // Abort request if page fetch failed
               if (f_message == null)
                    goto close_channel ;

          // Fetch the localized server's response (file content)
               byte[] abResponse = await f_message.Content.ReadAsByteArrayAsync () ;

          // Check if the asset was found on the localized server
               if (iStatus == (int) HttpStatusCode.OK)
               {
               // Prepare the response info
                    if (f_message.Content.Headers.ContentType == null)
                         f_response.ContentType = null ;
                    else
                         f_response.ContentType = f_message.Content.Headers.ContentType.ToString () ;
               // Apply default encoding if required
                    if (f_response.ContentEncoding == null)
                         f_response.ContentEncoding = Encoding.UTF8 ;

                    f_response.ContentLength64 = abResponse.LongLength ;
                    f_response.KeepAlive       = false ;
               }
          // Return data over the Internet to the client
               try
               {
               // Browser may have dumped the connection which is why we try/catch this write
                    f_response.OutputStream.Write (abResponse, 0, abResponse.Length) ;
               }
               catch (Exception e)
               {
                    ReportException (e) ;
                    goto close_channel ;
               }

     close_channel:

          // Send the response and close channel to client
               f_response.Close () ;
     }
}


//-----------------------------------------------------------------------------
//                                                                 CheckDomains
//-----------------------------------------------------------------------------
/*
 *   Checks the "DomainNames" list for matching domain or sub-domain names.
 *   Most hacks appear to use the IP address (since they are easier to generate.
value="programify.com, www.programify.com, 10.0.0.5" />
 */
public static bool CheckDomains (HttpListenerRequest f_request)
{
// Reject if no hostname supplied
     if (f_request.UserHostName == null)
          return false ;
// Accept is client-supplied domain name is recognised on white list
     for (var iIndex = 0 ; iIndex < gastrDomains.Length ; iIndex ++ )
     {
          if (f_request.UserHostName.Equals (gastrDomains [iIndex]))
               return true ;
     }
// Reject all other domain requests
     return false ;
}


//-----------------------------------------------------------------------------
//                                                                  ErrorReflex
//-----------------------------------------------------------------------------
/*
 *   ErrorReflex() responds directly to the client with an error page that is
 *   preloaded. This reduces the load on the system as the localized server
 *   is not involved.
 */
public static void ErrorReflex (HttpListenerResponse response, HttpStatusCode status)
{
     byte[]    abBuffer ;
     int       iStatus ;
     string    strPage ;
     string    strStatus ;

// Init
     iStatus = (int) status ;
     response.StatusCode = iStatus ;
// Distribute on HTTP status code
     switch (iStatus)
     {
          case 400 :  abBuffer = gaPage400 ; strStatus = "Bad Request" ; break ;

          default :
               strStatus = "Internal Server Error" ;
               strPage   = string.Format ("<h1>Internal Server Error</h1><p>HTTP Status Code <b>{0}</b> is not being handled.</p>", iStatus) ;
               abBuffer  = Encoding.ASCII.GetBytes (strPage) ;
               break ;
     }
// Report error on console
     DisplayStatus ('<', iStatus, strStatus, '>') ;
// Return data over the Internet to the client
     try
     {
     // Browser may have dumped the connection which is why we try/catch this write
          response.OutputStream.Write (abBuffer, 0, abBuffer.Length) ;
     }
     catch (Exception e)
     {
          ReportException (e) ;
     }
}


//-----------------------------------------------------------------------------
//                                                                DisplayStatus
//-----------------------------------------------------------------------------
public static void DisplayStatus (char cStart, int iStatus, string strStatus, char cEnd)
{
// Colourize server status code
     if (iStatus <= 299)
          Console.ForegroundColor = ConsoleColor.Green ;
     else
          Console.ForegroundColor = ConsoleColor.Red ;
// Report the HTTP status code
     string strReport = string.Format ("{0}{1} - {2}{3}", cStart, iStatus, strStatus, cEnd) ;
     Console.WriteLine (strReport) ;
     Console.ForegroundColor = ConsoleColor.White ;
}


//-----------------------------------------------------------------------------
//                                                                 DisplayEvent
//-----------------------------------------------------------------------------
public static void DisplayEvent (HttpListenerRequest f_request)
{
     DateTime  dtNow = DateTime.Now ;

// Get system timestamp
     string strDate = string.Format ("{0:0000}-{1:00}-{2:00} ", dtNow.Year, dtNow.Month, dtNow.Day) ;
     string strTime = string.Format ("{0:00}:{1:00}:{2:00}:{3:000}  ", dtNow.Hour, dtNow.Minute, dtNow.Second, dtNow.Millisecond) ;
// Display datestamp in sortable format
     Console.BackgroundColor = ConsoleColor.DarkBlue ; // rgb 0,0,32
     Console.ForegroundColor = ConsoleColor.Gray ;
     Console.Write (strDate) ;
     Console.Write (strTime) ;
     Console.ForegroundColor = ConsoleColor.White ;

// Report IP address
     Console.Write (f_request.RemoteEndPoint.Address.ToString ().PadRight (15) + " ") ;

// Colourize info about the request
     if (f_request.Url.LocalPath.Equals ("/"))
          Console.ForegroundColor = ConsoleColor.Yellow ;
     if (f_request.Url.LocalPath.Contains (".html"))
          Console.ForegroundColor = ConsoleColor.Yellow ;
     if (f_request.Url.LocalPath.Contains (".js"))
          Console.ForegroundColor = ConsoleColor.DarkYellow ;

// Report remote user accessing this proxy server
     Console.Write (string.Format ("{0} ", f_request.HttpMethod)) ;
     Console.Write (string.Format ("{0} ", f_request.Url.ToString ())) ;
// Report possible browser or bot identifier
     if (f_request.UserAgent != null)
     {
          Console.ForegroundColor = ConsoleColor.Gray ;
          Console.Write (string.Format ("({0})", f_request.UserAgent)) ;
     }
     Console.ForegroundColor = ConsoleColor.White ;
}


//-----------------------------------------------------------------------------
//                                                                   GetWebPage
//-----------------------------------------------------------------------------
public static string GetWebPage (string strUri)
{
     string    strPage ;
     string    strQuery ;
     string    strStatus ;

     Stream         response ;
     StreamReader   reader ;
     WebClient      wcClient ;

// Init
     strPage   = string.Format ("<p>Failed to preload <b>{0}</b> from localized server.</p>", strUri) ;
     strStatus = "Failed" ;
// Construct server query
     strQuery = string.Format ("http://{0}{1}", gstrServerIp, strUri) ;
// Attempt to fetch page from localized server
     try
     {
          wcClient  = new WebClient () ;
          response  = wcClient.OpenRead (strQuery) ;
          reader    = new StreamReader (response, Encoding.ASCII) ;
          strPage   = reader.ReadToEnd () ;
          strStatus = "OK" ;
          response.Close () ;
     }
     catch (Exception e)
     {
          ReportException (e) ;
     }
// Report action and outcome
     Console.WriteLine ("Preload \"{0}\" ({1} Bytes) - [{2}]", strQuery, strPage.Length, strStatus) ;
     return strPage ;
}


//-----------------------------------------------------------------------------
//                                                              ReportException
//-----------------------------------------------------------------------------
public static void ReportException (Exception e)
{
// Report failure to write back contents
     Console.ForegroundColor = ConsoleColor.Red ;
     Console.WriteLine ("*** " + e.Message) ;
     Console.ForegroundColor = ConsoleColor.White ;
}


//-----------------------------------------------------------------------------
//                                                              GetBuildVersion
//-----------------------------------------------------------------------------
/*
 *   GetBuildVersion() returns a string based on the assembly's major and minor
 *   version numbers and the date and time (UTC) when the executable file was
 *   last written to (this usually means when last compiled).
 */
public static string GetBuildVersion ()
{
     int       iYear ;

     Assembly       assembly ;
     DateTime       writetime ;
     FileInfo       fileinfo ;
     Version        version ;

// Access assembly metrics
     assembly = Assembly.GetExecutingAssembly () ;
     version  = assembly.GetName().Version ;
// Get information on executable's file
     fileinfo = new FileInfo (assembly.Location) ;
// Extract date and time EXE was last written to
     writetime = fileinfo.LastWriteTimeUtc ;
// Strip away the century
     iYear  = writetime.Year ;
     iYear %= 100 ;
// Construct date and time last written to
     string strDate = string.Format ("{0:00}{1:00}{2:00}", iYear, writetime.Month, writetime.Day) ;
     string strTime = string.Format ("{0:00}{1:00}{2:00}", writetime.Hour, writetime.Minute, writetime.Second) ;
// Construct version number with build date and time     
     return string.Format ("{0}.{1}.{2}.{3}", version.Major, version.Minor, strDate, strTime) ;
}


//*****************************************************************************
}
//*****************************************************************************
}
