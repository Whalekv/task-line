using System;
using System.Runtime.InteropServices;

namespace TaskLine.Helpers;

/// <summary>
/// 封装 Windows DWM（Desktop Window Manager）API，
/// 用于自定义窗口标题栏的视觉样式。
/// 仅在 Windows 11（Build 22000）及以上版本生效。
/// </summary>
internal static class DwmHelper
{
    #region 私有常量

    /// <summary>DWM 属性：标题栏背景色（COLORREF）。</summary>
    private const int DWMWA_CAPTION_COLOR = 35;

    /// <summary>DWM 属性：标题栏文字颜色（COLORREF）。</summary>
    private const int DWMWA_TEXT_COLOR = 36;

    #endregion

    #region P/Invoke

    /// <summary>设置指定窗口的 DWM 非客户区属性。</summary>
    /// <param name="hwnd">目标窗口句柄。</param>
    /// <param name="attr">要设置的属性 ID。</param>
    /// <param name="pvAttr">属性值的引用。</param>
    /// <param name="cbAttr">属性值的字节大小。</param>
    /// <returns>成功返回 S_OK（0），否则返回 HRESULT 错误码。</returns>
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int pvAttr, int cbAttr);

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置窗口标题栏的背景色与文字颜色。
    /// </summary>
    /// <param name="hwnd">目标窗口的 Win32 句柄。</param>
    /// <param name="captionColor">
    /// 标题栏背景色，格式为 COLORREF（<c>0x00BBGGRR</c>）。
    /// 例如白色为 <c>0x00FFFFFF</c>。
    /// </param>
    /// <param name="textColor">
    /// 标题栏文字颜色，格式同上。
    /// 例如深灰色为 <c>0x00424242</c>。
    /// </param>
    internal static void SetTitleBarColors(IntPtr hwnd, int captionColor, int textColor)
    {
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
        DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
    }

    #endregion
}
