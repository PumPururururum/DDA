using System.Collections.Generic;
using UnityEngine;

namespace _Project.Code.Scene.SpawnPoints
{
    public sealed class EnemySpawnPointsProvider : MonoBehaviour
    {
        [SerializeField] private Transform[] _spawnPoints;

        public IReadOnlyList<Transform> SpawnPoints => _spawnPoints;
    }
}
