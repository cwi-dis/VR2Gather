using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class PCCompressedDirectoryReader : PCBaseReader {
    string[] allFilenames;
    int      currentFile;
    cwipc.decoder  decoder;

    public PCCompressedDirectoryReader(string dirname) {
        currentFile = 0;
        allFilenames = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/" + dirname);

        decoder = cwipc.new_decoder();
        if (decoder == null)
        {
            Debug.LogError("PCCompressedDirectoryReader: cwipc_new_decoder: create failed"); // Shoulnd't happen, should raise exception
        }
    }

    public void free() {
    }

    public bool eof() {
        return allFilenames.Length == 0;
    }

    public bool available(bool wait) {
        return allFilenames.Length != 0;
    }

    public PointCloudFrame get()
    {
        if (decoder == null) {
            Debug.LogError("PCCompressedDirectoryReader: cwipc_decoder: no decoder available");
            return null;
        }

        string filename = allFilenames[currentFile];
        currentFile = (currentFile + 1) % allFilenames.Length;

        var bytes = System.IO.File.ReadAllBytes(filename);
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        decoder.feed(ptr, bytes.Length);
        bool ok = decoder.available(true);
        if (!ok) {
            Debug.LogError("PCCompressedDirectoryReader: cwipc_decoder: no pointcloud available");
            return null;
        }
        cwipc.pointcloud pc = decoder.get();
        if (pc == null) {
            Debug.LogError("PCCompressedDirectoryReader: cwipc_decoder: did not return a pointcloud");
            return null;
        }
        PointCloudFrame pointCloudFrame = new PointCloudFrame();
        pointCloudFrame.SetData(pc);
        return pointCloudFrame;
    }

    public virtual void update() { }
}
