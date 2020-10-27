#!/bin/bash
# Ensure the correct version of Unity is on PATH (for example C:\Program Files\Unity\Hub\Editor\2019.3.15f1\Editor\Unity.exe)
# Ensure the installed folder (on the same level as Testbed) has the DLLs and executables and such
set -e
dirname=`dirname $0`
dirname=`cd $dirname ; pwd`
INSTALL_PCL=false
INSTALL_TVM=false
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
    INSTALL_TVM=true
    shift # past argument
    ;;
	--help)
	echo "Usage: $0 [--pcl] [--tvm]"
	echo "Creates VRTogether app in $dirname/built-VRTogether"
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
rm -rf built-VRTogether
Unity -batchmode -projectPath . -buildWindows64Player built-VRTogether/VRTogether.exe -quit
if $INSTALL_PCL; then
	mkdir built-VRTogether/bin
	cp ../installed/bin/*.{exe,bat,sh} built-VRTogether/bin
	mkdir built-VRTogether/dll
	cp ../installed/bin/*.{dll,smd,so} built-VRTogether/dll
	cp -r ../installed/share built-VRTogether/share
fi
if $INSTALL_TVM; then
	# xxxjack should also copy tvm_dll, tvm_release, 3rdparty?
	echo "$0: --tvm not yet implemented"
	exit 1
fi
