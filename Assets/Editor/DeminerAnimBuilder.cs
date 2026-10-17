using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MinefieldClerk.Player;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Collects the deminer animation frames (Assets/Art/Characters/anim/[inj]front|back_walk|idle|probe|flag/NN.png)
    /// into the DeminerSprites component of the open scene. Menu: Minefield/Art/Build Deminer Animation
    /// </summary>
    public static class DeminerAnimBuilder
    {
        private const string Dir = "Assets/Art/Characters/anim";
        private const string FrameFormat = "00";   // frame files are 00.png, 01.png, ...
        private const string InjuredPrefix = "inj";
        private const string WalkAct = "walk", IdleAct = "idle", ProbeAct = "probe", FlagAct = "flag";

        // Playback speed in frames per second.
        private const float WalkFps = 10f, InjuredWalkFps = 7f, IdleFps = 4f, ProbeFps = 12f, FlagFps = 10f;

        private static float FpsFor(string act, bool injured)
        {
            switch (act)
            {
                case WalkAct: return injured ? InjuredWalkFps : WalkFps;
                case IdleAct: return IdleFps;
                case ProbeAct: return ProbeFps;
                default: return FlagFps;
            }
        }

        [MenuItem("Minefield/Art/Build Deminer Animation")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var ds = Object.FindAnyObjectByType<DeminerSprites>();
            if (ds == null)
            {
                Debug.LogError("[Anim] No DeminerSprites in the open scene. Open M2_Mines and run Apply To M2 Scene first.");
                return;
            }

            var clips = new List<DeminerSprites.Clip>();
            foreach (string state in new[] { "", InjuredPrefix })
            {
                foreach (string dir in new[] { "front", "back" })
                {
                    foreach (string act in new[] { WalkAct, IdleAct, ProbeAct, FlagAct })
                    {
                        string name = state + dir + "_" + act;
                        var frames = new List<Sprite>();
                        for (int i = 0; ; i++)
                        {
                            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "/" + name + "/" + i.ToString(FrameFormat) + ".png");
                            if (s == null) break;
                            frames.Add(s);
                        }
                        if (frames.Count == 0)
                        {
                            Debug.LogWarning("[Anim] No frames for " + name);
                            continue;
                        }

                        float fps = FpsFor(act, state == InjuredPrefix);
                        clips.Add(new DeminerSprites.Clip { name = name, frames = frames.ToArray(), fps = fps });
                    }
                }
            }

            ds.SetClips(clips, ds.GetComponent<PlayerHealth>());
            EditorUtility.SetDirty(ds);
            EditorSceneManager.MarkSceneDirty(ds.gameObject.scene);
            EditorSceneManager.SaveScene(ds.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Anim] Built " + clips.Count + " deminer clips.");
        }
    }
}
