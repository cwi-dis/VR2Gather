
public interface PCBaseReader {
    bool            eof();
    bool            available(bool wait);
    PointCloudFrame get();
    void            update();
}
