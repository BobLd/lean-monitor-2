using System.Diagnostics;
using System.Media;

namespace Panoptes.Model
{
    public static class PanoptesSounds
    {
        private static readonly SoundPlayer player = new SoundPlayer(@"C:\Users\Bob\Downloads\tests_test-audio_wav_mono_16bit_44100.wav");

        private static bool _canPlaySounds;
        /// <summary>
        /// Deactivate when in backtest.
        /// </summary>
        public static bool CanPlaySounds
        {
            get
            {
                return _canPlaySounds;
            }

            set
            {
                _canPlaySounds = value;
                Debug.WriteLine($"PanoptesSounds.CanPlaySounds: value set to '{_canPlaySounds}'.");
            }
        }

        /// <summary>
        /// Sound alert for new order.
        /// </summary>
        public static void PlayNewOrder()
        {
            if (!CanPlaySounds) return;
            SystemSounds.Hand.Play();
        }
    }
}
