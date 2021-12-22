//*****************************************************************************
//
//   RPEngine                                                          CConsole
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
 *   18-12-21  Started development.
 *   20-12-21  Improved OutputLine() to simplify machine state code.
 *   22-12-21  Installed WaitKeyDown().
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


//*****************************************************************************
//                                                                     RPEngine
//*****************************************************************************
namespace RPEngine
{


public static class ConsoleColour
{
     public static string BgBlack       { get { return "¬B00" ; } }
     public static string BgDarkBlue    { get { return "¬B01" ; } }
     public static string BgDarkGreen   { get { return "¬B02" ; } }
     public static string BgDarkCyan    { get { return "¬B03" ; } }
     public static string BgDarkRed     { get { return "¬B04" ; } }
     public static string BgDarkMagenta { get { return "¬B05" ; } }
     public static string BgDarkYellow  { get { return "¬B06" ; } }
     public static string BgGray        { get { return "¬B07" ; } }
     public static string BgDarkGray    { get { return "¬B08" ; } }
     public static string BgBlue        { get { return "¬B09" ; } }
     public static string BgGreen       { get { return "¬B10" ; } }
     public static string BgCyan        { get { return "¬B11" ; } }
     public static string BgRed         { get { return "¬B12" ; } }
     public static string BgMagenta     { get { return "¬B13" ; } }
     public static string BgYellow      { get { return "¬B14" ; } }
     public static string BgWhite       { get { return "¬B15" ; } }

     public static string FgBlack       { get { return "¬F00" ; } }
     public static string FgDarkBlue    { get { return "¬F01" ; } }
     public static string FgDarkGreen   { get { return "¬F02" ; } }
     public static string FgDarkCyan    { get { return "¬F03" ; } }
     public static string FgDarkRed     { get { return "¬F04" ; } }
     public static string FgDarkMagenta { get { return "¬F05" ; } }
     public static string FgDarkYellow  { get { return "¬F06" ; } }
     public static string FgGray        { get { return "¬F07" ; } }
     public static string FgDarkGray    { get { return "¬F08" ; } }
     public static string FgBlue        { get { return "¬F09" ; } }
     public static string FgGreen       { get { return "¬F10" ; } }
     public static string FgCyan        { get { return "¬F11" ; } }
     public static string FgRed         { get { return "¬F12" ; } }
     public static string FgMagenta     { get { return "¬F13" ; } }
     public static string FgYellow      { get { return "¬F14" ; } }
     public static string FgWhite       { get { return "¬F15" ; } }
}


//=============================================================================
//                                                                     CConsole
//-----------------------------------------------------------------------------
static class CConsole
{
     private static bool      m_bfRunService ;

     private static Queue<string>  m_queue ;  // Queued output to the shared system console resource.

     private static Thread         m_thread ;


//=============================================================================
//                                                                     CConsole
//-----------------------------------------------------------------------------
static CConsole ()
{
     m_queue = new Queue<string> () ;
     m_queue.Clear () ;
}


//-----------------------------------------------------------------------------
//                                                                        Start
//-----------------------------------------------------------------------------
public static void Start ()
{
     m_bfRunService = true ;
     m_thread = new Thread (new ThreadStart (ServiceThread)) ;
     m_thread.Start () ;
}


//-----------------------------------------------------------------------------
//                                                                ServiceThread
//-----------------------------------------------------------------------------
private static void ServiceThread ()
{
     string    strLine ;

// Start queue listening thread
     while (m_bfRunService)
     {
     // Check if any queued lines are waiting to be displayed in console window
          if (m_queue.Count > 0)
          {
               strLine = m_queue.Dequeue () ;
               OutputLine (strLine) ;
          }
          else
               Thread.Sleep (50) ;
     }
}


//-----------------------------------------------------------------------------
//                                                                         Stop
//-----------------------------------------------------------------------------
public static void Stop ()
{
     m_bfRunService = false ;
}


//-----------------------------------------------------------------------------
//                                                               WaitEmptyQueue
//-----------------------------------------------------------------------------
public static void WaitEmptyQueue ()
{
// Wait until no more lines are waiting for output
     while (m_queue.Count > 0)
     {
     // Exit if the service has been stopped (by another thread)
          if (! m_bfRunService)
               return ;
     // Release CPU time
          Thread.Sleep (50) ;
     }
}


//-----------------------------------------------------------------------------
//                                                                  WaitKeyDown
//-----------------------------------------------------------------------------
/*
 *   WaitKeyDown() waits for the specified Unicode character key to be pressed.
 */
public static void WaitKeyDown (char cTarget)
{
// Loop forever until a key is pressed
     while (true)
     {
          ConsoleKeyInfo cki = Console.ReadKey (true) ;

     // Check if a console key has been pressed
          if (cki.KeyChar == cTarget)
               return ;

     // Release CPU time
          Thread.Sleep (50) ;
     }
}


//-----------------------------------------------------------------------------
//                                                                GetColourCode
//-----------------------------------------------------------------------------
public static string GetColourCode (ConsoleColor color, bool bfForeground)
{
     string    strCode ;
     string    strPrefix ;

     if (bfForeground)
          strPrefix = "¬F" ;
     else
          strPrefix = "¬B" ;

     strCode = string.Format ("{0}{1:00}", strPrefix, (int) color) ;

     return strCode ;
}


//-----------------------------------------------------------------------------
//                                                                   OutputLine
//-----------------------------------------------------------------------------
private static void OutputLine (string strEntry)
{
     bool      bfBack ;
     bool      bfFore ;

     int       iColour ;
     int       iSequence ;

// Ignore null entries
     if (strEntry == null)
          return ;

// Init
     bfBack    = false ;
     bfFore    = false ;
     iColour   = 0 ;
     iSequence = 0 ;
// Convert string to simple byte array
     char[] aChars = strEntry.ToCharArray () ; // Encoding.ASCII.GetChars (strEntry) ;
// Decode embedded colour controls and regular text
     foreach (char cChar in aChars)
     {
     // Check if encountered escape character
          if (cChar == '¬')
               iSequence = 1 ;
     // Escape sequence state machine
          switch (iSequence)
          {
               case 0 :
               // Write regular character to console
                    Console.Write ((char) cChar) ;
                    break ;

               case 1 :
                    iSequence ++ ;
                    break ;

               case 2 :
                    bfBack = (cChar == 'B') ;
                    bfFore = (cChar == 'F') ;
                    iSequence ++ ;
                    break ;

               case 3 :
                    iColour = (int) cChar - 48 ;
                    iSequence ++ ;
                    break ;

               case 4 :
                    iColour = (iColour * 10) + ((int) cChar - 48) ;
               // Output colour code
                    SetColour (bfFore, bfBack, iColour) ;
               // Reset embedded colour trap
                    iSequence = 0 ;
                    break ;
          }
     }
     Console.WriteLine () ;
}


//-----------------------------------------------------------------------------
//                                                                    SetColour
//-----------------------------------------------------------------------------
private static void SetColour (bool bfFore, bool bfBack, int iColour)
{
     if (bfBack)
          Console.BackgroundColor = (ConsoleColor) iColour ;
     if (bfFore)
          Console.ForegroundColor = (ConsoleColor) iColour ;
}


//-----------------------------------------------------------------------------
//                                                                    WriteLine
//-----------------------------------------------------------------------------
public static void WriteLine (string strText)
{
     m_queue.Enqueue (strText) ;
}


//*****************************************************************************
}
//*****************************************************************************
}
