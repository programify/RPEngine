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
 *   All rights reserved.
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
 *        Firewall (WAF)[R1] to weed out persistant attackers using tarpits 
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
 *        [R1]. https://www.cloudflare.com/en-gb/learning/ddos/glossary/web-application-firewall-waf/
 */


//*****************************************************************************
//                                                                 Command Line
//*****************************************************************************
/*
 *   Syntax:
 *
 *        RPENGINE  [<switches>]
 *
 *   Where:
 *
 *        <switches>          One or more command line switches.
 *
 *   Switches:
 *
 *        -eco                Enable Console Output.
 *                            Display each server request event in real-time.
 *
 *        -elo                Enable Log Output.
 *                            Write server request events to the log file
 *                            specified in "RPEngine.exe.config".
 *
 *        -wait               Wait for SPACE key before terminating app.
 *
 *   Notes:
 *
 *   +    Prefixing a switch or parameter with two hyphens will cause the entry 
 *        to be ignored by the app's command line interpreter. E.g. (--elo)
 *
 *   +    The command line settings overrule the configuration file settings.
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
 *   20-12-21  Reduce code needed to pre-load error HTML files.
 *             Method Not Allowed now reported before Bad Request.
 *             GetWebPage() consolidated.
 *             Spun-out SetWorkerThreads() from Main().
 *
 *   21-12-21  v1.2.
 *             Added CommandLine() parameters "-eco", "-elo" and "-wait".
 *   22-12-21  Document command line in comments.
 *             Upgrade ServiceThread() to use new CRequest and CResponse.
 *             Installed new WriteLog() function.
 *   24-12-21  Add support for "HttpMethod" option in config file.
 *   25-12-21  Installed WriteLogHeader().
 *   26-12-21  Extended log file record to include new grouped HTTP headers.
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
 *   +    No way to stop application short of pressing 'Ctrl + C'.
 *        In practice it is unlikely you will want stop/start RPEngine since it 
 *        is front-ending your web server(s). Perhaps required most during 
 *        testing.
 *
 *   +    At least one stability issue exists where the app was seen to stop
 *        responding until 'Ctrl + C' was pressed once. The causal scenario is 
 *        believed to be if the task window is selected (for copying text) by 
 *        the mouse.
 *
 *   +    On some occassions the time at which a log entry appears on screen,
 *        is several minutes or hours before the current system time. This may
 *        be caused by the loosely coupled console queue but it not known how.
 */


//*****************************************************************************
//                                                                   References
//*****************************************************************************
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
using System.Security.Cryptography.X509Certificates;
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
     static bool gbfConsole ;           // True if "-eco" switch was supplied.
     static bool gbfLog ;               // True if "-elo" switch was supplied.
     static bool gbfRunServer ;         // Engine continues running while this flag is set true.
     static bool gbfWait ;              // True if "-wait" switch was supplied.

     static byte[] gaPage400 ;          // Preloaded HTTP error [400] page.
     static byte[] gaPage405 ;          // Preloaded HTTP error [405] page.
     static byte[] gaPage500 ;          // Preloaded HTTP error [500] page.

     static string[] gastrDomains ;     // Array of valid domain names to serve.

     static string gstrDomains ;        // Config: Whitelist of valid domain names to serve.
     static string gstrHttp ;           // Fully qualified HTTP address and port to listen on.
     static string gstrHttpMethods ;    // Separated list of permitted HTTP Methods.
     static string gstrHttps ;          // Fully qualified HTTPS address and port to listen on.
     static string gstrLogFolder ;      // Folder specification where log files are recorded.
     static string gstrLogName ;        // Suffix to full name of log file. Name is prefixed with a full time stamp.
     static string gstrProxyIp ;        // Config: "ReverseProxyIP" RPEngine's host IP address.
     static string gstrServer ;         // Fully qualified base address and port of localized server.
     static string gstrServerIp ;       // Config: "LocalizedServerIP" IP address of true localized server.

     static HttpClient     g_server ;   // Class support for localized server.

     static HttpListener   g_internet ; // Reverse proxy server for Internet clients.

     static readonly object   g_lockLog = new object () ;


//-----------------------------------------------------------------------------
//                                                                         Main
//-----------------------------------------------------------------------------
static void Main (string[] astrArgs)
{
// Prepare console task's window
     Console.Title = "RPEngine - Reverse Proxy Engine" ;
     Console.ForegroundColor = ConsoleColor.White ;
// Display application's title
     Console.WriteLine ("RPEngine v{0}", GetBuildVersion ()) ;
     Console.WriteLine ("Reverse Proxy Engine. (c) 2021, Programify.") ;
     Console.WriteLine ("") ;

// Perform all activities before the engine can run
     if (PrepareApp (astrArgs))
     {
     // Application's principal activity 
          try
          {
          // Invoke main server handler thread
               HandleIncomingConnections () ;
          }
          catch (Exception e)
          {
               Console.WriteLine (string.Format ("\n*** {1}\n", e.Message)) ;
          }
     }
// Stop the app
     StopRProxy () ;

// Option to wait on exit
     if (gbfWait)
     {
          CConsole.WriteLine (ConsoleColour.FgGray + "*** Press ESC to exit app.") ;
          CConsole.WaitKeyDown ((char) ConsoleKey.Escape) ;
     }
// Terminate this application
     Environment.Exit (0) ;
}


//-----------------------------------------------------------------------------
//                                                                   PrepareApp
//-----------------------------------------------------------------------------
private static bool PrepareApp (string[] astrArgs)
{
// Global inits
     gbfConsole = false ;
     gbfLog     = false ;
     gbfWait    = false ;
// Start thread-buffered console service
     CConsole.Start () ;

// Load configuration
     if (! GetConfiguration ())
          return false ;
// Process command line arguments and parameters (can override config)
     if (! CommandLine (astrArgs))
          return false ;
// Start listening for incoming http connections
     if (! StartRProxy ())
          return false ;
// Connect to localized server which actually handles the Internet client's requests
     if (! ConnectLocalizedServer ())
          return false ;
// Check if a log file is required
     if (gbfLog)
     {
     // Attempt to create a new log file
          if (! CLog.Open (gstrLogFolder, gstrLogName))
               return false ;
     // Provide CSV column header record
          WriteLogHeader () ;
     }

// Good start
     return true ;
}


//-----------------------------------------------------------------------------
//                                                             GetConfiguration
//-----------------------------------------------------------------------------
public static bool GetConfiguration ()
{
     string    strHttpMethods ;

// Get network settings
     gstrDomains     = ConfigurationManager.AppSettings ["DomainNames"] ;
      strHttpMethods = ConfigurationManager.AppSettings ["HttpMethods"] ;
     gstrLogFolder   = ConfigurationManager.AppSettings ["LogFolder"] ;
     gstrLogName     = ConfigurationManager.AppSettings ["LogName"] ;
     gstrServerIp    = ConfigurationManager.AppSettings ["LocalizedServerIP"] ;
     gstrProxyIp     = ConfigurationManager.AppSettings ["ReverseProxyIP"] ;

// Construct reverse proxy address (this code runs at this location)
     gstrHttp  = "http://"  + gstrProxyIp + ":80/" ;
     gstrHttps = "https://" + gstrProxyIp + ":443/" ;
// Construct base address of localized server
     gstrServer = "http://"  + gstrServerIp + ":80/" ;

// Convert DomainNames into an array of strings
     gastrDomains = gstrDomains.Split (',') ;
     for (var iIndex = 0 ; iIndex < gastrDomains.Length ; iIndex ++ )
          gastrDomains [iIndex] = gastrDomains [iIndex].Trim () ;

// Ensure Http Methods list is book-ended correctly
     gstrHttpMethods = string.Format ("¬{0}¬", strHttpMethods.ToUpper ()) ;

// Use size of ThreadPool for a worker thread
     SetWorkerThreads () ;
     return true ;
}


//-----------------------------------------------------------------------------
//                                                                  CommandLine
//-----------------------------------------------------------------------------
private static bool CommandLine (string[] astrArgs)
{
     bool      bfOkay ;
     int       iIndex ;
     string    strSwitchName ;

     SwitchId  swIdent ;

// Init
     bfOkay = true ;
// Enumerate the command line arguments
     for (iIndex = 0 ; iIndex < astrArgs.Length ; iIndex ++)
     {
     // Isolate each switch name in turn
          strSwitchName = astrArgs [iIndex] ;
     // Ignore switch if it begins with a double hyphen
          if (strSwitchName.StartsWith ("--"))
               continue ;
     // Attempt to identify the switch name
          swIdent = GetSwitchId (strSwitchName) ;
     // Handle the outcome of that attempt
          switch (swIdent)
          {
               case SwitchId.EnableConsole :  gbfConsole = true ;  break ;
               case SwitchId.EnableLog :      gbfLog     = true ;  break ;
               case SwitchId.Wait :           gbfWait    = true ;  break ;

               case SwitchId.Unknown :
                    Console.WriteLine (string.Format ("Command line switch '{0}' is not recognised.", strSwitchName)) ;
                    bfOkay = false ;
                    break ;
          }
     }
     return bfOkay ;
}


//-----------------------------------------------------------------------------
//                                                                  GetSwitchId
//-----------------------------------------------------------------------------
private static SwitchId GetSwitchId (string strName)
{
// Ensure name is in lower case
     strName = strName.ToLower () ;

// Check against known switch names
     if (strName.Equals ("-eco"))
          return SwitchId.EnableConsole ;
     if (strName.Equals ("-elo"))
          return SwitchId.EnableLog ;
     if (strName.Equals ("-wait"))
          return SwitchId.Wait ;

// Switch name is not recognised
     return SwitchId.Unknown ;
}


//-----------------------------------------------------------------------------
//                                                             SetWorkerThreads
//-----------------------------------------------------------------------------
private static void SetWorkerThreads ()
{
     int       iMinWorker ;
     int       iMinIOC ;

     ThreadPool.GetMinThreads (out iMinWorker, out iMinIOC) ;
     ThreadPool.SetMinThreads (4, iMinIOC) ;

     Console.WriteLine ("Using {0} worker threads.", iMinWorker) ;
     Console.WriteLine ("") ;
}


//-----------------------------------------------------------------------------
//                                                                  StartRProxy
//-----------------------------------------------------------------------------
public static bool StartRProxy ()
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
//                                                                   StopRProxy
//-----------------------------------------------------------------------------
public static void StopRProxy ()
{
// Flush unreported console text
     CConsole.WaitEmptyQueue () ;

// Close the listener (Stop Reverse Proxy Engine)
     if (g_internet != null)
          g_internet.Close () ;

// Close the app's log file
     CLog.Close () ;
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
     CConsole.WaitEmptyQueue () ;
     return true ;
}


//-----------------------------------------------------------------------------
//                                                    HandleIncomingConnections
//-----------------------------------------------------------------------------
public static void HandleIncomingConnections ()
{
// Init
     gbfRunServer = true ;
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

     CRequest       c_request ;
     CResponse      c_response ;

     HttpListenerContext      f_context ;
     HttpResponseMessage      f_message ;

// Init
     strLine = "" ;
// Isolate request and response objects
     f_context  = (HttpListenerContext) oContext ;
     c_request  = new CRequest  (f_context.Request) ;
     c_response = new CResponse (f_context.Response) ;
     f_message  = null ;

// Send info to console window without terminating the console line
     if (gbfConsole)
          strLine = DisplayEvent (c_request) ;
// Check if HTTP method is supported
     if (! CheckMethod (c_request.HttpMethod))
     {
          if (gbfConsole)
               strLine += ErrorReflex (c_response, HttpStatusCode.MethodNotAllowed) ;
          goto close_channel ;
     }
// Check if any known Domain Name is provided in client request
     if (! CheckDomains (c_request.UserHostName))
     {
          if (gbfConsole)
               strLine += ErrorReflex (c_response, HttpStatusCode.BadRequest) ;
          goto close_channel ;
     }
// Default to internal error
     iStatus   = (int) HttpStatusCode.InternalServerError ;
     strStatus = "Internal Server Error" ;
// Attempt to create connection to localized server to process redirected requests
     try
     {
     // GET Method
          f_message = await g_server.GetAsync (c_request.PathAndQuery) ;
     // Report the HTTP Statuc Code
          iStatus = ((int) f_message.StatusCode) ;
     }
     catch (Exception e)
     {
          if (gbfConsole)
               strLine += FormatException (e.Message) ;
     }
// Transfer http status code
     c_response.StatusCode = iStatus ;
     if (f_message != null)
          strStatus = f_message.StatusCode.ToString () ;
// Report the HTTP status code
     if (gbfConsole)
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
               c_response.ContentType = null ;
          else
               c_response.ContentType = f_message.Content.Headers.ContentType.ToString () ;
     // Apply default encoding if required
          if (c_response.ContentEncoding == null)
               c_response.ContentEncoding = Encoding.UTF8 ;
     }
// Return data over the Internet to the client
     try
     {
     // Respond to request
          c_response.ContentLength = abResponse.Length ;
          c_response.OutputStream.Write (abResponse, 0, abResponse.Length) ;
     }
     catch (Exception e)
     {
     // Browser may have dumped the connection which is why we try/catch this write
          if (gbfConsole)
               strLine += FormatException (e.Message) ;
          goto close_channel ;
     }

close_channel:

     if (gbfLog)
          WriteLog (c_request, c_response) ;
// Send the response to the client and close the channel
     try
     {
          c_response.OutputStream.Flush () ;
          c_response.OutputStream.Close () ;
          f_context.Response.Close () ;
     }
     catch (Exception ex)
     {
          CConsole.WriteLine (FormatException (ex.Message)) ;
     }
// Queue line to console
     if (gbfConsole)
     {
          CConsole.WriteLine (strLine) ;
          CConsole.WaitEmptyQueue () ;
     }
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
public static bool CheckDomains (string strHostName)
{
// Reject if no hostname supplied
     if (strHostName == null)
          return false ;

// Accept is client-supplied domain name is recognised on white list
     for (var iIndex = 0 ; iIndex < gastrDomains.Length ; iIndex ++ )
     {
          if (strHostName.Equals (gastrDomains [iIndex]))
               return true ;
     }
// Reject all other domain requests
     return false ;
}


//-----------------------------------------------------------------------------
//                                                                  CheckMethod
//-----------------------------------------------------------------------------
/*
 *   Known HTTP request methods are:
 *
 *        CONNECT, DELETE, GET, HEAD, OPTIONS, PATCH, POST, PUT, TRACE.
 *
 *   Currently, only GET is supported by RPEngine's ServiceThread(). However,
 *   in the near future we plan to support POST. You will be able to limit the
 *   supported methods for your installation by changing the "HttpMethods"
 *   list in the app's config file. For example:
 *
 *        <add key="HttpMethods" value="GET,POST" />
 *
 *   The string comparison is case-insensitive as all characters will be
 *   converted to uppercase prior to matching.
 */
public static bool CheckMethod (string strMethod)
{
// Approve only these methods
     return (gstrHttpMethods.Contains (strMethod.ToUpper ())) ;
}


//-----------------------------------------------------------------------------
//                                                                  ErrorReflex
//-----------------------------------------------------------------------------
/*
 *   ErrorReflex() responds directly to the client with an error page that is
 *   preloaded. This reduces the load on the system as the localized server
 *   is not involved.
 */
public static string ErrorReflex (CResponse response, HttpStatusCode status)
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
          strLine += FormatException (e.Message) ;
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
public static string DisplayEvent (CRequest c_request)
{
     string    strLine ;

     DateTime  dtNow = DateTime.Now ;

// Get system timestamp
     string strDate = string.Format ("{0:0000}-{1:00}-{2:00}", dtNow.Year, dtNow.Month, dtNow.Day) ;
     string strTime = string.Format ("{0:00}:{1:00}:{2:00}:{3:000}", dtNow.Hour, dtNow.Minute, dtNow.Second, dtNow.Millisecond) ;
// Display datestamp in sortable format
     strLine  = string.Format ("{0}{1}{2} {3}  ", ConsoleColour.BgDarkBlue, ConsoleColour.FgGray, strDate, strTime) ;
// Report IP address
     strLine += string.Format ("{0}{1} ", ConsoleColour.FgWhite, c_request.IpAddress.PadRight (15)) ;
// Determine colour based on the client request
     strLine += GetElementColour (c_request.LocalPath) ;
// Report remote user accessing this proxy server
     strLine += string.Format ("{0} ", c_request.HttpMethod) ;
     strLine += string.Format ("{0} ", c_request.Url) ;
// Report possible browser or bot identifier
     strLine += string.Format ("{0}({1}) ", ConsoleColour.FgGray, c_request.UserAgent) ;
// Revert console to default foreground colour
     strLine += ConsoleColour.FgWhite ;
     return strLine ;
}


//-----------------------------------------------------------------------------
//                                                             GetElementColour
//-----------------------------------------------------------------------------
private static string GetElementColour (string strLocalPath)
{
// Determine colour based on the path and filename extension code
     if (strLocalPath.Equals ("/"))
          return ConsoleColour.FgYellow ;
     if (strLocalPath.Contains (".html"))
          return ConsoleColour.FgYellow ;
     if (strLocalPath.Contains (".js"))
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

// Set default mini-web page
     strPage  = string.Format ("<p>Failed to preload <b>{0}</b> from localized server.</p>", strUri) ;
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
          strStatus = string.Format ("{0}{1}{2}", ConsoleColour.FgRed + ConsoleColour.BgBlack, e.Message, ConsoleColour.FgWhite + ConsoleColour.BgDarkBlue) ;
     }
// Report action and outcome
     CConsole.WriteLine (string.Format ("{0}Preload \"{1}\" ({2} Bytes) - {3}", ConsoleColour.FgYellow, strQuery, strPage.Length, strStatus)) ;
// Convert to array of bytes - ready to be transmitted
     return Encoding.ASCII.GetBytes (strPage) ;
}


//-----------------------------------------------------------------------------
//                                                              FormatException
//-----------------------------------------------------------------------------
public static string FormatException (string strMessage)
{
     return string.Format ("{0}*** {1}\n{2}", ConsoleColour.FgRed, strMessage, ConsoleColour.FgWhite) ;
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
     string    strDate ;
     string    strTime ;

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
     strDate = string.Format ("{0:00}{1:00}{2:00}", iYear, writetime.Month, writetime.Day) ;
     strTime = string.Format ("{0:00}{1:00}{2:00}", writetime.Hour, writetime.Minute, writetime.Second) ;
// Construct version number with build date and time     
     return string.Format ("{0}.{1}.{2}.{3}", version.Major, version.Minor, strDate, strTime) ;
}


//-----------------------------------------------------------------------------
//                                                               WriteLogHeader
//-----------------------------------------------------------------------------
public static void WriteLogHeader ()
{
// Output request details in CSV record
     CLog.WriteCsvField ("[REQ]") ;
     CLog.WriteCsvField ("LogDate") ;
     CLog.WriteCsvField ("LogTime") ;
     CLog.WriteCsvField ("IpAddress") ;
     CLog.WriteCsvField ("HttpMethod") ;
     CLog.WriteCsvField ("Url") ;
     CLog.WriteCsvField ("UserAgent") ;
     CLog.WriteCsvField ("HeaderCount") ;
     CLog.WriteCsvField ("Headers") ;
     CLog.WriteCsvField ("HdrHost") ;
     CLog.WriteCsvField ("HdrReferrer") ;
     CLog.WriteCsvField ("HdrContent") ;
     CLog.WriteCsvField ("HdrAccept") ;
     CLog.WriteCsvField ("HdrSecFetch") ;
     CLog.WriteCsvField ("HdrDeprecated") ;
     CLog.WriteCsvField ("IsAuthenticated") ;
     CLog.WriteCsvField ("IsLocal") ;
     CLog.WriteCsvField ("IsSecure") ;
     CLog.WriteCsvField ("EncBodyName") ;
     CLog.WriteCsvField ("EncHdrName") ;
     CLog.WriteCsvField ("EncName") ;
     CLog.WriteCsvField ("EncWebName") ;

// Output response details in CSV record
     CLog.WriteCsvField ("[RESP]") ;
     CLog.WriteCsvField ("StatusCode") ;
     CLog.WriteCsvField ("ContentType") ;
     CLog.WriteCsvField ("EncodingName") ;
     CLog.WriteCsvField ("ContentLength") ;
// Terminate record and flush buffers
     CLog.WriteCsvEnd ("[END]") ;
}


//-----------------------------------------------------------------------------
//                                                                     WriteLog
//-----------------------------------------------------------------------------
public static void WriteLog (CRequest c_request, CResponse c_response)
{
// Critical section
     lock (g_lockLog)
     {
     // Output request details in CSV record
          CLog.WriteCsvField ("[REQ]") ;
          CLog.WriteCsvField (c_request.LogDate) ;
          CLog.WriteCsvField (c_request.LogTime) ;
          CLog.WriteCsvField (c_request.IpAddress) ;
          CLog.WriteCsvField (c_request.HttpMethod) ;
          CLog.WriteCsvField (c_request.Url) ;
          CLog.WriteCsvField (c_request.UserAgent) ;
          CLog.WriteCsvField (c_request.HeaderCount.ToString ()) ;
          CLog.WriteCsvField (c_request.Headers) ;
          CLog.WriteCsvField (c_request.HdrHost) ;
          CLog.WriteCsvField (c_request.HdrReferrer) ;
          CLog.WriteCsvField (c_request.HdrContent) ;
          CLog.WriteCsvField (c_request.HdrAccept) ;
          CLog.WriteCsvField (c_request.HdrSecFetch) ;
          CLog.WriteCsvField (c_request.HdrDeprecated) ;
          CLog.WriteCsvField (c_request.IsAuthenticated.ToString ()) ;
          CLog.WriteCsvField (c_request.IsLocal.ToString ()) ;
          CLog.WriteCsvField (c_request.IsSecure.ToString ()) ;
          CLog.WriteCsvField (c_request.EncBodyName) ;
          CLog.WriteCsvField (c_request.EncHdrName) ;
          CLog.WriteCsvField (c_request.EncName) ;
          CLog.WriteCsvField (c_request.EncWebName) ;

     // Output response details in CSV record
          CLog.WriteCsvField ("[RESP]") ;
          CLog.WriteCsvField (c_response.StatusCode.ToString ()) ;
          CLog.WriteCsvField (c_response.ContentType) ;
          CLog.WriteCsvField (c_response.EncodingName) ;
          CLog.WriteCsvField (c_response.ContentLength.ToString ()) ;
     // Terminate record and flush buffers
          CLog.WriteCsvEnd ("[END]") ;
     }
}


//*****************************************************************************
}
//*****************************************************************************
}
