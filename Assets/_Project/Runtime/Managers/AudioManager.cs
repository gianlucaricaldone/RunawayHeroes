using UnityEngine;

namespace RunawayHeroes.Runtime.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // Singleton pattern
        public static AudioManager Instance { get; private set; }
        
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
