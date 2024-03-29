﻿/* ---------------------------------------
 * Author:          Martin Pane (martintayx@gmail.com) (@tayx94)
 * Collaborators:   Lars Aalbertsen (@Rockylars)
 * Project:         Graphy - Ultimate Stats Monitor
 * Date:            28-Feb-18
 * Studio:          Tayx
 * 
 * This project is released under the MIT license.
 * Attribution is not required, but it is always welcomed!
 * -------------------------------------*/

using System;
using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

//namespace Tayx.Graphy.CustomizationScene
//{
//    public class CustomizeGraphy : MonoBehaviour
//    {
//        /* ----- TODO: ----------------------------
//         * Check if we can seal this class.
//         * Add summaries to the variables.
//         * Add summaries to the functions.
//         * Check if we can remove "using System.Collections;".
//         * Check if we can remove "using Random = UnityEngine.Random;".
//         * Check if we can remove the UnityEngine prefix on the PlayRandomSFX function
//         * --------------------------------------*/

//        #region Variables -> Serialized Private
        
//        [Header("Customize Graphy")]

//        [SerializeField] private    G_CUIColorPicker    m_colorPicker = null;
        
//        [SerializeField] private    Toggle              m_backgroundToggle = null;

//        [SerializeField] private    Dropdown            m_graphyModeDropdown = null;

//        [SerializeField] private    Button              m_backgroundColorButton = null;
        
//        [SerializeField] private    Dropdown            m_graphModulePositionDropdown = null;

//        #region Section -> FPS

//        [Header("Fps")]

//        [SerializeField] private    Dropdown            m_fpsModuleStateDropdown = null;

//        [SerializeField] private    InputField          m_goodInputField = null;
//        [SerializeField] private    InputField          m_cautionInputField = null;
        
//        [SerializeField] private    Button              m_goodColorButton = null;
//        [SerializeField] private    Button              m_cautionColorButton = null;
//        [SerializeField] private    Button              m_criticalColorButton = null;
        
//        [SerializeField] private    Slider              m_timeToResetMinMaxSlider = null;
//        [SerializeField] private    Slider              m_fpsGraphResolutionSlider = null;
//        [SerializeField] private    Slider              m_fpsTextUpdateRateSlider = null;

//        #endregion

//        #region Section -> RAM

//        [Header("Memory")]

//        [SerializeField] private    Dropdown            m_ramModuleStateDropdown = null;
        
//        [SerializeField] private    Button              m_reservedColorButton = null;
//        [SerializeField] private    Button              m_allocatedColorButton = null;
//        [SerializeField] private    Button              m_monoColorButton = null;

//        [SerializeField] private    Slider              m_ramGraphResolutionSlider = null;
//        [SerializeField] private    Slider              m_ramTextUpdateRateSlider = null;

//        #endregion

//        #region Section -> Advanced

//        [Header("Advanced")]

//        [SerializeField] private    Dropdown            m_advancedModulePositionDropdown = null;

//        [SerializeField] private    Toggle              m_advancedModuleToggle = null;

//        #endregion

//        #endregion

//        #region Variables -> Private

//        private GraphyManager m_graphyManager = null;

//        #endregion

//        #region Methods -> Unity Callbacks

//        private void OnEnable()
//        {
//            m_graphyManager = GraphyManager.Instance;
            
//            SetupCallbacks(); 
//        }

//        #endregion

//        #region Methods -> Private

//        private void SetupCallbacks()
//        {
//            // Remove all listeners first --------------------------------

//            m_backgroundToggle.onValueChanged.RemoveAllListeners();

//            m_backgroundColorButton.onClick.RemoveAllListeners();
//            m_graphyModeDropdown.onValueChanged.RemoveAllListeners();
//            m_graphModulePositionDropdown.onValueChanged.RemoveAllListeners();

//            #region Section -> FPS
//            m_fpsModuleStateDropdown.onValueChanged.RemoveAllListeners();

//            m_goodInputField.onValueChanged.RemoveAllListeners();

//            m_cautionInputField.onValueChanged.RemoveAllListeners();

//            m_goodColorButton.onClick.RemoveAllListeners();

//            m_cautionColorButton.onClick.RemoveAllListeners();

//            m_criticalColorButton.onClick.RemoveAllListeners();

//            m_timeToResetMinMaxSlider.onValueChanged.RemoveAllListeners();

//            m_fpsGraphResolutionSlider.onValueChanged.RemoveAllListeners();

//            m_fpsTextUpdateRateSlider.onValueChanged.RemoveAllListeners();

//            #endregion

//            #region Section -> RAM

//            m_ramModuleStateDropdown.onValueChanged.RemoveAllListeners();

//            m_reservedColorButton.onClick.RemoveAllListeners();

//            m_allocatedColorButton.onClick.RemoveAllListeners();

//            m_monoColorButton.onClick.RemoveAllListeners();

//            m_ramGraphResolutionSlider.onValueChanged.RemoveAllListeners();

//            m_ramTextUpdateRateSlider.onValueChanged.RemoveAllListeners();

//            #endregion

//            #region Section -> Advanced

//            m_advancedModulePositionDropdown.onValueChanged.RemoveAllListeners();

//            m_advancedModuleToggle.onValueChanged.RemoveAllListeners();

//            #endregion

//            // Add listeners --------------------------------

//            m_backgroundToggle.onValueChanged.AddListener(
//                value => m_graphyManager.Background = value);
            
//            m_backgroundColorButton.onClick.AddListener(() =>
//            {
//                m_colorPicker.SetOnValueChangeCallback(null);
//                m_colorPicker.Color = m_backgroundColorButton.GetComponent<Image>().color;
//                m_colorPicker.SetOnValueChangeCallback(color =>
//                {
//                    m_backgroundColorButton.GetComponent<Image>().color = color;
//                    m_graphyManager.BackgroundColor = color;
//                });
//            });

//            m_graphyModeDropdown.onValueChanged.AddListener(value => 
//            {
//                switch ((GraphyManager.Mode)value)
//                {
//                    case GraphyManager.Mode.FULL:
//                        m_fpsGraphResolutionSlider.maxValue     = 300f;
//                        m_ramGraphResolutionSlider.maxValue     = 300f;
//                        break;

//                    case GraphyManager.Mode.LIGHT:
//                        m_fpsGraphResolutionSlider.maxValue     = 128f;
//                        m_ramGraphResolutionSlider.maxValue     = 128f;
//                        break;
//                }

//                m_graphyManager.GraphyMode = (GraphyManager.Mode)value;                    
//            });

//            m_graphModulePositionDropdown.onValueChanged.AddListener(
//                value => m_graphyManager.GraphModulePosition = (GraphyManager.ModulePosition)value);

//            #region Section -> FPS

//            m_fpsModuleStateDropdown.onValueChanged.AddListener(
//                value => m_graphyManager.FpsModuleState = (GraphyManager.ModuleState)value);

//            m_goodInputField.onValueChanged.AddListener(value =>
//            {
//                int threshold;
//                if (Int32.TryParse(value, out threshold))
//                {
//                    m_graphyManager.GoodFpsThreshold = threshold;
//                }
//            });

//            m_cautionInputField.onValueChanged.AddListener(value =>
//            {
//                int threshold;
//                if (Int32.TryParse(value, out threshold))
//                {
//                    m_graphyManager.CautionFpsThreshold = threshold;
//                }
//            });
            
//            m_goodColorButton.onClick.AddListener(() =>
//            {
//                m_colorPicker.SetOnValueChangeCallback(null);
//                m_colorPicker.Color = m_goodColorButton.GetComponent<Image>().color;
//                m_colorPicker.SetOnValueChangeCallback(color =>
//                {
//                    m_goodColorButton.GetComponent<Image>().color = color;
//                    m_graphyManager.GoodFPSColor = color;
//                });
//            });
            
//            m_cautionColorButton.onClick.AddListener(() =>
//            {
//                m_colorPicker.SetOnValueChangeCallback(null);
//                m_colorPicker.Color = m_cautionColorButton.GetComponent<Image>().color;
//                m_colorPicker.SetOnValueChangeCallback(color =>
//                {
//                    m_cautionColorButton.GetComponent<Image>().color = color;
//                    m_graphyManager.CautionFPSColor = color;
//                });
//            });
            
//            m_criticalColorButton.onClick.AddListener(() =>
//            {
//                m_colorPicker.SetOnValueChangeCallback(null);
//                m_colorPicker.Color = m_criticalColorButton.GetComponent<Image>().color;
//                m_colorPicker.SetOnValueChangeCallback(color =>
//                {
//                    m_criticalColorButton.GetComponent<Image>().color = color;
//                    m_graphyManager.CriticalFPSColor = color;
//                });
//            });
            
//            m_timeToResetMinMaxSlider.onValueChanged.AddListener(
//                value => m_graphyManager.TimeToResetMinMaxFps = (int)value);

//            m_fpsGraphResolutionSlider.onValueChanged.AddListener(
//                value => m_graphyManager.FpsGraphResolution = (int)value);

//            m_fpsTextUpdateRateSlider.onValueChanged.AddListener(
//                value => m_graphyManager.FpsTextUpdateRate = (int)value);

//            #endregion

//            #region Section -> RAM

//            m_ramModuleStateDropdown.onValueChanged.AddListener(
//                value => m_graphyManager.RamModuleState = (GraphyManager.ModuleState)value);
           
//            m_reservedColorButton.onClick.AddListener(() =>
//            {
//                m_colorPicker.SetOnValueChangeCallback(null);
//                m_colorPicker.Color = m_reservedColorButton.GetComponent<Image>().color;
//                m_colorPicker.SetOnValueChangeCallback(color =>
//                {
//                    m_reservedColorButton.GetComponent<Image>().color = color;
//                    m_graphyManager.ReservedRamColor = color;
//                });
//            });
            
//            m_allocatedColorButton.onClick.AddListener(() =>
//            {
//                m_colorPicker.SetOnValueChangeCallback(null);
//                m_colorPicker.Color = m_allocatedColorButton.GetComponent<Image>().color;
//                m_colorPicker.SetOnValueChangeCallback(color =>
//                {
//                    m_allocatedColorButton.GetComponent<Image>().color = color;
//                    m_graphyManager.AllocatedRamColor = color;
//                });
//            });
            
//            m_monoColorButton.onClick.AddListener(() =>
//            {
//                m_colorPicker.SetOnValueChangeCallback(null);
//                m_colorPicker.Color = m_monoColorButton.GetComponent<Image>().color;
//                m_colorPicker.SetOnValueChangeCallback(color =>
//                {
//                    m_monoColorButton.GetComponent<Image>().color = color;
//                    m_graphyManager.MonoRamColor = color;
//                });
//            });
            
//            m_ramGraphResolutionSlider.onValueChanged.AddListener(
//                value => m_graphyManager.RamGraphResolution = (int)value);

//            m_ramTextUpdateRateSlider.onValueChanged.AddListener(
//                value => m_graphyManager.RamTextUpdateRate = (int)value);

//            #endregion

//            #region Section -> Advanced

//            m_advancedModulePositionDropdown.onValueChanged.AddListener(
//                value => m_graphyManager.AdvancedModulePosition = (GraphyManager.ModulePosition)value);
            
//            m_advancedModuleToggle.onValueChanged.AddListener(
//                value => m_graphyManager.AdvancedModuleState = value ? GraphyManager.ModuleState.FULL : GraphyManager.ModuleState.OFF);

//            #endregion

//        }

//        #endregion
//    }
//}
