//*****************************************************************************
//
//   RPEngine                                                              CLog
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
 *   21-12-21  Started development.
 *   22-12-21  Extended Open() to report failure to create log file.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//*****************************************************************************
//                                                                     RPEngine
//*****************************************************************************
namespace RPEngine
{


//=============================================================================
//                                                                         CLog
//-----------------------------------------------------------------------------
static class CLog
{

//----------------------------------------------------------- Public Properties

     public static long       Count     { get { return m_lCount ; } }

     public static Exception  Exception { get { return m_ex ; } }

//------------------------------------------------------------- Private Members

     private static long           m_lCount ;

     private static Exception      m_ex ;

     private static FileStream     m_fsLog ;

     private static StreamWriter   m_swLog ;


//-----------------------------------------------------------------------------
//                                                                         Open
//-----------------------------------------------------------------------------
/*
 *   Open() constructs the output log file's specification which includs a
 *   date and time mark giving the system time at which the log file was opened.
 *   The file is opened as a sequential output stream.
 *
 *   Example:
 *
 *        D:\Programify\GitHub\RPEngine\Logs\20211222_140434_rpengine.log
 */
public static bool Open (string strFolder, string strName)
{
     bool      bfOkay ;
     string    strDate ;
     string    strFilespec ;
     string    strLine ;
     string    strTime ;

// Construct timestamp elements
     DateTime  dtNow = DateTime.Now ;

// Get system timestamp
     strDate = string.Format ("{0:0000}{1:00}{2:00}", dtNow.Year, dtNow.Month, dtNow.Day) ;
     strTime = string.Format ("{0:00}{1:00}{2:00}", dtNow.Hour, dtNow.Minute, dtNow.Second) ;
// Construct fully qualified filespec
     strFilespec = string.Format ("{0}{1}_{2}_{3}", strFolder, strDate, strTime, strName) ;

// Init
     bfOkay = false ;
// Create the log file
     try
     {
          m_fsLog = new FileStream (strFilespec, FileMode.Create, FileAccess.Write, FileShare.Read) ;
     }
     catch (Exception ex)
     {
          m_ex = ex ;
          goto exit_function ;
     }

// Create output stream to log file
     try
     {
          m_swLog = new StreamWriter (m_fsLog, ASCIIEncoding.UTF8) ;
     }
     catch (Exception ex)
     {
          m_ex = ex ;
          goto exit_function ;
     }
// Success
     bfOkay = true ;

exit_function:

// Check if failed
     if (! bfOkay)
     {
          strLine = string.Format ("{0}*** Log Folder : \"{1}\"", ConsoleColour.FgRed, strFolder) ;
          CConsole.WriteLine (strLine) ;

          strLine = string.Format ("{0}*** Log Name   : \"{1}\"", ConsoleColour.FgRed, strName) ;
          CConsole.WriteLine (strLine) ;

          strLine = string.Format ("{0}*** Exception  : {1}", ConsoleColour.FgRed, CLog.m_ex.Message) ;
          CConsole.WriteLine (strLine) ;
     }
     return bfOkay ;
}


//-----------------------------------------------------------------------------
//                                                                        Close
//-----------------------------------------------------------------------------
public static void Close ()
{
// Close the output stream and file
     if (m_swLog != null)
          m_swLog.Close () ;
     if (m_fsLog != null)
          m_fsLog.Close () ;
}


//-----------------------------------------------------------------------------
//                                                                WriteCsvField
//-----------------------------------------------------------------------------
public static void WriteCsvField (string strField)
{
     m_swLog.Write (strField) ;
     m_swLog.Write (",") ;
}


//-----------------------------------------------------------------------------
//                                                                  WriteCsvEnd
//-----------------------------------------------------------------------------
public static void WriteCsvEnd (string strField)
{
// Write last field
     m_swLog.WriteLine (strField) ;
     m_swLog.Flush () ;
// Increment record count
     m_lCount ++ ;
}


//*****************************************************************************
}
//*****************************************************************************
}
