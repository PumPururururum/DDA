using System;

namespace _Project.Code.Features.Test
{
    public enum CombatTeamId
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    [Serializable]
    public struct CombatTeam
    {
        public CombatTeamId Value;
    }
}
