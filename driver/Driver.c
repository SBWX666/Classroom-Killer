#include "driver.h"
#include "Common.h"

#define MAX_PROTECTED_PIDS  32

static PVOID    g_ObRegistration = NULL;
static ULONG    g_ProtectedPids[MAX_PROTECTED_PIDS] = { 0 };
static KSPIN_LOCK g_PidLock;
static volatile LONG g_CbCount = 0;
static volatile LONG g_CbEnter = 0;

// ---------------------------------------------------------------
// PID table
// ---------------------------------------------------------------
static BOOLEAN IsProtectedPid(ULONG pid)
{
    BOOLEAN found = FALSE;
    KIRQL irql;

    KeAcquireSpinLock(&g_PidLock, &irql);
    for (int i = 0; i < MAX_PROTECTED_PIDS; i++)
    {
        if (g_ProtectedPids[i] == pid && pid != 0)
        {
            found = TRUE;
            break;
        }
    }
    KeReleaseSpinLock(&g_PidLock, irql);
    return found;
}

NTSTATUS ProtectPidAdd(ULONG pid)
{
    KIRQL irql;
    NTSTATUS status = STATUS_INSUFFICIENT_RESOURCES;
    int slot = -1;

    if (pid == 0)
        return STATUS_INVALID_PARAMETER;

    KeAcquireSpinLock(&g_PidLock, &irql);
    for (int i = 0; i < MAX_PROTECTED_PIDS; i++)
    {
        if (g_ProtectedPids[i] == pid)
        {
            status = STATUS_SUCCESS;
            slot = i;
            break;
        }
        if (g_ProtectedPids[i] == 0)
        {
            g_ProtectedPids[i] = pid;
            status = STATUS_SUCCESS;
            slot = i;
            break;
        }
    }
    KeReleaseSpinLock(&g_PidLock, irql);

    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
        "[Protect] ProtectPidAdd pid=%lu status=0x%X slot=%d\n",
        pid, status, slot);

    return status;
}

void ProtectPidRemove(ULONG pid)
{
    KIRQL irql;

    KeAcquireSpinLock(&g_PidLock, &irql);
    for (int i = 0; i < MAX_PROTECTED_PIDS; i++)
    {
        if (g_ProtectedPids[i] == pid)
        {
            g_ProtectedPids[i] = 0;
            break;
        }
    }
    KeReleaseSpinLock(&g_PidLock, irql);
}

// ---------------------------------------------------------------
// ObRegisterCallbacks
// ---------------------------------------------------------------
static OB_PREOP_CALLBACK_STATUS
PreOperationCallback(
    _In_ PVOID RegistrationContext,
    _Inout_ POB_PRE_OPERATION_INFORMATION OperationInformation
)
{
    UNREFERENCED_PARAMETER(RegistrationContext);

    /* 只保留前 5 条入口日志，确认回调活着即可，避免刷日志拖慢系统 */
    {
        LONG n = InterlockedIncrement(&g_CbEnter);
        if (n <= 5)
        {
            DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
                "[Cb] ENTER #%ld op=%lu kernel=%lu\n",
                n,
                (ULONG)OperationInformation->Operation,
                (ULONG)OperationInformation->KernelHandle);
        }
    }

    if (OperationInformation->KernelHandle)
        return OB_PREOP_SUCCESS;

    if (OperationInformation->ObjectType != *PsProcessType)
        return OB_PREOP_SUCCESS;

    PEPROCESS process = (PEPROCESS)OperationInformation->Object;
    HANDLE    pid = PsGetProcessId(process);

    // 进程自己操作自己（例如 .NET 的 Process.Kill() 会走 OpenProcess 拿自己的 PID），
    // 必须放行，否则程序连自己都退不出去。
    if (PsGetCurrentProcess() == process)
        return OB_PREOP_SUCCESS;

    if (!IsProtectedPid((ULONG)(ULONG_PTR)pid))
        return OB_PREOP_SUCCESS;

    /* 命中保护名单：这条日志只在真正要拦的时候出现 */
    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
        "[Cb] HIT pid=%lu op=%lu kernel=0\n",
        (ULONG)(ULONG_PTR)pid,
        (ULONG)OperationInformation->Operation);

    ACCESS_MASK* access = NULL;
    if (OperationInformation->Operation == OB_OPERATION_HANDLE_CREATE)
    {
        access = &OperationInformation->Parameters->CreateHandleInformation.DesiredAccess;
    }
    else if (OperationInformation->Operation == OB_OPERATION_HANDLE_DUPLICATE)
    {
        access = &OperationInformation->Parameters->DuplicateHandleInformation.DesiredAccess;
    }

    if (access)
    {
        ACCESS_MASK before = *access;

        *access &= ~(PROCESS_TERMINATE
            | PROCESS_VM_WRITE
            | PROCESS_VM_OPERATION
            | PROCESS_CREATE_THREAD);

        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Cb] STRIP pid=%lu op=%lu access 0x%X -> 0x%X (term %s)\n",
            (ULONG)(ULONG_PTR)pid,
            (ULONG)OperationInformation->Operation,
            before, *access,
            (before & PROCESS_TERMINATE) ? "was set" : "not requested");
    }

    return OB_PREOP_SUCCESS;
}

static NTSTATUS RegisterObCallbacks(VOID)
{
    NTSTATUS                status;
    OB_OPERATION_REGISTRATION operations[1];
    OB_CALLBACK_REGISTRATION  registration;
    UNICODE_STRING            altitude;

    RtlZeroMemory(operations, sizeof(operations));
    operations[0].ObjectType = PsProcessType;
    operations[0].Operations = OB_OPERATION_HANDLE_CREATE | OB_OPERATION_HANDLE_DUPLICATE;
    operations[0].PreOperation = PreOperationCallback;
    operations[0].PostOperation = NULL;

    RtlZeroMemory(&registration, sizeof(registration));
    registration.Version = OB_FLT_REGISTRATION_VERSION;
    registration.OperationRegistrationCount = 1;
    registration.OperationRegistration = operations;
    registration.RegistrationContext = NULL;

    RtlInitUnicodeString(&altitude, L"370000");
    registration.Altitude = altitude;

    status = ObRegisterCallbacksPatched(&registration, &g_ObRegistration);
    if (!NT_SUCCESS(status))
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] ObRegisterCallbacks FAILED status=0x%08X - protection DISABLED\n", status);
    }
    else
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] ObRegisterCallbacks registered OK\n");
    }
    return status;
}

// ---------------------------------------------------------------
// Entry / cleanup
// ---------------------------------------------------------------
NTSTATUS
DriverEntry(
    _In_ PDRIVER_OBJECT  DriverObject,
    _In_ PUNICODE_STRING RegistryPath
)
{
    WDF_DRIVER_CONFIG config;
    WDFDRIVER         driver;
    NTSTATUS          status;

    KeInitializeSpinLock(&g_PidLock);
    RtlZeroMemory(g_ProtectedPids, sizeof(g_ProtectedPids));
    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "[Protect] DriverEntry START\n");
    RegisterObCallbacks();

    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "[Protect] WdfDriverCreate...\n");
    // No EvtDeviceAdd: this is a control device driver, we create it manually
    WDF_DRIVER_CONFIG_INIT(&config, NULL);

    status = WdfDriverCreate(DriverObject, RegistryPath,
        WDF_NO_OBJECT_ATTRIBUTES, &config, &driver);
    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
        "[Protect] WdfDriverCreate -> 0x%X\n", status);

    if (NT_SUCCESS(status))
    {
        // KMDF installs its own DriverUnload during WdfDriverCreate.
        // Clearing it afterwards makes NtUnloadDriver fail with
        // STATUS_INVALID_DEVICE_REQUEST, so "sc stop" can no longer
        // tear the protection down while the machine is running.
        DriverObject->DriverUnload = NULL;
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] DriverUnload cleared - service can no longer be stopped\n");
    }
    if (!NT_SUCCESS(status))
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] WdfDriverCreate failed 0x%X\n", status);
        if (g_ObRegistration) { ObUnRegisterCallbacks(g_ObRegistration); g_ObRegistration = NULL; }
        return status;
    }

    status = KMDFDriverCreateDevice(driver);
    if (!NT_SUCCESS(status))
    {
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL,
            "[Protect] KMDFDriverCreateDevice failed 0x%X\n", status);
        return status;
    }

    return STATUS_SUCCESS;
}

VOID
KMDFDriverEvtDriverContextCleanup(
    _In_ WDFOBJECT DriverObject
)
{
    UNREFERENCED_PARAMETER(DriverObject);

    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "[Protect] cleanup\n");

    if (g_ObRegistration)
    {
        ObUnRegisterCallbacks(g_ObRegistration);
        g_ObRegistration = NULL;
    }
}
