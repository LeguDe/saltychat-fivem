using System;
using System.Collections.Generic;
using System.Text;

namespace SaltyShared
{
    public class Configuration
    {
        public bool Enabled { get; set; } = true;
        public float RangeModifier { get; set; } = 1f;
        public string RangeText { get; set; } = "{voicerange} meters";
        public string RadioText { get; set; } = "Channel {channel}";
        public int[] Position { get; set; } = { 0, 0 };
        public int[] MarkerColor { get; set; } = { 40, 255, 140, 50 };
        public int MarkerType { get; set; } = 1;
        public bool HideWhilePauseMenuOpen { get; set; } = true;
        public DrawMarkerTypes DrawMarker { get; set; } = DrawMarkerTypes.OnChange;
    }

    public enum DrawMarkerTypes
    {
        Never = 0,
        OnChange = 1,
        OnStartTalking = 2,
        OnTalking = 3,
        Always = 4
    }
}
