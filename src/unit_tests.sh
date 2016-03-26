#!/bin/bash

mono packages/Machine.Specifications-Signed.0.5.12/tools/mspec-clr4.exe tests/NanoMessageBus.UnitTests/bin/Debug/NanoMessageBus.UnitTests.dll
mono packages/Machine.Specifications-Signed.0.5.12/tools/mspec-clr4.exe tests/NanoMessageBus.JsonSerializer.UnitTests/bin/Debug/NanoMessageBus.JsonSerializer.UnitTests.dll
mono packages/Machine.Specifications-Signed.0.5.12/tools/mspec-clr4.exe tests/NanoMessageBus.RabbitChannel.UnitTests/bin/Debug/NanoMessageBus.RabbitChannel.UnitTests.dll
mono packages/Machine.Specifications-Signed.0.5.12/tools/mspec-clr4.exe tests/NanoMessageBus.RabbitChannel.IntegrationTests/bin/Debug/NanoMessageBus.RabbitChannel.IntegrationTests.dll

