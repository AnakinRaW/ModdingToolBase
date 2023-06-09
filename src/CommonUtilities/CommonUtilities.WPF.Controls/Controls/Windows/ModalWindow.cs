﻿using System;
using System.Windows;
using AnakinRaW.CommonUtilities.Wpf.Utilities;
using Vanara.PInvoke;

namespace AnakinRaW.CommonUtilities.Wpf.Controls;

public class ModalWindow : WindowBase
{
    public static readonly DependencyProperty HasDialogFrameProperty = DependencyProperty.Register(
        nameof(HasDialogFrame), typeof(bool), typeof(ModalWindow),
        new FrameworkPropertyMetadata(Boxes.BooleanTrue, OnWindowStyleChanged));

    public static readonly DependencyProperty IsCloseButtonEnabledProperty =
        DependencyProperty.Register(nameof(IsCloseButtonEnabled), typeof(bool), typeof(ModalWindow),
            new PropertyMetadata(Boxes.BooleanTrue, OnWindowStyleChanged));

    public bool HasDialogFrame
    {
        get => (bool)GetValue(HasDialogFrameProperty);
        set => SetValue(HasDialogFrameProperty, Boxes.Box(value));
    }

    public bool IsCloseButtonEnabled
    {
        get => (bool)GetValue(IsCloseButtonEnabledProperty);
        set => SetValue(IsCloseButtonEnabledProperty, value);
    }

    static ModalWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ModalWindow), new FrameworkPropertyMetadata(typeof(ModalWindow)));
    }

    public ModalWindow(IModalWindowViewModel viewModel) : base(viewModel)
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public bool? ShowModal()
    {
        return Utilities.WindowHelper.ShowModal(this);
    }

    public void EnableOwner()
    {
        if (Owner is null)
            return;
        Owner.IsEnabled = true;
        if (Owner.IsActive)
            return;
        Owner.Activate();
    }

    protected override void UpdateWindowStyle()
    {
        base.UpdateWindowStyle();

        if (HwndSource == null)
            return;
        var handle = HwndSource.Handle;
        if (handle == IntPtr.Zero)
            return;

        var extendedStyle = (User32.WindowStylesEx)User32.GetWindowLong(handle, User32.WindowLongFlags.GWL_EXSTYLE);
        var newExStyle = !HasDialogFrame ? extendedStyle & ~User32.WindowStylesEx.WS_EX_DLGMODALFRAME : extendedStyle | User32.WindowStylesEx.WS_EX_DLGMODALFRAME;
        User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_EXSTYLE, (int)newExStyle);
        User32.SendMessage(handle, 128, new IntPtr(1), IntPtr.Zero);
        User32.SendMessage(handle, 128, new IntPtr(0), IntPtr.Zero);
        var systemMenu = User32.GetSystemMenu(handle, false);
        if (systemMenu != IntPtr.Zero)
        {
            var closeEnabled = IsCloseButtonEnabled ? User32.MenuFlags.MF_ENABLED : User32.MenuFlags.MF_GRAYED;
            User32.EnableMenuItem(systemMenu, 61536U, closeEnabled);
        }

        User32.SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
            User32.SetWindowPosFlags.SWP_DRAWFRAME | User32.SetWindowPosFlags.SWP_NOMOVE |
            User32.SetWindowPosFlags.SWP_NOSIZE);
    }

    private static void OnWindowStyleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        ((ModalWindow)obj).UpdateWindowStyle();
    }
}