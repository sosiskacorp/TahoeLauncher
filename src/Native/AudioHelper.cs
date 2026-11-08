using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MacOSTahoe
{
    public enum AudioDeviceType
    {
        Speakers,
        Headphones,
        Display,
        Digital,
        Bluetooth,
        Other
    }

    public class AudioDeviceItem
    {
        public string Id;
        public string Name;
        public AudioDeviceType DeviceType;
        public bool IsDefault;

        public string TypeIcon
        {
            get
            {
                switch (DeviceType)
                {
                    case AudioDeviceType.Headphones: return "🎧";
                    case AudioDeviceType.Display: return "📺";
                    case AudioDeviceType.Bluetooth: return "📡";
                    case AudioDeviceType.Digital: return "📻";
                    default: return "🔊";
                }
            }
        }
    }

    public static class AudioHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct PropertyKey
        {
            public Guid fmtid;
            public int pid;
            public PropertyKey(Guid f, int p) { fmtid = f; pid = p; }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct PropVariant
        {
            [FieldOffset(0)] public short vt;
            [FieldOffset(8)] public IntPtr pwszVal;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        class MMDeviceEnumeratorComObject { }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
            int GetDevice(string pwstrId, out IMMDevice device);
            int RegisterEndpointNotificationCallback(IntPtr client);
            int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceCollection
        {
            int GetCount(out int count);
            int Item(int index, out IMMDevice device);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice
        {
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
            int OpenPropertyStore(int stgmAccess, out IPropertyStore properties);
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
            int GetState(out int pdwState);
        }

        [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPropertyStore
        {
            int GetCount(out int count);
            int GetAt(int iProp, out PropertyKey pkey);
            int GetValue(ref PropertyKey key, out PropVariant pv);
            int SetValue(ref PropertyKey key, ref PropVariant pv);
            int Commit();
        }

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioEndpointVolume
        {
            int RegisterControlChangeNotify(IntPtr pNotify);
            int UnregisterControlChangeNotify(IntPtr pNotify);
            int GetChannelCount(out uint pnChannelCount);
            int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
            int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
            int GetMasterVolumeLevel(out float pfLevelDB);
            int GetMasterVolumeLevelScalar(out float pfLevel);
            int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
            int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
            int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
            int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
            int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
            int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
            int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
            int VolumeStepUp(ref Guid pguidEventContext);
            int VolumeStepDown(ref Guid pguidEventContext);
            int QueryHardwareSupport(out uint pdwHardwareSupportMask);
            int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
        }

        public enum ERole
        {
            eConsole = 0,
            eMultimedia = 1,
            eCommunications = 2
        }

        [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPolicyConfig
        {
            [PreserveSig] int GetMixFormat([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, out IntPtr ppFormat);
            [PreserveSig] int GetDeviceFormat([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] bool bShared, out IntPtr ppFormat);
            [PreserveSig] int ResetDeviceFormat([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName);
            [PreserveSig] int SetDeviceFormat([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] IntPtr pEndpointFormat, [In] IntPtr pMixFormat);
            [PreserveSig] int GetProcessingPeriod([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] bool bDefault, out long PDefaultPeriod, out long PMinPeriod);
            [PreserveSig] int SetProcessingPeriod([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] ref long pPeriod);
            [PreserveSig] int GetShareMode([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, out IntPtr pMode);
            [PreserveSig] int SetShareMode([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] IntPtr pMode);
            [PreserveSig] int GetPropertyValue([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] IntPtr pKey, out IntPtr pvValue);
            [PreserveSig] int SetPropertyValue([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] IntPtr pKey, [In] IntPtr pvValue);
            [PreserveSig] int SetDefaultEndpoint([In, MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId, [In, MarshalAs(UnmanagedType.U4)] ERole eRole);
            [PreserveSig] int SetEndpointVisibility([In, MarshalAs(UnmanagedType.LPWStr)] string pszDeviceName, [In] bool bVisible);
        }

        [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        class CPolicyConfigClient { }

        static readonly PropertyKey PKEY_Device_FriendlyName = new PropertyKey(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);

        public static List<AudioDeviceItem> GetPlaybackDevices()
        {
            List<AudioDeviceItem> list = new List<AudioDeviceItem>();
            try
            {
                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                string defId = "";
                try
                {
                    IMMDevice defaultDev;
                    enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 0 /* eConsole */, out defaultDev);
                    if (defaultDev != null) defaultDev.GetId(out defId);
                }
                catch { }

                IMMDeviceCollection col;
                enumerator.EnumAudioEndpoints(0 /* eRender */, 1 /* DEVICE_STATE_ACTIVE */, out col);
                int count;
                col.GetCount(out count);

                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        IMMDevice dev;
                        col.Item(i, out dev);
                        string id;
                        dev.GetId(out id);
                        IPropertyStore ps;
                        dev.OpenPropertyStore(0, out ps);
                        PropVariant pv;
                        PropertyKey key = PKEY_Device_FriendlyName;
                        ps.GetValue(ref key, out pv);
                        string name = Marshal.PtrToStringUni(pv.pwszVal);
                        if (string.IsNullOrEmpty(name)) name = "Аудиоустройство " + (i + 1);

                        AudioDeviceType type = ClassifyType(name);
                        list.Add(new AudioDeviceItem
                        {
                            Id = id,
                            Name = name,
                            DeviceType = type,
                            IsDefault = string.Equals(id, defId, StringComparison.OrdinalIgnoreCase)
                        });
                    }
                    catch { }
                }
            }
            catch { }
            return list;
        }

        private static AudioDeviceType ClassifyType(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("headphone") || lower.Contains("наушник") || lower.Contains("headset") || lower.Contains("гарнитур") || lower.Contains("airpod"))
                return AudioDeviceType.Headphones;
            if (lower.Contains("nvidia") || lower.Contains("amd") || lower.Contains("hdmi") || lower.Contains("tv") || lower.Contains("монитор") || lower.Contains("display") || lower.Contains("rg"))
                return AudioDeviceType.Display;
            if (lower.Contains("bluetooth") || lower.Contains("bt"))
                return AudioDeviceType.Bluetooth;
            if (lower.Contains("digital") || lower.Contains("s/pdif") || lower.Contains("optical") || lower.Contains("цифров"))
                return AudioDeviceType.Digital;
            return AudioDeviceType.Speakers;
        }

        public static bool SetDefaultAudioDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return false;
            try
            {
                IPolicyConfig config = (IPolicyConfig)new CPolicyConfigClient();
                config.SetDefaultEndpoint(deviceId, ERole.eConsole);
                config.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
                config.SetDefaultEndpoint(deviceId, ERole.eCommunications);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int GetMasterVolume(out bool isMuted)
        {
            isMuted = false;
            try
            {
                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice defaultDev;
                enumerator.GetDefaultAudioEndpoint(0, 0, out defaultDev);
                if (defaultDev != null)
                {
                    Guid iid = typeof(IAudioEndpointVolume).GUID;
                    object volObj;
                    defaultDev.Activate(ref iid, 1, IntPtr.Zero, out volObj);
                    IAudioEndpointVolume vol = (IAudioEndpointVolume)volObj;
                    float scalar;
                    vol.GetMasterVolumeLevelScalar(out scalar);
                    vol.GetMute(out isMuted);
                    return (int)Math.Round(scalar * 100.0);
                }
            }
            catch { }
            return 50;
        }

        public static void SetMasterVolume(int volumePercent)
        {
            try
            {
                float scalar = (float)Math.Max(0.0, Math.Min(1.0, volumePercent / 100.0));
                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice defaultDev;
                enumerator.GetDefaultAudioEndpoint(0, 0, out defaultDev);
                if (defaultDev != null)
                {
                    Guid iid = typeof(IAudioEndpointVolume).GUID;
                    object volObj;
                    defaultDev.Activate(ref iid, 1, IntPtr.Zero, out volObj);
                    IAudioEndpointVolume vol = (IAudioEndpointVolume)volObj;
                    Guid empty = Guid.Empty;
                    vol.SetMasterVolumeLevelScalar(scalar, ref empty);
                }
            }
            catch { }
        }

        public static void ToggleMute()
        {
            try
            {
                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice defaultDev;
                enumerator.GetDefaultAudioEndpoint(0, 0, out defaultDev);
                if (defaultDev != null)
                {
                    Guid iid = typeof(IAudioEndpointVolume).GUID;
                    object volObj;
                    defaultDev.Activate(ref iid, 1, IntPtr.Zero, out volObj);
                    IAudioEndpointVolume vol = (IAudioEndpointVolume)volObj;
                    bool muted;
                    vol.GetMute(out muted);
                    Guid empty = Guid.Empty;
                    vol.SetMute(!muted, ref empty);
                }
            }
            catch { }
        }
    }
}
