using System;
using System.Drawing;
using System.Windows.Forms;

namespace Classroom_Killer
{
    /// <summary>
    /// 高 DPI 下手动缩放窗体。
    ///
    /// 不用 AutoScaleMode.Font：它按字体的相对比例推算缩放比，
    /// 在 125% DPI 的机器上算出来是错的，控件会互相挤压。
    /// 直接读 Graphics.DpiX 最准。
    /// </summary>
    internal static class DpiHelper
    {
        public static void Apply(Form form)
        {
            if (form == null)
                return;

            try
            {
                using (Graphics g = form.CreateGraphics())
                {
                    float scale = g.DpiX / 96f;

                    if (scale <= 1.05f || scale >= 3f)
                        return;

                    form.SuspendLayout();

                    form.Scale(new SizeF(scale, scale));
                    form.ClientSize = new Size(
                        (int)(form.ClientSize.Width * scale),
                        (int)(form.ClientSize.Height * scale));

                    form.ResumeLayout(true);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
