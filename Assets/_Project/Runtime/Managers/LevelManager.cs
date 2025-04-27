using UnityEngine;

namespace RunawayHeroes.Runtime.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        // Singleton pattern
        public static LevelManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
