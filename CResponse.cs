//*****************************************************************************
//
//   RPEngine                                                         CResponse
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
using System.IO;
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
//                                                                    CResponse
//-----------------------------------------------------------------------------
class CResponse
{

//----------------------------------------------------------- Public Properties

     public Encoding     ContentEncoding     { get { return m_encContent ; }  set { m_encContent = value ; } }

     public int          StatusCode          { get { return m_iStatusCode ; }  set { m_iStatusCode = value ; } }

     public long         ContentLength       { get { return m_lContentLength ; }  set { m_lContentLength = value ; } }

     public Stream       OutputStream        { get { return m_streamOutput ; } }

     public string       ContentType         { get { return m_strContentType ; }   set { m_strContentType = value ; } }
     public string       EncodingName        { get { return m_strEndodingName ; } }


//------------------------------------------------------------- Private Members

     private Encoding    m_encContent ;

     private int         m_iStatusCode ;

     private long        m_lContentLength ;

     private Stream      m_streamOutput ;

     private string      m_strContentType ;
     private string      m_strEndodingName ;


//=============================================================================
//                                                                     CRequest
//=============================================================================
public CResponse (HttpListenerResponse f_response)
{
     m_iStatusCode     = f_response.StatusCode ;
     m_streamOutput    = f_response.OutputStream ;
     if (f_response.ContentEncoding == null)
          m_strEndodingName = "" ;
     else
          m_strEndodingName = f_response.ContentEncoding.EncodingName ;
     m_strContentType  = f_response.ContentType ;
     m_encContent      = f_response.ContentEncoding ;
     m_lContentLength  = f_response.ContentLength64 ;
}


//*****************************************************************************
}
//*****************************************************************************
}
