using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoDashReceiver : MonoBehaviour
{
    Workers.BaseWorker reader0;
    Workers.BaseWorker codec0;
    Workers.BaseWorker preparer0;
    Workers.Token token0;
    Workers.BaseWorker reader1;
    Workers.BaseWorker codec1;
    Workers.BaseWorker preparer1;
    Workers.Token token1;
    public string url;

    private void Start() {
        Init(url);
    }

    // Start is called before the first frame update
    public void Init(string url) {
        Debug.Log($"Config.Instance.memoryDamping {Config.Instance.memoryDamping}");
        try {
            reader0 = new Workers.SUBReader(url, 0);
            codec0 = new Workers.VideoDecoder();
            preparer0 = new Workers.VideoPreparer();
            reader0.AddNext(codec0).AddNext(preparer0).AddNext(reader0);
            reader0.token = token0 = new Workers.Token();

            reader1 = new Workers.SUBReader(url, 1);
            codec1 = new Workers.VideoDecoder();
            preparer1 = new Workers.VideoPreparer();
            reader1.AddNext(codec1).AddNext(preparer1).AddNext(reader1);
            reader1.token = token1 = new Workers.Token();
        }
        catch (System.Exception e) {
            Debug.Log($">>ERROR {e}");
        }
    }

    void OnDestroy() {
        reader0?.Stop();
        codec0?.Stop();
        preparer0?.Stop();
        reader1?.Stop();
        codec1?.Stop();
        preparer1?.Stop();
    }
}
