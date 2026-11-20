// ---------------------------------------------------------------
// Patch.c
// ObRegisterCallbacks signature-check bypass (Win10 19041.1288 verified)
//
//   ObRegisterCallbacks  RVA 0x7B7B80
//     +0x106  call 0x3AB8D0        ; verify helper
//     +0x10B  test eax,eax
//     +0x10D  jz   -> 0xC0000022
//
//   Verify helper        RVA 0x3AB8D0
//     mov edi,edx / mov rsi,rcx / call lookup / test [mod+0x68],0x20
//
// Replace the helper entry with "push 1; pop rax; ret" for the duration of
// the ObRegisterCallbacks call, then restore the original four bytes.
// Every stage is logged so a failure point is unambiguous.
// ---------------------------------------------------------------
#include "driver.h"
#include <intrin.h>
#include <ntstrsafe.h>
#include <ntimage.h>

NTSYSAPI
PVOID
NTAPI
RtlPcToFileHeader(
    _In_  PVOID  PcValue,
    _Out_ PVOID* BaseOfImage
    );

NTSYSAPI
PIMAGE_NT_HEADERS
NTAPI
RtlImageNtHeader(
    _In_ PVOID BaseAddress
    );

#define CR0_WP_MASK     0x00010000
#define SCAN_LIMIT      0x400
#define DUMP_BYTES      0x140

// mov [rsp+8],rbx / mov [rsp+0x10],rbp / mov [rsp+0x18],rsi / push rdi
// sub rsp,0x20 / mov edi,edx / mov rsi,rcx
static const UCHAR kVerifySig[] = {
    0x48, 0x89, 0x5C, 0x24, 0x08,
    0x48, 0x89, 0x6C, 0x24, 0x10,
    0x48, 0x89, 0x74, 0x24, 0x18,
    0x57,
    0x48, 0x83, 0xEC, 0x20,
    0x8B, 0xFA,
    0x48, 0x8B, 0xF1
};

static BOOLEAN g_Dumped = FALSE;

#define P(...) DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, __VA_ARGS__)

static VOID WriteTextBytes(PVOID dst, const UCHAR* src, SIZE_T len)
{
    KIRQL     irql;
    ULONG_PTR cr0;

    irql = KeRaiseIrqlToDpcLevel();
    cr0  = __readcr0();
    __writecr0(cr0 & ~CR0_WP_MASK);
    RtlCopyMemory(dst, src, len);
    __writecr0(cr0);
    KeLowerIrql(irql);
}

static BOOLEAN MatchSig(PUCHAR p, const UCHAR* sig, SIZE_T n)
{
    SIZE_T i;

    for (i = 0; i < n; i++)
    {
        if (p[i] != sig[i])
            return FALSE;
    }
    return TRUE;
}

static PUCHAR FindVerifyFunction(PUCHAR ob, SIZE_T len)
{
    PUCHAR p;
    PUCHAR t;
    LONG   disp;
    PVOID  ntBase   = NULL;
    PVOID  pcBase   = NULL;
    SIZE_T imageEnd = 0;
    PIMAGE_NT_HEADERS nth;

    /* Resolve the ntoskrnl image range once; anything outside is noise. */
    RtlPcToFileHeader(ob, &ntBase);
    if (!ntBase)
    {
        P("[S3] cannot resolve ntoskrnl base for %p\n", ob);
        return NULL;
    }

    nth = RtlImageNtHeader(ntBase);
    if (nth)
        imageEnd = (SIZE_T)nth->OptionalHeader.SizeOfImage;

    for (p = ob; p + 5 <= ob + len; p++)
    {
        if (p[0] != 0xE8)
            continue;

        disp = *(LONG*)(p + 1);
        t    = p + 5 + disp;

        /* Must lie inside the ntoskrnl image, with room for the whole sig. */
        if (t < (PUCHAR)ntBase)
            continue;
        if ((SIZE_T)(t - (PUCHAR)ntBase) + sizeof(kVerifySig) > imageEnd)
            continue;
        if (t[0] != 0x48)
            continue;

        /* And it must belong to the very same module. */
        pcBase = NULL;
        RtlPcToFileHeader(t, &pcBase);
        if (pcBase != ntBase)
            continue;

        if (MatchSig(t, kVerifySig, sizeof(kVerifySig)))
        {
            P("[S3] verify sig matched at ob+0x%llX -> %p\n",
              (ULONGLONG)(p - ob), t);
            return t;
        }
    }
    return NULL;
}

// ---------------------------------------------------------------
NTSTATUS ObRegisterCallbacksPatched(
    _In_  POB_CALLBACK_REGISTRATION Registration,
    _Out_ PVOID*                    RegistrationHandle)
{
    UNICODE_STRING sym;
    PUCHAR         ob;
    PUCHAR         target;
    UCHAR          saved[4];
    UCHAR          stub[4];
    NTSTATUS       status;
    BOOLEAN        patched = FALSE;

    P("[S1] enter, irql=%lu\n", (ULONG)KeGetCurrentIrql());

    RtlInitUnicodeString(&sym, L"ObRegisterCallbacks");
    ob = (PUCHAR)MmGetSystemRoutineAddress(&sym);
    if (!ob)
    {
        P("[S2] export NOT FOUND\n");
        return ObRegisterCallbacks(Registration, RegistrationHandle);
    }
    P("[S2] ob=%p\n", ob);

    target = FindVerifyFunction(ob, SCAN_LIMIT);
    if (!target)
    {
        P("[S3] signature NOT matched, calling unprotected\n");
        status = ObRegisterCallbacks(Registration, RegistrationHandle);
        P("[S7] returned 0x%08X\n", status);
        if (!NT_SUCCESS(status) && !g_Dumped)
        {
            g_Dumped = TRUE;
            P("[Dump] body follows\n");
            {
                SIZE_T i, j;
                CHAR   line[128];
                CHAR   hex[8];
                int    pos;
                for (i = 0; i < DUMP_BYTES; i += 32)
                {
                    pos = 0;
                    for (j = 0; j < 32 && i + j < DUMP_BYTES; j++)
                    {
                        RtlStringCchPrintfA(hex, sizeof(hex), "%02X", ob[i + j]);
                        line[pos++] = hex[0];
                        line[pos++] = hex[1];
                        line[pos++] = ' ';
                        if ((j & 7) == 7 && j != 31)
                            line[pos++] = ' ';
                    }
                    line[pos] = 0;
                    P("[Dump] ob+%03llX  %s\n", (ULONGLONG)i, line);
                }
            }
        }
        return status;
    }

    stub[0] = 0x6A; stub[1] = 0x01; stub[2] = 0x58; stub[3] = 0xC3;

    RtlCopyMemory(saved, target, 4);
    P("[S4] original=%02X %02X %02X %02X  writing stub\n",
      saved[0], saved[1], saved[2], saved[3]);

    WriteTextBytes(target, stub, 4);
    patched = TRUE;
    P("[S5] stub written, bytes now=%02X %02X %02X %02X\n",
      target[0], target[1], target[2], target[3]);

    P("[S6] calling ObRegisterCallbacks\n");
    status = ObRegisterCallbacks(Registration, RegistrationHandle);
    P("[S7] returned 0x%08X\n", status);

    if (patched)
    {
        WriteTextBytes(target, saved, 4);
        P("[S8] restored, bytes now=%02X %02X %02X %02X\n",
          target[0], target[1], target[2], target[3]);
    }

    P("[S9] done status=0x%08X\n", status);
    return status;
}
