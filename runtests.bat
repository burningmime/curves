@echo off
set PERFTEST_EXE="%~dp0\bin\release\burningmime.curves.perftest.exe"
if not exist "%PERFTEST_EXE%" (
    echo Could not find file "%PERFTEST_EXE%"
    goto END
)

setlocal
set COMPLUS_AltJit=
set COMPLUS_FeatureSIMD=0
"%PERFTEST_EXE%"

set COMPLUS_AltJit=*
set COMPLUS_FeatureSIMD=0
"%PERFTEST_EXE%"

set COMPLUS_AltJit=*
set COMPLUS_FeatureSIMD=1
"%PERFTEST_EXE%"

:END