using System;
using System.Configuration;
using System.IO;
//using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using CoreAudioApi;
using System.Threading;
//using System.Text;

namespace CitrixSoundLevelKeeper
{
    class CitrixSoundLevelKeeperClass
    {
        static StreamWriter logfile;
        static bool blnDebug = false;
        static void Main()
        {
            string [] Arguments = Environment.GetCommandLineArgs();
            string strLogPath = Environment.ExpandEnvironmentVariables(CitrixSoundLevelKeeper.Properties.Settings.Default.LOGPATH);
            foreach(string Argument in Arguments)
            {
                if(Argument == "/d" || Argument == "-d")
                {
                    blnDebug = true;
                }
            }
            if(blnDebug)
            {
                try
                {
                    if (System.IO.File.Exists(strLogPath + "\\CSLK.log"))
                    {
                        long sizeoflog = new System.IO.FileInfo(strLogPath + "\\CSLK.log").Length;
                        if (sizeoflog > CitrixSoundLevelKeeper.Properties.Settings.Default.LOGMAXSIZE)
                            System.IO.File.Delete(strLogPath + "\\CSLK.log");
                    }
                }
                catch
                {
                    MessageBox.Show("Cannot Remove Log File.", Application.ProductName + " " + Application.ProductVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }
                // open logfile
                try
                {
                    FileStream fslogfile = new FileStream(strLogPath + "\\CSLK.log", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    logfile = new StreamWriter(fslogfile);
                }
                catch
                {
                    MessageBox.Show("Cannot Access Log File.", Application.ProductName + " " + Application.ProductVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                }
                Log("////// Citrix Sound Level Keeper STARTING //////", logfile);
            }
            SystemEvents.SessionEnding += new SessionEndingEventHandler(ExitProgram);
            RegistryKey KeySoundLevel;
            MMDeviceEnumerator devEnum = new MMDeviceEnumerator();
            MMDevice defaultDevice = devEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            try
            {
                KeySoundLevel = Registry.CurrentUser.OpenSubKey(CitrixSoundLevelKeeper.Properties.Settings.Default.REGSTORE);
                try
                {
                    defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = float.Parse(KeySoundLevel.GetValue("Volume").ToString());
                    defaultDevice.AudioEndpointVolume.Mute = bool.Parse(KeySoundLevel.GetValue("Muted").ToString());
                }
                catch(Exception ex)
                {
                    Log("Error on restore Volume Setting " + ex.ToString(),logfile);
                }
            }
            catch
            {
                Log("No Restore Volume Settings",logfile);
            }

            defaultDevice.AudioEndpointVolume.OnVolumeNotification += new AudioEndpointVolumeNotificationDelegate(
    AudioEndpointVolume_OnVolumeNotification);

            while (true)
            {
                Thread.Sleep(5000);
            }
        }
        static void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            RegistryKey KeySoundLevel;
            float MasterVolume = data.MasterVolume;
            bool Muted = data.Muted;
            Log("Set Volume to : " + MasterVolume.ToString(), logfile);
            Log("Set Muted to :" + Muted.ToString(), logfile);
            try
            {
                KeySoundLevel = Registry.CurrentUser.CreateSubKey(CitrixSoundLevelKeeper.Properties.Settings.Default.REGSTORE, RegistryKeyPermissionCheck.ReadWriteSubTree);
            }
            catch
            {
                KeySoundLevel = Registry.CurrentUser.OpenSubKey(CitrixSoundLevelKeeper.Properties.Settings.Default.REGSTORE, true); 
            }
            try
            {
                KeySoundLevel.SetValue(@"Volume", MasterVolume.ToString(), RegistryValueKind.String);
            }
            catch(Exception ex)
            {
                Log("Error on store Volume " + ex.ToString(),logfile);
            }
            try
            {
                KeySoundLevel.SetValue(@"Muted", Muted.ToString(),RegistryValueKind.String);
            }
            catch(Exception ex)
            {
                Log("Error on store Muted " +  ex.ToString(),logfile);
            }
        }
        static void ExitProgram(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
                
        static void Log(string logMessage, TextWriter w)
        {
            if (blnDebug)
            {
                w.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " | " + Environment.UserDomainName + "\\" + Environment.UserName + " : " + logMessage);
                w.Flush();
            }
        }
    }

}
