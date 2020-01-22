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

namespace Tayx.Graphy.Rtt
{
    public class G_RttMonitor : MonoBehaviour
    {
        /* ----- TODO: ----------------------------
         * Add summaries to the variables.
         * Add summaries to the functions.
         * --------------------------------------*/

        #region Variables -> Serialized Private

        [SerializeField] private int m_averageSamples = 120;

        #endregion

        #region Variables -> Private

        private GraphyManager m_graphyManager;

        // Rolling Float

        private FloatRollingAverage rtt;

        // Others 
        private float m_currentRtt = 0f;
        private float m_avgRtt = 0f;
        private float m_minRtt = 0f;
        private float m_maxRtt = 0f;

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

        #endregion

        #region Methods -> Public

        public void UpdateRtt(int _rtt)
        {
            m_currentRtt = _rtt;

            // Updating the public variables
            if (m_currentRtt > 0)
                rtt.Update(Mathf.Min(m_currentRtt, 999));

            // Update avg rtt
            m_avgRtt = rtt.average;
            // Update min rtt
            m_minRtt = rtt.min;
            // Update max rtt
            m_maxRtt = rtt.max;
        }


        public void UpdateParameters()
        {
            rtt.Reset();
        }

        #endregion

        #region Methods -> Private

        private void Init()
        {
            m_graphyManager = transform.root.GetComponentInChildren<GraphyManager>();

            rtt = new FloatRollingAverage(m_averageSamples);
        }

        #endregion
    }
}
