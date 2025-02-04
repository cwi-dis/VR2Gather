$myPath = $MyInvocation.MyCommand.Path
$myDir = Split-Path $myPath -Parent
$venvDir = Join-Path -Path $myDir -ChildPath VR2Gather_Data\venv
$scriptDir = Join-Path -Path $venvDir -ChildPath VR2Gather_Data\venv\Scripts
$runServer = Join-Path -Path $scriptDir -ChildPath VRTrunserver.exe

if (-not (Test-Path $runServer)) {
	"Creating venv..."
	python -m venv $venvDir
	"Activating venv..."
	&$scriptDir\Activate.ps1
	"Installing VRTrunserver..."
	pip install "git+https://github.com/cwi-dis/VRTStatistics@10-restructure#subdirectory=VRTrunserver"
}
"Running $runServer..."
&$runServer
	