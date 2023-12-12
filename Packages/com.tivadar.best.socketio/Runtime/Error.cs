namespace Best.SocketIO
{
    public class Error
    {
        public string message;

        public Error() { }

        public Error(string msg)
        {
            this.message = msg;
        }

        public override string ToString()
        {
            return this.message;
        }
    }
}
