#!/bin/bash
# Ensure the correct version of Unity is on PATH (for example C:\Program Files\Unity\Hub\Editor\2019.3.15f1\Editor\Unity.exe)
# Ensure the installed folder (on the same level as Testbed) has the DLLs and executables and such
set -e
dirname=`dirname $0`
dirname=`cd $dirname ; pwd`
INSTALL_PCL=false
INSTALL_TVM=false
DEST=built-VRTogether

unitycmd=Unity
unityflag=
output=
mac=false
win=false
linux=false
case `uname -s` in
Darwin)
	mac=true
	unitycmd=/Applications/Unity/Hub/Editor/2019.4.31f1/Unity.app/Contents/MacOS/Unity
	unityflag=-buildOSXUniversalPlayer
	output=VRTogether.app
	;;
MINGW64*)
	win=true
	unitycmd=Unity
	unityflag=-buildWindows64Player
	output=VRTogether.exe
	;;
Linux*)
	linux=true
	unitycmd=/Applications/Unity/Hub/Editor/2019.4.31f1/Unity.app/Contents/MacOS/Unity
	unityflag=-buildLinux64Player
	output=VRTogether
	;;
*)
	echo Unknown system `uname -s`
	exit 1
	;;
esac

while [[ $# -gt 0 ]]
do
	key="$1"

	case $key in
		--verbose)
		set -x
		shift
		;;
		--pcl)
		INSTALL_PCL=true
		shift # past argument
		;;
		--tvm)
		echo "$0: --tvm not implemented"
		exit 1
		INSTALL_TVM=true
		shift # past argument
		;;
		--dest)
		DEST="$2"
		shift
		shift
		;;
		--help)
		echo "Usage: $0 [--pcl] [--tvm]"
		echo "Creates VRTogether app in $dirname/$DEST"
		echo "--pcl Includes pointcloud DLLs and support programs (must be in expected location)"
		echo "--tvm Includes TVM DLLs and support programs (unimplemented)"
		exit 0
		;;
		*)    # unknown option
		echo Unknown option: $key
		exit 1
		;;
	esac
done
set -x
cd $dirname
rm -rf $DEST buildlog.txt
if ! $unitycmd -batchmode -projectPath . $unityflag $DEST/$output -quit -logfile buildlog.txt; then
	echo ============= Unity build failed. Logfile contents below ==========
	cat buildlog.txt
	echo ============= Unity build failed. Logfile contents above ==========
	exit 1
fi
if $INSTALL_PCL; then
	if $win; then
		mkdir $DEST/bin
		mkdir $DEST/dll
		cp ../installed/bin/*.{exe,bat,sh} $DEST/bin
		cp ../installed/bin/*.{dll,smd,so} $DEST/dll
		cp -r ../installed/share $DEST/share
	fi
	if $mac; then
		echo Copying pointcloud dynamic libraries not yet implemented for mac
	fi
	if $linux; then
		echo Copying pointcloud dynamic libraries not yet implemented for linux
	fi
fi
