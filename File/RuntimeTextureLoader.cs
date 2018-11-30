using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace File
{
    /// <summary>
    /// Load picture to texture in runtime while keep scene performance
    /// Texture is generated with direct set pixel colors, source pixel colors are received in separated thread
    /// </summary>
    /// <code>
    /// RuntimeTextureLoader.Instance("someFile.jpg", (tex) => { /* do what you need */ });
    /// </code>
    /// <see cref="https://github.com/stefanak-michal/Unity-Stuff"/>
    /// <see cref="http://stefanak.blogspot.com/2018/01/runtime-texture-loading-and-performance.html"/>
    /// <author>Michal Stefanak</author>
    public class RuntimeTextureLoader : MonoBehaviour
    {
        struct Pixel
        {
            public int x, y;
            public Color c;
        }

        string file;
        Texture2D texture;
        bool done;
        float timer;
        List<Pixel> buffer = new List<Pixel>();
        int bufferIndex = 0;
        int width, height;
        Action<Texture2D> onDone, onProgress;

        static Queue<RuntimeTextureLoader> queue = new Queue<RuntimeTextureLoader>();
        static bool running;
        static List<string> processing = new List<string>();

        /// <summary>
        /// Initialize instance for loading picture to texture and queue it
        /// </summary>
        /// <param name="file"></param>
        /// <param name="onDone"></param>
        /// <param name="onProgress">Affect performance!</param>
        public static void Instance(string file, Action<Texture2D> onDone, Action<Texture2D> onProgress = null)
        {
            if (processing.Contains(file))
                return;

            processing.Add(file);
            queue.Enqueue(new GameObject("Picture processor").AddComponent<RuntimeTextureLoader>().Initialize(file, onDone, onProgress));
            Execute();
        }

        static void Execute()
        {
            if (!running && queue.Count > 0)
                queue.Dequeue().Run();
        }

        /// <summary>
        /// Create picture processing instance prepared to be execute
        /// </summary>
        RuntimeTextureLoader Initialize(string file, Action<Texture2D> onDone, Action<Texture2D> onProgress)
        {
            this.file = file;
            this.onDone = onDone;
            this.onProgress = onProgress;
            gameObject.SetActive(false);

            return this;
        }

        /// <summary>
        /// Execute picture processing
        /// </summary>
        void Run()
        {
            running = true;
            timer = Time.unscaledTime;
            gameObject.SetActive(true);
            new Thread(FillBufferThread).Start();
        }

        void Update()
        {
            if (buffer.Count > bufferIndex)
            {
                while (buffer.Count > bufferIndex)
                {
                    if (texture == null)
                        texture = new Texture2D(width, height, TextureFormat.ARGB32, true, false);

                    texture.SetPixel(buffer[bufferIndex].x, buffer[bufferIndex].y, buffer[bufferIndex].c);
                    bufferIndex++;
                }

                if (onProgress != null)
                {
                    texture.Apply();
                    onProgress(texture);
                }
            }

            if (done)
            {
                if (Debug.isDebugBuild)
                    Debug.Log("Picture \"" + file + "\" processed in " + (Time.unscaledTime - timer));

                buffer.Clear();
                texture.Apply();
                onDone(texture);
                running = false;
                Execute();

                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Separated thread to load picture pixel colors
        /// </summary>
        void FillBufferThread()
        {
            Thread.Sleep(100); //because of some reason we need this wait at the start of thread

            System.Drawing.Bitmap b = new System.Drawing.Bitmap(file);
            System.Drawing.Color c;
            width = b.Width;
            height = b.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    c = b.GetPixel(x, y);
                    buffer.Add(new Pixel()
                    {
                        x = x,
                        y = height - y,
                        c = new Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f) //faster way of InverseLerp(0, 255, x)
                    });
                }
            }

            processing.Remove(file);
            done = true;
        }
    }
}
