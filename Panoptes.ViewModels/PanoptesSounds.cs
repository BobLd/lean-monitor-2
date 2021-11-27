using System.Media;

namespace Panoptes.ViewModels
{
    internal static class PanoptesSounds
    {
        private static readonly SoundPlayer player = new SoundPlayer(@"C:\Users\Bob\Downloads\tests_test-audio_wav_mono_16bit_44100.wav");

        /// <summary>
        /// Deactivate when in backtest.
        /// </summary>
        public static bool CanPlaySounds { get; set; } = true;

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
