//*****************************************************************************
//
//                                                                     RPEngine
//
//*****************************************************************************
/*
 *   CONTENTS
 *
 *   1.   Make and deploy a self-certified SSL certificate
 *   2.   Generate PFX file from KEY and CRT.
 */


//*****************************************************************************
//                             Make and deploy a self-certified SSL certificate
//*****************************************************************************
/*
 *   1.   Locate the latest Windows Developer's Kit, e.g.:
 *
 *        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\"
 *
 *   2.   Open Command Prompt with Administrator Privileges.
 *
 *   3.   Create a Certificate Authority. E.g.:
 *
 *        C:
 *        CD C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\
 *        makecert  -n "CN=ProgramifyCA"  -r  -sv D:\Apache24\ProgramifyCA.pvk  D:\Apache24\ProgramifyCA.cer
 *
 *             (Prompted to Create Private Key Password)
 *             Enter complex password, e.g.: 058d3d24-d1ac-4486-95ab-f32e27a4d6e9-ALPHA
 *
 *             (Prompted to Enter Key Password) - again.
 *
 *   4.   Use the CA certificate to generate an SSL Certificate:
 *
 *        makecert -sk D:\Apache24\ProgramifySSL -iv D:\Apache24\ProgramifyCA.pvk -n "CN=ProgramifySSL" -ic D:\Apache24\ProgramifyCA.cer D:\Apache24\ProgramifySSL.cer -sr localmachine -ss MY
 *
 *             (Prompted to Enter Private Key Password)
 *             Enter same complex password, e.g.: 058d3d24-d1ac-4486-95ab-f32e27a4d6e9-ALPHA
 *
 *   5.   Install ProgramifyCA certificate in your machine's Trusted Authority store.
 *
 *             Right-click "ProgramifyCA.cer" in Windows Explorer and select Install Certificate.
 *             The "Certificate Import Wizard" should appear.
 *             Set the Store Location to Local Machine.
 *             Click Next.
 *             Certificate Store: Select "Place all certificates in the following store"
 *             Click the Browse button and from the tree list, select "Trusted Root Certification Authorities".
 *             Click Next.
 *             Click Finish to import the CA certificate into the Trusted Root CA store.
 *             Click OK (asuming it finished successfully).
 *
 *   6.   Install ProgramifySSL certificate in your machine's Personal store.
 *
 *             Right-click "ProgramifySSL.cer" in Windows Explorer and select Install Certificate.
 *             The "Certificate Import Wizard" should appear.
 *             Set the Store Location to Local Machine.
 *             Click Next.
 *             Certificate Store: Select "Place all certificates in the following store"
 *             Click the Browse button and from the tree list, select "Personal".
 *             Click Next.
 *             Click Finish to import the SSL certificate into the Personal store.
 *             Click OK (asuming it finished successfully).
 *
 *   7.   Get the SSL certificate's thumbprint.
 *
 *             Run the MMC application (Windows key + R and type MMC).
 *             File / Add or Remove Snap-ins [Ctrl+M].
 *             In the left pane select "Certificates" and click on the Add button.
 *             Select the "Computer account" and click Finish.
 *             Double click "Certificate (Local Computer)".
 *             Double click "Personal".
 *             Double click "Certificates".
 *             Double click "ProgramifySSL" to open the Certificate dialog window.
 *             Select the Details tab.
 *             Locate and click once on the Thumbprint field.
 *             Select and copy the text of the Thumbprint in the value textbox window.
 *                  (E.g.: "57cce8979c356d4f8767ab5aa31a6cd22d02d119")
 *
 *   8.   Get the application's identifier code.
 *
 *             In Visual Studio with the application's solution loaded (RPEngine),
 *             right-click the project name "RPEngine" and select Properties [Alt + Enter).
 *             Select the Application page.
 *             Click the Assembly Information button.
 *             Locate the GUI field and copy its text as the ID.
 *                  (E.g.: "058d3d24-d1ac-4486-95ab-f32e27a4d6e9")
 *
 *   9.   Bind the SSL certificate to an IP address:port and application.
 *
 *             Construct the NETSH command line using the SSL certificate's thumbprint and application's GUID:
 *
 *             "netsh  http add sslcert  ipport=10.0.0.5:443  certhash=57cce8979c356d4f8767ab5aa31a6cd22d02d119  appid={058d3d24-d1ac-4486-95ab-f32e27a4d6e9}"
 *
 *             Hopefully you will see the message : "SSL Certificate successfully added".
 *             If the add fails, look at how to "Generate PFX file from KEY and CRT" below.
 *             
 *
 *   10.  Verify the binding took place.
 *
 *             Run this command to list all known SSL certificates, and the latest addition might be the last one listed.
 *
 *                  "netsh  http show sslcert"
 *
 *             There appears to be 100 bindings of the same certificate to the range 0.0.0.0:44300 to 0.0.0.0:44399 on each system inspected so far.
 *
 *   References:
 *
 *        a)   MAKECERT command line: https://docs.microsoft.com/en-us/windows/win32/seccrypto/makecert
 *        b)   https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-configure-a-port-with-an-ssl-certificate
 */


//*****************************************************************************
//                                           Generate PFX file from KEY and CRT
//*****************************************************************************
/*
 *   Scenario:
 *
 *        You have bought a new SSL certificate and are trying to install it
 *        for use by a Windows application you are developing.
 *
 *        You attempted to bind the certificate (after having installed it
 *        via MMC) to the application using the "netsh http add" command.
 *
 *        The MMC/Certificates snap-in shows the installed certificate does NOT 
 *        have access to a private key (which it must) that corresponds to the 
 *        SSL certificate.
 *
 *   Solution 1:
 *
 *        You must modify the installed SSL certificate so that it includes
 *        the private .KEY data which you generated when you originally
 *        requested the SSL certificate from the vendors.
 *
 *        You will need to operate on the machine which is to host the
 *        application and that machine must have OpenSSL installed on it.
 *        Ideally the machine will also have the Apache server installed.
 *
 *   1.   Generate the PFX file from the private .KEY and CRT (SSL cert) files.
 *
 *        Example for "programify.com":
 *
 *             "openssl  pkcs12 -export -out programify_com.pfx -inkey programify_com.key -in programify_com.crt"
 *
 *        After pressing ENTER you will be asked to enter an Export Password, 
 *        and then to re-enter it to verify it. You can make up any password
 *        you like, it just used to prevent unwanted use of your .PFX file.
 *        It contains a copy of your private key so people can immitate you.
 *        If you forget your password, just regenerate the PFX file again.
 *
 *   2.   To install the new PFX file.
 *
 *             Right-click the ".PFX" in Windows Explorer and select Install Certificate.
 *             The "Certificate Import Wizard" should appear.
 *             Set the Store Location to Local Machine.
 *             Click Next.
 *             File to Import should already be filled in, so click Next.
 *             Private key protection: Enter the password you chose,
 *             Checkbox: Mark this key as exportable - Enable.
 *             Certificate Store: Select "Place all certificates in the following store"
 *             Click the Browse button and from the tree list, select "Personal".
 *             Click Next.
 *             Click Finish to import the SSL certificate into the Personal store.
 *             Click OK (asuming it finished successfully).
 *
 *        This should not change the SSL certificate's thumbprint.
 */