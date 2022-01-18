//*****************************************************************************
//
//   RPEngine                                                          CRequest
//   Open Source Edition
//
//*****************************************************************************
/*
 *   Reverse Proxy Engine.
 *
 *   (c) Copyright 2021, Programify Ltd.
 *   All commercialization rights reserved.
 */


//*****************************************************************************
//                                                                  Development
//*****************************************************************************
/*
 *   22-12-21  Started development.
 *   25-12-21  Included Encoding name fields.
 *             Installed code to generate a multisegmented request headers
 *             string and its property 'Headers'. The rarely used negation
 *             character '¬' is used as the field (segment) separator. The 
 *             first encountered ':' colon is used to introduce the value of
 *             the key.
 *   26-12-21  Installed GetHeaderId() and enabled module to related HTTP
 *             headers into groups.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


//*****************************************************************************
//                                                                     RPEngine
//*****************************************************************************
namespace RPEngine
{


//=============================================================================
//                                                                     CRequest
//-----------------------------------------------------------------------------
class CRequest
{

//----------------------------------------------------------- Public Properties

     public bool         IsAuthenticated { get { return m_bfIsAuthenticated ; } }
     public bool         IsLocal         { get { return m_bfIsLocal ;         } }
     public bool         IsSecure        { get { return m_bfIsSecure ;        } }

     public DateTime     DateTime      { get { return m_dtLog ; } }

     public int          HeaderCount   { get { return m_iHeaderCount ; } }

     public string       EncBodyName   { get { return m_strEncBodyName ;   } }
     public string       EncHdrName    { get { return m_strEncHdrName ;    } }
     public string       EncName       { get { return m_strEncName ;       } }
     public string       EncWebName    { get { return m_strEncWebName ;    } }
     public string       HdrAccept     { get { return m_strHdrAccept ;     } }
     public string       HdrContent    { get { return m_strHdrContent ;    } }
     public string       HdrDeprecated { get { return m_strHdrDeprecated ; } }
     public string       HdrHost       { get { return m_strHdrHost ;       } }
     public string       HdrReferrer   { get { return m_strHdrReferrer ;   } }
     public string       HdrSecFetch   { get { return m_strHdrSecFetch ;   } }
     public string       Headers       { get { return m_strHeaders ;       } }
     public string       HttpMethod    { get { return m_strHttpMethod ;    } }
     public string       IpAddress     { get { return m_strIpAddr ;        } }
     public string       LocalPath     { get { return m_strLocalPath ;     } }
     public string       LogDate       { get { return m_strLogDate ;       } }
     public string       LogTime       { get { return m_strLogTime ;       } }
     public string       PathAndQuery  { get { return m_strPathAndQuery ;  } }
     public string       Url           { get { return m_strUrl ;           } }
     public string       UserAgent     { get { return m_strUserAgent ;     } }
     public string       UserHostName  { get { return m_strHostName ;      } }

//------------------------------------------------------------- Private Members

     private bool        m_bfIsAuthenticated ;
     private bool        m_bfIsLocal ;
     private bool        m_bfIsSecure ;

     private DateTime    m_dtLog ;

     private int         m_iHeaderCount ;

     private string      m_strEncBodyName ;
     private string      m_strEncHdrName ;
     private string      m_strEncName ;
     private string      m_strEncWebName ;
     private string      m_strHdrAccept ;
     private string      m_strHdrContent ;
     private string      m_strHdrDeprecated ;
     private string      m_strHdrHost ;
     private string      m_strHdrReferrer ;
     private string      m_strHdrSecFetch ;
     private string      m_strHeaders ;
     private string      m_strHttpMethod ;
     private string      m_strIpAddr ;
     private string      m_strLocalPath ;
     private string      m_strLogDate ;
     private string      m_strLogTime ;
     private string      m_strPathAndQuery ;
     private string      m_strUrl ;
     private string      m_strUserAgent ;
     private string      m_strHostName ;

//--------------------------------------------------------------- Private Enums

private enum HeaderId
{
     Unknown         = 0,
     Accept          = 1,
     AcceptCharset   = 2,
     AcceptEncoding  = 3,
     AcceptLanguage  = 4,
     ContentEncoding = 5,
     ContentLanguage = 6,
     ContentLength   = 7,
     ContentLocation = 8,
     ContentType     = 9,
     DNT             = 10,
     From            = 11,
     Host            = 12,
     Referer         = 13,
     SecFetchSite    = 14,
     SecFetchMode    = 15,
     SecFetchUser    = 16,
     SecFetchDest    = 17
}


//=============================================================================
//                                                                     CRequest
//=============================================================================
public CRequest (HttpListenerRequest f_request)
{
     string    strValue ;

// Transcribe request fields to stable CRequest store
     m_bfIsAuthenticated = f_request.IsAuthenticated ;
     m_bfIsLocal         = f_request.IsLocal ;
     m_bfIsSecure        = f_request.IsSecureConnection ;
     m_dtLog             = DateTime.Now ;
     m_iHeaderCount      = f_request.Headers.Count ;
     m_strHttpMethod     = f_request.HttpMethod ;
     m_strIpAddr         = f_request.RemoteEndPoint.Address.ToString () ;
     m_strPathAndQuery   = f_request.Url.PathAndQuery ;
     m_strUrl            = f_request.Url.ToString () ;
     m_strUserAgent      = f_request.UserAgent ;
     m_strHostName       = f_request.UserHostName ;
     m_strLocalPath      = f_request.Url.LocalPath ;
     m_strEncBodyName    = f_request.ContentEncoding.BodyName ;
     m_strEncHdrName     = f_request.ContentEncoding.HeaderName ;
     m_strEncName        = f_request.ContentEncoding.EncodingName ;
     m_strEncWebName     = f_request.ContentEncoding.WebName ;
    
// Get system timestamp
     m_strLogDate = string.Format ("{0:0000}-{1:00}-{2:00}",       m_dtLog.Year, m_dtLog.Month, m_dtLog.Day) ;
     m_strLogTime = string.Format ("{0:00}:{1:00}:{2:00}:{3:000}", m_dtLog.Hour, m_dtLog.Minute, m_dtLog.Second, m_dtLog.Millisecond) ;

// Build multiple headers string
     m_strHeaders       = "" ;
     m_strHdrAccept     = "" ;
     m_strHdrContent    = "" ;
     m_strHdrDeprecated = "" ;
     m_strHdrHost       = "" ;
     m_strHdrReferrer   = "" ;
     m_strHdrSecFetch   = "" ;
     foreach (string strKey in f_request.Headers)
     {
     // Ignore User-Agent
          if (strKey.Equals ("User-Agent", StringComparison.InvariantCultureIgnoreCase))
               continue ;
     // Extract header value
          strValue = f_request.Headers [strKey] ;
     // Remove any existing separators
          strValue = strValue.Replace ('¬', ' ') ;

          switch (GetHeaderId (strKey))
          {
               case HeaderId.Accept :
               case HeaderId.AcceptCharset :
               case HeaderId.AcceptEncoding :
               case HeaderId.AcceptLanguage :
                    if (m_strHdrAccept.Length > 0)
                         m_strHdrAccept += string.Format ("¬{0}:{1}", strKey, strValue) ;
                    else
                         m_strHdrAccept += string.Format ("{0}:{1}", strKey, strValue) ;
                    break ;

               case HeaderId.ContentEncoding :
               case HeaderId.ContentLanguage :
               case HeaderId.ContentLength :
               case HeaderId.ContentLocation :
               case HeaderId.ContentType :
                    if (m_strHdrContent.Length > 0)
                         m_strHdrContent += string.Format ("¬{0}:{1}", strKey, strValue) ;
                    else
                         m_strHdrContent += string.Format ("{0}:{1}", strKey, strValue) ;
                    break ;

               case HeaderId.DNT :
               // Build list of deprecated headers
                    if (m_strHdrDeprecated.Length > 0)
                         m_strHdrDeprecated += string.Format ("¬{0}:{1}", strKey, strValue) ;
                    else
                         m_strHdrDeprecated += string.Format ("{0}:{1}", strKey, strValue) ;
                    break ;

               case HeaderId.Host :
                    if (m_strHdrHost.Length > 0)
                         m_strHdrHost += string.Format ("¬{0}:{1}", strKey, strValue) ;
                    else
                         m_strHdrHost += string.Format ("{0}:{1}", strKey, strValue) ;
                    break ;

               case HeaderId.From :
               case HeaderId.Referer :
                    if (m_strHdrReferrer.Length > 0)
                         m_strHdrReferrer += string.Format ("¬{0}:{1}", strKey, strValue) ;
                    else
                         m_strHdrReferrer += string.Format ("{0}:{1}", strKey, strValue) ;
                    break ;

               case HeaderId.SecFetchSite :
               case HeaderId.SecFetchMode :
               case HeaderId.SecFetchUser :
               case HeaderId.SecFetchDest :
                    if (m_strHdrSecFetch.Length > 0)
                         m_strHdrSecFetch += string.Format ("¬{0}:{1}", strKey, strValue) ;
                    else
                         m_strHdrSecFetch += string.Format ("{0}:{1}", strKey, strValue) ;
                    break ;

               default :
               // Construct list of uncategorized headers
                    if (m_strHeaders.Length > 0)
                         m_strHeaders += string.Format ("¬{0}:{1}", strKey, strValue) ;
                    else
                         m_strHeaders += string.Format ("{0}:{1}", strKey, strValue) ;
                    break ;
          }
     }
}


//-----------------------------------------------------------------------------
//                                                                  GetHeaderId
//-----------------------------------------------------------------------------
private HeaderId GetHeaderId (string strKeyName)
{
     char[]   acName ;

     strKeyName = strKeyName.ToUpper () ;
     acName = strKeyName.ToCharArray () ;
     switch (acName [0])
     {
          case 'A' :
               if (strKeyName.Equals ("ACCEPT"))
                    return HeaderId.Accept ;
               if (strKeyName.StartsWith ("ACCEPT-"))
               {
                    switch (acName [7])
                    {
                         case 'C' : return HeaderId.AcceptCharset ;
                         case 'E' : return HeaderId.AcceptEncoding ;
                         case 'L' : return HeaderId.AcceptLanguage ;
                    }
               }
               break ;

          case 'C' :
               if (strKeyName.StartsWith ("CONTENT-"))
               {
                    switch (acName [9])
                    {
                         case 'N' : return HeaderId.ContentEncoding ;
                         case 'A' : return HeaderId.ContentLanguage ;
                         case 'E' : return HeaderId.ContentLength ;
                         case 'O' : return HeaderId.ContentLocation ;
                         case 'Y' : return HeaderId.ContentType ;
                    }
               }
               break ;

          case 'D' :
               if (strKeyName.Equals ("DNT"))
                    return HeaderId.DNT ;
               break ;

          case 'F' :
               if (strKeyName.Equals ("FROM"))
                    return HeaderId.From ;
               break ;

          case 'H' :
               if (strKeyName.Equals ("HOST"))
                    return HeaderId.Host ;
               break ;

          case 'R' :
               if (strKeyName.Equals ("REFERER"))
                    return HeaderId.Referer ;
               break ;

          case 'S' :
               if (strKeyName.StartsWith ("SEC-FETCH-"))
               {
                    switch (acName [10])
                    {
                         case 'S' : return HeaderId.SecFetchSite ;
                         case 'M' : return HeaderId.SecFetchMode ;
                         case 'U' : return HeaderId.SecFetchUser ;
                         case 'D' : return HeaderId.SecFetchDest ;
                    }
               }
               break ;
     }
     return HeaderId.Unknown ;
}


//*****************************************************************************
}
//*****************************************************************************
}
