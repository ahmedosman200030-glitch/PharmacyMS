using Avalonia.Controls;
using System;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class CalculatorWindow : Window
{
    private string _expr = "";
    private decimal _memory = 0;
    private bool _justEvaled = false;

    public CalculatorWindow()
    {
        InitializeComponent();
        WireButtons();
    }

    private void WireButtons()
    {
        Btn0.Click += (_, _) => Input("0");
        Btn1.Click += (_, _) => Input("1");
        Btn2.Click += (_, _) => Input("2");
        Btn3.Click += (_, _) => Input("3");
        Btn4.Click += (_, _) => Input("4");
        Btn5.Click += (_, _) => Input("5");
        Btn6.Click += (_, _) => Input("6");
        Btn7.Click += (_, _) => Input("7");
        Btn8.Click += (_, _) => Input("8");
        Btn9.Click += (_, _) => Input("9");
        BtnDot.Click     += (_, _) => Input(".");
        BtnAdd.Click     += (_, _) => Input("+");
        BtnSub.Click     += (_, _) => Input("-");
        BtnMul.Click     += (_, _) => Input("*");
        BtnDiv.Click     += (_, _) => Input("/");
        BtnEquals.Click  += (_, _) => Evaluate();
        BtnC.Click       += (_, _) => Clear();
        BtnCE.Click      += (_, _) => ClearEntry();
        BtnBack.Click    += (_, _) => Backspace();
        BtnSign.Click    += (_, _) => ToggleSign();
        BtnPercent.Click += (_, _) => Percent();
        BtnMC.Click      += (_, _) => { _memory = 0; };
        BtnMR.Click      += (_, _) => { _expr += _memory.ToString(); RefreshDisplay(); };
        BtnMPlus.Click   += (_, _) => { _memory += GetCurrentValue(); };
        BtnMMinus.Click  += (_, _) => { _memory -= GetCurrentValue(); };
        BtnMS.Click      += (_, _) => { _memory = GetCurrentValue(); };
    }

    private void Input(string val)
    {
        bool isOp = val is "+" or "-" or "*" or "/";
        if (_justEvaled && !isOp) { _expr = ""; _justEvaled = false; }
        if (val == "." && _expr.Split('+', '-', '*', '/')[^1].Contains('.')) return;
        _expr += val;
        RefreshDisplay();
    }

    private void Evaluate()
    {
        if (string.IsNullOrEmpty(_expr)) return;
        ExprText.Text = _expr + " =";
        try
        {
            var result = EvalMath(_expr);
            _expr = result.ToString("G");
            ResultText.Text = _expr;
            _justEvaled = true;
        }
        catch { ResultText.Text = "Error"; _expr = ""; }
    }

    private void Clear()
    {
        _expr = ""; _justEvaled = false;
        ExprText.Text = ""; ResultText.Text = "0";
    }

    private void ClearEntry()
    {
        var i = _expr.LastIndexOfAny(new[] { '+', '-', '*', '/' });
        _expr = i >= 0 ? _expr[..(i + 1)] : "";
        RefreshDisplay();
    }

    private void Backspace()
    {
        if (_justEvaled) { Clear(); return; }
        if (_expr.Length > 0) _expr = _expr[..^1];
        RefreshDisplay();
    }

    private void ToggleSign()
    {
        _expr = _expr.StartsWith('-') ? _expr[1..] : "-" + _expr;
        RefreshDisplay();
    }

    private void Percent()
    {
        try { var v = EvalMath(_expr) / 100; _expr = v.ToString("G"); RefreshDisplay(); }
        catch { }
    }

    private decimal GetCurrentValue()
    {
        try { return EvalMath(_expr); } catch { return 0; }
    }

    private void RefreshDisplay()
    {
        ExprText.Text = _expr;
        try { ResultText.Text = EvalMath(_expr).ToString("G"); }
        catch { ResultText.Text = _expr.Length > 0 ? _expr[^1].ToString() : "0"; }
    }

    private static decimal EvalMath(string expr)
    {
        var table = new System.Data.DataTable();
        return Convert.ToDecimal(table.Compute(expr, null));
    }
}
