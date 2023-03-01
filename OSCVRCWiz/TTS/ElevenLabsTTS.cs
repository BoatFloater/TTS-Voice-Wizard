﻿using Amazon.Polly.Model;
using Amazon.Runtime.Internal;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using Octokit.Internal;
using OSCVRCWiz.Settings;
using OSCVRCWiz.Text;
using Resources;
using SpotifyAPI.Web.Http;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using static OSCVRCWiz.TTS.ElevenLabsTTS;
using static SpotifyAPI.Web.SearchRequest;
using static System.Net.Mime.MediaTypeNames;

namespace OSCVRCWiz.TTS
{
    public class ElevenLabsTTS
    {

        private static readonly HttpClient client = new HttpClient();
        public static Dictionary<string, string> voiceDict =null;
        public static bool elevenFirstLoad = true;
        public static async Task ElevenLabsTextAsSpeech(string text)
        {

            // if ("tiktokvoice.mp3" == null)
            //   throw new NullReferenceException("Output path is null");
            //text = FormatInputText(text);
            string voice = "";
            VoiceWizardWindow.MainFormGlobal.Invoke((MethodInvoker)delegate ()
            {
                voice = VoiceWizardWindow.MainFormGlobal.comboBox2.Text.ToString();
            });
            System.Diagnostics.Debug.WriteLine("eleven speech ran " + voice);
            try
            {
                var voiceID = voiceDict.FirstOrDefault(x => x.Value == voice).Key;
                //  byte[] result = await CallTikTokAPIAsync(text, voice);
                //  File.WriteAllBytes("TikTokTTS.mp3", result);          
                //  Task.Run(() => PlayAudioHelper());

                MemoryStream memoryStream = new MemoryStream();

                Task<Stream> streamTask = CallElevenLabsAPIAsync(text, voiceID);
                Stream stream = streamTask.Result;

                AmazonPollyTTS.WriteSpeechToStream(stream, memoryStream);



                memoryStream.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);
                Mp3FileReader wav = new Mp3FileReader(memoryStream); //it does not have a wav file header so it is mp3 formate unless systemspeech, and fonixtalk
               // var wav = new RawSourceWaveStream(memoryStream, new WaveFormat(11000, 16, 1));
                var output = new WaveOut();
                output.DeviceNumber = AudioDevices.getCurrentOutputDevice();
                output.Init(wav);
                output.Play();


            }
            catch (Exception ex)
            {
                OutputText.outputLog("[ElevenLabs TTS Error: " + ex.Message + "]", Color.Red);

            }
            //System.Diagnostics.Debug.WriteLine("tiktok speech ran"+result.ToString());
        }

        public static async Task<Stream> CallElevenLabsAPIAsync(string textIn, string voice)
        {

            //modified from https://github.com/connorbutler44/bingbot/blob/main/Service/ElevenLabsTextToSpeechService.cs


            var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voice}";
            var apiKey = Settings1.Default.elevenLabsAPIKey;

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Content = JsonContent.Create(new { text = textIn });

           
            request.Headers.Add("xi-api-key", apiKey);
            request.Headers.Add("Accept", "audio/mpeg");

            //  var data = "{\"text\":\"" + text + "\"}";

            HttpResponseMessage response = await client.SendAsync(request);

            System.Diagnostics.Debug.WriteLine("Eleven Response:"+ response.StatusCode);



           
            return await response.Content.ReadAsStreamAsync();

        }
        public class Voice
        {
            public string voice_id { get; set; }
            public string name { get; set; }
        }
        public class Voices
        {
            public List<Voice> voices { get; set; }
        }


        public static void CallElevenVoices()
        {
            try
            {

                //modified from https://github.com/connorbutler44/bingbot/blob/main/Service/ElevenLabsTextToSpeechService.cs


                var url = $"https://api.elevenlabs.io/v1/voices";
            var apiKey = Settings1.Default.elevenLabsAPIKey;
          




        WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("xi-api-key", apiKey);
            using (WebResponse response = request.GetResponse())
            {

                using (Stream stream = response.GetResponseStream())
                {

                    using (var streamReader = new StreamReader(stream))
                    {
                        var result = streamReader.ReadToEnd();
                        // var dataHere = JObject.Parse(result.ToString()).SelectToken("data").ToString();
                        // audioInBase64 = dataHere.ToString();

                        System.Diagnostics.Debug.WriteLine(result.ToString());
                        var json = result.ToString();

                        Voices voices = JsonConvert.DeserializeObject<Voices>(json);
                       voiceDict = voices.voices.ToDictionary(v => v.voice_id, v => v.name);
                    }

                }
            }
                /*foreach (KeyValuePair<string, string> kvp in dict)
                  {
                      Console.WriteLine("Voice ID: " + kvp.Key + ", Name: " + kvp.Value);
                  }*/

                // System.Diagnostics.Debug.WriteLine(audioInBase64);
                // return Convert.FromBase64String(audioInBase64);
                elevenFirstLoad = false;
            }
            catch (Exception ex)
            {
                OutputText.outputLog("[ElevenLabs Voice Load Error: " + ex.Message + "]", Color.Red);

            }

        }




    }
}