// SPDX-License-Identifier: GPL-3.0-only

using Holo.Models;

namespace Ameko.Services;

public sealed class LayoutStateManager
{
    private string _layoutName = string.Empty;
    private double[] _columnRatios = [];
    private double[] _rowRatios = [];

    public void ActivateLayout(Layout layout)
    {
        if (_layoutName == layout.Name)
            return;

        _layoutName = layout.Name;
        _columnRatios = [];
        _rowRatios = [];
    }

    public void GetRatios(out double[]? columnRatios, out double[]? rowRatios)
    {
        columnRatios = (double[])_columnRatios.Clone();
        rowRatios = (double[])_rowRatios.Clone();
    }

    public void Update(Layout layout, double[] columnRatios, double[] rowRatios)
    {
        if (_layoutName != layout.Name)
            return;

        _columnRatios = columnRatios;
        _rowRatios = rowRatios;
    }
}
