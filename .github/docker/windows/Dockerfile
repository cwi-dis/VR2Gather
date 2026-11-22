# escape=`
# game-ci's Unity image with cwipc added, for the CI builds in a Windows container
# (VR2Gather#355, copied from cwi-dis/cwipc_unity#71). Built in the CI job and passed to game-ci/unity-builder
# as customImage. The Windows counterpart of ../linux/Dockerfile.
#
# UNITY_VERSION: the project's Unity version (VRTApp-Develop/ProjectSettings/ProjectVersion.txt).
# CWIPC_VERSION: a cwipc release tag, or "nightly". The -built- zip of that release is
# unpacked and its bin directory put on the machine PATH; that zip has all of cwipc's
# DLLs, but not the Microsoft C++ runtime.
#
# The images are Windows Server 2022 (10.0.20348), so the job needs a windows-2022 runner.

ARG UNITY_VERSION
ARG GAMECI_IMAGE_VERSION=4
FROM unityci/editor:windows-${UNITY_VERSION}-windows-il2cpp-${GAMECI_IMAGE_VERSION}

ARG CWIPC_VERSION=v8.3.0

SHELL ["powershell", "-NoProfile", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# The Microsoft C++ runtime (vcruntime140, msvcp140, vcomp140) that cwipc's DLLs need.
RUN Invoke-WebRequest -Uri https://aka.ms/vs/17/release/vc_redist.x64.exe -OutFile C:\vc_redist.x64.exe; `
    Start-Process C:\vc_redist.x64.exe -ArgumentList '/install', '/quiet', '/norestart' -Wait; `
    Remove-Item C:\vc_redist.x64.exe

# The zip has the tree under installed/.
RUN $url = 'https://github.com/cwi-dis/cwipc/releases/download/' + $env:CWIPC_VERSION + '/cwipc-windows-intel-built-' + $env:CWIPC_VERSION + '.zip'; `
    Write-Host ('Downloading ' + $url); `
    Invoke-WebRequest -Uri $url -OutFile C:\cwipc.zip; `
    Expand-Archive C:\cwipc.zip -DestinationPath C:\cwipc-unpacked; `
    Move-Item C:\cwipc-unpacked\installed C:\cwipc; `
    Remove-Item C:\cwipc.zip; `
    Remove-Item C:\cwipc-unpacked -Recurse

# On the machine PATH, so Unity's DllImport finds cwipc_util.dll and its dependencies:
# game-ci passes only a fixed set of environment variables into the container.
RUN [Environment]::SetEnvironmentVariable('PATH', 'C:\cwipc\bin;' + [Environment]::GetEnvironmentVariable('PATH', 'Machine'), 'Machine'); `
    Get-ChildItem C:\cwipc\bin\cwipc_util.dll
