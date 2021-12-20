//*****************************************************************************
//
//   RPEngine                                                          CProgram
//   Open Source Edition
//
//*****************************************************************************
/*
 *   Reverse Proxy Engine.
 *
 *   (c) Copyright 2021, Programify Ltd.
 *   All commercialization rights reserved.
 *
 *   RPEngine will become an HTTP/HTTPS reverse proxy server designed to 
 *   operate as a firewalled load balancing router. Let's break that 
 *   description down...
 *
 *   1)   Firewalled.
 *
 *        Only valid server requests are allowed to proceed to your production
 *        servers. Most often, invalid and hostile requests arrive addressed
 *        to your server's IP address - sometimes a completely different IP!
 *
 *        This simple check prevents many attacks which are domain-name blind.
 *        Your attacker must at least know your domain name (or names) that 
 *        your RPEngine is configured to recognise.
 *
 *        All requests to bogus domains or direct IP addresses are blocked
 *        and the client end receives an error "400 - Bad Request". This takes
 *        place within RPEngine with zero load on your production servers.
 *
 *        With our open source code you can craft you own Web Application
 *        Firewall (WAF)[Ref.1] to weed out persistant attackers using tarpits 
 *        or stone-walls, blocking by country, IP address range, agent-id, etc.
 *
 *   2)   Load balancing.
 *
 *        RPEngine can direct valid incoming traffic to one or more production
 *        servers running on physically separate machine depending on your 
 *        configuration.
 *
 *        For example your brochure website and image server can be physically
 *        separated from your application server(s) to help speed up page
 *        serving. Client sessions running on application servers have no 
 *        impact on your front-of-house brochure web pages.
 *
 *   3)   Router.
 *
 *        A single instance of RPEngine can receive requests from multiple
 *        domain names which can be concentrated or routed to a single server
 *        or indeed physically separate and domain-specific servers.
 *
 *   This appplication is written in .NET C#, so while it's not as quick as C 
 *   or Assembler, it might be easier to modify and maintain. This app does not 
 *   claim to be super fast and all powerful, but it should cope with modest-
 *   volume SME business installations. It is free after all!
 *
 *   References:
 *
 *        [1]. https://www.cloudflare.com/en-gb/learning/ddos/glossary/web-application-firewall-waf/
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
 *   Microsoft's HttpListener is built on top of http.sys which will listen on 
 *   the port you specified on behalf of your program. You must limit your 
 *   inbound rule to system components only by:
 *
 *   1.   Open Windows Defender Firewall with Advanced Security.
 *        Select "Inbound Rules".
 *
 *   2.   Add a new rule for a "Program", next
 *        This program path is simply "system", next
 *        Allow the connection, next
 *        Apply rule to "Private and Public", next
 *        Give the rule a name "HTTP.SYS Visibility", 
 *        The description "Allows remote access to applications using the system's .NET HTTP client.",
 *        and click finish.
 *
 *   3.   Double-click the rule to see the rule's properties dialog.
 *
 *   4.   Select the "Protocol and Ports" tab.
 *
 *   5.   Protocol Type: TCP,
 *        Local Port: Specific Ports,
 *        enter underneath: "80, 443". Click OK.
 */


//*****************************************************************************
//                                                                  Development
//*****************************************************************************
/*
 *   14-12-21  Started development.
 *
 *   18-12-21  v1.0.
 *             Save restore point before next significant release.
 *
 *   19-12-21  v1.1.
 *             Multi-threaded service request handling with decoupled console
 *             output thread.
 */


//*****************************************************************************
//                                                                 Known Issues
//*****************************************************************************
/*
 *   +    Currently RPEngine only recognizes the GET method. Specifically it
 *        will reject POST and HEAD methods until further development of the 
 *        query service thread has been carried out.
 *
 *   +    Cookies are not passed between client and localized server. We hope
 *        to implement this feature some time in the future.
 *
 *   +    No way to stop application short of pressing 'Ctrl + C' a few times.
 *        In practice it is unlikely you will want stop/start RPEngine since it 
 *        is front-ending your web server(s). Perhaps during testing.
 *
 *   +    At least one stability issue exists where the app was seen to stop
 *        responding until 'Ctrl + C' was pressed once. The causal scenario is 
 *        currently unknown.
 */


using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Security.Principal;
using System.Reflection;


//*****************************************************************************
//                                                                     RPEngine
//*****************************************************************************
namespace RPEngine
{


//=============================================================================
//                                                                     CProgram
//-----------------------------------------------------------------------------
class CProgram
{
     static bool gbfRunServer ;       // Engine continues running while this flag is set true.

     static byte[] gaPage400 ;        // Preloaded HTTP error [400] page.
     static byte[] gaPage405 ;        // Preloaded HTTP error [405] page.
     static byte[] gaPage500 ;        // Preloaded HTTP error [500] page.

     static string[] gastrDomains ;   // Array of valid domain names to serve.

     static string gstrDomains ;      // Config: Whitelist of valid domain names to serve.
     static string gstrHttp ;         // Fully qualified HTTP address and port to listen on.
     static string gstrHttps ;        // Fully qualified HTTPS address and port to listen on.
     static string gstrProxyIp ;      // Config: "ReverseProxyIP" RPEngine's host IP address.
     static string gstrServer ;       // Fully qualified base address and port of localized server.
     static string gstrServerIp ;     // Config: "LocalizedServerIP" IP address of true localized server.
     static string gstrVersion ;      // Full version number for application including build code.

     static HttpClient     g_server ;     // Class support for localized server.

     static HttpListener   g_internet ;   // Reverse proxy server for Internet clients.


//-----------------------------------------------------------------------------
//                                                                         Main
//-----------------------------------------------------------------------------
static void Main ()
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
     if (! GetConfiguration ())
          return ;
// Start listening for incoming http connections
     if (! StartReverseProxy ())
          return ;
// Connect to localized server which actually handles the Internet client's requests
     if (! ConnectLocalizedServer ())
          return ;

// Use ThreadPool for a worker thread        
     int minWorker, minIOC;  
     ThreadPool.GetMinThreads(out minWorker, out minIOC);  
     ThreadPool.SetMinThreads (4, minIOC);  
     Console.WriteLine ("Using {0} worker threads.", minWorker) ;
     Console.WriteLine ("") ;

// Start console service
     CConsole.Start () ;

// Invoke main server handler thread
     HandleIncomingConnections () ;

// Close the listenere (Stop Reverse Proxy Engine)
     g_internet.Close () ;

     Console.WriteLine ("*** Press SPACE to exit app. ***") ;
     Console.ReadKey () ;  
}


//-----------------------------------------------------------------------------
//                                                             GetConfiguration
//-----------------------------------------------------------------------------
public static bool GetConfiguration ()
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
     gaPage400 = GetWebPage ("/errors/400.html") ;
     gaPage405 = GetWebPage ("/errors/405.html") ;
     gaPage500 = GetWebPage ("/errors/500.html") ;
// Display blank line and switch to default output colour
     CConsole.WriteLine (ConsoleColour.FgWhite) ;
// Wait until exception has been reported before proceeding
     CConsole.Wait () ;
     return true ;
}


//-----------------------------------------------------------------------------
//                                                    HandleIncomingConnections
//-----------------------------------------------------------------------------
public static void HandleIncomingConnections ()
{
// Init
     gbfRunServer = true ;
// While a user hasn't visited the `shutdown` url, keep on handling requests
     while (gbfRunServer)
     {
     // Pass an incoming connection to a server thread
          ThreadPool.QueueUserWorkItem (ServiceThread, g_internet.GetContext ()) ;
     }
}


//=============================================================================
//                                                                ServiceThread
//=============================================================================
public static async void ServiceThread (Object oContext)
{
     int       iStatus ;
     string    strLine ;
     string    strStatus ;

     HttpListenerContext      f_context ;
     HttpResponseMessage      f_message ;
     HttpListenerRequest      f_request ;
     HttpListenerResponse     f_response ;

// Isolate request and response objects
     f_context  = (HttpListenerContext) oContext ;
     f_request  = f_context.Request ;
     f_response = f_context.Response ;
     f_message  = null ;
// Send info to console window without terminating the console line
     strLine = DisplayEvent (f_request) ;
// Check if HTTP method is supported
     if (! CheckMethod (f_request.HttpMethod))
     {
          strLine += ErrorReflex (f_response, HttpStatusCode.MethodNotAllowed) ;
          goto close_channel ;
     }
// Check if any known Domain Name is provided in client request
     if (! CheckDomains (f_request))
     {
          strLine += ErrorReflex (f_response, HttpStatusCode.BadRequest) ;
          goto close_channel ;
     }
// Default to internal error
     iStatus   = (int) HttpStatusCode.InternalServerError ;
     strStatus = "Internal Server Error" ;
// Attempt to create connection to localized server to process redirected requests
     try
     {
     // GET Method
          f_message = await g_server.GetAsync (f_request.Url.PathAndQuery) ;
     // Report the HTTP Statuc Code
          iStatus = ((int) f_message.StatusCode) ;
     }
     catch (Exception e)
     {
          strLine += ReportException (e) ;
     }
// Transfer http status code
     f_response.StatusCode = iStatus ;
     if (f_message != null)
          strStatus = f_message.StatusCode.ToString () ;
// Report the HTTP status code
     strLine += DisplayStatus ('[', iStatus, strStatus, ']') ;
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
          strLine += ReportException (e) ;
          goto close_channel ;
     }

close_channel:

// Send the response to the client and close the channel
     f_response.Close () ;
// Queue line to console
     CConsole.WriteLine (strLine) ;
}


//-----------------------------------------------------------------------------
//                                                                 CheckDomains
//-----------------------------------------------------------------------------
/*
 *   Checks the "DomainNames" list for matching domain or sub-domain names.
 *   Most attackers appear to use the IP address because they are easier to 
 *   generate than enumerating domain names.
 *
 *   You can include IP addresses in the domain name whitelist if you wish, 
 *   but this will increase your exposure to hostile clients. It is most likely 
 *   that you would use this capability during testing.
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
//                                                                  CheckMethod
//-----------------------------------------------------------------------------
/*
 *   HTTP request methods are:
 *
 *        CONNECT, DELETE, GET, HEAD, OPTIONS, PATCH, POST, PUT, TRACE.
 *
 *   Currently, only GET is supported by RPEngine's ServiceThread().
 */
public static bool CheckMethod (string strMethod)
{
// Approve only these methods
     if (strMethod.Equals ("GET"))
          return true ;

// Reject all other methods
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
public static string ErrorReflex (HttpListenerResponse response, HttpStatusCode status)
{
     byte[]    abBuffer ;
     int       iStatus ;
     string    strLine ;
     string    strStatus ;

// Init
     iStatus = (int) status ;
     response.StatusCode = iStatus ;
// Distribute on HTTP status code
     switch (iStatus)
     {
          case 400 :  abBuffer = gaPage400 ; strStatus = "Bad Request" ;            break ;
          case 405 :  abBuffer = gaPage405 ; strStatus = "Method Not Allowed" ;     break ;

          default :   abBuffer = gaPage500 ; strStatus = "Internal Server Error" ;  break ;
     }
// Report error on console
     strLine = DisplayStatus ('<', iStatus, strStatus, '>') ;
// Return data over the Internet to the client
     try
     {
     // Browser may have dumped the connection which is why we try/catch this write
          response.OutputStream.Write (abBuffer, 0, abBuffer.Length) ;
     }
     catch (Exception e)
     {
          strLine += ReportException (e) ;
     }
// Return console line
     return strLine ;
}


//-----------------------------------------------------------------------------
//                                                                DisplayStatus
//-----------------------------------------------------------------------------
public static string DisplayStatus (char cStart, int iStatus, string strStatus, char cEnd)
{
     string    strFg ;

// Colourize server status code
     if (iStatus <= 299)
          strFg = ConsoleColour.FgGreen ;
     else
          strFg = ConsoleColour.FgRed ;
// Report the HTTP status code
     return string.Format ("{0}{1}{2} - {3}{4}", strFg, cStart, iStatus, strStatus, cEnd) ;
}


//-----------------------------------------------------------------------------
//                                                                 DisplayEvent
//-----------------------------------------------------------------------------
public static string DisplayEvent (HttpListenerRequest f_request)
{
     string    strLine ;

     DateTime  dtNow = DateTime.Now ;

// Get system timestamp
     string strDate = string.Format ("{0:0000}-{1:00}-{2:00}", dtNow.Year, dtNow.Month, dtNow.Day) ;
     string strTime = string.Format ("{0:00}:{1:00}:{2:00}:{3:000}", dtNow.Hour, dtNow.Minute, dtNow.Second, dtNow.Millisecond) ;
// Display datestamp in sortable format
     strLine  = string.Format ("{0}{1}{2} {3}  ", ConsoleColour.BgDarkBlue, ConsoleColour.FgGray, strDate, strTime) ;
// Report IP address
     strLine += string.Format ("{0}{1} ", ConsoleColour.FgWhite, f_request.RemoteEndPoint.Address.ToString ().PadRight (15)) ;
// Determine colour based on the client request
     strLine += GetElementColour (f_request) ;
// Report remote user accessing this proxy server
     strLine += string.Format ("{0} ", f_request.HttpMethod) ;
     strLine += string.Format ("{0} ", f_request.Url.ToString ()) ;
// Report possible browser or bot identifier
     if (f_request.UserAgent != null)
          strLine += string.Format ("{0}({1}) ", ConsoleColour.FgGray, f_request.UserAgent) ;
// Revert console to default foreground colour
     strLine += ConsoleColour.FgWhite ;
     return strLine ;
}


//-----------------------------------------------------------------------------
//                                                             GetElementColour
//-----------------------------------------------------------------------------
private static string GetElementColour (HttpListenerRequest f_request)
{
// Determine colour based on the path and filename extension code
     if (f_request.Url.LocalPath.Equals ("/"))
          return ConsoleColour.FgYellow ;
     if (f_request.Url.LocalPath.Contains (".html"))
          return ConsoleColour.FgYellow ;
     if (f_request.Url.LocalPath.Contains (".js"))
          return ConsoleColour.FgDarkYellow ;
// Default to neutral colour
     return ConsoleColour.FgWhite ;
}


//-----------------------------------------------------------------------------
//                                                                   GetWebPage
//-----------------------------------------------------------------------------
public static byte[] GetWebPage (string strUri)
{
     string    strPage ;
     string    strQuery ;
     string    strStatus ;

     Stream         response ;
     StreamReader   reader ;
     WebClient      wcClient ;

// Init
     strPage   = string.Format ("<p>Failed to preload <b>{0}</b> from localized server.</p>", strUri) ;
     strStatus = string.Format ("{0}[Failed]{1}", ConsoleColour.FgRed, ConsoleColour.FgWhite) ;
// Construct server query
     strQuery = string.Format ("http://{0}{1}", gstrServerIp, strUri) ;
// Attempt to fetch page from localized server
     try
     {
          wcClient  = new WebClient () ;
          response  = wcClient.OpenRead (strQuery) ;
          reader    = new StreamReader (response, Encoding.ASCII) ;
          strPage   = reader.ReadToEnd () ;
          strStatus = string.Format ("{0}OK{1}", ConsoleColour.FgGreen, ConsoleColour.FgWhite) ;
          response.Close () ;
     }
     catch (Exception e)
     {
     // Immediately report exception on console
          //CConsole.WriteLine (ReportException (e)) ;
          strStatus = string.Format ("{0}{1}{2}", ConsoleColour.FgRed + ConsoleColour.BgBlack, e.Message, ConsoleColour.FgWhite + ConsoleColour.BgDarkBlue) ;
     }
// Report action and outcome
     CConsole.WriteLine (string.Format ("{0}Preload \"{1}\" ({2} Bytes) - {3}", ConsoleColour.FgYellow, strQuery, strPage.Length, strStatus)) ;
// Convert to array of bytes - ready to be transmitted
     return Encoding.ASCII.GetBytes (strPage) ;
}


//-----------------------------------------------------------------------------
//                                                              ReportException
//-----------------------------------------------------------------------------
public static string ReportException (Exception e)
{
     return string.Format ("\n{0}*** {1}\n{2}", ConsoleColour.FgRed, e.Message, ConsoleColour.FgWhite) ;
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
