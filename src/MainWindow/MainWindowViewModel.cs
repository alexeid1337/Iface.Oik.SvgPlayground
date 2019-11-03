using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Iface.Oik.SvgPlayground.Model;
using Iface.Oik.SvgPlayground.Util;
using Microsoft.Win32;
using Svg;

namespace Iface.Oik.SvgPlayground.MainWindow
{
  public class MainWindowViewModel : INotifyPropertyChanged
  {
    private SvgDocument   _svgDocument;
    private List<Element> _elements = new List<Element>();

    private BitmapImage _svgImage;

    public BitmapImage SvgImage
    {
      get => _svgImage;
      set
      {
        _svgImage = value;
        NotifyOnPropertyChanged();
      }
    }


    private string _title;

    public string Title
    {
      get => _title;
      set
      {
        _title = value;
        NotifyOnPropertyChanged();
      }
    }


    public ObservableCollection<TmStatus> TmStatuses { get; } = new ObservableCollection<TmStatus>();
    public ObservableCollection<TmAnalog> TmAnalogs  { get; } = new ObservableCollection<TmAnalog>();


    public ICommand OpenFileCommand { get; }
    public ICommand UpdateCommand   { get; }


    public MainWindowViewModel()
    {
      Title = "SVG";

      OpenFileCommand = new RelayCommand(_ => OpenFile());
      UpdateCommand   = new RelayCommand(_ => Update());
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

      try
      {
        ClearEverything();
        _svgDocument = SvgDocument.Open(openFileDialog.FileName);
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

      SvgImage = null;
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
      foreach (var element in _elements)
      {
        element.Update();
      }
      try
      {
        Application.Current
                   .Dispatcher
                   ?.Invoke(new Action(() => SvgImage = BitmapUtil.ConvertBitmap(_svgDocument?.Draw())));
      }
      catch (Exception ex)
      {
        SvgImage = null;
        MessageBox.Show("Ошибка при попытке отрисовки: " + ex.Message);
      }
    }


    public int InitTmStatus(int ch, int rtu, int point, string _ = null)
    {
      // todo al не добавлять дублирующиеся адреса, просто возвращать уже имеющийся индекс
      var tmStatus = new TmStatus(ch, rtu, point);

      TmStatuses.Add(tmStatus);

      return TmStatuses.Count - 1;
    }


    public int InitTmAnalog(int ch, int rtu, int point, string _ = null)
    {
      // todo al не добавлять дублирующиеся адреса, просто возвращать уже имеющийся индекс
      var tmAnalog = new TmAnalog(ch, rtu, point);

      TmAnalogs.Add(tmAnalog);

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


    public string GetTmAnalogValue(int idx)
    {
      var tmAnalog = TmAnalogs.ElementAtOrDefault(idx);
      if (tmAnalog == null)
      {
        return TmAnalog.InvalidValueString;
      }
      return tmAnalog.ValueWithUnitString;
    }


    public event PropertyChangedEventHandler PropertyChanged;


    private void NotifyOnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}