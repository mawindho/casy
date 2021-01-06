using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;

namespace OLS.Casy.Controller.Speech
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    public class SpeechController : AbstractService
    {
        private CancellationTokenSource _tokenSource;
        private SpeechRecognitionEngine _engine;
        private SpeechSynthesizer _synthesizer;

        private IEventAggregatorProvider _eventAggregatorProvider;

        [ImportingConstructor]
        public SpeechController(IConfigService configService, IEventAggregatorProvider eventAggregatorProvider)
            : base(configService)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
        }

        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Task.Factory.StartNew(StartSpeechRecognition, token);
        }

        private void StartSpeechRecognition()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SelectVoice(_synthesizer.GetInstalledVoices(new CultureInfo("en-us")).FirstOrDefault().VoiceInfo.Name);
            _synthesizer.SetOutputToDefaultAudioDevice();

            _engine = new SpeechRecognitionEngine(new CultureInfo("en-us"));
            _engine.SetInputToDefaultAudioDevice();
            _engine.SpeechRecognized += OnSpeechRecognized;

            var ch_numCelan = new Choices();
            ch_numCelan.Add("1");
            ch_numCelan.Add("2");
            ch_numCelan.Add("3");
            ch_numCelan.Add("4");
            ch_numCelan.Add("5");
            ch_numCelan.Add("6");
            ch_numCelan.Add("7");
            ch_numCelan.Add("8");
            ch_numCelan.Add("9");
            GrammarBuilder gb_clean = new GrammarBuilder();
            gb_clean.Append("Purge");
            gb_clean.Append(ch_numCelan);
            gb_clean.Append("times");

            Grammar cleanGrammar = new Grammar(gb_clean);
            _engine.LoadGrammarAsync(cleanGrammar);
            _engine.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            var text = e.Result.Text;

            Debug.WriteLine("Speech recognition: " + text);

            var confidence = e.Result.Confidence;
            //if (confidence < 0.65) return;

            

            if (text.IndexOf("Purge") >= 0 && text.IndexOf("times") >= 0)
            {
                var words = text.Split(' ');
                _synthesizer.Speak($"Okay. Start purging the system {words[1]} times.");

                _eventAggregatorProvider.Instance.GetEvent<RemoteCommandEvent>().Publish(new RemoteCommand()
                    {Command = "Clean", Parameter1 = int.Parse(words[1])});
            }
        }

        public override void Deinitialize(IProgress<string> progress)
        {
            _tokenSource.Cancel();
            base.Deinitialize(progress);
        }

        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_tokenSource != null)
                    {
                        this._tokenSource.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        ~SpeechController()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
