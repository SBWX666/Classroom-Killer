/*++

Module Name:

    public.h

Abstract:

    This module contains the common declarations shared by driver
    and user applications.

Environment:

    user and kernel

--*/

//
// Define an Interface Guid so that apps can find the device and talk to it.
//

DEFINE_GUID (GUID_DEVINTERFACE_KMDFDriver,
    0x264ba654,0x88af,0x4a32,0x91,0x35,0xc9,0x5d,0x11,0x0f,0x76,0xa3);
// {264ba654-88af-4a32-9135-c95d110f76a3}
