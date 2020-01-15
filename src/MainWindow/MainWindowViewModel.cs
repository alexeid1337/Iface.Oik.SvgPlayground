using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Iface.Oik.SvgPlayground.Model;
using Iface.Oik.SvgPlayground.Util;
using Microsoft.Win32;
using SkiaSharp;
using Svg;

namespace Iface.Oik.SvgPlayground.MainWindow
{
  public class MainWindowViewModel : INotifyPropertyChanged
  {
    private const double ScaleStep = 1.5;


    private readonly MainWindowView _view;
    

    private          string        _svgFilename;
    private          SvgDocument   _svgDocument;
    private readonly List<Element> _elements = new List<Element>();


    private string      _title;
    private double      _scale = 1;


    public string Title
    {
      get => _title;
      set
      {
        _title = value;
        NotifyOnPropertyChanged();
      }
    }


    public double Scale
    {
      get => _scale;
      set
      {
        _scale = value;
        Update();
        NotifyOnPropertyChanged();
      }
    }


    public ObservableCollection<TmStatus> TmStatuses { get; } = new ObservableCollection<TmStatus>();
    public ObservableCollection<TmAnalog> TmAnalogs  { get; } = new ObservableCollection<TmAnalog>();
    public ObservableCollection<Variable> Variables  { get; } = new ObservableCollection<Variable>();


    public ICommand OpenFileCommand   { get; }
    public ICommand ReloadFileCommand { get; }
    public ICommand ZoomInCommand     { get; }
    public ICommand ZoomOutCommand    { get; }


    public MainWindowViewModel(MainWindowView view)
    {
      _view = view;
      
      Title = "SVG";

      OpenFileCommand   = new RelayCommand(_ => OpenFile());
      ReloadFileCommand = new RelayCommand(_ => ReloadFile());
      ZoomInCommand     = new RelayCommand(_ => ZoomIn());
      ZoomOutCommand    = new RelayCommand(_ => ZoomOut());

      FixTextBoxFloatValue();
    }


    private static void FixTextBoxFloatValue()
    {
      FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;
    }


    private void OpenFile()
    {
      var openFileDialog = new OpenFileDialog
      {
        Filter = "SVG (*.svg)|*.svg",
      };
      if (openFileDialog.ShowDialog() != true)
      {
        return;
      }

      OpenFile(openFileDialog.FileName);
    }


    private void ReloadFile()
    {
      OpenFile(_svgFilename);
    }


    private void OpenFile(string filename)
    {
      if (filename == null) return;

      _svgFilename = filename;
      try
      {
        ClearEverything();
        _svgDocument = SvgDocument.Open(filename);
        Title        = SvgUtil.FindTitleElement(_svgDocument)?.Content ?? "SVG";
        ParseElements();
        Update();
      }
      catch (Exception ex)
      {
        _svgDocument = null;
        MessageBox.Show("Ошибка при открытии файла: " + ex.Message);
      }
    }


    private void ClearEverything()
    {
      TmStatuses.Clear();
      TmAnalogs.Clear();
      _elements.Clear();
    }


    private void ParseElements()
    {
      _elements.Clear();

      try
      {
        var svgElements = SvgUtil.GetElementsCollectionWithAttribute(_svgDocument, "oikelement");
        foreach (var svgElement in svgElements)
        {
          var element = Element.Create(this, svgElement);
          if (element != null)
          {
            _elements.Add(element);
          }
        }
      }
      catch (Exception ex)
      {
        _elements.Clear();
        MessageBox.Show("Ошибка при разборе SVG, схема не будет оживлена: " + ex.Message);
      }
    }


    private void Update()
    {
      if (_svgDocument == null) return;

      foreach (var element in _elements)
      {
        element.Update();
      }
      try
      {
        InvalidateCanvas();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка при попытке отрисовки: " + ex.Message);
      }
    }


    private void InvalidateCanvas()
    {
      Application.Current
                 .Dispatcher
                 ?.Invoke(() => _view?.InvalidateCanvas());
    }



    public void OnCanvasPaintSurface(SKCanvas canvas)
    {
      try
      {
        SkiaSvgUtil.PaintSvgDocumentToSkiaCanvas(_svgDocument, canvas, (float) Scale);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка при попытке отрисовки: " + ex.Message);
      }
    }


    private void ZoomIn()
    {
      Zoom(ScaleStep);
    }


    private void ZoomOut()
    {
      Zoom(1 / ScaleStep);
    }


    private void Zoom(double scale)
    {
      var tempScale = Scale * scale;
      if (tempScale < 1.1f && tempScale > 0.9f && !tempScale.Equals(1.0f)) // округляем до единицы, если рядом
      {
        scale /= tempScale;
      }

      Scale *= scale;
    }


    private int FindTmStatus(int ch, int rtu, int point)
    {
      var index = 0;
      foreach (var tmStatus in TmStatuses)
      {
        if (tmStatus.AddrEquals(ch, rtu, point))
        {
          return index;
        }
        index++;
      }
      return -1;
    }


    private int FindTmAnalog(int ch, int rtu, int point)
    {
      var index = 0;
      foreach (var tmAnalog in TmAnalogs)
      {
        if (tmAnalog.AddrEquals(ch, rtu, point))
        {
          return index;
        }
        index++;
      }
      return -1;
    }


    private int FindVariable(string id)
    {
      var index = 0;
      foreach (var variable in Variables)
      {
        if (variable.Id == id)
        {
          return index;
        }
        index++;
      }
      return -1;
    }


    public int InitVariable(string id, string _ = null)
    {
      var existingVariable = FindVariable(id);
      if (existingVariable >= 0)
      {
        return existingVariable;
      }

      var variable = new Variable(id);
      Variables.Add(variable);

      return Variables.Count - 1;
    }


    public int InitTmStatus(int ch, int rtu, int point, string _ = null)
    {
      var existingTmStatusIndex = FindTmStatus(ch, rtu, point);
      if (existingTmStatusIndex >= 0)
      {
        return existingTmStatusIndex;
      }

      var tmStatus = new TmStatus(ch, rtu, point);

      TmStatuses.Add(tmStatus);

      tmStatus.PropertyChanged += (s, ea) => Update();

      return TmStatuses.Count - 1;
    }


    public int InitTmAnalog(int ch, int rtu, int point, string _ = null)
    {
      var existingTmAnalogIndex = FindTmAnalog(ch, rtu, point);
      if (existingTmAnalogIndex >= 0)
      {
        return existingTmAnalogIndex;
      }

      var tmAnalog = new TmAnalog(ch, rtu, point);

      TmAnalogs.Add(tmAnalog);

      tmAnalog.PropertyChanged += (s, ea) => Update();

      return TmAnalogs.Count - 1;
    }


    public int GetTmStatusStatus(int idx)
    {
      var tmStatus = TmStatuses.ElementAtOrDefault(idx);
      if (tmStatus == null)
      {
        return -1;
      }
      return tmStatus.IsOn ? 1 : 0;
    }


    public bool IsTmStatusOn(int idx)
    {
      var tmStatus = TmStatuses.ElementAtOrDefault(idx);
      if (tmStatus == null)
      {
        return false;
      }
      return tmStatus.IsOn;
    }


    public bool IsTmStatusUnreliable(int idx)
    {
      var tmStatus = TmStatuses.ElementAtOrDefault(idx);
      if (tmStatus == null)
      {
        return false;
      }
      return tmStatus.IsUnreliable;
    }


    public bool IsTmStatusMalfunction(int idx)
    {
      var tmStatus = TmStatuses.ElementAtOrDefault(idx);
      if (tmStatus == null)
      {
        return false;
      }
      return tmStatus.IsMalfunction;
    }


    public bool IsTmStatusIntermediate(int idx)
    {
      var tmStatus = TmStatuses.ElementAtOrDefault(idx);
      if (tmStatus == null)
      {
        return false;
      }
      return tmStatus.IsIntermediate;
    }


    public bool IsTmAnalogUnreliable(int idx)
    {
      var tmAnalog = TmAnalogs.ElementAtOrDefault(idx);
      if (tmAnalog == null)
      {
        return false;
      }
      return tmAnalog.IsUnreliable;
    }


    public float GetTmAnalogValue(int idx)
    {
      var tmAnalog = TmAnalogs.ElementAtOrDefault(idx);
      if (tmAnalog == null)
      {
        return 0;
      }
      return tmAnalog.Value;
    }


    public string GetTmAnalogValueString(int idx)
    {
      var tmAnalog = TmAnalogs.ElementAtOrDefault(idx);
      if (tmAnalog == null)
      {
        return TmAnalog.InvalidValueString;
      }
      return tmAnalog.ValueString;
    }


    public string GetTmAnalogUnit(int idx)
    {
      var tmAnalog = TmAnalogs.ElementAtOrDefault(idx);
      if (tmAnalog == null)
      {
        return string.Empty;
      }
      return tmAnalog.Unit;
    }


    public string GetTmAnalogValueWithUnitString(int idx)
    {
      var tmAnalog = TmAnalogs.ElementAtOrDefault(idx);
      if (tmAnalog == null)
      {
        return TmAnalog.InvalidValueString;
      }
      return tmAnalog.ValueWithUnitString;
    }


    public bool IsVariableUnreliable(int idx)
    {
      var variable = Variables.ElementAtOrDefault(idx);
      if (variable == null)
      {
        return false;
      }
      return variable.IsUnreliable;
    }


    public bool IsVariableOn(int idx)
    {
      var variable = Variables.ElementAtOrDefault(idx);
      if (variable == null)
      {
        return false;
      }
      return variable.IsOn;
    }


    public void SetVariable(int idx, bool? isOn)
    {
      var variable = Variables.ElementAtOrDefault(idx);
      if (variable == null)
      {
        return;
      }
      if (!isOn.HasValue)
      {
        variable.IsUnreliable = true;
        return;
      }
      variable.IsUnreliable = false;
      variable.IsOn         = isOn.Value;
    }


    public event PropertyChangedEventHandler PropertyChanged;


    private void NotifyOnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}