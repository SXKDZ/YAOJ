#pragma once

#ifdef WINRUNNER_EXPORTS
#define WINRUNNER_API __declspec(dllexport)
#else
#define WINRUNNER_API __declspec(dllimport)
#endif

#define PSAPI_VERSION 1
#include <windows.h>
#include <psapi.h>

#pragma comment(lib, "Psapi")
#pragma comment(lib, "Psapi.lib")

namespace WinRunner {
    enum class Status { OK, TLE, MLE, RE, ERR };

    class Result {
    public:
        Status status;
        double usedTime;
        int usedMemory;
    };

    extern "C" WINRUNNER_API void StartRestrictedProcess(Result *result, LPCWSTR cmd, LPWSTR arg,
        LPCWSTR infile, LPCWSTR outfile, LPCWSTR errfile,
        unsigned int time, unsigned int memory, 
        bool restrictProcess, ULONG_PTR affinity);
    static double MakeTime(FILETIME const& kernel_time, FILETIME const& user_time);

    extern "C" WINRUNNER_API void StartCompiler(Result *result, LPWSTR cmd, LPCWSTR errfile,
        unsigned int time, unsigned int memory);
    extern "C" WINRUNNER_API void StartProcess(Result *result, LPWSTR cmd, LPCWSTR infile, LPCWSTR outfile,
        unsigned int time, unsigned int memory, ULONG_PTR affinity);
}
