#include "winrunner.h"
#include <cstdio>

double WinRunner::MakeTime(FILETIME const & kernel_time, FILETIME const & user_time) {
    ULARGE_INTEGER kernel;
    ULARGE_INTEGER user;
    kernel.HighPart = kernel_time.dwHighDateTime;
    kernel.LowPart = kernel_time.dwLowDateTime;
    user.HighPart = user_time.dwHighDateTime;
    user.LowPart = user_time.dwLowDateTime;
    return (static_cast<double>(kernel.QuadPart) +
        static_cast<double>(user.QuadPart)) *
        1e-7;
}

void WinRunner::StartRestrictedProcess(Result * result, LPCWSTR cmd, LPWSTR arg,
    LPCWSTR infile, LPCWSTR outfile, LPCWSTR errfile,
    unsigned int time, unsigned int memory,
    bool restrictProcess, ULONG_PTR affinity) {

    result->status = Status::OK;

    BOOL bInJob = FALSE;
    IsProcessInJob(GetCurrentProcess(), NULL, &bInJob);
    if (bInJob) {
        result->status = Status::ERR;
        return;
    }

    HANDLE hjob = CreateJobObject(NULL, TEXT("RestrictedProcessJob"));

    // basic limitation
    JOBOBJECT_BASIC_LIMIT_INFORMATION jobli = { 0 };
    // cannot use more than 1 sec (in 100-ns intervals) of CPU time
    jobli.PerJobUserTimeLimit.QuadPart = 10000 * time * 2;
    jobli.MinimumWorkingSetSize = 1;
    jobli.MaximumWorkingSetSize = memory * 1024 * 1024;
    jobli.LimitFlags = JOB_OBJECT_LIMIT_JOB_TIME | JOB_OBJECT_LIMIT_WORKINGSET;

    if (restrictProcess) {
        jobli.ActiveProcessLimit = 1;
        jobli.LimitFlags |= JOB_OBJECT_LIMIT_ACTIVE_PROCESS;
        if (affinity != NULL) {
            jobli.Affinity = affinity;
            jobli.LimitFlags |= JOB_OBJECT_LIMIT_AFFINITY;
        }
    }
    SetInformationJobObject(hjob, JobObjectBasicLimitInformation, &jobli, sizeof(jobli));

    // UI restrictions
    JOBOBJECT_BASIC_UI_RESTRICTIONS jobuir;
    jobuir.UIRestrictionsClass |= JOB_OBJECT_UILIMIT_ALL;
    SetInformationJobObject(hjob, JobObjectBasicUIRestrictions, &jobuir, sizeof(jobuir));

    // spawn the process that is to be in the job, 
    // i.e., the process' thread must be initially suspended
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    HANDLE hin, hout, herr;
    SECURITY_ATTRIBUTES sa;
    sa.nLength = sizeof(sa);
    sa.lpSecurityDescriptor = NULL;
    sa.bInheritHandle = TRUE;

    // prevent from report errors
    SetErrorMode(SEM_NOGPFAULTERRORBOX);

    ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));
    ZeroMemory(&si, sizeof(STARTUPINFO));

    if (infile != NULL) {
        hin = CreateFile(infile, GENERIC_READ, FILE_SHARE_READ, &sa,
            OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
        si.hStdInput = hin;
    }
    if (outfile != NULL) {
        hout = CreateFile(outfile, GENERIC_WRITE, FILE_SHARE_WRITE, &sa,
            CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        si.hStdOutput = hout;
    }
    if (errfile != NULL) {
        herr = CreateFile(errfile, GENERIC_WRITE, FILE_SHARE_WRITE, &sa,
            CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        si.hStdError = herr;
    }

    // redirect stdin and stdout to files
    si.cb = sizeof(si);
    GetStartupInfo(&si);
    si.dwFlags = STARTF_USESTDHANDLES;
    si.wShowWindow = SW_HIDE;

    BOOL bResult = CreateProcess(cmd, arg, NULL, NULL, TRUE,
        CREATE_SUSPENDED | CREATE_NO_WINDOW, NULL, NULL, &si, &pi);

    // place the process in the job
    AssignProcessToJobObject(hjob, pi.hProcess);

    // allow the child process' thread to execute code
    ResumeThread(pi.hThread);
    CloseHandle(pi.hThread);

    // wait for the process to terminate or for all the job's allotted CPU time to be used
    HANDLE h[2] = { pi.hProcess, hjob };
    if (WaitForMultipleObjects(2, h, FALSE, time * 1500) == WAIT_TIMEOUT) {
        result->status = Status::TLE;
    }

    DWORD ec;
    GetExitCodeProcess(pi.hProcess, &ec);
    if (ec == STILL_ACTIVE) {
        TerminateJobObject(hjob, -1);
    }
    else if (ec != 0x0) {
        result->status = Status::RE;
    }

    FILETIME creationTime, exitTime, kernelTime, userTime;
    GetProcessTimes(pi.hProcess, &creationTime, &exitTime, &kernelTime, &userTime);
    auto usedTime = MakeTime(kernelTime, userTime);

    PROCESS_MEMORY_COUNTERS pmc;
    GetProcessMemoryInfo(pi.hProcess, &pmc, sizeof(pmc));
    auto usedMemory = pmc.PeakPagefileUsage / 1024 / 1024;  // in MBytes
    if (usedMemory > memory) {
        result->status = Status::MLE;
    }

    result->usedTime = usedTime;
    result->usedMemory = usedMemory;

    CloseHandle(pi.hProcess);
    CloseHandle(hjob);
    CloseHandle(hin);
    CloseHandle(hout);
    CloseHandle(herr);
}

void WinRunner::StartCompiler(Result *result, LPWSTR cmd, LPCWSTR errfile,
    unsigned int time, unsigned int memory) {
    StartRestrictedProcess(result, NULL, cmd, NULL, NULL, errfile, time, memory, false, NULL);
}

void WinRunner::StartProcess(Result *result, LPWSTR cmd, LPCWSTR infile, LPCWSTR outfile,
    unsigned int time, unsigned int memory, ULONG_PTR affinity) {
    StartRestrictedProcess(result, NULL, cmd, infile, outfile, NULL, time, memory, true, affinity);
}
