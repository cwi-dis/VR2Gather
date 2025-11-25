#!/bin/bash
myDir=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
contentsDir=$(dirname ${myDir})
venvDir="${contentsDir}/venv"
runServer="${venvDir}/bin/VRTrunserver"
executable="${myDir}/VR2Gather"
topworkdir="${contentsDir}/VRTrunserver-workdir"

if ! [ -f ${runServer} ]; then
	echo "Testing whether Python is installed..."
	python3 --version
	echo "Creating venv..."
	python3 -m venv ${venvDir}
	echo "Activating venv..."
	source "${venvDir}/bin/activate"
	echo "Installing VRTrunserver..."
	pip install "git+https://github.com/cwi-dis/VRTStatistics#subdirectory=VRTrunserver"
fi
echo "Running $runServer..."
exec "$runServer" --executable "${executable}" --topworkdir "${topworkdir}"
	
