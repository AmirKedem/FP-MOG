/* ---------------------------------------
 * Author:          Martin Pane (martintayx@gmail.com) (@tayx94)
 * Collaborators:   Lars Aalbertsen (@Rockylars)
 * Project:         Graphy - Ultimate Stats Monitor
 * Date:            20-Dec-17
 * Studio:          Tayx
 * 
 * This project is released under the MIT license.
 * Attribution is not required, but it is always welcomed!
 * -------------------------------------*/

using System;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace Tayx.Graphy
{
    [CustomEditor(typeof(GraphyManager))]
    internal class GraphyManagerEditor : Editor
    {
        /* ----- TODO: ----------------------------
         * Add summaries to the variables.
         * Add summaries to the functions.
         * --------------------------------------*/

        #region Variables -> Private

        private GraphyManager       m_target;

        private GUISkin             m_skin;

        private GUIStyle            m_headerStyle1;
        private GUIStyle            m_headerStyle2;

        private Texture2D           m_logoTexture;

        #region Section -> Settings

        private SerializedProperty  m_graphyMode;

        private SerializedProperty  m_enableOnStartup;

        private SerializedProperty  m_keepAlive;

        private SerializedProperty  m_background;
        private SerializedProperty  m_backgroundColor;

        private SerializedProperty  m_enableHotkeys;

        private SerializedProperty  m_toggleModeKeyCode;
        private SerializedProperty  m_toggleModeCtrl;
        private SerializedProperty  m_toggleModeAlt;

        private SerializedProperty  m_toggleActiveKeyCode;
        private SerializedProperty  m_toggleActiveCtrl;
        private SerializedProperty  m_toggleActiveAlt;


        private SerializedProperty  m_graphModulePosition;

        #endregion

        #region Section -> FPS

        private bool                m_fpsModuleInspectorToggle          = true;
            
        private SerializedProperty  m_fpsModuleState;

        private SerializedProperty  m_timeToResetMinMaxFps;

        private SerializedProperty  m_goodFpsColor;
        private SerializedProperty  m_goodFpsThreshold;

        private SerializedProperty  m_cautionFpsColor;
        private SerializedProperty  m_cautionFpsThreshold;

        private SerializedProperty  m_criticalFpsColor;

        private SerializedProperty  m_fpsGraphResolution;

        private SerializedProperty  m_fpsTextUpdateRate;

        #endregion

        #region Section -> RTT

        private bool m_rttModuleInspectorToggle = true;

        private SerializedProperty m_rttModuleState;

        private SerializedProperty m_timeToResetMinMaxRtt;

        private SerializedProperty m_goodRttColor;
        private SerializedProperty m_goodRttThreshold;

        private SerializedProperty m_cautionRttColor;
        private SerializedProperty m_cautionRttThreshold;

        private SerializedProperty m_criticalRttColor;

        private SerializedProperty m_rttGraphResolution;

        private SerializedProperty m_rttTextUpdateRate;

        #endregion

        #region Section -> Advanced Settings

        private bool                m_advancedModuleInspectorToggle     = true;
            
        private SerializedProperty  m_advancedModulePosition;

        private SerializedProperty  m_advancedModuleState;

        #endregion

        #endregion

        #region Methods -> Unity Callbacks

        private void OnEnable()
        {
            m_target                            = (GraphyManager)target;

            SerializedObject serObj             = serializedObject;

            #region Section -> Settings

            m_graphyMode                        = serObj.FindProperty("m_graphyMode");

            m_enableOnStartup                   = serObj.FindProperty("m_enableOnStartup");

            m_keepAlive                         = serObj.FindProperty("m_keepAlive");

            m_background                        = serObj.FindProperty("m_background");
            m_backgroundColor                   = serObj.FindProperty("m_backgroundColor");

            m_enableHotkeys                     = serObj.FindProperty("m_enableHotkeys");

            m_toggleModeKeyCode                 = serObj.FindProperty("m_toggleModeKeyCode");

            m_toggleModeCtrl                    = serObj.FindProperty("m_toggleModeCtrl");
            m_toggleModeAlt                     = serObj.FindProperty("m_toggleModeAlt");

            m_toggleActiveKeyCode               = serObj.FindProperty("m_toggleActiveKeyCode");

            m_toggleActiveCtrl                  = serObj.FindProperty("m_toggleActiveCtrl");
            m_toggleActiveAlt                   = serObj.FindProperty("m_toggleActiveAlt");

            m_graphModulePosition               = serObj.FindProperty("m_graphModulePosition");

            #endregion

            #region Section -> FPS

            m_fpsModuleState                    = serObj.FindProperty("m_fpsModuleState");

            m_timeToResetMinMaxFps              = serObj.FindProperty("m_timeToResetMinMaxFps");

            m_goodFpsColor                      = serObj.FindProperty("m_goodFpsColor");
            m_goodFpsThreshold                  = serObj.FindProperty("m_goodFpsThreshold");

            m_cautionFpsColor                   = serObj.FindProperty("m_cautionFpsColor");
            m_cautionFpsThreshold               = serObj.FindProperty("m_cautionFpsThreshold");

            m_criticalFpsColor                  = serObj.FindProperty("m_criticalFpsColor");

            m_fpsGraphResolution                = serObj.FindProperty("m_fpsGraphResolution");

            m_fpsTextUpdateRate                 = serObj.FindProperty("m_fpsTextUpdateRate");

            #endregion

            #region Section -> RTT

            m_rttModuleState = serObj.FindProperty("m_rttModuleState");

            m_timeToResetMinMaxRtt = serObj.FindProperty("m_timeToResetMinMaxRtt");

            m_goodRttColor = serObj.FindProperty("m_goodRttColor");
            m_goodRttThreshold = serObj.FindProperty("m_goodRttThreshold");

            m_cautionRttColor = serObj.FindProperty("m_cautionRttColor");
            m_cautionRttThreshold = serObj.FindProperty("m_cautionRttThreshold");

            m_criticalRttColor = serObj.FindProperty("m_criticalRttColor");

            m_rttGraphResolution = serObj.FindProperty("m_rttGraphResolution");

            m_rttTextUpdateRate = serObj.FindProperty("m_rttTextUpdateRate");

            #endregion

            #region Section -> Advanced Settings

            m_advancedModulePosition = serObj.FindProperty("m_advancedModulePosition");

            m_advancedModuleState               = serObj.FindProperty("m_advancedModuleState");

            #endregion

        }

        #endregion

        #region Methods -> Public Override

        public override void OnInspectorGUI()
        {
            if (m_target == null && target == null)
            {
                base.OnInspectorGUI();
                return;
            }

            LoadGuiStyles();

            float defaultLabelWidth = EditorGUIUtility.labelWidth;
            float defaultFieldWidth = EditorGUIUtility.fieldWidth;

            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                font            = m_headerStyle2.font,
                fontStyle       = m_headerStyle2.fontStyle,
                contentOffset   = Vector2.down * 3f //TODO: Maybe replace this with "new Vector2(0f, -3f);"
            };

            SetGuiStyleFontColor
            (
                guiStyle:   foldoutStyle,
                color:      EditorGUIUtility.isProSkin ? Color.white : Color.black
            );

            //===== CONTENT REGION ========================================================================

            GUILayout.Space(20);

            #region Section -> Logo

            if (m_logoTexture != null)
            {
                GUILayout.Label
                (
                    image: m_logoTexture,
                    style: new GUIStyle(GUI.skin.GetStyle("Label"))
                    {
                        alignment = TextAnchor.UpperCenter
                    }
                );

                GUILayout.Space(10);
            }
            else
            {
                EditorGUILayout.LabelField
                (
                    label: "[ GRAPHY - MANAGER ]",
                    style: m_headerStyle1
                );
            }

            #endregion

            GUILayout.Space(5); //Extra pixels added when the logo is used.

            #region Section -> Settings

            EditorGUIUtility.labelWidth = 130;
            EditorGUIUtility.fieldWidth = 35;

            EditorGUILayout.PropertyField
            (
                m_graphyMode,
                new GUIContent
                (
                    text:       "Graphy Mode",
                    tooltip:    "LIGHT mode increases compatibility with mobile and older, less powerful GPUs, but reduces the maximum graph resolutions to 128."
                )
            );

            GUILayout.Space(10);

            m_enableOnStartup.boolValue = EditorGUILayout.Toggle
            (
                new GUIContent
                (
                    text:       "Enable On Startup",
                    tooltip:    "If ticked, Graphy will be displayed by default on startup, otherwise it will initiate and hide."
                ),
                value:          m_enableOnStartup.boolValue
            );

            // This is a neat trick to hide Graphy in the Scene if it's going to be deactivated in play mode so that it doesn't use screen space.
            if (!Application.isPlaying)
            {
                m_target.GetComponent<Canvas>().enabled = m_enableOnStartup.boolValue;
            }

            m_keepAlive.boolValue = EditorGUILayout.Toggle
            (
                new GUIContent
                (
                    text:       "Keep Alive",
                    tooltip:    "If ticked, it will survive scene changes.\n\nCAREFUL, if you set Graphy as a child of another GameObject, the root GameObject will also survive scene changes. If you want to avoid that put Graphy in the root of the Scene as its own entity."
                ),
                value:          m_keepAlive.boolValue
            );
               
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            m_background.boolValue = EditorGUILayout.Toggle
            (
                new GUIContent
                (
                    text:       "Background",
                    tooltip:    "If ticked, it will show a background overlay to improve readability in cluttered scenes."
                ),
                value:          m_background.boolValue
            );

            m_backgroundColor.colorValue = EditorGUILayout.ColorField(m_backgroundColor.colorValue);

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            m_enableHotkeys.boolValue = EditorGUILayout.Toggle
            (
                new GUIContent
                (
                    text:       "Enable Hotkeys",
                    tooltip:    "If ticked, it will enable the hotkeys to be able to modify Graphy in runtime with custom keyboard shortcuts."
                ),
                value:          m_enableHotkeys.boolValue
            );

            if (m_enableHotkeys.boolValue)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.labelWidth = 130;
                EditorGUIUtility.fieldWidth = 35;

                EditorGUILayout.PropertyField
                (
                    m_toggleModeKeyCode,
                    new GUIContent
                    (
                        text:       "Toggle Mode Key",
                        tooltip:    "If ticked, it will require clicking this key and the other ones you have set up."
                    )
                );

                EditorGUIUtility.labelWidth = 30;
                EditorGUIUtility.fieldWidth = 35;

                m_toggleModeCtrl.boolValue = EditorGUILayout.Toggle
                (
                    new GUIContent
                    (
                        text:       "Ctrl",
                        tooltip:    "If ticked, it will require clicking Ctrl and the other keys you have set up."
                    ),
                    value:          m_toggleModeCtrl.boolValue
                );

                m_toggleModeAlt.boolValue = EditorGUILayout.Toggle
                (
                    new GUIContent
                    (
                        text:       "Alt",
                        tooltip:    "If ticked, it will require clicking Alt and the other keys you have set up."
                    ),
                    value:          m_toggleModeAlt.boolValue
                );

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.labelWidth = 130;
                EditorGUIUtility.fieldWidth = 35;

                EditorGUILayout.PropertyField
                (
                    m_toggleActiveKeyCode,
                    new GUIContent
                    (
                        text:       "Toggle Active Key",
                        tooltip:    "If ticked, it will require clicking this key and the other ones you have set up."
                    )
                );

                EditorGUIUtility.labelWidth = 30;
                EditorGUIUtility.fieldWidth = 35;

                m_toggleActiveCtrl.boolValue = EditorGUILayout.Toggle
                (
                    new GUIContent
                    (
                        text:       "Ctrl",
                        tooltip:    "If ticked, it will require clicking Ctrl and the other kesy you have set up."
                    ),
                    value:          m_toggleActiveCtrl.boolValue
                );

                m_toggleActiveAlt.boolValue = EditorGUILayout.Toggle
                (
                    new GUIContent
                    (
                        text:       "Alt",
                        tooltip:    "If ticked, it will require clicking Alt and the other keys you have set up."
                    ),
                    value:          m_toggleActiveAlt.boolValue
                );

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(15);

            EditorGUIUtility.labelWidth = 155;
            EditorGUIUtility.fieldWidth = 35;

            EditorGUILayout.PropertyField
            (
                m_graphModulePosition,
                new GUIContent
                (
                    text:       "Graph modules position",
                    tooltip:    "Defines in which corner the modules will be located."
                )
            );

            #endregion

            GUILayout.Space(20);

            #region Section -> FPS

            m_fpsModuleInspectorToggle = EditorGUILayout.Foldout
            (
                m_fpsModuleInspectorToggle,
                content:    " [ FPS ]",
                style:      foldoutStyle
            );
            
            GUILayout.Space(5);

            if (m_fpsModuleInspectorToggle)
            {
                EditorGUILayout.PropertyField
                (
                    m_fpsModuleState,
                    new GUIContent
                    (
                        text:       "Module state",
                        tooltip:    "FULL -> Text + Graph \nTEXT -> Just text \nOFF -> Turned off"
                    )
                );

                GUILayout.Space(5);

                EditorGUILayout.LabelField("Fps thresholds and colors:");

                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();

                m_goodFpsThreshold.intValue = EditorGUILayout.IntField
                (
                    new GUIContent
                    (
                        text:       "- Good",
                        tooltip:    "When FPS rise above this value, this color will be used."
                    ),
                    value:          m_goodFpsThreshold.intValue
                );
                
                m_goodFpsColor.colorValue = EditorGUILayout.ColorField(m_goodFpsColor.colorValue);

                EditorGUILayout.EndHorizontal();

                if (m_goodFpsThreshold.intValue <= m_cautionFpsThreshold.intValue && m_goodFpsThreshold.intValue > 1)
                {
                    m_cautionFpsThreshold.intValue = m_goodFpsThreshold.intValue - 1;
                }
                else if (m_goodFpsThreshold.intValue <= 1)
                {
                    m_goodFpsThreshold.intValue = 2;
                }

                EditorGUILayout.BeginHorizontal();

                m_cautionFpsThreshold.intValue = EditorGUILayout.IntField
                (
                    new GUIContent
                    (
                        text:       "- Caution",
                        tooltip:    "When FPS falls between this and the Good value, this color will be used."
                    ),
                    value:          m_cautionFpsThreshold.intValue
                );

                m_cautionFpsColor.colorValue = EditorGUILayout.ColorField(m_cautionFpsColor.colorValue);

                EditorGUILayout.EndHorizontal();

                if (m_cautionFpsThreshold.intValue >= m_goodFpsThreshold.intValue)
                {
                    m_cautionFpsThreshold.intValue = m_goodFpsThreshold.intValue - 1;
                }
                else if (m_cautionFpsThreshold.intValue <= 0)
                {
                    m_cautionFpsThreshold.intValue = 1;
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.IntField
                (
                    new GUIContent
                    (
                        text:       "- Critical",
                        tooltip:    "When FPS falls below the Caution value, this color will be used. (You can't have negative FPS, so this value is just for reference, it can't be changed)."
                    ),
                    value:          0
                );

                m_criticalFpsColor.colorValue = EditorGUILayout.ColorField(m_criticalFpsColor.colorValue);

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;

                if (m_fpsModuleState.intValue == 0)
                {
                    m_fpsGraphResolution.intValue = EditorGUILayout.IntSlider
                    (
                        new GUIContent
                        (
                            text:       "Graph resolution",
                            tooltip:    "Defines the amount of points in the graph"
                        ),
                        m_fpsGraphResolution.intValue,
                        leftValue:      20,
                        rightValue:     m_graphyMode.intValue == 0 ? 300 : 128
                    );
                }

                EditorGUIUtility.labelWidth = 180;
                EditorGUIUtility.fieldWidth = 35;

                m_timeToResetMinMaxFps.intValue = EditorGUILayout.IntSlider
                (
                    new GUIContent
                    (
                        text:       "Time to reset min/max values",
                        tooltip:    "If the min/max value doesn't change in the specified time, they will be reset. This allows tracking the min/max fps in a shorter interval. \n\nSet it to 0 if you don't want it to reset."
                    ),
                    m_timeToResetMinMaxFps.intValue,
                    leftValue:      0,
                    rightValue:     120
                );

                EditorGUIUtility.labelWidth = 155;
                EditorGUIUtility.fieldWidth = 35;

                m_fpsTextUpdateRate.intValue = EditorGUILayout.IntSlider
                (
                    new GUIContent
                    (
                        text:       "Text update rate",
                        tooltip:    "Defines the amount times the text is updated in 1 second."
                    ),
                    m_fpsTextUpdateRate.intValue,
                    leftValue:      1,
                    rightValue:     60
                );
            }

            #endregion

            GUILayout.Space(20);

            #region Section -> RTT

            m_rttModuleInspectorToggle = EditorGUILayout.Foldout
            (
                m_rttModuleInspectorToggle,
                content: " [ RTT ]",
                style: foldoutStyle
            );

            GUILayout.Space(5);

            if (m_rttModuleInspectorToggle)
            {
                EditorGUILayout.PropertyField
                (
                    m_rttModuleState,
                    new GUIContent
                    (
                        text: "Module state",
                        tooltip: "FULL -> Text + Graph \nTEXT -> Just text \nOFF -> Turned off"
                    )
                );

                GUILayout.Space(5);

                EditorGUILayout.LabelField("Rtt thresholds and colors:");

                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();

                m_goodRttThreshold.intValue = EditorGUILayout.IntField
                (
                    new GUIContent
                    (
                        text: "- Good",
                        tooltip: "When RTT rise above this value, this color will be used."
                    ),
                    value: m_goodRttThreshold.intValue
                );

                m_goodRttColor.colorValue = EditorGUILayout.ColorField(m_goodRttColor.colorValue);

                EditorGUILayout.EndHorizontal();

                if (m_goodRttThreshold.intValue <= m_cautionRttThreshold.intValue && m_goodRttThreshold.intValue > 1)
                {
                    m_cautionRttThreshold.intValue = m_goodRttThreshold.intValue - 1;
                }
                else if (m_goodRttThreshold.intValue <= 1)
                {
                    m_goodRttThreshold.intValue = 2;
                }

                EditorGUILayout.BeginHorizontal();

                m_cautionRttThreshold.intValue = EditorGUILayout.IntField
                (
                    new GUIContent
                    (
                        text: "- Caution",
                        tooltip: "When RTT falls between this and the Good value, this color will be used."
                    ),
                    value: m_cautionRttThreshold.intValue
                );

                m_cautionRttColor.colorValue = EditorGUILayout.ColorField(m_cautionRttColor.colorValue);

                EditorGUILayout.EndHorizontal();

                if (m_cautionRttThreshold.intValue >= m_goodRttThreshold.intValue)
                {
                    m_cautionRttThreshold.intValue = m_goodRttThreshold.intValue - 1;
                }
                else if (m_cautionRttThreshold.intValue <= 0)
                {
                    m_cautionRttThreshold.intValue = 1;
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.IntField
                (
                    new GUIContent
                    (
                        text: "- Critical",
                        tooltip: "When RTT falls below the Caution value, this color will be used. (You can't have negative RTT, so this value is just for reference, it can't be changed)."
                    ),
                    value: 0
                );

                m_criticalRttColor.colorValue = EditorGUILayout.ColorField(m_criticalRttColor.colorValue);

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;

                if (m_rttModuleState.intValue == 0)
                {
                    m_rttGraphResolution.intValue = EditorGUILayout.IntSlider
                    (
                        new GUIContent
                        (
                            text: "Graph resolution",
                            tooltip: "Defines the amount of points in the graph"
                        ),
                        m_rttGraphResolution.intValue,
                        leftValue: 20,
                        rightValue: m_graphyMode.intValue == 0 ? 300 : 128
                    );
                }

                EditorGUIUtility.labelWidth = 180;
                EditorGUIUtility.fieldWidth = 35;

                m_timeToResetMinMaxRtt.intValue = EditorGUILayout.IntSlider
                (
                    new GUIContent
                    (
                        text: "Time to reset min/max values",
                        tooltip: "If the min/max value doesn't change in the specified time, they will be reset. This allows tracking the min/max rtt in a shorter interval. \n\nSet it to 0 if you don't want it to reset."
                    ),
                    m_timeToResetMinMaxRtt.intValue,
                    leftValue: 0,
                    rightValue: 120
                );

                EditorGUIUtility.labelWidth = 155;
                EditorGUIUtility.fieldWidth = 35;

                m_rttTextUpdateRate.intValue = EditorGUILayout.IntSlider
                (
                    new GUIContent
                    (
                        text: "Text update rate",
                        tooltip: "Defines the amount times the text is updated in 1 second."
                    ),
                    m_rttTextUpdateRate.intValue,
                    leftValue: 1,
                    rightValue: 60
                );
            }

            #endregion

            GUILayout.Space(20);

            #region Section -> Advanced Settings

            m_advancedModuleInspectorToggle = EditorGUILayout.Foldout
            (
                m_advancedModuleInspectorToggle,
                content:    " [ ADVANCED DATA ]",
                style:      foldoutStyle
            );

            GUILayout.Space(5);

            if (m_advancedModuleInspectorToggle)
            {
                EditorGUILayout.PropertyField(m_advancedModulePosition);

                EditorGUILayout.PropertyField
                (
                    m_advancedModuleState,
                    new GUIContent
                    (
                        text:       "Module state",
                        tooltip:    "FULL -> Text \nOFF -> Turned off"
                    )
                );
            }

            #endregion;

            EditorGUIUtility.labelWidth = defaultLabelWidth;
            EditorGUIUtility.fieldWidth = defaultFieldWidth;

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Methods -> Private

        private void LoadGuiStyles()
        {
            string path = GetMonoScriptFilePath(this);

            path = path.Split(separator: new string[] { "Assets" }, options: StringSplitOptions.None)[1]
                       .Split(separator: new string[] { "Tayx"   }, options: StringSplitOptions.None)[0];

            m_logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>
            (
                "Assets" +
                path +
                "Tayx/Graphy - Ultimate Stats Monitor/Textures/Manager_Logo_" +
                (EditorGUIUtility.isProSkin ? "White.png" : "Dark.png")
            );

            m_skin = AssetDatabase.LoadAssetAtPath<GUISkin>
            (
                "Assets" +
                path +
                "Tayx/Graphy - Ultimate Stats Monitor/GUI/Graphy.guiskin"
            );

            if (m_skin != null)
            {
                m_headerStyle1 = m_skin.GetStyle("Header1");
                m_headerStyle2 = m_skin.GetStyle("Header2");

                SetGuiStyleFontColor
                (
                    guiStyle:   m_headerStyle2,
                    color:      EditorGUIUtility.isProSkin ? Color.white : Color.black
                );
            }
            else
            {
                m_headerStyle1 = EditorStyles.boldLabel;
                m_headerStyle2 = EditorStyles.boldLabel;
            }
        }

        /// <summary>
        /// Sets the colors of the GUIStyle's text.
        /// </summary>
        /// <param name="guiStyle">
        /// The GUIStyle to be altered.
        /// </param>
        /// <param name="color">
        /// The color for the text.
        /// </param>
        private void SetGuiStyleFontColor(GUIStyle guiStyle, Color color) //TODO: Perhaps add a null check.
        {
            guiStyle.normal     .textColor = color;
            guiStyle.hover      .textColor = color;
            guiStyle.active     .textColor = color;
            guiStyle.focused    .textColor = color;
            guiStyle.onNormal   .textColor = color;
            guiStyle.onHover    .textColor = color;
            guiStyle.onActive   .textColor = color;
            guiStyle.onFocused  .textColor = color;
        }

        private string GetMonoScriptFilePath(ScriptableObject scriptableObject) //TODO: Perhaps add a null check.
        {
            MonoScript ms = MonoScript.FromScriptableObject(scriptableObject);
            string filePath = AssetDatabase.GetAssetPath(ms);

            FileInfo fi = new FileInfo(filePath);

            if (fi.Directory != null)
            {
                filePath = fi.Directory.ToString();
                return filePath.Replace
                (
                    oldChar: '\\',
                    newChar: '/'
                );
            }
            return null;
        }

        #endregion
    }
}