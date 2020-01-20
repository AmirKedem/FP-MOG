/* ---------------------------------------
 * Author:          Martin Pane (martintayx@gmail.com) (@tayx94)
 * Collaborators:   Lars Aalbertsen (@Rockylars)
 * Project:         Graphy - Ultimate Stats Monitor
 * Date:            15-Dec-17
 * Studio:          Tayx
 * 
 * This project is released under the MIT license.
 * Attribution is not required, but it is always welcomed!
 * -------------------------------------*/

using UnityEngine;
using System.Runtime.CompilerServices;

namespace Tayx.Graphy.Rtt
{
    public class G_RttMonitor : MonoBehaviour
    {
        /* ----- TODO: ----------------------------
         * Add summaries to the variables.
         * Add summaries to the functions.
         * --------------------------------------*/

        #region Variables -> Serialized Private

        [SerializeField] private int m_averageSamples = 200;

        #endregion

        #region Variables -> Private

        private GraphyManager m_graphyManager;

        private float m_currentRtt = 0f;
        private float m_avgRtt = 0f;
        private float m_minRtt = 0f;
        private float m_maxRtt = 0f;

        private float[] m_averageRttSamples;
        private int m_avgRttSamplesOffset = 0;
        private int m_indexMask = 0;
        private int m_avgRttSamplesCapacity = 0;
        private int m_avgRttSamplesCount = 0;
        private int m_timeToResetMinMaxRtt = 10;

        private float m_timeToResetMinRttPassed = 0f;
        private float m_timeToResetMaxRttPassed = 0f;

        private float unscaledDeltaTime = 0f;

        #endregion

        #region Properties -> Public

        public float CurrentRTT { get { return m_currentRtt; } }
        public float AverageRTT { get { return m_avgRtt; } }

        public float MinRTT { get { return m_minRtt; } }
        public float MaxRTT { get { return m_maxRtt; } }

        #endregion

        #region Methods -> Unity Callbacks

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            unscaledDeltaTime = Time.unscaledDeltaTime;

            m_timeToResetMinRttPassed += unscaledDeltaTime;
            m_timeToResetMaxRttPassed += unscaledDeltaTime;

            // Update rtt and ms

            m_currentRtt = 1 / unscaledDeltaTime;

            // Update avg rtt

            m_avgRtt = 0;

            m_averageRttSamples[ToBufferIndex(m_avgRttSamplesCount)] = m_currentRtt;
            m_avgRttSamplesOffset = ToBufferIndex(m_avgRttSamplesOffset + 1);

            if (m_avgRttSamplesCount < m_avgRttSamplesCapacity)
            {
                m_avgRttSamplesCount++;
            }

            for (int i = 0; i < m_avgRttSamplesCount; i++)
            {
                m_avgRtt += m_averageRttSamples[i];
            }

            m_avgRtt /= m_avgRttSamplesCount;

            // Checks to reset min and max rtt

            if (m_timeToResetMinMaxRtt > 0
                && m_timeToResetMinRttPassed > m_timeToResetMinMaxRtt)
            {
                m_minRtt = 0;
                m_timeToResetMinRttPassed = 0;
            }

            if (m_timeToResetMinMaxRtt > 0
                && m_timeToResetMaxRttPassed > m_timeToResetMinMaxRtt)
            {
                m_maxRtt = 0;
                m_timeToResetMaxRttPassed = 0;
            }

            // Update min rtt

            if (m_currentRtt < m_minRtt || m_minRtt <= 0)
            {
                m_minRtt = m_currentRtt;

                m_timeToResetMinRttPassed = 0;
            }

            // Update max rtt

            if (m_currentRtt > m_maxRtt || m_maxRtt <= 0)
            {
                m_maxRtt = m_currentRtt;

                m_timeToResetMaxRttPassed = 0;
            }
        }

        #endregion

        #region Methods -> Public

        public void UpdateParameters()
        {
            m_timeToResetMinMaxRtt = m_graphyManager.TimeToResetMinMaxRtt;
        }

        #endregion

        #region Methods -> Private

        private void Init()
        {
            m_graphyManager = transform.root.GetComponentInChildren<GraphyManager>();

            ResizeSamplesBuffer(m_averageSamples);

            UpdateParameters();
        }


        private void ResizeSamplesBuffer(int size)
        {
            m_avgRttSamplesCapacity = Mathf.NextPowerOfTwo(size);

            m_averageRttSamples = new float[m_avgRttSamplesCapacity];

            m_indexMask = m_avgRttSamplesCapacity - 1;
            m_avgRttSamplesOffset = 0;
        }

#if NET_4_6 || NET_STANDARD_2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int ToBufferIndex(int index)
        {
            return (index + m_avgRttSamplesOffset) & m_indexMask;
        }

        #endregion
    }
}
