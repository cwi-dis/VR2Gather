#!/bin/bash
# Ensure the correct version of Unity is on PATH (for example C:\Program Files\Unity\Hub\Editor\2019.3.15f1\Editor\Unity.exe)
# Ensure the installed folder (on the same level as Testbed) has the DLLs and executables and such
set -e
dirname=`dirname $0`
dirname=`cd $dirname ; pwd`
INSTALL_PCL=false
INSTALL_TVM=false
DEST=built-VRTogether
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
if ! Unity -batchmode -projectPath . -buildWindows64Player $DEST/VRTogether.exe -quit -logfile buildlog.txt; then
	echo ============= Unity build failed. Logfile contents below ==========
	cat buildlog.txt
	echo ============= Unity build failed. Logfile contents above ==========
	exit 1
fi
if $INSTALL_PCL; then
	mkdir $DEST/bin
	cp ../installed/bin/*.{exe,bat,sh} $DEST/bin
	mkdir $DEST/dll
	cp ../installed/bin/*.{dll,smd,so} $DEST/dll
	cp -r ../installed/share $DEST/share
fi
if $INSTALL_TVM; then
	# xxxjack should also copy tvm_dll, tvm_release, 3rdparty?
	echo "$0: --tvm not yet implemented"
	exit 1
fi
