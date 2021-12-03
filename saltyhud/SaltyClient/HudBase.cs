using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using SaltyShared;

namespace SaltyClient
{
    public class HudBase : BaseScript
    {
        #region Props/Fields
        internal Configuration Configuration { get; set; }

        internal bool IsMenuOpen { get; set; }
        internal float VoiceRange { get; set; }
        internal int DrawMarkerUntil { get; set; }
        internal bool IsTalking { get; set; }
        internal bool IsMicrophoneMuted { get; set; }
        internal bool IsSoundMuted { get; set; }
        internal bool RadioActive { get; set; }
        internal int TickCounter { get; set; }
        #endregion

        #region CTOR
        public HudBase()
        {
            this.Exports.Add("SetEnabled", new Action<bool>(this.SetEnabled));

            API.RegisterCommand("voicehud-range", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (args.ToList().Count() >= 1)
                {
                    Configuration.DrawMarker = (DrawMarkerTypes)Convert.ToInt32(args[0]);

                    Debug.WriteLine($"Changed range marker type to " + Configuration.DrawMarker);
                }
                else
                {
                    Debug.WriteLine($"Missing parameter: Never = 0, OnChange = 1, OnStartTalking = 2, OnTalking = 3, Always = 4");
                }
            }), false);

            API.RegisterCommand("voicehud-position", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (args.ToList().Count() >= 2)
                {
                    this.UpdatePosition(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]));
                }
                else
                {
                    Debug.WriteLine($"Missing parameter: X Y (pixel from top left corner)");
                }
            }), false);

            API.RegisterCommand("voicehud-color", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (args.ToList().Count() >= 3)
                {
                    Configuration.MarkerColor[0] = Convert.ToInt32(args[0]);
                    Configuration.MarkerColor[1] = Convert.ToInt32(args[1]);
                    Configuration.MarkerColor[2] = Convert.ToInt32(args[2]);

                    if (args.ToList().Count() >= 4)
                    {
                        Configuration.MarkerColor[3] = Convert.ToInt32(args[3]);
                    }
                }
                else
                {
                    Debug.WriteLine($"Missing parameter: Red Green Blue [Alpha] (0-255)");
                }
            }), false);

            API.RegisterCommand("voicehud-type", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (args.ToList().Count() >= 1)
                {
                    int type = Convert.ToInt32(args[0]);

                    if (type >= 1 && type <= 44)
                    {
                        Configuration.MarkerType = type;
                    }
                    else
                    {
                        Debug.WriteLine($"Wrong parameter: Type must be betwen 1-44");
                    }
                }
                else
                {
                    Debug.WriteLine($"Missing parameter: Type (1-44)");
                }
            }), false);
        }
        #endregion

        #region Exports
        public void SetEnabled(bool enable)
        {
            if (enable && !this.Configuration.Enabled)
                this.Display(true);
            else if (!enable && this.Configuration.Enabled)
                this.Display(false);

            this.Configuration.Enabled = enable;
        }
        #endregion

        #region Salty Chat Exports
        public float GetVoiceRange() => this.Exports["saltychat"].GetVoiceRange();
        public String GetRadioChannel() => this.Exports["saltychat"].GetRadioChannel(true);
        #endregion

        #region Event Handler
        [EventHandler("SaltyChat_PluginStateChanged")]
        public void OnPluginStateChanged(int pluginState)
        {
            this.SendNuiMessage(MessageType.PluginState, pluginState);
        }

        [EventHandler("SaltyChat_VoiceRangeChanged")]
        public void OnVoiceRangeChanged(float voiceRange, int index, int availableVoiceRanges)
        {
            this.VoiceRange = voiceRange;
            float range = this.VoiceRange * this.Configuration.RangeModifier;

            if (Configuration.DrawMarker >= DrawMarkerTypes.OnChange)
            {
                this.DrawMarkerUntil = API.GetGameTimer() + 500;
            }

            this.SendNuiMessage(MessageType.SetRange, this.Configuration.RangeText.Replace("{voicerange}", range.ToString("0.#")));
        }

        [EventHandler("SaltyChat_TalkStateChanged")]
        public void OnTalkStateChanged(bool isTalking)
        {
            this.IsTalking = isTalking;

            if (isTalking)
            {
                if (Configuration.DrawMarker >= DrawMarkerTypes.OnStartTalking)
                {
                    if (API.GetGameTimer() > DrawMarkerUntil + 10000)
                    {
                        this.DrawMarkerUntil = API.GetGameTimer() + 500;
                    }
                }

                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Talking);
            }
            else if (this.IsSoundMuted)
            {
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.SoundMuted);
            }
            else if (this.IsMicrophoneMuted)
            {
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.MicrophoneMuted);
            }
            else
            {
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Idle);
            }
        }

        [EventHandler("SaltyChat_MicStateChanged")]
        public void OnMicStateChanged(bool isMicrophoneMuted)
        {
            this.IsMicrophoneMuted = isMicrophoneMuted;

            if (this.IsSoundMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.SoundMuted);
            else if (this.IsMicrophoneMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.MicrophoneMuted);
            else
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Idle);
        }

        [EventHandler("SaltyChat_SoundStateChanged")]
        public void OnSoundStateChanged(bool isSoundMuted)
        {
            this.IsSoundMuted = isSoundMuted;

            if (this.IsSoundMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.SoundMuted);
            else if (this.IsMicrophoneMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.MicrophoneMuted);
            else
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Idle);
        }
        [EventHandler("SaltyChat_RadioTrafficStateChanged")]
        public void OnRadioStateChanged(String name, bool isSending, bool isPrimaryChannel, bool activeRelay)
        {
            if (isSending)
                this.SendNuiMessage(MessageType.SetRadioState, SoundState.Talking);
            else
                this.SendNuiMessage(MessageType.SetRadioState, SoundState.Idle);
        }


        #endregion

        #region Tick
        [Tick]
        private async Task FirstTickAsync()
        {
            this.Configuration = JsonConvert.DeserializeObject<Configuration>(API.LoadResourceFile(API.GetCurrentResourceName(), "config.json"));

            this.VoiceRange = this.GetVoiceRange();

            if (this.Configuration.Enabled && (!this.Configuration.HideWhilePauseMenuOpen || !API.IsPauseMenuActive()))
                this.Display(true);

            this.Tick += this.ControlTickAsync;

            this.Tick -= this.FirstTickAsync;

            this.UpdatePosition(Configuration.Position[0], Configuration.Position[1]);

            await Task.FromResult(0);
        }

        private async Task ControlTickAsync()
        {

            if (this.Configuration.HideWhilePauseMenuOpen)
            {
                if (API.IsPauseMenuActive() && !this.IsMenuOpen)
                {
                    this.IsMenuOpen = true;

                    this.Display(false);
                }
                else if (!API.IsPauseMenuActive() && this.IsMenuOpen)
                {
                    this.IsMenuOpen = false;

                    this.Display(true);
                }
            }

            this.TickCounter = (this.TickCounter + 1) % 10;

            if (this.TickCounter == 0)
            {
                UpdateRadioChannel();
            }

            if (API.GetGameTimer() < this.DrawMarkerUntil ||
                (this.IsTalking && Configuration.DrawMarker == DrawMarkerTypes.OnTalking) ||
                (Configuration.DrawMarker == DrawMarkerTypes.Always))
            {
                DrawMarker();
            }
            await Task.FromResult(0);
        }
        #endregion

        #region Methods
        public void Display(bool display) => this.SendNuiMessage(MessageType.Display, display);

        public void SendNuiMessage(MessageType type, object data)
        {
            API.SendNuiMessage(
                JsonConvert.SerializeObject(
                    new NuiMessage(type, data)
                )
            );
        }

        private void UpdatePosition(int x, int y)
        {
            Configuration.Position[0] = x;
            Configuration.Position[1] = y;

            int[] positions = { Configuration.Position[0], Configuration.Position[1] };
            this.SendNuiMessage(MessageType.SetPosition, positions);
        }

        private void DrawMarker()
        {
            Player localPlayer = Game.Player;
            Ped playerPed = localPlayer.Character;
            float range = this.VoiceRange * this.Configuration.RangeModifier;
            Vector3 pedCoords = API.GetEntityCoords(playerPed.Handle, true);

            float height;

            if (range <= 3.0)
            {
                height = 0.3f;
            }
            else if (range <= 8.0f)
            {
                height = 0.5f;
            }
            else if (range <= 15.0f)
            {
                height = 0.8f;
            }
            else
            {
                height = 1.2f;
            }

            API.DrawMarker(Configuration.MarkerType, pedCoords.X, pedCoords.Y, pedCoords.Z - 1, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, range * 2.0f, range * 2.0f, height, 
                Configuration.MarkerColor[0], Configuration.MarkerColor[1], Configuration.MarkerColor[2], Configuration.MarkerColor[3], false, false, 2, false, null, null, false);
        }

        private void UpdateRadioChannel()
        {
            String radioChannel = this.GetRadioChannel();

            if (radioChannel == null)
            {
                //this.SendNuiMessage(MessageType.SetRadioRange, "");
                if (this.RadioActive == true)
                {
                    this.SendNuiMessage(MessageType.SetRadioState, SoundState.SoundMuted);
                    this.RadioActive = false;
                }
            }
            else
            {
                if (this.RadioActive == false)
                {
                    this.SendNuiMessage(MessageType.SetRadioRange, this.Configuration.RadioText.Replace("{channel}", radioChannel));
                    this.SendNuiMessage(MessageType.SetRadioState, SoundState.Idle);
                    this.RadioActive = true;
                }
            }
        }
        #endregion
    }

    public enum SoundState
    {
        Idle = 0,
        Talking = 1,
        MicrophoneMuted = 2,
        SoundMuted = 3
    }

}
