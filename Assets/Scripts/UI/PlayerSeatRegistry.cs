namespace ScrewGame.UI
{
    /// <summary>Registry mapping player IDs to seat indices.</summary>
    public static class PlayerSeatRegistry
    {
        private static readonly System.Collections.Generic.Dictionary<string, int> _seatMap = new();

        public static void Register(string playerId, int seatIndex) => _seatMap[playerId] = seatIndex;
        public static int GetLocalSeat(string playerId)
            => _seatMap.TryGetValue(playerId, out int seat) ? seat : 0;
    }
}
