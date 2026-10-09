// SPDX-License-Identifier: GPL-3.0-only

using Ameko.DataModels;

namespace Ameko.Messages;

public sealed class ResolutionSelectionMessage(ResolutionSelection selection)
{
    public ResolutionSelection Selection { get; } = selection;
}
