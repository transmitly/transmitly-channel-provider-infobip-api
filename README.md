# Transmitly.ChannelProvider.Infobip.Api

Infobip HTTP API-based channel-provider implementation for [Transmitly](https://github.com/transmitly/transmitly).

## Recommended Package

Most users should use the convenience package instead:

- [Transmitly.ChannelProvider.Infobip](https://github.com/transmitly/transmitly-channel-provider-infobip)

That package registers this API implementation for you.

## What This Package Provides

- `SmsChannelProviderDispatcher` for `Sms`.
- `EmailChannelProviderDispatcher` for `Email`.
- `VoiceChannelProviderDispatcher` for `Voice`.
- Delivery-report request adaptors for Infobip email, SMS, and voice webhooks.
- Concrete channel-property adaptors for Infobip email, SMS, and voice features.

## Using This Package Directly (Advanced)

```csharp
using Transmitly;
using Transmitly.ChannelProvider.Infobip.Api.Email;
using Transmitly.ChannelProvider.Infobip.Api.Sms;
using Transmitly.ChannelProvider.Infobip.Api.Voice;
using Transmitly.ChannelProvider.Infobip.Configuration;

var builder = new CommunicationsClientBuilder();

var options = new InfobipChannelProviderConfiguration
{
	ApiKey = "your-infobip-api-key",
	BasePath = "https://base.infobip.com",
	ApiKeyPrefix = "App"
};

builder.ChannelProvider
	.Build(Id.ChannelProvider.Infobip(), options)
	.AddDispatcher<SmsChannelProviderDispatcher, ISms>(Id.Channel.Sms())
	.AddDispatcher<EmailChannelProviderDispatcher, IEmail>(Id.Channel.Email())
	.AddDispatcher<VoiceChannelProviderDispatcher, IVoice>(Id.Channel.Voice())
	.AddDeliveryReportRequestAdaptor<SmsDeliveryStatusReportAdaptor>()
	.AddDeliveryReportRequestAdaptor<VoiceDeliveryStatusReportAdaptor>()
	.AddDeliveryReportRequestAdaptor<EmailDeliveryStatusReportAdaptor>()
	.AddSmsExtendedPropertiesAdaptor<SmsExtendedChannelProperties>()
	.AddVoiceExtendedPropertiesAdaptor<VoiceExtendedChannelProperties>()
	.AddEmailExtendedPropertiesAdaptor<EmailExtendedChannelProperties>()
	.Register();
```

## Related Packages

- [Transmitly](https://github.com/transmitly/transmitly)
- [Transmitly.ChannelProvider.Infobip](https://github.com/transmitly/transmitly-channel-provider-infobip)
- [Transmitly.ChannelProvider.Infobip.Configuration](https://github.com/transmitly/transmitly-channel-provider-infobip-configuration)

---
_Copyright (c) Code Impressions, LLC. This open-source project is sponsored and maintained by Code Impressions and is licensed under the [Apache License, Version 2.0](http://apache.org/licenses/LICENSE-2.0.html)._
