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

using Tayx.Graphy.Graph;
using UnityEngine;
using UnityEngine.UI;

namespace Tayx.Graphy.Rtt
{

    public class G_RttGraph : G_Graph
    {
        /* ----- TODO: ----------------------------
         * Add summaries to the variables.
         * Add summaries to the functions.
         * Check if we should add a "RequireComponent" for "RttMonitor".
         * --------------------------------------*/

        #region Variables -> Serialized Private

        [SerializeField] private Image m_imageGraph = null;

        [SerializeField] private Shader ShaderFull = null;
        [SerializeField] private Shader ShaderLight = null;

        #endregion

        #region Variables -> Private

        private GraphyManager m_graphyManager = null;

        private G_RttMonitor m_rttMonitor = null;

        private int m_resolution = 150;

        private G_GraphShader m_shaderGraph = null;

        private int[] m_rttArray;

        private int m_highestRtt;

        #endregion

        #region Methods -> Unity Callbacks

        private void OnEnable()
        {
            Init();
        }

        private void Update()
        {
            UpdateGraph();
        }

        #endregion

        #region Methods -> Public

        public void UpdateParameters()
        {
            switch (m_graphyManager.GraphyMode)
            {
                case GraphyManager.Mode.FULL:
                    m_shaderGraph.ArrayMaxSize = G_GraphShader.ArrayMaxSizeFull;
                    m_shaderGraph.Image.material = new Material(ShaderFull);
                    break;

                case GraphyManager.Mode.LIGHT:
                    m_shaderGraph.ArrayMaxSize = G_GraphShader.ArrayMaxSizeLight;
                    m_shaderGraph.Image.material = new Material(ShaderLight);
                    break;
            }

            m_shaderGraph.InitializeShader();

            m_resolution = m_graphyManager.RttGraphResolution;

            CreatePoints();
        }

        #endregion

        #region Methods -> Protected Override

        protected override void UpdateGraph()
        {
            int rtt = (int)(1 / Time.unscaledDeltaTime);

            int currentMaxRtt = 0;

            for (int i = 0; i <= m_resolution - 1; i++)
            {
                if (i >= m_resolution - 1)
                {
                    m_rttArray[i] = rtt;
                }
                else
                {
                    m_rttArray[i] = m_rttArray[i + 1];
                }

                // Store the highest rtt to use as the highest point in the graph

                if (currentMaxRtt < m_rttArray[i])
                {
                    currentMaxRtt = m_rttArray[i];
                }

            }

            m_highestRtt = m_highestRtt < 1 || m_highestRtt <= currentMaxRtt ? currentMaxRtt : m_highestRtt - 1;

            for (int i = 0; i <= m_resolution - 1; i++)
            {
                m_shaderGraph.Array[i] = m_rttArray[i] / (float)m_highestRtt;
            }

            // Update the material values

            m_shaderGraph.UpdatePoints();

            m_shaderGraph.Average = m_rttMonitor.AverageRTT / m_highestRtt;
            m_shaderGraph.UpdateAverage();

            m_shaderGraph.GoodThreshold = (float)m_graphyManager.GoodRttThreshold / m_highestRtt;
            m_shaderGraph.CautionThreshold = (float)m_graphyManager.CautionRttThreshold / m_highestRtt;
            m_shaderGraph.UpdateThresholds();
        }

        protected override void CreatePoints()
        {
            m_shaderGraph.Array = new float[m_resolution];

            m_rttArray = new int[m_resolution];

            for (int i = 0; i < m_resolution; i++)
            {
                m_shaderGraph.Array[i] = 0;
            }

            m_shaderGraph.GoodColor = m_graphyManager.GoodRTTColor;
            m_shaderGraph.CautionColor = m_graphyManager.CautionRTTColor;
            m_shaderGraph.CriticalColor = m_graphyManager.CriticalRTTColor;

            m_shaderGraph.UpdateColors();

            m_shaderGraph.UpdateArray();
        }

        #endregion

        #region Methods -> Private

        private void Init()
        {
            m_graphyManager = transform.root.GetComponentInChildren<GraphyManager>();

            m_rttMonitor = GetComponent<G_RttMonitor>();

            m_shaderGraph = new G_GraphShader
            {
                Image = m_imageGraph
            };

            UpdateParameters();
        }

        #endregion
    }

}
