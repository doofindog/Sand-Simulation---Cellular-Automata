namespace Azen.Logger
{
    public static partial class CustomLogger
    {
        public enum LogCategory
        {
            None = 0,
            System = 1,
            UI = 2,
            Network = 3,
            Gameplay = 4,
            Audio = 5,
            Error = 6,
            Warning = 7,
            Other = 8,
            ParticleLogic = 9,
            WorldChunk = 10,
            ResistanceCheck = 11,
        }
    }
}
