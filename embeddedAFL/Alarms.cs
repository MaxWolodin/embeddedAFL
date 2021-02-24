using System;
using NAudio.Wave;
using System.Threading;

namespace embeddedAFL
{
    public static class Alarms
    {
        /// <summary>
        /// Plays an alarm at full volume infinitely until space bar is pressed
        /// </summary>
        public static void PlayAlarmInfinite()
        {
            do
            {
                PlayAlarm(1.0f);
            } while (!Console.KeyAvailable);
        }

        /// <summary>
        /// Plays an onetime alarm with configured volume
        /// </summary>
        /// <param name="volume">volume as float e.g 1.0f=100% or 0.2f=20%</param>
        public static void PlayAlarm(float volume)
        {
            using var audioFile = new AudioFileReader("nokiamarbl.mp3");
            using var outputDevice = new WaveOutEvent();
            outputDevice.Volume = volume;
            outputDevice.Init(audioFile);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
