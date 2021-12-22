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

     public DateTime     DateTime     { get { return m_dtLog ; } }

     public int          HeaderCount  { get { return m_iHeaderCount ; } }

     public string       HttpMethod   { get { return m_strHttpMethod ;   } }
     public string       IpAddress    { get { return m_strIpAddr ;       } }
     public string       LocalPath    { get { return m_strLocalPath ;    } }
     public string       LogDate      { get { return m_strLogDate ;      } }
     public string       LogTime      { get { return m_strLogTime ;      } }
     public string       PathAndQuery { get { return m_strPathAndQuery ; } }
     public string       Url          { get { return m_strUrl ;          } }
     public string       UserAgent    { get { return m_strUserAgent ;    } }
     public string       UserHostName { get { return m_strHostName ;     } }

//------------------------------------------------------------- Private Members

     private bool        m_bfIsAuthenticated ;
     private bool        m_bfIsLocal ;
     private bool        m_bfIsSecure ;

     private DateTime    m_dtLog ;

     private int         m_iHeaderCount ;

     private string      m_strHttpMethod ;
     private string      m_strIpAddr ;
     private string      m_strLocalPath ;
     private string      m_strLogDate ;
     private string      m_strLogTime ;
     private string      m_strPathAndQuery ;
     private string      m_strUrl ;
     private string      m_strUserAgent ;
     private string      m_strHostName ;


//=============================================================================
//                                                                     CRequest
//=============================================================================
public CRequest (HttpListenerRequest f_request)
{
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
     
// Get system timestamp
     m_strLogDate = string.Format ("{0:0000}-{1:00}-{2:00}",       m_dtLog.Year, m_dtLog.Month, m_dtLog.Day) ;
     m_strLogTime = string.Format ("{0:00}:{1:00}:{2:00}:{3:000}", m_dtLog.Hour, m_dtLog.Minute, m_dtLog.Second, m_dtLog.Millisecond) ;
}



//*****************************************************************************
}
//*****************************************************************************
}
