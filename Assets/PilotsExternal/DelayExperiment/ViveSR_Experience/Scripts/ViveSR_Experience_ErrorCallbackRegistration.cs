namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_ErrorCallbackRegistration
    {
        private ViveSR_Experience_ErrorHandler ErrorHandler;
        public ViveSR_Experience_ErrorCallbackRegistration(ViveSR_Experience_ErrorHandler errorHandler)
        {
            this.ErrorHandler = errorHandler;

            // Register runtime status events.
            ViveSR.RuntimeStatusEvent.RegisterHandler(RuntimeStatusFlag.CAM_NON_4K_NOT_ALIVE, PassThroughNoDataCallback);
            ViveSR.RuntimeStatusEvent.RegisterHandler(RuntimeStatusFlag.CAM_4K_NOT_ALIVE, PassThrough4KNoDataCallback);
        }

        /// <summary>
        /// Callback for pass through module no data.
        /// </summary>
        private void PassThroughNoDataCallback(bool cameraNotAlive)
        {
            if (cameraNotAlive)
            {
                ErrorHandler.EnablePanel("No data from non-4K camera.\nPlease restart VIVE Console and try again.");
            }
            else
            {
                ErrorHandler.DisableAllErrorPanels();
            }
        }

        /// <summary>
        /// Callback for pass through 4K module no data.
        /// </summary>
        private void PassThrough4KNoDataCallback(bool cameraNotAlive)
        {
            if (cameraNotAlive)
            {
                ErrorHandler.EnablePanel("No data from 4K camera.\nPlease restart VIVE Console and try again.");
            }
            else
            {
                ErrorHandler.DisableAllErrorPanels();
            }
        }
    }
}
