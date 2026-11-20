#include "driver.h"
#include "Common.h"

/* SY and BA get full control; Authenticated Users get read + write.
   SDDL needs one ACE per access mask - "GRGW" is not a valid combination,
   so GR and GW are declared separately.
   Restricting to SY|BA only means the UI process started via the Run key
   or by the service runs with a filtered token, cannot open the device,
   and ProtectSelf() fails - leaving the UI unprotected. */
static const UNICODE_STRING g_Sddl = RTL_CONSTANT_STRING(
    L"D:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;GR;;;AU)(A;;GW;;;AU)");

NTSTATUS
KMDFDriverCreateDevice(
    _In_ WDFDRIVER Driver
)
{
    PWDFDEVICE_INIT       pInit = NULL;
    WDFDEVICE             device;
    WDF_IO_QUEUE_CONFIG   queueConfig;
    WDFQUEUE              queue;
    NTSTATUS              status;
    UNICODE_STRING        deviceName;
    UNICODE_STRING        symName;

    pInit = WdfControlDeviceInitAllocate(Driver, &g_Sddl);
    if (pInit == NULL)
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "[Protect] alloc init failed\n");
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    // 1. Give the device an explicit name
    RtlInitUnicodeString(&deviceName, L"\\Device\\ClassKillerProtect");
    status = WdfDeviceInitAssignName(pInit, &deviceName);
    if (!NT_SUCCESS(status))
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] AssignName failed 0x%X\n", status);
        WdfDeviceInitFree(pInit);
        return status;
    }

    // 2. Correct characteristics flag
    WdfDeviceInitSetCharacteristics(pInit, FILE_DEVICE_SECURE_OPEN, FALSE);

    // 3. Create device
    status = WdfDeviceCreate(&pInit, WDF_NO_OBJECT_ATTRIBUTES, &device);
    if (!NT_SUCCESS(status))
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] WdfDeviceCreate failed 0x%X\n", status);
        return status;
    }

    // 4. Native symbolic link creation (bypass WDF state checks)
    RtlInitUnicodeString(&symName, L"\\DosDevices\\ClassKillerProtect");
    IoDeleteSymbolicLink(&symName);
    status = IoCreateSymbolicLink(&symName, &deviceName);
    if (!NT_SUCCESS(status))
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] IoCreateSymbolicLink failed 0x%X\n", status);
        return status;
    }

    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
        "[Protect] symlink created OK\n");

    // 5. Default queue
    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&queueConfig,
        WdfIoQueueDispatchSequential);
    queueConfig.EvtIoDeviceControl = EvtIoDeviceControl;

    status = WdfIoQueueCreate(device, &queueConfig,
        WDF_NO_OBJECT_ATTRIBUTES, &queue);
    if (!NT_SUCCESS(status))
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] WdfIoQueueCreate failed 0x%X\n", status);
        return status;
    }

    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "[Protect] device READY\n");
    return STATUS_SUCCESS;
}