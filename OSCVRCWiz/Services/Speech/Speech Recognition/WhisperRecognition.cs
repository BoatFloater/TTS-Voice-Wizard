﻿using CoreOSC;
using NAudio.Wave;
using OSCVRCWiz.Resources.Audio;
using OSCVRCWiz.Resources.StartUp.StartUp;
using OSCVRCWiz.Resources.Whisper;
using OSCVRCWiz.Services.Speech.TextToSpeech;
using OSCVRCWiz.Services.Text;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Vosk;
using Whisper;
//using Whisper;
//using Whisper.Internal;
using static System.Net.WebRequestMethods;


namespace OSCVRCWiz.Speech_Recognition
{
    public class WhisperRecognition
	{
       static bool WhisperEnabled = false;
        public static string WhisperString = "";
        public static string WhisperPrevText = "";
        private static string langcode = "en";
        private static bool WhisperError = false;
       
        public static void toggleWhisper()
        {
            if (WhisperEnabled == false)
            {



                
                WhisperEnabled = true;

                string UseThisMic = getWhisperInputDevice().ToString();

              //  var language = "";
                VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
                {
                    fromLanguageID(VoiceWizardWindow.MainFormGlobal.comboBoxSpokenLanguage.SelectedItem.ToString());//set lang code for recognition
                  //  language = VoiceWizardWindow.MainFormGlobal.comboBox3.SelectedItem.ToString();

                });

               
                
             //   if(language!= "English[en]")
            //   {

                    string[] args = {
                "-c",UseThisMic,
                "-m",  VoiceWizardWindow.MainFormGlobal.whisperModelTextBox.Text,
                "-l", langcode,
              //  "-tr", VoiceWizardWindow.MainFormGlobal.rjToggleButtonWhisperToEnglish.Checked.ToString()
              //  "-ml", "300"
                 };
                    Task.Run(() => doWhisper(args));

                    OutputText.outputLog("[Whisper Listening]");
                // }
                if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonOSC.Checked == true || VoiceWizardWindow.MainFormGlobal.rjToggleButtonChatBox.Checked == true)
                {
                    var sttListening = new OscMessage("/avatar/parameters/stt_listening", true);
                    OSC.OSCSender.Send(sttListening);
                }


            }

            else
            {
                try
                {
                    WhisperString = "";
                    CaptureThread.stopWhisper();
                    WhisperEnabled = false;
                    OutputText.outputLog("[Whisper Stopped Listening]");
                    WhisperError = false;

                    if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonOSC.Checked == true || VoiceWizardWindow.MainFormGlobal.rjToggleButtonChatBox.Checked == true)
                    {
                        var sttListening = new OscMessage("/avatar/parameters/stt_listening", false);
                        OSC.OSCSender.Send(sttListening);
                    }
                }
                catch(Exception ex) {
                    OutputText.outputLog("[Error Stopping Whisper]");
                }

            }
        }
        public static void autoStopWhisper()
        {
            try
            {
                if (WhisperEnabled == true)
                {
                    WhisperString = "";
                    CaptureThread.stopWhisper();
                    WhisperEnabled = false;
                    OutputText.outputLog("[Whisper Stopped Listening]");
                    WhisperError = false;
                    if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonOSC.Checked == true || VoiceWizardWindow.MainFormGlobal.rjToggleButtonChatBox.Checked == true)
                    {
                        var sttListening = new OscMessage("/avatar/parameters/stt_listening", false);
                        OSC.OSCSender.Send(sttListening);
                    }

                }
           
              }
                catch(Exception ex) {
                OutputText.outputLog("[Error Stopping Whisper]");
            }
}

            public static int getWhisperInputDevice()
        {

            // Setting to Correct Input Device
            using iMediaFoundation mf = Library.initMediaFoundation();
            CaptureDeviceId[] devices = mf.listCaptureDevices() ??
                throw new ApplicationException("This computer has no audio capture devices");

            for (int i = 0; i < devices.Length; i++)
            {
               // Debug.WriteLine("#{0}: {1}", i, devices[i].displayName);
                if (AudioDevices.currentInputDeviceName.ToString() == devices[i].displayName.ToString())
                {
                    return i;

                }
            }
           
            return 0;

        }

        public static void fromLanguageID(string fullname)
        {
            langcode = "en-US";
            switch (fullname)
            {
                case "Arabic [ar-EG]": langcode = "ar"; break;
                case "Chinese [zh-CN]": langcode = "zh"; break;
                case "Czech [cs-CZ]": langcode = "cs"; break;
                case "Danish [da-DK]": langcode = "da"; break;
                case "Dutch [nl-NL]": langcode = "nl"; break;
                case "English [en-US] (Default)": langcode = "en"; break;
                case "Estonian [et-EE]": langcode = "et"; break;
                case "Filipino [fil-PH]": langcode = "tl"; break;
                case "Finnish [fi-FI]": langcode = "fi"; break;
                case "French [fr-FR]": langcode = "fr"; break;
                case "German [de-DE]": langcode = "de"; break;
                case "Hindi [hi-IN]": langcode = "hi"; break;
                case "Hungarian [hu-HU]": langcode = "hu"; break;
                case "Indonesian [id-ID]": langcode = "id"; break;
                case "Irish [ga-IE]": langcode = "ga"; break;
                case "Italian [it-IT]": langcode = "it"; break;
                case "Japanese [ja-JP]": langcode = "ja"; break;
                case "Korean [ko-KR]": langcode = "ko"; break;
                case "Norwegian [nb-NO]": langcode = "nb"; break;
                case "Polish [pl-PL]": langcode = "pl"; break;
                case "Portuguese [pt-BR]": langcode = "pt"; break;
                //place holder^^
                case "Russian [ru-RU]": langcode = "ru"; break;
                case "Spanish [es-MX]": langcode = "es"; break;
                //place holder^^
                case "Swedish [sv-SE]": langcode = "sv"; break;
                case "Thai [th-TH]": langcode = "th"; break;
                case "Ukrainian [uk-UA]": langcode = "uk"; break;
                case "Vietnamese [vi-VN]": langcode = "vi"; break;
                default: langcode = "en"; break; // if translation to english happens something is wrong
            }
        }




        public static int doWhisper(string[] args)
		{


            try
           {
                CommandLineArgs cla;
                try
                {
                    cla = new CommandLineArgs(args);

                }
                catch (OperationCanceledException)
                {
                    return 1;
                }
                const eLoggerFlags loggerFlags = eLoggerFlags.UseStandardError | eLoggerFlags.SkipFormatMessage;
               Library.setLogSink(eLogLevel.Debug, loggerFlags);

                using iMediaFoundation mf = Library.initMediaFoundation();
                CaptureDeviceId[] devices = mf.listCaptureDevices() ??
                    throw new ApplicationException("This computer has no audio capture devices");


              
                
                if (cla.captureDeviceIndex < 0 || cla.captureDeviceIndex >= devices.Length)
                    throw new ApplicationException($"Capture device index is out of range; the valid range is [ 0 .. {devices.Length - 1} ]");
                
                sCaptureParams cp = new sCaptureParams();
            try
            {
                
                cp.minDuration = (float)Convert.ToDouble(VoiceWizardWindow.MainFormGlobal.textBoxWhisperMinDuration.Text.ToString(), CultureInfo.InvariantCulture); //1
                cp.maxDuration = (float)Convert.ToDouble(VoiceWizardWindow.MainFormGlobal.textBoxWhisperMaxDuration.Text.ToString(), CultureInfo.InvariantCulture); //8
                cp.dropStartSilence = (float)Convert.ToDouble(VoiceWizardWindow.MainFormGlobal.textBoxWhisperDropSilence.Text.ToString(), CultureInfo.InvariantCulture);   // 250 ms
                cp.pauseDuration = (float)Convert.ToDouble(VoiceWizardWindow.MainFormGlobal.textBoxWhisperPauseDuration.Text.ToString(), CultureInfo.InvariantCulture);  //1
                //we need culture invariant or for some languages like german 8.0 will be converted to 80 because they use "," instead of "."
            }
            catch (Exception ex)
            {
                    cp.minDuration = 1.0f;
                    cp.maxDuration = 8.0f;
                    cp.dropStartSilence = 0.25f;
                    cp.pauseDuration = 1.0f;
                    if (WhisperError == false)
                    {
                        OutputText.outputLog("[WARNING: Error Occured loading Whisper custom values. Forcing defaults]", Color.DarkOrange);
                    }
                    WhisperError = true;
                    VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
                    {
                        
                  
                    VoiceWizardWindow.MainFormGlobal.textBoxWhisperMinDuration.Text = "1.0";
                    VoiceWizardWindow.MainFormGlobal.textBoxWhisperMaxDuration.Text = "8.0";
                    VoiceWizardWindow.MainFormGlobal.textBoxWhisperDropSilence.Text = "0.25";
                    VoiceWizardWindow.MainFormGlobal.textBoxWhisperPauseDuration.Text = "1.0";
                    });

                }

                if (cla.diarize)
                    cp.flags |= eCaptureFlags.Stereo;
                using iAudioCapture captureDev = mf.openCaptureDevice(devices[cla.captureDeviceIndex], cp);
                
                using iModel model = Library.loadModel(cla.model);
                using Whisper.Context context = model.createContext();

                


                cla.apply(ref context.parameters);
                

                CaptureThread thread = new CaptureThread(cla, context, captureDev);
                thread.join();

                


                context.timingsPrint();
                Debug.WriteLine("finished");
                return 0;
              }
            catch (Exception ex)
            { 
                
               
                OutputText.outputLog("[Whisper Error: " + ex.Message.ToString()+ "]", Color.Red);
                OutputText.outputLog("[Whisper Setup Guide: https://github.com/VRCWizard/TTS-Voice-Wizard/wiki/Whisper ", Color.DarkOrange);


               WhisperEnabled = false;

                if (VoiceWizardWindow.MainFormGlobal.rjToggleButtonOSC.Checked == true || VoiceWizardWindow.MainFormGlobal.rjToggleButtonChatBox.Checked == true)
                {
                    var sttListening = new OscMessage("/avatar/parameters/stt_listening", false);
                    OSC.OSCSender.Send(sttListening);
                }

                return ex.HResult;
            }
        }



        public static System.Threading.Timer whisperTimer;

        public static void initiateTimer()
        {
            whisperTimer = new System.Threading.Timer(whispertimertick);
            whisperTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        public static  void whispertimertick(object sender)
        {

            Thread t = new Thread(doWhisperTimerTick);
            t.IsBackground = true; // Set the thread as a background thread
            t.Start();
        }

        private static void doWhisperTimerTick() //Whisper on timer to prevent double outputs at the same time
        {

            string text = WhisperRecognition.WhisperString;

            TTSMessageQueue.QueueMessage(text, "Whisper");
            WhisperRecognition.WhisperPrevText = WhisperRecognition.WhisperString;
            WhisperRecognition.WhisperString = "";

        }

        public static void downloadWhisperModel()
        {
            string address = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/";
            string path = "Assets/models/";


            switch (VoiceWizardWindow.MainFormGlobal.comboBoxWhisperModelDownload.Text.ToString())
            {
                case "ggml-tiny.bin (75 MB)":
                    path += "ggml-tiny.bin";
                    address += "ggml-tiny.bin";

                    break;

                case "ggml-base.bin (142 MB)":
                    path += "ggml-base.bin";
                    address += "ggml-base.bin";

                    break;

                case "ggml-small.bin (466 MB)":
                    path += "ggml-small.bin";
                    address += "ggml-small.bin";

                    break;

                case "ggml-medium.bin (1.5 GB)":
                    path += "ggml-medium.bin";
                    address += "ggml-medium.bin";

                    break;

                default: break;
            }

            if (!System.IO.File.Exists(path))
            {
                VoiceWizardWindow.MainFormGlobal.modelLabel.ForeColor = Color.DarkOrange;
                VoiceWizardWindow.MainFormGlobal.modelLabel.Text = "model downloading... PLEASE WAIT";


                WebClient client = new WebClient();
                Uri uri = new Uri(address);

                // Call DownloadFileCallback2 when the download completes.
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback2);

                // Specify a progress notification handler here ...

                client.DownloadFileAsync(uri, path);
            }
            VoiceWizardWindow.MainFormGlobal.whisperModelTextBox.Text = path;
        }
        private static void DownloadFileCallback2(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                // Console.WriteLine("File download cancelled.");
                MessageBox.Show("File download cancelled");
                OutputText.outputLog("[Whisper Model Download Cancelled: Model Download was cancelled, If this was un-intentional try manually downloading the model from here]", Color.Red);
                VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
                {
                    VoiceWizardWindow.MainFormGlobal.modelLabel.ForeColor = Color.Red;
                    VoiceWizardWindow.MainFormGlobal.modelLabel.Text = "model error";
                });
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show("Error while downloading file.");
                OutputText.outputLog("[Whisper Model Download Error: " + e.Error.Message + "]", Color.Red);
                //Console.WriteLine(e.Error.ToString());
                VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
                {
                    VoiceWizardWindow.MainFormGlobal.modelLabel.ForeColor = Color.Red;
                    VoiceWizardWindow.MainFormGlobal.modelLabel.Text = "model error";
                });
                return;
            }
            VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
            {
                VoiceWizardWindow.MainFormGlobal.modelLabel.ForeColor = Color.Green;
                VoiceWizardWindow.MainFormGlobal.modelLabel.Text = "model downloaded";
            });
            OutputText.outputLog("[Your Whisper Model has completed downloading]", Color.Green);
            MessageBox.Show("Your Whisper Model has completed downloading");



        }



    }
}
