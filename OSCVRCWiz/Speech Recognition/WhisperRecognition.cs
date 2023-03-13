﻿using NAudio.Wave;
using OSCVRCWiz.Resources;
using OSCVRCWiz.Text;
using Resources;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whisper;
using Whisper.Internal;


namespace OSCVRCWiz.Speech_Recognition
{
	public class WhisperRecognition
	{
       static bool WhisperEnabled = false;
        public static string WhisperString = "";
        public static string WhisperPrevText = "";
        private static string langcode = "en";
       
        public static void toggleWhisper()
        {
            if (WhisperEnabled == false)
            {



                
                WhisperEnabled = true;

                string UseThisMic = getWhisperInputDevice().ToString();

              //  var language = "";
                VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
                {
                    fromLanguageID(VoiceWizardWindow.MainFormGlobal.comboBox4.SelectedItem.ToString());//set lang code for recognition
                  //  language = VoiceWizardWindow.MainFormGlobal.comboBox3.SelectedItem.ToString();

                });

               
                
             //   if(language!= "English[en]")
            //   {

                    string[] args = {
                "-c",UseThisMic,
                "-m",  VoiceWizardWindow.MainFormGlobal.whisperModelTextBox.Text,
                "-l", langcode
              //  "-ml", "300"
                 };
                    Task.Run(() => doWhisper(args));

                    OutputText.outputLog("[Whisper Listening]");
               // }
            /*    else//translate to english
                {
                    string[] args = {
                "-c",UseThisMic,
                "-m",  VoiceWizardWindow.MainFormGlobal.whisperModelTextBox.Text,
                "-l", langcode,
                "-tr"
              //  "-ml", "300"
                 };
                    Task.Run(() => doWhisper(args));

                    OutputText.outputLog("[Whisper Listening]");
                }*/
               
            }

            else
            {
                WhisperString = "";
                CaptureThread.stopWhisper();
                WhisperEnabled = false;
                OutputText.outputLog("[Whisper Stopped Listening]");

            }
        }
        public static void autoStopWhisper()
        {
            if (WhisperEnabled == true)
            {
                WhisperString = "";
                CaptureThread.stopWhisper();
                WhisperEnabled = false;
                OutputText.outputLog("[Whisper Stopped Listening]");

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
                //cla.max_len = 300;
                    
                  //  cla.captureDeviceIndex = 0;
                  //  cla.model = @"C:\Users\\bdw10\Downloads\base.en.pt";
                    

                    //i can set all the cla argument here very easily
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
                if (cla.diarize)
                    cp.flags |= eCaptureFlags.Stereo;
                using iAudioCapture captureDev = mf.openCaptureDevice(devices[cla.captureDeviceIndex], cp);
                
                using iModel model = Library.loadModel(cla.model);
                using Whisper.Context context = model.createContext();


                //attempted fix will break program
              //  context.parameters.audioContextSize = 150;
           //   context.parameters.n_max_text_ctx = 300;
           //    context.parameters.max_tokens = 300;
          // context.parameters.hold

                


                cla.apply(ref context.parameters);
                

                CaptureThread thread = new CaptureThread(cla, context, captureDev);
                thread.join();

                


                context.timingsPrint();
                Debug.WriteLine("finished");
                return 0;
            }
            catch (Exception ex)
            {
                // Console.WriteLine( ex.Message );
                // Debug.WriteLine(ex.ToString());
                OutputText.outputLog("Whisper Error: " + ex.Message.ToString(), Color.Red);
                WhisperEnabled = false;
                // return;
                return ex.HResult;
            }
        }
			
			
				
			
    }
}
