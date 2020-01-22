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

namespace Tayx.Graphy.Fps
{
    public class G_FpsMonitor : MonoBehaviour
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

        private FloatRollingAverage fps;

        // Others 
        private float m_currentFps = 0f;
        private float m_avgFps = 0f;
        private float m_minFps = 0f;
        private float m_maxFps = 0f;

        private float unscaledDeltaTime = 0f;

        #endregion

        #region Properties -> Public

        public float CurrentFPS { get { return m_currentFps; } }
        public float AverageFPS { get { return m_avgFps; } }
        public float MinFPS { get { return m_minFps; } }
        public float MaxFPS { get { return m_maxFps; } }

        #endregion

        #region Methods -> Unity Callbacks

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            // Actual Fps Calculation
            unscaledDeltaTime = Time.unscaledDeltaTime;

            // Update fps and ms
            m_currentFps = 1 / unscaledDeltaTime;

            // End Actual Fps Calculation

            // Updating the public variables
            if (m_currentFps > 0)
                fps.Update(Mathf.Min(m_currentFps, 999));

            // Update avg fps
            m_avgFps = fps.average;
            // Update min fps
            m_minFps = fps.min;
            // Update max fps
            m_maxFps = fps.max;
        }

        #endregion

        #region Methods -> Public

        public void UpdateParameters()
        {
            fps.Reset();
        }

        #endregion

        #region Methods -> Private

        private void Init()
        {
            m_graphyManager = transform.root.GetComponentInChildren<GraphyManager>();

            fps = new FloatRollingAverage(m_averageSamples);
        }

        #endregion
    }
}
