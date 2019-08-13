
public interface PCBaseReader {
    bool            eof();
    bool            available(bool wait);
    PointCloudFrame get();
    void            update(); // xxxjack wondering whether we should simply add a thread to PCRealSense2Reader and get rid of this...
}
