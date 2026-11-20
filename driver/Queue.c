#include "driver.h"
#include "Common.h"

VOID
EvtIoDeviceControl(
    _In_ WDFQUEUE   Queue,
    _In_ WDFREQUEST Request,
    _In_ size_t     OutputBufferLength,
    _In_ size_t     InputBufferLength,
    _In_ ULONG      IoControlCode
    )
{
    UNREFERENCED_PARAMETER(Queue);
    UNREFERENCED_PARAMETER(OutputBufferLength);

    NTSTATUS         status = STATUS_INVALID_DEVICE_REQUEST;
    PPROTECT_REQUEST req    = NULL;
    size_t           len    = 0;

    if (InputBufferLength < sizeof(PROTECT_REQUEST))
    {
        WdfRequestComplete(Request, STATUS_BUFFER_TOO_SMALL);
        return;
    }

    status = WdfRequestRetrieveInputBuffer(Request, sizeof(PROTECT_REQUEST),
                                           (PVOID*)&req, &len);
    if (!NT_SUCCESS(status))
    {
        WdfRequestComplete(Request, status);
        return;
    }

    switch (IoControlCode)
    {
    case IOCTL_PROTECT_PROCESS:
        status = ProtectPidAdd(req->Pid);
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL,
            "[Protect] protect pid=%u status=0x%X\n", req->Pid, status);
        break;

    case IOCTL_UNPROTECT_PROCESS:
        ProtectPidRemove(req->Pid);
        status = STATUS_SUCCESS;
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL,
            "[Protect] unprotect pid=%u\n", req->Pid);
        break;
    }

    WdfRequestComplete(Request, status);
}
