using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.IO.Api;
using OLS.Com.WebSockets.Common;
using OLS.OmniBus.Subsribtion;
using OLS.OmniBus.Subsribtion.Api;
using OLS.OmniBus.Subsribtion.Messages;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Com.OmniBus
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IOmniBusConnector))]
    [Export(typeof(IService))]
    public class OmniBusConnector : AbstractService, IOmniBusConnector
    {
        private ISubscribtionService _subscribtionService;
        private IWebSocketLogger _webSocketLogger;
        private readonly IDatabaseStorageService _databaseStorageService;

        [ImportingConstructor]
        public OmniBusConnector(IConfigService configService, IWebSocketLogger webSocketLogger, IDatabaseStorageService databaseStorageService)
            : base(configService)
        {
            this._webSocketLogger = webSocketLogger;
            this._databaseStorageService = databaseStorageService;
        }

        [ConfigItem("localhost")]
        public string OmniBusSubscribtionServer { get; set; }

        [ConfigItem("53783")]
        public string OmniBusSubscribtionServerPort { get; set; }

        [ConfigItem("omnibus")]
        public string OmniBusSubscribtionServerSuffix { get; set; }

        [ConfigItem("415d21ac-804c-11e8-adc0-fa7ae01bbebc")]
        public string OmniBusInstrumentType { get; set; }

        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);

            this._subscribtionService = new WebSocketSubcribtionService(null, _webSocketLogger);
            this._subscribtionService.UpdateConnectionDetails(
                OmniBusSubscribtionServer,
                OmniBusSubscribtionServerPort,
                OmniBusSubscribtionServerSuffix);

            this._subscribtionService.CreateSubscription(SubscrptionChannel.Recipient, OnPrivateMessage);

            string omniBusIdentifier;
            if (!_databaseStorageService.GetSettings().TryGetValue("OmniBusIdentifier", out omniBusIdentifier))
            {
                omniBusIdentifier = "";
            }

            this._subscribtionService.PublishMessage(new InstrumentStateMessage()
            {
                Channel = SubscrptionChannel.InstrumentStateChannel,
                InstrumentState = InstrumentState.Online,
                InstrumentTypeIdentifier = OmniBusInstrumentType,
                InstrumentIdentifier = omniBusIdentifier,
                Message = string.Format("Hello. Here is a CASY! Jihaaa. My identifier is: {0}", omniBusIdentifier)
            });
        }

        private void OnPrivateMessage(object baseMessage)
        {
            var instrumentStateMessage = baseMessage as InstrumentStateMessage;
            if (instrumentStateMessage != null)
            {
                _databaseStorageService.SaveSetting("OmniBusIdentifier", instrumentStateMessage.InstrumentIdentifier);
            }
        }

        public override void Deinitialize(IProgress<string> progress)
        {
            string omniBusIdentifier;
            if (!_databaseStorageService.GetSettings().TryGetValue("OmniBusIdentifier", out omniBusIdentifier))
            {
                omniBusIdentifier = "";
            }

            this._subscribtionService.PublishMessage(new InstrumentStateMessage()
            {
                Channel = SubscrptionChannel.InstrumentStateChannel,
                InstrumentState = InstrumentState.Offline,
                InstrumentTypeIdentifier = OmniBusInstrumentType,
                InstrumentIdentifier = omniBusIdentifier,
                Message = "CASY is now going offline. OOooooohhhhhh :/"
            });

            this._subscribtionService.UnSubscribeAll();
            base.Deinitialize(progress);
        }
    }
}
