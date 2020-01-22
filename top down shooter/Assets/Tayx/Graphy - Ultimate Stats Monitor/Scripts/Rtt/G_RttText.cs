/* ---------------------------------------
 * Author:          Martin Pane (martintayx@gmail.com) (@tayx94)
 * Collaborators:   Lars Aalbertsen (@Rockylars)
 * Project:         Graphy - Ultimate Stats Monitor
 * Date:            22-Nov-17
 * Studio:          Tayx
 * 
 * This project is released under the MIT license.
 * Attribution is not required, but it is always welcomed!
 * -------------------------------------*/

using UnityEngine;
using UnityEngine.UI;
using Tayx.Graphy.Utils.NumString;

namespace Tayx.Graphy.Rtt
{
    public class G_RttText : MonoBehaviour
    {
        /* ----- TODO: ----------------------------
         * Add summaries to the variables.
         * Add summaries to the functions.
         * Check if we should add a "RequireComponent" for "RttMonitor".
         * Improve the IntString Init to come from the core instead.
         * --------------------------------------*/

        #region Variables -> Serialized Private

        [SerializeField] private Text m_rttText = null;

        [SerializeField] private Text m_avgRttText = null;
        [SerializeField] private Text m_minRttText = null;
        [SerializeField] private Text m_maxRttText = null;

        #endregion

        #region Variables -> Private

        private GraphyManager m_graphyManager = null;

        private G_RttMonitor m_rttMonitor = null;

        private int m_updateRate = 4;  // 4 updates per sec.

        private float m_deltaTime = 0f;

        private const int m_minRtt = 0;
        private const int m_maxRtt = 10000;

        #endregion

        #region Methods -> Unity Callbacks

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            m_deltaTime += Time.unscaledDeltaTime;

            // Only update texts 'm_updateRate' times per second

            if (m_deltaTime > 1f / m_updateRate)
            {
                // Reset variable
                m_deltaTime = 0f;

                // Start Updating Text
                // Update rtt main field

                m_rttText.text = Mathf.RoundToInt(m_rttMonitor.CurrentRTT).ToStringNonAlloc();

                // Update min rtt

                m_minRttText.text = m_rttMonitor.MinRTT.ToInt().ToStringNonAlloc();

                SetRttRelatedTextColor(m_minRttText, m_rttMonitor.MinRTT);

                // Update max rtt

                m_maxRttText.text = m_rttMonitor.MaxRTT.ToInt().ToStringNonAlloc();

                SetRttRelatedTextColor(m_maxRttText, m_rttMonitor.MaxRTT);

                // Update avg rtt

                m_avgRttText.text = m_rttMonitor.AverageRTT.ToInt().ToStringNonAlloc();

                SetRttRelatedTextColor(m_avgRttText, m_rttMonitor.AverageRTT);
            }
        }

        #endregion

        #region Methods -> Public

        public void UpdateParameters()
        {
            m_updateRate = m_graphyManager.RttTextUpdateRate;
        }

        #endregion

        #region Methods -> Private

        /// <summary>
        /// Assigns color to a text according to their rtt numeric value and
        /// the colors specified in the 3 categories (Good, Caution, Critical).
        /// </summary>
        /// 
        /// <param name="text">
        /// UI Text component to change its color
        /// </param>
        /// 
        /// <param name="rtt">
        /// Numeric rtt value
        /// </param>
        private void SetRttRelatedTextColor(Text text, float rtt)
        {
            if (rtt > m_graphyManager.GoodRttThreshold)
            {
                text.color = m_graphyManager.GoodRTTColor;
            }
            else if (rtt > m_graphyManager.CautionRttThreshold)
            {
                text.color = m_graphyManager.CautionRTTColor;
            }
            else
            {
                text.color = m_graphyManager.CriticalRTTColor;
            }
        }

        private void Init()
        {
            //TODO: Replace this with one activated from the core and figure out the min value.
            if (!G_IntString.Inited || G_IntString.MinValue > m_minRtt || G_IntString.MaxValue < m_maxRtt)
            {
                G_IntString.Init
                (
                    minNegativeValue: m_minRtt,
                    maxPositiveValue: m_maxRtt
                );
            }

            m_graphyManager = transform.root.GetComponentInChildren<GraphyManager>();

            m_rttMonitor = GetComponent<G_RttMonitor>();

            UpdateParameters();
        }

        #endregion
    }
}
