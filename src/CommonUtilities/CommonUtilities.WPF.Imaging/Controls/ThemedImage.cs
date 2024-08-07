﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AnakinRaW.CommonUtilities.Wpf.Converters;
using AnakinRaW.CommonUtilities.Wpf.DPI;
using AnakinRaW.CommonUtilities.Wpf.Imaging.Converters;

namespace AnakinRaW.CommonUtilities.Wpf.Imaging.Controls;

public class ThemedImage : Image
{ 
    public static readonly DependencyProperty ActualDpiProperty = DependencyProperty.Register(nameof(ActualDpi),
        typeof(double), typeof(ThemedImage), new FrameworkPropertyMetadata(0.0));

    public static readonly DependencyProperty ActualGrayscaleBiasColorProperty =
        DependencyProperty.Register(nameof(ActualGrayscaleBiasColor), typeof(Color), typeof(ThemedImage));

    public static readonly DependencyProperty ActualHighContrastProperty =
        DependencyProperty.Register(nameof(ActualHighContrast), typeof(bool), typeof(ThemedImage));

    public static readonly DependencyProperty DpiProperty = DependencyProperty.RegisterAttached("Dpi", typeof(double),
        typeof(ThemedImage),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty GrayscaleProperty = DependencyProperty.Register(nameof(Grayscale),
        typeof(bool), typeof(ThemedImage), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty GrayscaleBiasColorProperty =
        DependencyProperty.RegisterAttached("GrayscaleBiasColor", typeof(Color?), typeof(ThemedImage),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty HighContrastProperty = DependencyProperty.RegisterAttached("HighContrast",
        typeof(bool?), typeof(ThemedImage),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
    
    public static readonly DependencyProperty ImakgeKeyProperty = DependencyProperty.Register(nameof(ImakgeKey),
        typeof(ImageKey), typeof(ThemedImage));

    public static readonly DependencyProperty ScaleFactorProperty = DependencyProperty.RegisterAttached("ScaleFactor",
        typeof(double), typeof(ThemedImage),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty SystemHighContrastProperty =
        DependencyProperty.Register(nameof(SystemHighContrast), typeof(bool), typeof(ThemedImage));

    public static double DefaultDpi { get; }

    public double ActualDpi => (double)GetValue(ActualDpiProperty);

    public bool ActualHighContrast => (bool)GetValue(ActualHighContrastProperty);

    public Color ActualGrayscaleBiasColor => (Color)GetValue(ActualGrayscaleBiasColorProperty);

    public bool SystemHighContrast => (bool)GetValue(SystemHighContrastProperty);
    
    public static Color? GetGrayscaleBiasColor(DependencyObject element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        return (Color?)element.GetValue(GrayscaleBiasColorProperty);
    }

    public static void SetGrayscaleBiasColor(DependencyObject element, Color? value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        element.SetValue(GrayscaleBiasColorProperty, value);
    }

    public static bool? GetHighContrast(DependencyObject element)
    {
        if (element == null) 
            throw new ArgumentNullException(nameof(element));
        return (bool?)element.GetValue(HighContrastProperty);
    }

    public static void SetHighContrast(DependencyObject element, bool? value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        element.SetValue(HighContrastProperty, value);
    }

    public static double GetDpi(DependencyObject element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        return (double)element.GetValue(DpiProperty);
    }

    public static void SetDpi(DependencyObject element, double value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        element.SetValue(DpiProperty, value);
    }

    public static double GetScaleFactor(DependencyObject element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        return (double)element.GetValue(ScaleFactorProperty);
    }

    public static void SetScaleFactor(DependencyObject element, double value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        element.SetValue(ScaleFactorProperty, value);
    }

    public bool Grayscale
    {
        get => (bool)GetValue(GrayscaleProperty);
        set => SetValue(GrayscaleProperty, value);
    }

    public ImageKey ImakgeKey
    {
        get => (ImageKey)GetValue(ImakgeKeyProperty);
        set => SetValue(ImakgeKeyProperty, value);
    }

    static ThemedImage()
    {
        try
        {
            DefaultDpi = DpiHelper.SystemDpiX;
        }
        catch
        {
            DefaultDpi = 96.0;
        }
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedImage), new FrameworkPropertyMetadata(typeof(ThemedImage)));
    }


    public ThemedImage()
    {
        InitializeBindings();
        this.HookDpiChanged(DisplayDpiChanged);
    }

    public static ThemedImage ForMenuItem(ImageKey imageKey, MenuItem item)
    {
        var themedImage = new ThemedImage
        {
            ImakgeKey = imageKey
        };

        themedImage.SetBinding(GrayscaleProperty, new Binding
        {
            Converter = new BooleanInverseConverter(),
            Source = item,
            Path = new PropertyPath("IsEnabled")
        });

        return themedImage;
    }

    private static void DisplayDpiChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not Visual visual)
            return;
        SetDpi(visual, visual.GetDpiX());
    }

    private void InitializeBindings()
    {
        InitializeDpiBindings();
        InitializeGrayscaleBiasColorBindings();
        InitializeHighContrastBindings();
    }

    private void InitializeDpiBindings()
    {
        BindingOperations.SetBinding(this, ActualDpiProperty, new Binding
        {
            Source = this,
            Path = new PropertyPath(DpiProperty),
            Converter = ActualDpiConverter.Instance
        });
    }

    private void InitializeGrayscaleBiasColorBindings()
    {
        BindingOperations.SetBinding(this, ActualGrayscaleBiasColorProperty, new MultiBinding
        {
            Bindings =
            {
                new Binding
                {
                    Source = this,
                    Path = new PropertyPath(GrayscaleBiasColorProperty)
                },
                new Binding
                {
                    Source = this,
                    Path = new PropertyPath(ActualHighContrastProperty)
                }
            },
            Converter = ActualGrayscaleBiasColorConverter.Instance
        });
    }

    private void InitializeHighContrastBindings()
    {
        SetResourceReference(SystemHighContrastProperty, SystemParameters.HighContrastKey);
        BindingOperations.SetBinding(this, ActualHighContrastProperty, new MultiBinding
        {
            Bindings = {
                new Binding
                {
                    Source = this,
                    Path = new PropertyPath(HighContrastProperty)
                },
                new Binding
                {
                    Source = this,
                    Path = new PropertyPath(SystemHighContrastProperty)
                }
            },
            Converter = ActualHighContrastConverter.Instance
        });
    }
}