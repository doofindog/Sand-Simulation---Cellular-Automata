using System;
using UnityEngine;
using UnityEngine.Serialization;

    public class ParticleManager : MonoBehaviour
    {
        [FormerlySerializedAs("_particleDatas")] [SerializeField] private ParticleData[] m_particleDatas;
        //private Dictionary<ParticleType, ParticleData> _particleDataDict;
        private static ParticleData[] s_particleDataLookUp;

        private static ParticleManager instance;

        public void Awake()
        {
            Init();
        }

        private void Init()
        {
            instance = this;
            s_particleDataLookUp = new ParticleData[Enum.GetValues(typeof(ParticleType)).Length];
            
            // === Rearranging Particles according to define enum ===
            for (int i = 0; i < s_particleDataLookUp.Length; i++)
            {
                var data = m_particleDatas[i];
                if (data == null)
                {
                    continue;
                }
                
                int idx = (int)data.particleType;
                if (s_particleDataLookUp[idx] != null)
                {
                    continue;
                }
                
                s_particleDataLookUp[idx] = data;
            }
        }

        // === Slightly Fast lookup ===
        public static ParticleData GetParticleData(ParticleType particleType) => s_particleDataLookUp[(byte)particleType];
        public static ParticleData GetParticleAtIndex(int index) => s_particleDataLookUp[index];
        
        // === Safe for Testing === //
        // public ParticleData GetDataInstance(ParticleType type) => s_particleDataLookUp[(byte)type];
        // public ParticleData GetParticleAtIndexInstance(int index) => s_particleDataLookUp[index];
    }
