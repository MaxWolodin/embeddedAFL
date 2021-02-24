namespace embeddedAFL
{
    public class DataTypes
    {
        /// <summary>
        /// Enum that defines the status that will be returned to Kelinci
        /// </summary>
        public enum FuzzingStatusEnum
        {
            StatusSuccess,
            StatusTimeout,
            StatusCrash,
            StatusQueueFull,
            StatusCommError
        }

        /// <summary>
        /// Enum that defines the current status of the fuzzing target
        /// </summary>
        public enum TargetStateEnum
        {
            TracingOn,
            TracingOff,
            TracingInitialized,
            WaitTracingOn,
            WaitTracingOff,
            WaitTracingInitialized,
            PinRequired,
            Rebooting,
            AfterBoot,
            Ready,
            Unknown
        }

        public class FuzzingResult
        {
            public FuzzingResult(FuzzingStatusEnum fuzzingStatus, byte[] bitmap)
            {
                FuzzingStatus = fuzzingStatus;
                Bitmap = bitmap;
            }

            public FuzzingStatusEnum FuzzingStatus;

            public byte[] FuzzingStatusByte
            {
                get
                {
                    switch (FuzzingStatus)
                    {
                        case FuzzingStatusEnum.StatusSuccess: return new byte[] { 0 };
                        case FuzzingStatusEnum.StatusTimeout: return new byte[] {  1 };
                        case FuzzingStatusEnum.StatusCrash: return new byte[] { 2 };
                        case FuzzingStatusEnum.StatusQueueFull: return new byte[] {  3 };
                        case FuzzingStatusEnum.StatusCommError: return new byte[] { 4 };
                    }
                    return new byte[] { 0 };
                }
            }

            public byte[] Bitmap;
        }
    }
}
