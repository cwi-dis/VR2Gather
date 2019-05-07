
public interface PCBaseReader {
    void            free();
    bool            eof();
    bool            available(bool wait);
    PointCloudFrame get();
}
