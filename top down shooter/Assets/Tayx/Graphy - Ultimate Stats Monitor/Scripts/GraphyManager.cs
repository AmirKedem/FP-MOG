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

using System;
using UnityEngine;
using Tayx.Graphy.Fps;
using Tayx.Graphy.Rtt;
using Tayx.Graphy.Utils;
using Tayx.Graphy.Advanced;

namespace Tayx.Graphy
{
    //[ExecuteInEditMode]
    public class GraphyManager : G_Singleton<GraphyManager>
    {
        /* ----- TODO: ----------------------------
         * Add summaries to the variables.
         * Add summaries to the functions.
         * --------------------------------------*/

        protected GraphyManager () { }

        //Enums
        #region Enums -> Public

        public enum Mode
        {
            FULL            = 0,
            LIGHT           = 1
        }

        public enum ModuleType
        {
            FPS             = 0,
            RTT             = 1,
            ADVANCED        = 2
        }

        public enum ModuleState
        {
            FULL            = 0,
            TEXT            = 1,
            BASIC           = 2,
            BACKGROUND      = 3,
            OFF             = 4
        }

        public enum ModulePosition
        {
            TOP_RIGHT       = 0,
            TOP_LEFT        = 1,
            BOTTOM_RIGHT    = 2,
            BOTTOM_LEFT     = 3,
            FREE            = 4
        }

        public enum ModulePreset
        {
            FPS_BASIC = 0,
            FPS_TEXT  = 1,
            FPS_FULL  = 2,

            FPS_TEXT_RTT_TEXT = 3,
            FPS_FULL_RTT_TEXT = 4,
            FPS_FULL_RTT_FULL = 5,

            FPS_FULL_RTT_FULL_ADVANCED_FULL = 6
        }

        #endregion

        #region Variables -> Serialized Private

        [SerializeField] private    Mode                    m_graphyMode                        = Mode.FULL;

        [SerializeField] private    bool                    m_enableOnStartup                   = true;

        [SerializeField] private    bool                    m_keepAlive                         = true;
        
        [SerializeField] private    bool                    m_background                        = true;
        [SerializeField] private    Color                   m_backgroundColor                   = new Color(0, 0, 0, 0.3f);

        [SerializeField] private    bool                    m_enableHotkeys                     = true;

        [SerializeField] private    KeyCode                 m_toggleModeKeyCode                 = KeyCode.G;
        [SerializeField] private    bool                    m_toggleModeCtrl                    = true;
        [SerializeField] private    bool                    m_toggleModeAlt                     = false;

        [SerializeField] private    KeyCode                 m_toggleActiveKeyCode               = KeyCode.H;
        [SerializeField] private    bool                    m_toggleActiveCtrl                  = true;
        [SerializeField] private    bool                    m_toggleActiveAlt                   = false;
        
        [SerializeField] private    ModulePosition          m_graphModulePosition               = ModulePosition.TOP_RIGHT;
        
        // Fps ---------------------------------------------------------------------------

        [SerializeField] private    ModuleState             m_fpsModuleState                    = ModuleState.FULL;

        [Range(0, 200)]
        [Tooltip("Time (in seconds) to reset the minimum and maximum framerates if they don't change in the specified time. Set to 0 if you don't want it to reset.")]
        [SerializeField] private    int                     m_timeToResetMinMaxFps              = 10;

        [SerializeField] private    Color                   m_goodFpsColor                      = new Color32(118, 212, 58, 255);
        [SerializeField] private    int                     m_goodFpsThreshold                  = 50;

        [SerializeField] private    Color                   m_cautionFpsColor                   = new Color32(243, 232, 0, 255);
        [SerializeField] private    int                     m_cautionFpsThreshold               = 40;

        [SerializeField] private    Color                   m_criticalFpsColor                  = new Color32(220, 41, 30, 255);

        [Range(10, 300)]
        [SerializeField] private    int                     m_fpsGraphResolution                = 150;

        [Range(1, 200)]
        [SerializeField] private    int                     m_fpsTextUpdateRate                 = 3;  // 3 updates per sec.

        // Rtt ---------------------------------------------------------------------------

        [SerializeField] private ModuleState m_rttModuleState = ModuleState.FULL;

        [Range(0, 200)]
        [Tooltip("Time (in seconds) to reset the minimum and maximum rtts if they don't change in the specified time. Set to 0 if you don't want it to reset.")]
        [SerializeField] private int m_timeToResetMinMaxRtt = 10;

        [SerializeField] private Color m_goodRttColor = new Color32(118, 212, 58, 255);
        [SerializeField] private int m_goodRttThreshold = 50;

        [SerializeField] private Color m_cautionRttColor = new Color32(243, 232, 0, 255);
        [SerializeField] private int m_cautionRttThreshold = 40;

        [SerializeField] private Color m_criticalRttColor = new Color32(220, 41, 30, 255);

        [Range(10, 300)]
        [SerializeField] private int m_rttGraphResolution = 150;

        [Range(1, 200)]
        [SerializeField] private int m_rttTextUpdateRate = 3;  // 3 updates per sec.

        // Advanced ----------------------------------------------------------------------

        [SerializeField] private    ModulePosition          m_advancedModulePosition            = ModulePosition.BOTTOM_LEFT;

        [SerializeField] private    ModuleState             m_advancedModuleState               = ModuleState.FULL;

        #endregion

        #region Variables -> Private

        private                     bool                    m_initialized                       = false;
        private                     bool                    m_active                            = true;
        private                     bool                    m_focused                           = true;

        private                     G_FpsManager            m_fpsManager                        = null;
        private                     G_RttManager            m_rttManager                        = null;
        private                     G_AdvancedData          m_advancedData                      = null;

        private                     G_FpsMonitor            m_fpsMonitor                        = null;
        private                     G_RttMonitor            m_rttMonitor                        = null;

        private                     ModulePreset            m_modulePresetState                 = ModulePreset.FPS_FULL;

        #endregion

        //TODO: Maybe sort these into Get and GetSet sections.
        #region Properties -> Public

        public Mode GraphyMode                          { get { return m_graphyMode; }
                                                          set { m_graphyMode = value; UpdateAllParameters(); } }

        public bool EnableOnStartup                     { get { return m_enableOnStartup; } }

        public bool KeepAlive                           { get { return m_keepAlive; } }

        public bool Background                          { get { return m_background; } 
                                                          set { m_background = value; UpdateAllParameters(); } }

        public Color BackgroundColor                    { get { return m_backgroundColor; } 
                                                          set { m_backgroundColor = value; UpdateAllParameters(); } }

        public ModulePosition GraphModulePosition
        {
            get { return m_graphModulePosition; }
            set
            {
                m_graphModulePosition = value;
                m_fpsManager    .SetPosition(m_graphModulePosition);
                m_rttManager    .SetPosition(m_graphModulePosition);
            }
        }

        // Fps ---------------------------------------------------------------------------

        // Setters & Getters

        public ModuleState FpsModuleState
        {
            get { return m_fpsModuleState; }
            set { m_fpsModuleState = value; m_fpsManager.SetState(m_fpsModuleState); }
        }

        public int TimeToResetMinMaxFps
        {
            get { return m_timeToResetMinMaxFps; }
            set { m_timeToResetMinMaxFps = value; m_fpsManager.UpdateParameters(); }
        }

        public Color GoodFPSColor
        {
            get { return m_goodFpsColor; }
            set { m_goodFpsColor = value; m_fpsManager.UpdateParameters(); }
        }
        public Color CautionFPSColor
        {
            get { return m_cautionFpsColor; }
            set { m_cautionFpsColor = value; m_fpsManager.UpdateParameters(); }
        }
        public Color CriticalFPSColor
        {
            get { return m_criticalFpsColor; }
            set { m_criticalFpsColor = value; m_fpsManager.UpdateParameters(); }
        }

        public int GoodFpsThreshold
        {
            get { return m_goodFpsThreshold; }
            set { m_goodFpsThreshold = value; m_fpsManager.UpdateParameters(); }
        }
        public int CautionFpsThreshold
        {
            get { return m_cautionFpsThreshold; }
            set { m_cautionFpsThreshold = value; m_fpsManager.UpdateParameters(); }
        }

        public int FpsGraphResolution
        {
            get { return m_fpsGraphResolution; }
            set { m_fpsGraphResolution = value; m_fpsManager.UpdateParameters(); }
        }

        public int FpsTextUpdateRate
        {
            get { return m_fpsTextUpdateRate; }
            set { m_fpsTextUpdateRate = value; m_fpsManager.UpdateParameters(); }
        }

        // Getters

        public float CurrentFPS { get { return m_fpsMonitor.CurrentFPS; } }
        public float AverageFPS { get { return m_fpsMonitor.AverageFPS; } }
        public float MinFPS { get { return m_fpsMonitor.MinFPS; } }
        public float MaxFPS { get { return m_fpsMonitor.MaxFPS; } }

        // Rtt ---------------------------------------------------------------------------

        // Setters & Getters

        public ModuleState RttModuleState
        {
            get { return m_rttModuleState; }
            set { m_rttModuleState = value; m_rttManager.SetState(m_rttModuleState); }
        }

        public int TimeToResetMinMaxRtt
        {
            get { return m_timeToResetMinMaxRtt; }
            set { m_timeToResetMinMaxRtt = value; m_rttManager.UpdateParameters(); }
        }

        public Color GoodRTTColor
        {
            get { return m_goodRttColor; }
            set { m_goodRttColor = value; m_rttManager.UpdateParameters(); }
        }
        public Color CautionRTTColor
        {
            get { return m_cautionRttColor; }
            set { m_cautionRttColor = value; m_rttManager.UpdateParameters(); }
        }
        public Color CriticalRTTColor
        {
            get { return m_criticalRttColor; }
            set { m_criticalRttColor = value; m_rttManager.UpdateParameters(); }
        }

        public int GoodRttThreshold
        {
            get { return m_goodRttThreshold; }
            set { m_goodRttThreshold = value; m_rttManager.UpdateParameters(); }
        }
        public int CautionRttThreshold
        {
            get { return m_cautionRttThreshold; }
            set { m_cautionRttThreshold = value; m_rttManager.UpdateParameters(); }
        }

        public int RttGraphResolution
        {
            get { return m_rttGraphResolution; }
            set { m_rttGraphResolution = value; m_rttManager.UpdateParameters(); }
        }

        public int RttTextUpdateRate
        {
            get { return m_rttTextUpdateRate; }
            set { m_rttTextUpdateRate = value; m_rttManager.UpdateParameters(); }
        }

        // Getters

        public float CurrentRtt { get { return m_rttMonitor.CurrentRTT; } }
        public float AverageRtt { get { return m_rttMonitor.AverageRTT; } }
        public float MinRtt { get { return m_rttMonitor.MinRTT; } }
        public float MaxRtt { get { return m_rttMonitor.MaxRTT; } }

        // Advanced ---------------------------------------------------------------------

        // Setters & Getters

        public ModuleState AdvancedModuleState          { get { return m_advancedModuleState; } 
                                                          set { m_advancedModuleState = value; m_advancedData.SetState(m_advancedModuleState); } }
        
        public ModulePosition AdvancedModulePosition    { get { return m_advancedModulePosition; } 
                                                          set { m_advancedModulePosition = value; m_advancedData.SetPosition(m_advancedModulePosition); } }

        #endregion

        #region Methods -> Unity Callbacks

        private void Start()
        {
            Init();
        }

        private void Update()
        {
            if (m_focused && m_enableHotkeys)
            {
                CheckForHotkeyPresses();
            }
        }

        private void OnApplicationFocus(bool isFocused)
        {
            m_focused = isFocused;

            if (m_initialized && isFocused)
            {
                RefreshAllParameters();
            }
        }

        #endregion

        #region Methods -> Public

        public void SetModulePosition(ModuleType moduleType, ModulePosition modulePosition)
        {
            switch (moduleType)
            {
                case ModuleType.FPS:
                case ModuleType.RTT:
                    m_graphModulePosition = modulePosition;

                    m_rttManager.SetPosition(modulePosition);
                    m_fpsManager.SetPosition(modulePosition);
                    break;

                case ModuleType.ADVANCED:
                    m_advancedData.SetPosition(modulePosition);
                    break;
            }
        }

        public void SetModuleMode(ModuleType moduleType, ModuleState moduleState)
        {
            switch (moduleType)
            {
                case ModuleType.FPS:
                    m_fpsManager.SetState(moduleState);
                    break;

                case ModuleType.RTT:
                    m_rttManager.SetState(moduleState);
                    break;

                case ModuleType.ADVANCED:
                    m_advancedData.SetState(moduleState);
                    break;
            }
        }

        public void ToggleModes()
        {
            int len = Enum.GetNames(typeof(ModulePreset)).Length;
            m_modulePresetState = (ModulePreset) (((int)m_modulePresetState + 1) % len);
            SetPreset(m_modulePresetState);
        }

        public void SetPreset(ModulePreset modulePreset)
        {
            m_modulePresetState = modulePreset;

            switch (m_modulePresetState)
            {
                case ModulePreset.FPS_BASIC:
                    m_fpsManager.SetState(ModuleState.BASIC);
                    m_rttManager.SetState(ModuleState.OFF);
                    m_advancedData.SetState(ModuleState.OFF);
                    break;

                case ModulePreset.FPS_TEXT:
                    m_fpsManager.SetState(ModuleState.TEXT);
                    m_rttManager.SetState(ModuleState.OFF);
                    m_advancedData.SetState(ModuleState.OFF);
                    break;

                case ModulePreset.FPS_FULL:
                    m_fpsManager.SetState(ModuleState.FULL);
                    m_rttManager.SetState(ModuleState.OFF);
                    m_advancedData.SetState(ModuleState.OFF);
                    break;

                case ModulePreset.FPS_TEXT_RTT_TEXT:
                    m_fpsManager.SetState(ModuleState.TEXT);
                    m_rttManager.SetState(ModuleState.TEXT);
                    m_advancedData.SetState(ModuleState.OFF);
                    break;

                case ModulePreset.FPS_FULL_RTT_TEXT:
                    m_fpsManager.SetState(ModuleState.FULL);
                    m_rttManager.SetState(ModuleState.TEXT);
                    m_advancedData.SetState(ModuleState.OFF);
                    break;

                case ModulePreset.FPS_FULL_RTT_FULL:
                    m_fpsManager.SetState(ModuleState.FULL);
                    m_rttManager.SetState(ModuleState.FULL);
                    m_advancedData.SetState(ModuleState.OFF);
                    break;

                case ModulePreset.FPS_FULL_RTT_FULL_ADVANCED_FULL:
                    m_fpsManager.SetState(ModuleState.FULL);
                    m_rttManager.SetState(ModuleState.FULL);
                    m_advancedData.SetState(ModuleState.FULL);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ToggleActive()
        {
            if (!m_active)
            {
                Enable();
            }
            else
            {
                Disable();
            }
        }

        public void Enable()
        {
            m_fpsManager    .RestorePreviousState();
            m_rttManager    .RestorePreviousState();
            m_advancedData  .RestorePreviousState();

            m_active = true;
        }

        public void Disable()
        {
            m_fpsManager    .SetState(ModuleState.OFF);
            m_rttManager    .SetState(ModuleState.OFF);
            m_advancedData  .SetState(ModuleState.OFF);

            m_active = false;
        }

        #endregion

        #region Methods -> Private

        private void Init()
        {
            if (m_keepAlive)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }
            
            m_fpsMonitor    = GetComponentInChildren(typeof(G_FpsMonitor),    true) as G_FpsMonitor;
            m_rttMonitor    = GetComponentInChildren(typeof(G_RttMonitor),    true) as G_RttMonitor;
            
            m_fpsManager    = GetComponentInChildren(typeof(G_FpsManager),    true) as G_FpsManager;
            m_rttManager    = GetComponentInChildren(typeof(G_RttManager),    true) as G_RttManager;
            m_advancedData  = GetComponentInChildren(typeof(G_AdvancedData),  true) as G_AdvancedData;

            m_fpsManager    .SetPosition(m_graphModulePosition);
            m_rttManager    .SetPosition(m_graphModulePosition);
            m_advancedData  .SetPosition(m_advancedModulePosition);

            m_fpsManager    .SetState   (m_fpsModuleState);
            m_rttManager    .SetState   (m_rttModuleState);
            m_advancedData  .SetState   (m_advancedModuleState);

            if (!m_enableOnStartup)
            {
                ToggleActive();

                // We need to enable this on startup because we disable it in GraphyManagerEditor
                GetComponent<Canvas>().enabled = true;
            }

            // Set the Preset as the chosen Preset.
            // If the ModulePreset has changed we call set preset here to init with the correct selected state.
            SetPreset(m_modulePresetState);

            m_initialized = true;
        }

        private void CheckForHotkeyPresses()
        {
            // Toggle Mode ---------------------------------------

            if (m_toggleModeCtrl && m_toggleModeAlt)
            {
                if (CheckFor3KeyPress(m_toggleModeKeyCode, KeyCode.LeftControl, KeyCode.LeftAlt)
                    || CheckFor3KeyPress(m_toggleModeKeyCode, KeyCode.RightControl, KeyCode.LeftAlt)
                    || CheckFor3KeyPress(m_toggleModeKeyCode, KeyCode.RightControl, KeyCode.RightAlt)
                    || CheckFor3KeyPress(m_toggleModeKeyCode, KeyCode.LeftControl, KeyCode.RightAlt))
                {
                    ToggleModes();
                }
            }
            else if (m_toggleModeCtrl)
            {
                if (    CheckFor2KeyPress(m_toggleModeKeyCode, KeyCode.LeftControl)
                    ||  CheckFor2KeyPress(m_toggleModeKeyCode, KeyCode.RightControl))
                {
                    ToggleModes();
                }
            }
            else if (m_toggleModeAlt)
            {
                if (    CheckFor2KeyPress(m_toggleModeKeyCode, KeyCode.LeftAlt)
                    ||  CheckFor2KeyPress(m_toggleModeKeyCode, KeyCode.RightAlt))
                {
                    ToggleModes();
                }
            }
            else
            {
                if (CheckFor1KeyPress(m_toggleModeKeyCode))
                {
                    ToggleModes();
                }
            }

            // Toggle Active ---------------------------------------

            if (m_toggleActiveCtrl && m_toggleActiveAlt)
            {
                if (    CheckFor3KeyPress(m_toggleActiveKeyCode, KeyCode.LeftControl, KeyCode.LeftAlt)
                    ||  CheckFor3KeyPress(m_toggleActiveKeyCode, KeyCode.RightControl, KeyCode.LeftAlt)
                    ||  CheckFor3KeyPress(m_toggleActiveKeyCode, KeyCode.RightControl, KeyCode.RightAlt)
                    ||  CheckFor3KeyPress(m_toggleActiveKeyCode, KeyCode.LeftControl, KeyCode.RightAlt))
                {
                    ToggleActive();
                }
            }
            
            else if (m_toggleActiveCtrl)
            {
                if (    CheckFor2KeyPress(m_toggleActiveKeyCode, KeyCode.LeftControl)
                    ||  CheckFor2KeyPress(m_toggleActiveKeyCode, KeyCode.RightControl))
                {
                    ToggleActive();
                }
            }
            else if (m_toggleActiveAlt)
            {
                if (    CheckFor2KeyPress(m_toggleActiveKeyCode, KeyCode.LeftAlt)
                    ||  CheckFor2KeyPress(m_toggleActiveKeyCode, KeyCode.RightAlt))
                {
                    ToggleActive();
                }
            }
            else
            {
                if (CheckFor1KeyPress(m_toggleActiveKeyCode))
                {
                    ToggleActive();
                }
            }
        }

        private bool CheckFor1KeyPress(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        private bool CheckFor2KeyPress(KeyCode key1, KeyCode key2)
        {
            return Input.GetKeyDown(key1) && Input.GetKey(key2)
                || Input.GetKeyDown(key2) && Input.GetKey(key1);
        }

        private bool CheckFor3KeyPress(KeyCode key1, KeyCode key2, KeyCode key3)
        {
            return Input.GetKeyDown(key1) && Input.GetKey(key2) && Input.GetKey(key3)
                || Input.GetKeyDown(key2) && Input.GetKey(key1) && Input.GetKey(key3)
                || Input.GetKeyDown(key3) && Input.GetKey(key1) && Input.GetKey(key2);
        }

        private void UpdateAllParameters()
        {
            m_fpsManager    .UpdateParameters();
            m_rttManager    .UpdateParameters();
            m_advancedData  .UpdateParameters();
        }

        private void RefreshAllParameters()
        {
            m_fpsManager    .RefreshParameters();
            m_rttManager    .RefreshParameters();
            m_advancedData  .RefreshParameters();
        }
        
        #endregion
    }
}