#pragma once

// Device names
#define DEVICE_NAME     L"\\Device\\ClassKillerProtect"
#define SYMBOLIC_NAME   L"\\DosDevices\\ClassKillerProtect"

// IOCTL codes
// CTL_CODE(FILE_DEVICE_UNKNOWN=0x22, func, METHOD_BUFFERED=0, FILE_ANY_ACCESS=0)
#define IOCTL_PROTECT_PROCESS   CTL_CODE(FILE_DEVICE_UNKNOWN, 0x800, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_UNPROTECT_PROCESS CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Process access rights (not exposed by kernel headers)
#ifndef PROCESS_TERMINATE
#define PROCESS_TERMINATE        0x0001
#endif
#ifndef PROCESS_CREATE_THREAD
#define PROCESS_CREATE_THREAD    0x0002
#endif
#ifndef PROCESS_VM_OPERATION
#define PROCESS_VM_OPERATION     0x0008
#endif
#ifndef PROCESS_VM_READ
#define PROCESS_VM_READ          0x0010
#endif
#ifndef PROCESS_VM_WRITE
#define PROCESS_VM_WRITE         0x0020
#endif
#ifndef PROCESS_DUP_HANDLE
#define PROCESS_DUP_HANDLE       0x0040
#endif

#pragma pack(push, 8)
typedef struct _PROTECT_REQUEST
{
    ULONG Pid;
    ULONG Reserved;
} PROTECT_REQUEST, *PPROTECT_REQUEST;
#pragma pack(pop)