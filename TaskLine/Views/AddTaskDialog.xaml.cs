using System;
using System.Windows;
using System.Windows.Interop;
using TaskLine.Helpers;

namespace TaskLine.Views;

/// <summary>
/// 新建任务对话框，供用户输入任务名称并选择所属日期。
/// 通过 <see cref="DialogResult"/> 返回用户的操作结果。
/// </summary>
public partial class AddTaskDialog : Window
{
    #region 常量

    /// <summary>标题栏背景色：白色（COLORREF 格式）。</summary>
    private const int TitleBarCaptionColor = 0x00FFFFFF;

    /// <summary>标题栏文字色：深灰色（COLORREF 格式）。</summary>
    private const int TitleBarTextColor = 0x00424242;

    #endregion

    #region 属性

    /// <summary>获取用户输入的任务标题（已去除首尾空白）。</summary>
    public string TaskTitle => TitleTextBox.Text.Trim();

    /// <summary>获取用户选择的任务日期；未选择时回退到今天。</summary>
    public DateTime TaskDate => DatePickerControl.SelectedDate?.Date ?? DateTime.Today;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化对话框，并将日期选择器的默认值设置为指定日期。
    /// </summary>
    /// <param name="defaultDate">日期选择器的初始值，通常为当前选中日期。</param>
    public AddTaskDialog(DateTime defaultDate)
    {
        InitializeComponent();

        DatePickerControl.DisplayDateStart = DateTime.Today;
        DatePickerControl.SelectedDate = defaultDate >= DateTime.Today ? defaultDate : DateTime.Today;

        SourceInitialized += OnSourceInitialized;
        TitleTextBox.TextChanged += (_, _) =>
            ConfirmButton.IsEnabled = !string.IsNullOrWhiteSpace(TitleTextBox.Text);

        Loaded += (_, _) => TitleTextBox.Focus();
    }

    #endregion

    #region 私有方法

    /// <summary>在 Win32 句柄就绪后，通过 DWM API 应用标题栏颜色。</summary>
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        DwmHelper.SetTitleBarColors(hwnd, TitleBarCaptionColor, TitleBarTextColor);
    }

    /// <summary>点击"确认"或按 Enter 时，校验标题后关闭对话框并返回成功。</summary>
    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text)) return;
        DialogResult = true;
    }

    /// <summary>点击"取消"、按 Escape 或关闭按钮时，取消操作并关闭对话框。</summary>
    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    #endregion
}
