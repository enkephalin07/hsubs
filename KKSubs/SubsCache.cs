﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using MessagePack;
using UnityEngine;

namespace KKSubs
{
    public class SubsCache
    {
        public const string SSURL = "https://docs.google.com/spreadsheets/d/";
        public const string SHEET_KEY = "1U0pRyY8e2fIg0E4iBXXRIzpGGDBs5W_g9KfjObS-xI0";
        public const string GID = "677855862";
        public const string RANGE = "A1:C";

        public static string fileCache => Path.Combine(Paths.PluginPath, "translation\\hsubs.msgpack");

        internal static bool UpdateSubs()
        {
            if ((KKSubsPlugin.Updatemode.Value == KKSubsPlugin.UpdateMode.None || KKSubsPlugin.Updatemode.Value == KKSubsPlugin.UpdateMode.Scene) && File.Exists(fileCache))
            {
                var dic = LoadFromMessagepack(fileCache); // integrity test
            }
            else
            {
                BepInEx.Logger.Log(LogLevel.Info, KKSubsPlugin.BEPNAME + ((File.Exists(fileCache) ? "" : fileCache + "Cache not found. ") + "Updating subs..."));
                KKSubsPlugin.Plugin.StartCoroutine(DownloadSubs());
            }
            return true;
        }

        internal static IEnumerator DownloadSubs()
        {
            BepInEx.Logger.Log(LogLevel.Info, KKSubsPlugin.BEPNAME + "Downloading subs from " + SSURL + SHEET_KEY);
            // + $"export?exportFormat=csv&gid={GID}&range={VOICEID}");
            var dl = new WWW(SSURL + SHEET_KEY + "/export?exportFormat=csv&gid=" 
                + GID + "&range=" + RANGE);
            while (!dl.isDone)
            {
                BepInEx.Logger.Log(LogLevel.Debug, KKSubsPlugin.BEPNAME + $"DownloadSubs(): {dl.url}");
                yield return new WaitForSeconds(30);
            }
            BepInEx.Logger.Log(LogLevel.Debug, KKSubsPlugin.BEPNAME + "DownloadSubs(): Complete");

            if (dl.error != null)
            {
                BepInEx.Logger.Log(LogLevel.Warning, KKSubsPlugin.BEPNAME + "Failed to fetch latest subtitles. Going to use cached ones.");
                yield break;
            }

            BepInEx.Logger.Log(LogLevel.Info, KKSubsPlugin.BEPNAME + "Downloaded " + dl.bytesDownloaded + " bytes. Parsing...");

            var cnt = VoiceCtrl.BuildDictionary(dl.text);

            BepInEx.Logger.Log(LogLevel.Info, KKSubsPlugin.BEPNAME + "Done parsing subtitles: " + cnt + " lines found.");
            if (cnt > 60000)
                File.WriteAllBytes(fileCache, LZ4MessagePackSerializer.Serialize(VoiceCtrl.subtitlesDict));
            else
                BepInEx.Logger.Log(LogLevel.Warning, KKSubsPlugin.BEPNAME + "The amount of lines is suspiciously low (defaced sheet?); not caching.");
        }

        internal static Dictionary<string, KeyValuePair<string, string>> LoadFromMessagepack(string file = "")
        {
            string cache = (file.IsNullOrEmpty() ? fileCache : file);
            var dict = LZ4MessagePackSerializer.Deserialize<Dictionary<string, KeyValuePair<string, string>>>(File.ReadAllBytes(cache));
            BepInEx.Logger.Log(LogLevel.Info, KKSubsPlugin.BEPNAME + $"{dict.Count}  lines parsed in {cache}");

            return dict;
        }

        internal static bool SaveToMessagepack(Dictionary<string, KeyValuePair<string, string>> dict)
        {
            try
            {
                var cached = LZ4MessagePackSerializer.Deserialize<Dictionary<string, KeyValuePair<string, string>>>(File.ReadAllBytes(fileCache));
                if (cached == null)
                    cached = dict;

                else if (cached != dict)
                {
                    if (cached.Count > dict.Count)
                    {
                        foreach (var file in dict.Keys)
                        {
                            if (!dict[file].Key.IsNullOrEmpty() && cached.ContainsKey(file) && cached[file].Key != dict[file].Key)
                                cached.Remove(file);

                            cached.Add(file, dict[file]);
                        }
                    }
                    else cached = dict;
                }
                else return true;

                File.WriteAllBytes(fileCache, LZ4MessagePackSerializer.Serialize(cached));
            }
            catch { return false;  }

            return true;
        }

        internal static void GetRemote()
        {

        }
    }
}