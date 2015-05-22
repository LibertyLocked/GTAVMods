/*
 * Internet Radio
 * Author: libertylocked
 * Version: 0.1a
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NAudio;
using NAudio.Wave;

namespace InternetRadio
{
    public class InternetRadio : Script
    {
        IPlayer radioPlayer;
        bool isEnabled = false;
        //bool playerWasInVeh = false;

        Keys toggleKey;
        string streamUrl;
        int volume;

        public InternetRadio()
        {
            ParseSettings();
            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;

            radioPlayer = new ShoutcastPlayer();
            //radioPlayer.OnException += OnPlayerException;
        }

        void OnTick(object sender, EventArgs e)
        {
            Player player = Game.Player;
            if (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null
                && player.Character.IsInVehicle())
            {
                // disable/enable vehicle radio if internet radio is enabled/disabled
                Function.Call(Hash.SET_VEHICLE_RADIO_ENABLED, player.Character.CurrentVehicle, !isEnabled);
            }

            //if (radioPlayer.State != StreamingPlaybackState.Stopped)
                radioPlayer.Update();
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == toggleKey)
            {
                isEnabled = !isEnabled;
                if (isEnabled)
                {
                    PlayFromURL(streamUrl);
                    PlayerSetVolume(volume);
                    UI.ShowSubtitle("Internet Radio ~r~On" + " ~w~" + streamUrl, 5000);
                }
                else
                {
                    PlayerStop();
                    PlayerSetVolume(volume);
                    UI.ShowSubtitle("Internet Radio ~r~Off", 5000);
                }
            }
        }

        void ParseSettings()
        {
            using (Stream setupIn = File.Open(@".\scripts\InternetRadio.ini", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Structs.SetupFile settings = new Structs.SetupFile(setupIn);

                this.toggleKey = settings.GetEntryByName("Core", "ToggleKey").KeyValue;
                this.streamUrl = settings.GetDataByName("Core", "StreamUrl");
                this.volume = settings.GetEntryByName("Core", "Volume").Intvalue;
            }
        }

        void OnPlayerException(object sender, UnhandledExceptionEventArgs e)
        {
            UI.ShowSubtitle("~r~Oh noes. Internet Radio broke. Restarting");
            Wait(5000);
            radioPlayer = new ShoutcastPlayer();
            radioPlayer.OnException += OnPlayerException;
        }

        void PlayFromURL(string url)
        {
            try
            {
                radioPlayer.StartPlayback(url);
            }
            catch { }
        }

        void PlayerStop()
        {
            try
            {
                radioPlayer.StopPlayback();
            }
            catch { }
        }

        void PlayerSetVolume(int volume)
        {
            try
            {
                radioPlayer.Volume = 100 / 100f;
            }
            catch { }
        }
    }

    public static class Structs
    {
        public abstract class Keyboard
        {
            [Flags]
            private enum KeyStates
            {
                None = 0,
                Down = 1,
                Toggled = 2
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            private static extern short GetKeyState(int keyCode);

            private static KeyStates GetKeyState(Keys key)
            {
                KeyStates state = KeyStates.None;

                short retVal = GetKeyState((int)key);

                //If the high-order bit is 1, the key is down
                //otherwise, it is up.
                if ((retVal & 0x8000) == 0x8000)
                    state |= KeyStates.Down;

                //If the low-order bit is 1, the key is toggled.
                if ((retVal & 1) == 1)
                    state |= KeyStates.Toggled;

                return state;
            }

            public static bool IsKeyDown(Keys key)
            {
                return KeyStates.Down == (GetKeyState(key) & KeyStates.Down);
            }

            public static bool IsKeyToggled(Keys key)
            {
                return KeyStates.Toggled == (GetKeyState(key) & KeyStates.Toggled);
            }
        }
        public class SetupFile
        {
            Dictionary<string, List<SetupFileEntry>> Entries { get; set; }

            public string GetDataByName(string section, string name)
            {
                foreach (SetupFileEntry _entry in Entries[section])
                {
                    if (_entry.Name == name)
                        return _entry.Data;
                }
                return "NO_ENTRY";
            }
            public SetupFileEntry GetEntryByName(string section, string name)
            {
                foreach (SetupFileEntry _entry in Entries[section])
                {
                    if (_entry.Name == name)
                        return _entry;
                }
                return null;
            }
            public SetupFile()
            {
                Entries = new Dictionary<string, List<SetupFileEntry>>();
            }
            public SetupFile(Stream xIn)
            {
                Entries = new Dictionary<string, List<SetupFileEntry>>();
                ReadFromStream(xIn);
            }
            public void ReadFromStream(Stream xIn)
            {
                StreamReader reader = new StreamReader(xIn);

                string currentSection = string.Empty;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == null || line == "" || string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("."))
                    {
                        Entries.Add(line.Substring(1), new List<SetupFileEntry>());
                        currentSection = line.Substring(1);
                    }
                    else
                    {
                        int assignerIndex = line.IndexOf('=');
                        string entryName = line.Substring(0, assignerIndex - 1);
                        string entryData = line.Substring(assignerIndex + 2);
                        Entries[currentSection].Add(new SetupFileEntry() { Name = entryName, Data = entryData });

                    }
                }
            }
            public class SetupFileEntry
            {
                public string Name;
                public string Data;

                public int Intvalue
                {
                    get
                    {
                        return int.Parse(Data, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                public float FloatValue
                {
                    get
                    {
                        return float.Parse(Data, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                public bool BoolValue
                {
                    get
                    {
                        return bool.Parse(Data);
                    }
                }
                public System.Windows.Forms.Keys KeyValue
                {
                    get
                    {
                        return (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), Data);
                    }
                }
            }
        }
    }

    public delegate void StreamTitleChangedEventHandler(object sender, StreamTitleChangedEventArgs e);

    public class ShoutcastStream : Stream
    {
        private int metaInt;
        private int receivedBytes;
        private Stream netStream;
        private bool connected = false;
        private string streamTitle;

        private int read;
        private int leftToRead;
        private int thisOffset;
        private int bytesRead;
        private int bytesLeftToMeta;
        private int metaLen;
        private byte[] metaInfo;

        private long pos;

        /// <summary>
        /// Is fired, when a new StreamTitle is received
        /// </summary>
        public event StreamTitleChangedEventHandler StreamTitleChanged;

        public int MetaInt
        {
            get { return metaInt; }
            set { metaInt = value; }
        }

        /// <summary>
        /// Creates a new ShoutcastStream and connects to the specified Url
        /// </summary>
        /// <param name="url">Url of the Shoutcast stream</param>
        public ShoutcastStream(string url)
        {
            HttpWebResponse response;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Headers.Clear();
            request.Headers.Add("Icy-MetaData", "1");
            request.KeepAlive = false;
            request.UserAgent = "VLC media player";

            response = (HttpWebResponse)request.GetResponse();

            metaInt = int.Parse(response.Headers["Icy-MetaInt"]);
            receivedBytes = 0;

            netStream = response.GetResponseStream();

            connected = true;
        }

        public ShoutcastStream(Stream respStream)
        {
            netStream = respStream;

            connected = true;
        }

        /// <summary>
        /// Parses the received Meta Info
        /// </summary>
        /// <param name="metaInfo"></param>
        private void ParseMetaInfo(byte[] metaInfo)
        {
            string metaString = Encoding.ASCII.GetString(metaInfo);

            string newStreamTitle = Regex.Match(metaString, "(StreamTitle=')(.*)(';StreamUrl)").Groups[2].Value.Trim();
            if (!newStreamTitle.Equals(streamTitle))
            {
                streamTitle = newStreamTitle;
                OnStreamTitleChanged();
            }
        }

        /// <summary>
        /// Fires the StreamTitleChanged event
        /// </summary>
        protected virtual void OnStreamTitleChanged()
        {
            if (StreamTitleChanged != null)
            {
                StreamTitleChanged(this, new StreamTitleChangedEventArgs(this.StreamTitle));
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the ShoutcastStream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return connected; }
        }

        /// <summary>
        /// Gets a value that indicates whether the ShoutcastStream supports seeking.
        /// This property will always be false.
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value that indicates whether the ShoutcastStream supports writing.
        /// This property will always be false.
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the title of the stream
        /// </summary>
        public string StreamTitle
        {
            get { return streamTitle; }
        }

        /// <summary>
        /// Flushes data from the stream.
        /// This method is currently not supported
        /// </summary>
        public override void Flush()
        {
            return;
        }

        /// <summary>
        /// Gets the length of the data available on the Stream.
        /// This property is not currently supported and always thows a <see cref="NotSupportedException"/>.
        /// </summary>
        public override long Length
        {
            get
            {
                return pos;
                //throw new NotSupportedException(); 
            }
        }

        /// <summary>
        /// Gets or sets the current position in the stream.
        /// This property is not currently supported and always thows a <see cref="NotSupportedException"/>.
        /// </summary>
        public override long Position
        {
            get
            {
                return pos;
                //throw new NotSupportedException();
            }
            set
            {
                //pos = value;
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Reads data from the ShoutcastStream.
        /// </summary>
        /// <param name="buffer">An array of bytes to store the received data from the ShoutcastStream.</param>
        /// <param name="offset">The location in the buffer to begin storing the data to.</param>
        /// <param name="count">The number of bytes to read from the ShoutcastStream.</param>
        /// <returns>The number of bytes read from the ShoutcastStream.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                read = 0;
                leftToRead = count;
                thisOffset = offset;
                bytesRead = 0;
                bytesLeftToMeta = ((metaInt - receivedBytes) > count) ? count : (metaInt - receivedBytes);

                while (bytesLeftToMeta > 0 && (read = netStream.Read(buffer, thisOffset, bytesLeftToMeta)) > 0)
                {
                    leftToRead -= read;
                    thisOffset += read;
                    bytesRead += read;
                    receivedBytes += read;
                    bytesLeftToMeta -= read;
                }

                // read metadata
                if (receivedBytes == metaInt)
                {
                    readMetaData();
                }

                while (leftToRead > 0 && (read = netStream.Read(buffer, thisOffset, leftToRead)) > 0)
                {
                    leftToRead -= read;
                    thisOffset += read;
                    bytesRead += read;
                    receivedBytes += read;
                }

                pos += bytesRead;
                return bytesRead;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void readMetaData()
        {
            metaLen = netStream.ReadByte();
            if (metaLen > 0)
            {
                metaInfo = new byte[metaLen * 16];
                int len = 0;
                while ((len += netStream.Read(metaInfo, len, metaInfo.Length - len)) < metaInfo.Length) ;
                ParseMetaInfo(metaInfo);
            }
            receivedBytes = 0;
        }

        /// <summary>
        /// Closes the ShoutcastStream.
        /// </summary>
        public override void Close()
        {
            connected = false;
            netStream.Close();
        }

        /// <summary>
        /// Sets the current position of the stream to the given value.
        /// This Method is not currently supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the length of the stream.
        /// This Method always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes data to the ShoutcastStream.
        /// This method is not currently supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    public class StreamTitleChangedEventArgs : EventArgs
    {
        public readonly string Title;

        public StreamTitleChangedEventArgs(string title)
        {
            this.Title = title;
        }
    }

    public interface IPlayer : IDisposable
    {
        StreamingPlaybackState State { get; }
        string StreamTitle { get; }
        int StreamBitrate { get; }
        float Volume { get; set; }
        event UnhandledExceptionEventHandler OnException;

        void StartPlayback(string url);
        void PausePlayback();
        void StopPlayback();
        void Update();
    }

    public enum StreamingPlaybackState
    {
        Stopped,
        Playing,
        Buffering,
        Paused
    }

    public class ShoutcastPlayer : IPlayer
    {
        private BufferedWaveProvider bufferedWaveProvider;
        private IWavePlayer waveOut;
        private volatile StreamingPlaybackState playbackState;
        private volatile bool fullyDownloaded;
        private HttpWebRequest webRequest;
        private VolumeWaveProvider16 volumeProvider;
        private float volume;

        private const string STR_TITLE_NOT_AVAIL = "Stream title not available";

        public event UnhandledExceptionEventHandler OnException;

        public string StreamTitle
        {
            get;
            private set;
        }

        public int StreamBitrate
        {
            get;
            private set;
        }

        public float Volume
        {
            get { return volume; }
            set
            {
                if (value > 1f) volume = 1f;
                else if (value < 0) volume = 0;
                else volume = value;
            }
        }

        public StreamingPlaybackState State
        {
            get { return playbackState; }
        }

        public ShoutcastPlayer()
        {
            volume = 1f;
        }

        public void StartPlayback(string url)
        {
            if (playbackState == StreamingPlaybackState.Stopped)
            {
                playbackState = StreamingPlaybackState.Buffering;
                bufferedWaveProvider = null;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        StreamMp3(url);
                    }
                    catch (Exception ex)
                    {
                        if (OnException != null)
                            OnException(this, new UnhandledExceptionEventArgs(ex, true));
                    }
                }
                );
                //timer1.Enabled = true;
            }
            else if (playbackState == StreamingPlaybackState.Paused)
            {
                playbackState = StreamingPlaybackState.Buffering;
            }
        }

        public void PausePlayback()
        {
            if (playbackState == StreamingPlaybackState.Playing || playbackState == StreamingPlaybackState.Buffering)
            {
                waveOut.Pause();
                playbackState = StreamingPlaybackState.Paused;
            }
        }

        public void StopPlayback()
        {
            if (playbackState != StreamingPlaybackState.Stopped)
            {
                if (!fullyDownloaded)
                {
                    if (webRequest != null)
                        webRequest.Abort();
                }

                playbackState = StreamingPlaybackState.Stopped;
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                //timer1.Enabled = false;
                // n.b. streaming thread may not yet have exited
                Thread.Sleep(500);
            }
        }

        public void Update()
        {
            if (playbackState != StreamingPlaybackState.Stopped)
            {
                if (waveOut == null && bufferedWaveProvider != null)
                {
                    waveOut = CreateWaveOut();
                    waveOut.PlaybackStopped += OnPlaybackStopped;
                    volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                    volumeProvider.Volume = Volume;
                    waveOut.Init(volumeProvider);
                }
                else if (bufferedWaveProvider != null)
                {
                    volumeProvider.Volume = Volume;

                    var bufferedSeconds = bufferedWaveProvider.BufferedDuration.TotalSeconds;
                    // make it stutter less if we buffer up a decent amount before playing
                    if (bufferedSeconds < 0.5 && playbackState == StreamingPlaybackState.Playing && !fullyDownloaded)
                    {
                        Pause();
                    }
                    else if (bufferedSeconds > 4 && playbackState == StreamingPlaybackState.Buffering)
                    {
                        Play();
                    }
                    else if (fullyDownloaded && bufferedSeconds == 0)
                    {
                        StopPlayback();
                    }
                }

            }
            else
            {
                this.StreamTitle = "";
            }
        }

        public void Dispose()
        {
            this.webRequest = null;
            try
            {
                waveOut.Stop();
                waveOut.Dispose();
            }
            catch { }
            waveOut = null;
            //this.StopPlayback();
        }

        private void StreamMp3(object state)
        {
            fullyDownloaded = false;
            var url = (string)state;
            webRequest = (HttpWebRequest)WebRequest.Create(url);

            // To retrieve ShoutCAST metadata
            int metaInt = 0; // blocksize of mp3 data
            webRequest.Headers.Clear();
            webRequest.Headers.Add("GET", "/ HTTP/1.0");
            // needed to receive metadata informations
            webRequest.Headers.Add("Icy-MetaData", "1");
            webRequest.UserAgent = "WinampMPEG/5.09";

            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse)webRequest.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    throw;
                    //ShowError(e.Message);
                }
                return;
            }
            var buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            try
            {
                // read blocksize to find metadata block
                metaInt = Convert.ToInt32(resp.GetResponseHeader("icy-metaint"));
            }
            catch
            {
            }


            IMp3FrameDecompressor decompressor = null;
            try
            {
                using (var responseStream = resp.GetResponseStream())
                {
                    var readFullyStream = new ShoutcastStream(responseStream);
                    readFullyStream.MetaInt = metaInt;

                    do
                    {
                        if (IsBufferNearlyFull)
                        {
                            Thread.Sleep(500);
                        }
                        else
                        {
                            Mp3Frame frame;
                            try
                            {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);

                                if (metaInt > 0)
                                    StreamTitle = (readFullyStream.StreamTitle);
                                else
                                    StreamTitle = STR_TITLE_NOT_AVAIL;
                            }
                            catch (EndOfStreamException)
                            {
                                fullyDownloaded = true;
                                // reached the end of the MP3 file / stream
                                break;
                            }
                            catch (WebException)
                            {
                                // probably we have aborted download from the GUI thread
                                break;
                            }
                            if (decompressor == null)
                            {
                                // don't think these details matter too much - just help ACM select the right codec
                                // however, the buffered provider doesn't know what sample rate it is working at
                                // until we have a frame
                                decompressor = CreateFrameDecompressor(frame);
                                bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                                bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(20); // allow us to get well ahead of ourselves
                                //this.bufferedWaveProvider.BufferedDuration = 250;

                                // Set the bitrate property
                                this.StreamBitrate = frame.BitRate;
                            }
                            try
                            {
                                int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                                //Debug.WriteLine(String.Format("Decompressed a frame {0}", decompressed));
                                bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                            }
                            catch
                            {
                                if (this.State == StreamingPlaybackState.Stopped) return;
                                else throw;
                            }
                        }

                    } while (playbackState != StreamingPlaybackState.Stopped);
                    // was doing this in a finally block, but for some reason
                    // we are hanging on response stream .Dispose so never get there
                    decompressor.Dispose();
                }
            }
            finally
            {
                if (decompressor != null)
                {
                    decompressor.Dispose();
                }
            }
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        private bool IsBufferNearlyFull
        {
            get
            {
                return bufferedWaveProvider != null &&
                       bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                       < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
            }
        }

        private void Play()
        {
            waveOut.Play();
            playbackState = StreamingPlaybackState.Playing;
        }

        private void Pause()
        {
            playbackState = StreamingPlaybackState.Buffering;
            waveOut.Pause();
        }

        private IWavePlayer CreateWaveOut()
        {
            return new WaveOut();
        }

        private void MP3StreamingPanel_Disposing(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                throw e.Exception;
                //MessageBox.Show(String.Format("Playback Error {0}", e.Exception.Message));
            }
        }
    }
}
