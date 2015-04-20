@echo off
setlocal

rem find path of test executable
set PERFTEST_EXE="%~dp0\bin\release (system.numerics)\burningmime.curves.perftest.exe"
if not exist %PERFTEST_EXE% (
    echo Could not find file %PERFTEST_EXE%
    goto end
)

rem old jit
set COMPLUS_AltJit=
set COMPLUS_FeatureSIMD=0
%PERFTEST_EXE%

rem new jit, simd off
set COMPLUS_AltJit=*
set COMPLUS_FeatureSIMD=0
%PERFTEST_EXE%

rem new jit, simd on
set COMPLUS_AltJit=*
set COMPLUS_FeatureSIMD=1
%PERFTEST_EXE%

:end
