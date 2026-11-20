#pragma once

/*++

Module Name:

    driver.h

Abstract:

    This file contains the driver definitions.

Environment:

    Kernel-mode Driver Framework

--*/

#include <ntddk.h>
#include <wdf.h>
#include <initguid.h>

#include "device.h"
#include "queue.h"
EXTERN_C_START

//
// WDFDRIVER Events
//

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_OBJECT_CONTEXT_CLEANUP KMDFDriverEvtDriverContextCleanup;

//
// Process protection API (implemented in Driver.c, used by Queue.c)
//
NTSTATUS ProtectPidAdd(_In_ ULONG pid);
VOID     ProtectPidRemove(_In_ ULONG pid);

//
// IOCTL handler (implemented in Queue.c, referenced by Device.c)
//
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL EvtIoDeviceControl;

//
// Signature-check bypass (implemented in Patch.c)
//
NTSTATUS ObRegisterCallbacksPatched(
    _In_  POB_CALLBACK_REGISTRATION Registration,
    _Out_ PVOID*                    RegistrationHandle);

EXTERN_C_END
